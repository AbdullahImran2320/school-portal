// Services/StudentImportService.cs
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SchoolPortal.API.Data;
using SchoolPortal.API.DTOs;
using SchoolPortal.API.Models;

namespace SchoolPortal.API.Services
{
    public interface IStudentImportService
    {
        Task<StudentImportResultDto> ImportAsync(Stream fileStream);
        byte[] BuildTemplateWorkbook();
    }

    public class StudentImportService : IStudentImportService
    {
        private readonly SchoolPortalDbContext _context;
        private readonly IStudentService _studentService;

        // Same pattern CreateStudentDto itself enforces — kept in sync
        // deliberately rather than relying on model-binding validation,
        // which never runs for a DTO built by hand in C# like this one is.
        private static readonly Regex BFormPattern = new(@"^\d{5}-\d{7}-\d{1}$");

        private const int NameCol = 1;
        private const int BFormCol = 2;
        private const int DobCol = 3;
        private const int GenderCol = 4;
        private const int AdmissionDateCol = 5;
        private const int ClassNameCol = 6;
        private const int SectionCol = 7;
        private const int FatherNameCol = 8;
        private const int FatherMobileCol = 9;
        private const int FatherOccupationCol = 10;
        private const int MotherNameCol = 11;
        private const int MotherMobileCol = 12;
        private const int PrimaryGuardianCol = 13;
        private const int AddressCol = 14;
        private const int DiscountAmountCol = 15;
        private const int DiscountReasonCol = 16;

        public StudentImportService(SchoolPortalDbContext context, IStudentService studentService)
        {
            _context = context;
            _studentService = studentService;
        }

        public byte[] BuildTemplateWorkbook()
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Students");

            var headers = new[]
            {
                "Student Name*", "B-Form Number*", "Date of Birth*", "Gender*", "Admission Date*",
                "Class Name*", "Section", "Father Name*", "Father Mobile*", "Father Occupation",
                "Mother Name", "Mother Mobile", "Primary Guardian", "Address",
                "Monthly Discount Amount", "Discount Reason"
            };
            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];
            ws.Row(1).Style.Font.Bold = true;
            ws.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");

            // One filled-in example row so the expected format (especially
            // the B-Form pattern and date format) is obvious rather than
            // guessed at from a column header alone. Set individually with
            // each value's real type — ClosedXML's cell.Value only has
            // implicit conversions from concrete types (string, DateTime,
            // decimal...), not from a generic object, so a loop over a
            // boxed object[] wouldn't compile.
            ws.Cell(2, NameCol).Value = "Ayesha Khan";
            ws.Cell(2, BFormCol).Value = "12345-1234567-1";
            ws.Cell(2, DobCol).Value = new DateTime(2016, 3, 14);
            ws.Cell(2, GenderCol).Value = "Female";
            ws.Cell(2, AdmissionDateCol).Value = new DateTime(2026, 4, 1);
            ws.Cell(2, ClassNameCol).Value = "Class 1";
            ws.Cell(2, SectionCol).Value = "A";
            ws.Cell(2, FatherNameCol).Value = "Muhammad Khan";
            ws.Cell(2, FatherMobileCol).Value = "0300-1234567";
            ws.Cell(2, FatherOccupationCol).Value = "Businessman";
            ws.Cell(2, MotherNameCol).Value = "Sana Khan";
            ws.Cell(2, MotherMobileCol).Value = "0300-7654321";
            ws.Cell(2, PrimaryGuardianCol).Value = "Father";
            ws.Cell(2, AddressCol).Value = "Model Town, Lahore";
            ws.Cell(2, DiscountAmountCol).Value = 0;
            ws.Cell(2, DiscountReasonCol).Value = "";

            ws.Cell(2, DobCol).Style.DateFormat.Format = "yyyy-mm-dd";
            ws.Cell(2, AdmissionDateCol).Style.DateFormat.Format = "yyyy-mm-dd";

            ws.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<StudentImportResultDto> ImportAsync(Stream fileStream)
        {
            using var workbook = new XLWorkbook(fileStream);
            var ws = workbook.Worksheets.First();
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

            var result = new StudentImportResultDto();

            // Loaded once up front rather than re-queried per row — with
            // 1000+ rows this avoids 1000+ round trips just to look up a
            // class or check for a duplicate B-Form number.
            var classesByKey = await _context.Classes.ToDictionaryAsync(
                c => (c.ClassName.Trim().ToUpperInvariant(), c.Section.Trim().ToUpperInvariant()),
                c => c);
            var existingBFormNumbers = (await _context.Students.Select(s => s.BFormNumber).ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var parentsByMobile = await _context.Parents
                .Where(p => p.FatherMobile != "")
                .GroupBy(p => p.FatherMobile)
                .ToDictionaryAsync(g => g.Key, g => g.First());

            for (int row = 2; row <= lastRow; row++)
            {
                var name = ws.Cell(row, NameCol).GetString().Trim();

                // A fully blank row (common at the end of a spreadsheet
                // someone's been editing) isn't an error — just skip it
                // silently rather than reporting it as a failed row.
                if (string.IsNullOrWhiteSpace(name) && ws.Row(row).IsEmpty())
                    continue;

                result.TotalRows++;
                var rowResult = new StudentImportRowResult { RowNumber = row, StudentName = name };

                try
                {
                    if (string.IsNullOrWhiteSpace(name))
                        throw new ImportRowException("Student Name is required.");

                    var bForm = ws.Cell(row, BFormCol).GetString().Trim();
                    if (!BFormPattern.IsMatch(bForm))
                        throw new ImportRowException("B-Form Number must be in the format 12345-1234567-1.");

                    if (existingBFormNumbers.Contains(bForm))
                    {
                        rowResult.Outcome = "Skipped";
                        rowResult.Detail = "A student with this B-Form number already exists — not imported again.";
                        result.SkippedCount++;
                        result.Results.Add(rowResult);
                        continue;
                    }

                    var dob = ReadDate(ws.Cell(row, DobCol), "Date of Birth");
                    var admissionDate = ReadDate(ws.Cell(row, AdmissionDateCol), "Admission Date");

                    var genderRaw = ws.Cell(row, GenderCol).GetString().Trim();
                    var gender = genderRaw.Equals("M", StringComparison.OrdinalIgnoreCase) ? "Male"
                        : genderRaw.Equals("F", StringComparison.OrdinalIgnoreCase) ? "Female"
                        : genderRaw;
                    if (gender != "Male" && gender != "Female")
                        throw new ImportRowException($"Gender must be Male or Female (got '{genderRaw}').");

                    var className = ws.Cell(row, ClassNameCol).GetString().Trim();
                    var section = ws.Cell(row, SectionCol).GetString().Trim();
                    if (string.IsNullOrWhiteSpace(className))
                        throw new ImportRowException("Class Name is required.");

                    var classKey = (className.ToUpperInvariant(), section.ToUpperInvariant());
                    if (!classesByKey.TryGetValue(classKey, out var schoolClass))
                    {
                        throw new ImportRowException(string.IsNullOrEmpty(section)
                            ? $"Class '{className}' was not found."
                            : $"Class '{className}' with section '{section}' was not found. Check Admin -> Manage Classes.");
                    }

                    var fatherName = ws.Cell(row, FatherNameCol).GetString().Trim();
                    var fatherMobile = ws.Cell(row, FatherMobileCol).GetString().Trim();
                    if (string.IsNullOrWhiteSpace(fatherName))
                        throw new ImportRowException("Father Name is required.");
                    if (string.IsNullOrWhiteSpace(fatherMobile))
                        throw new ImportRowException("Father Mobile is required.");

                    // Siblings on later rows reuse the parent created for an
                    // earlier row in the SAME file, not just parents already
                    // in the database — otherwise importing two siblings in
                    // one file would still create two separate Parent rows.
                    int parentId;
                    if (parentsByMobile.TryGetValue(fatherMobile, out var existingParent))
                    {
                        parentId = existingParent.ParentId;
                    }
                    else
                    {
                        var guardianRaw = ws.Cell(row, PrimaryGuardianCol).GetString().Trim();
                        var guardian = Enum.TryParse<PrimaryGuardian>(guardianRaw, true, out var parsedGuardian)
                            ? parsedGuardian
                            : PrimaryGuardian.Father;

                        var newParent = new Parent
                        {
                            FatherName = fatherName,
                            FatherMobile = fatherMobile,
                            FatherOccupation = EmptyToNull(ws.Cell(row, FatherOccupationCol).GetString().Trim()),
                            MotherName = EmptyToNull(ws.Cell(row, MotherNameCol).GetString().Trim()),
                            MotherMobile = EmptyToNull(ws.Cell(row, MotherMobileCol).GetString().Trim()),
                            PrimaryGuardian = guardian,
                            Address = ws.Cell(row, AddressCol).GetString().Trim()
                        };
                        _context.Parents.Add(newParent);
                        await _context.SaveChangesAsync();

                        parentsByMobile[fatherMobile] = newParent;
                        parentId = newParent.ParentId;
                    }

                    var discountCell = ws.Cell(row, DiscountAmountCol);
                    var discountAmount = discountCell.IsEmpty() ? 0m : discountCell.GetValue<decimal>();
                    var discountReason = EmptyToNull(ws.Cell(row, DiscountReasonCol).GetString().Trim());

                    var dto = new CreateStudentDto
                    {
                        Name = name,
                        BFormNumber = bForm,
                        DateOfBirth = dob,
                        Gender = gender,
                        AdmissionDate = admissionDate,
                        // Bulk import is for onboarding students who are
                        // already enrolled, not new applicants — the single-
                        // student form still defaults to "Applied" for that
                        // separate case.
                        AdmissionStatus = "Admitted",
                        ClassId = schoolClass.ClassId,
                        ParentId = parentId
                    };

                    var created = await _studentService.CreateStudentAsync(dto);

                    if (discountAmount > 0)
                    {
                        var studentEntity = await _context.Students.FindAsync(created.StudentId);
                        if (studentEntity != null)
                        {
                            studentEntity.MonthlyDiscountAmount = discountAmount;
                            studentEntity.DiscountReason = discountReason;
                            await _context.SaveChangesAsync();
                        }
                    }

                    existingBFormNumbers.Add(bForm);
                    rowResult.Outcome = "Imported";
                    result.ImportedCount++;
                }
                catch (ImportRowException ex)
                {
                    rowResult.Outcome = "Failed";
                    rowResult.Detail = ex.Message;
                    result.FailedCount++;
                }
                catch (Exception ex)
                {
                    // Anything unexpected (a constraint violation, a null
                    // reference from an oddly-shaped cell) still gets
                    // reported against ITS row instead of aborting the
                    // whole import — one bad row shouldn't cost the other
                    // 999 good ones.
                    rowResult.Outcome = "Failed";
                    rowResult.Detail = $"Unexpected error: {ex.Message}";
                    result.FailedCount++;
                }

                result.Results.Add(rowResult);
            }

            return result;
        }

        private static string? EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

        private static DateTime ReadDate(IXLCell cell, string fieldName)
        {
            if (cell.TryGetValue<DateTime>(out var date)) return date;

            var raw = cell.GetString().Trim();
            if (DateTime.TryParse(raw, out var parsed)) return parsed;

            throw new ImportRowException($"{fieldName} isn't a valid date (got '{raw}').");
        }
    }

    // Represents one row's data being invalid — distinct from a genuine
    // unexpected exception, so the catch blocks above can report each with
    // an appropriately worded message rather than both looking like a bug.
    public class ImportRowException : Exception
    {
        public ImportRowException(string message) : base(message) { }
    }
}
