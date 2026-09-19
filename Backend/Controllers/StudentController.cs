using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using SchoolPortal.API.Data;
using SchoolPortal.API.DTOs;
using SchoolPortal.API.Models;
using SchoolPortal.API.Services;

namespace SchoolPortal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Accountant,Teacher")]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly IStudentImportService _importService;
        private readonly IAttendanceService _attendanceService;
        private readonly IResultService _resultService;
        private readonly SchoolPortalDbContext _context;
        private readonly IConfiguration _config;
        private readonly int _gracePeriodDay;
        private readonly decimal _lateFeeAmount;

        public StudentsController(
            IStudentService studentService,
            IStudentImportService importService,
            IAttendanceService attendanceService,
            IResultService resultService,
            SchoolPortalDbContext context,
            IConfiguration config)
        {
            _studentService = studentService;
            _importService = importService;
            _attendanceService = attendanceService;
            _resultService = resultService;
            _context = context;
            _config = config;
            _gracePeriodDay = config.GetValue<int>("LateFeeSettings:GracePeriodDay");
            _lateFeeAmount = config.GetValue<decimal>("LateFeeSettings:LateFeeAmount");
        }

        [HttpGet]
        public async Task<ActionResult<List<StudentDto>>> GetAll()
        {
            return Ok(await _studentService.GetAllStudentsAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<StudentDto>> GetById(int id)
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            if (student == null) return NotFound();
            return Ok(student);
        }

        // Pulls together everything about one student that's otherwise
        // scattered across the Fee Grid, Attendance, and Academics screens
        // (each filtered by class, not by student) — the single "click on
        // a student, see everything" view the rest of the app doesn't have.
        // Every figure here is computed with the exact same formulas as its
        // own dedicated screen, not a separate approximation, so nothing
        // shown here can quietly disagree with what the Fee Grid or
        // Attendance Register say for the same student.
        [HttpGet("{id}/profile")]
        public async Task<ActionResult<StudentProfileDto>> GetProfile(int id)
        {
            var student = await _context.Students
                .Include(s => s.Class)
                .Include(s => s.Parent)
                .FirstOrDefaultAsync(s => s.StudentId == id);
            if (student == null) return NotFound();

            var now = DateTime.Now;

            // --- Fees: same formula as the fee-grid/defaulters totals ---
            var ledgers = await _context.FeeLedgers
                .Where(l => l.StudentId == id && l.Year == now.Year)
                .ToListAsync();
            var applicableLedgers = ledgers
                .Where(l => FeeCalculator.IsApplicableMonth(student.AdmissionDate, l.MonthNumber, l.Year))
                .ToList();
            var monthlyOutstanding = applicableLedgers.Sum(l => Math.Max(
                (l.DueAmount - Math.Max(0, l.DiscountAmount) + FeeCalculator.GetLateFee(l, now, _gracePeriodDay, _lateFeeAmount)) - l.PaidAmount,
                0));
            var overdueMonthsCount = applicableLedgers.Count(l =>
                FeeCalculator.GetEffectiveStatus(l, now, _gracePeriodDay) == "Overdue");

            // SQLite's EF Core provider can't translate Sum() over a decimal
            // expression into SQL (same limitation worked around elsewhere
            // in this codebase, e.g. the dashboard's collection figure) —
            // fetch the individual balances, then sum them in .NET instead.
            var chargeBalances = await _context.StudentCharges
                .Where(c => c.StudentId == id && c.Status != ChargeStatus.Paid)
                .Select(c => (c.DueAmount - c.DiscountAmount) - c.PaidAmount)
                .ToListAsync();
            var chargesOutstanding = Math.Max(0, chargeBalances.Sum());

            var recentLedgerPayments = await _context.Payments
                .Where(p => p.Ledger != null && p.Ledger.StudentId == id)
                .OrderByDescending(p => p.PaymentDate)
                .Take(5)
                .Select(p => new RecentPaymentDto
                {
                    PaymentDate = p.PaymentDate,
                    AmountPaid = p.AmountPaid,
                    PaidAgainst = "Tuition Fee",
                    ReceiptNumber = p.ReceiptNumber
                })
                .ToListAsync();
            var recentChargePayments = await _context.Payments
                .Where(p => p.Charge != null && p.Charge.StudentId == id)
                .OrderByDescending(p => p.PaymentDate)
                .Take(5)
                .Select(p => new RecentPaymentDto
                {
                    PaymentDate = p.PaymentDate,
                    AmountPaid = p.AmountPaid,
                    PaidAgainst = p.Charge!.ChargeType,
                    ReceiptNumber = p.ReceiptNumber
                })
                .ToListAsync();
            var recentPayments = recentLedgerPayments.Concat(recentChargePayments)
                .OrderByDescending(p => p.PaymentDate)
                .Take(5)
                .ToList();

            // --- Attendance: current calendar month, same logic as the
            // dedicated summary endpoint, reused rather than duplicated ---
            var attendanceSummary = await _attendanceService.GetStudentMonthlySummaryAsync(id, now.Month, now.Year);

            // --- Academics: most recent exam this student has any results
            // for. Exams have no date field, so ExamId (creation order) is
            // used as the recency proxy — the same assumption a school
            // would make by simply looking at which exam was entered last. ---
            var latestExamId = await _context.Results
                .Where(r => r.StudentId == id)
                .OrderByDescending(r => r.ExamId)
                .Select(r => (int?)r.ExamId)
                .FirstOrDefaultAsync();

            string? latestExamName = null, latestExamTerm = null, latestExamResult = null;
            double? latestExamPercentage = null;
            if (latestExamId.HasValue)
            {
                var reportCard = await _resultService.GetReportCardAsync(id, latestExamId.Value);
                if (reportCard != null)
                {
                    latestExamName = reportCard.ExamName;
                    latestExamTerm = reportCard.Term;
                    latestExamPercentage = reportCard.OverallPercentage;
                    latestExamResult = reportCard.OverallResult;
                }
            }

            return Ok(new StudentProfileDto
            {
                SchoolName = _config["SchoolSettings:SchoolName"] ?? "",
                CampusName = _config["SchoolSettings:CampusName"] ?? "",

                StudentId = student.StudentId,
                Name = student.Name,
                RollNumber = student.RollNumber,
                BFormNumber = student.BFormNumber,
                DateOfBirth = student.DateOfBirth,
                Gender = student.Gender,
                AdmissionDate = student.AdmissionDate,
                AdmissionStatus = student.AdmissionStatus.ToString(),
                ClassId = student.ClassId,
                ClassName = student.Class?.ClassName ?? "",
                Section = student.Class?.Section ?? "",
                HasPhoto = !string.IsNullOrEmpty(student.PhotoFileName),
                FatherName = student.Parent?.FatherName ?? "",
                FatherMobile = student.Parent?.FatherMobile ?? "",
                MotherName = student.Parent?.MotherName,
                MotherMobile = student.Parent?.MotherMobile,
                MonthlyDiscountAmount = student.MonthlyDiscountAmount,
                DiscountReason = student.DiscountReason,

                TotalOutstanding = monthlyOutstanding + chargesOutstanding,
                OverdueMonthsCount = overdueMonthsCount,
                RecentPayments = recentPayments,

                AttendanceMonth = now.Month,
                AttendanceYear = now.Year,
                PresentDays = attendanceSummary?.PresentDays ?? 0,
                AbsentDays = attendanceSummary?.AbsentDays ?? 0,
                LeaveDays = attendanceSummary?.LeaveDays ?? 0,
                LateDays = attendanceSummary?.LateDays ?? 0,
                AttendancePercentage = attendanceSummary?.AttendancePercentage ?? 0,

                LatestExamName = latestExamName,
                LatestExamTerm = latestExamTerm,
                LatestExamPercentage = latestExamPercentage,
                LatestExamResult = latestExamResult
            });
        }

        // Photos live in a StudentPhotos folder resolved the same way as
        // Backups — relative to wherever the live SQLite file actually is,
        // not a hardcoded path, and never inside wwwroot. See the comment
        // on Student.PhotoFileName for why wwwroot specifically is unsafe
        // for this: build.bat deletes and recreates it on every production
        // build, which would silently destroy every uploaded photo.
        private string GetPhotoFolder()
        {
            var connection = (SqliteConnection)_context.Database.GetDbConnection();
            var dbFolder = Path.GetDirectoryName(Path.GetFullPath(connection.DataSource)) ?? AppContext.BaseDirectory;
            var photoFolder = Path.Combine(dbFolder, "StudentPhotos");
            Directory.CreateDirectory(photoFolder);
            return photoFolder;
        }

        private static readonly HashSet<string> AllowedPhotoContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp"
        };

        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/photo")]
        [RequestSizeLimit(5_000_000)] // 5MB — generous for a headshot, small enough to not bloat the database folder
        public async Task<IActionResult> UploadPhoto(int id, IFormFile file)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null) return NotFound();

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            if (!AllowedPhotoContentTypes.Contains(file.ContentType))
                return BadRequest(new { message = "Photo must be a JPEG, PNG, or WEBP image." });

            var extension = file.ContentType.ToLowerInvariant() switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".jpg"
            };

            // A fresh filename per upload (not a fixed {id}.jpg) so a
            // browser never shows a stale cached photo after someone
            // replaces it — the old file is deleted right after the new
            // one is written, not before, so a failed upload never leaves
            // the student with no photo at all.
            var folder = GetPhotoFolder();
            var oldFileName = student.PhotoFileName;
            var newFileName = $"{id}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
            var newPath = Path.Combine(folder, newFileName);

            using (var stream = new FileStream(newPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            student.PhotoFileName = newFileName;
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(oldFileName))
            {
                var oldPath = Path.Combine(folder, oldFileName);
                if (System.IO.File.Exists(oldPath))
                {
                    try { System.IO.File.Delete(oldPath); }
                    catch { /* leftover old file is harmless clutter, not worth failing the request over */ }
                }
            }

            return Ok(new { photoFileName = newFileName });
        }

        [HttpGet("{id}/photo")]
        public async Task<IActionResult> GetPhoto(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null || string.IsNullOrEmpty(student.PhotoFileName)) return NotFound();

            var path = Path.Combine(GetPhotoFolder(), student.PhotoFileName);
            if (!System.IO.File.Exists(path)) return NotFound();

            var contentType = Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };
            return PhysicalFile(path, contentType);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}/photo")]
        public async Task<IActionResult> DeletePhoto(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null) return NotFound();
            if (string.IsNullOrEmpty(student.PhotoFileName)) return NoContent();

            var path = Path.Combine(GetPhotoFolder(), student.PhotoFileName);
            if (System.IO.File.Exists(path))
            {
                try { System.IO.File.Delete(path); }
                catch { /* removing the DB reference below is what actually matters to the user */ }
            }

            student.PhotoFileName = null;
            await _context.SaveChangesAsync();
            return NoContent();
        }


        [Authorize(Roles = "Admin")]
        [HttpPost]

        public async Task<ActionResult<StudentDto>> Create(CreateStudentDto dto)
        {
            if (!Enum.TryParse<AdmissionStatus>(dto.AdmissionStatus, out _))
                return BadRequest(new { message = "Invalid AdmissionStatus. Must be Applied, Admitted, Withdrawn, Rejected, or Graduated." });

            try
            {
                var created = await _studentService.CreateStudentAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.StudentId }, created);
            }
            catch (DuplicateRollNumberException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (RollNumberGenerationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Separate from the general student edit — see UpdateStudentAsync's
        // comment on why changing class clears the roll number instead of
        // silently keeping a stale one. This is the deliberate "assign a
        // new one" action that follows. The value here is a position
        // within the student's own class/section, not the formatted code
        // itself — the formatted code is always regenerated from it.
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/roll-number")]
        public async Task<IActionResult> SetRollNumber(int id, SetRollNumberDto dto)
        {
            var (success, error) = await _studentService.SetRollNumberAsync(id, dto.RollNumberSequence);
            if (!success)
            {
                // "Already taken" can no longer happen (SetRollNumberAsync
                // reorders instead of rejecting), so the only failures left
                // are genuine 400/404 cases, not conflicts.
                return error == "Student not found."
                    ? NotFound(new { message = error })
                    : BadRequest(new { message = error });
            }
            return NoContent();
        }

        // For onboarding a school that already has enrolled students:
        // roll numbers only ever auto-assign at creation time, so anyone
        // who existed before that feature (or was bulk-imported before a
        // number was set) has none. This assigns sequential positions, in
        // admission-date order, to every currently-admitted student
        // missing one — per class, since a formatted code's sequence only
        // has meaning within its own class/section. Safe to run more than
        // once, since it only ever touches students that still have no
        // number. Students in a class with no ClassCode set yet are
        // skipped (reported separately) rather than failing the whole run.
        [Authorize(Roles = "Admin")]
        [HttpPost("assign-missing-roll-numbers")]
        public async Task<ActionResult<AssignRollNumbersResultDto>> AssignMissingRollNumbers()
        {
            var settings = await _context.RollNumberSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new SchoolPortal.API.Models.RollNumberSettings();
                _context.RollNumberSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            var studentsNeedingNumbers = await _context.Students
                .Include(s => s.Class)
                .Where(s => s.RollNumber == null && s.AdmissionStatus == AdmissionStatus.Admitted)
                .OrderBy(s => s.AdmissionDate)
                .ThenBy(s => s.StudentId)
                .ToListAsync();

            if (studentsNeedingNumbers.Count == 0)
                return Ok(new AssignRollNumbersResultDto { AssignedCount = 0, SkippedNoClassCodeCount = 0 });

            var assignedCount = 0;
            var skippedCount = 0;

            // Group by class so each class's next position picks up after
            // whatever that class's highest existing position already is,
            // rather than everyone starting from 1 and colliding.
            foreach (var group in studentsNeedingNumbers.GroupBy(s => s.ClassId))
            {
                var schoolClass = group.First().Class;
                if (schoolClass == null || string.IsNullOrWhiteSpace(schoolClass.ClassCode))
                {
                    skippedCount += group.Count();
                    continue;
                }

                var nextSequence = (await _context.Students
                    .Where(s => s.ClassId == group.Key && s.RollNumberSequence != null)
                    .MaxAsync(s => (int?)s.RollNumberSequence)) ?? 0;
                nextSequence++;

                foreach (var student in group)
                {
                    student.RollNumberSequence = nextSequence;
                    student.RollNumber = BuildRollNumberCodeForAssignment(settings, student.AdmissionDate, schoolClass.ClassCode, nextSequence);
                    nextSequence++;
                    assignedCount++;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new AssignRollNumbersResultDto { AssignedCount = assignedCount, SkippedNoClassCodeCount = skippedCount });
        }

        // Mirrors StudentService's private BuildRollNumberCode — duplicated
        // here rather than exposed from the service because this bulk-assign
        // action queries/updates students directly for grouping efficiency
        // instead of going through the service's one-student-at-a-time API.
        private static string BuildRollNumberCodeForAssignment(SchoolPortal.API.Models.RollNumberSettings settings, DateTime admissionDate, string classCode, int sequence)
        {
            var yearSuffix = (admissionDate.Year % 100).ToString("D2");
            var paddedSequence = sequence.ToString().PadLeft(Math.Max(1, settings.SequenceDigits), '0');
            return $"{settings.Prefix}{yearSuffix}{classCode}{paddedSequence}";
        }

        // Blank workbook with headers, formatted example row, and a * on
        // required columns — so the format is shown, not just documented,
        // and a typo'd column order (the #1 way bulk imports silently
        // corrupt data) is much less likely.
        [Authorize(Roles = "Admin")]
        [HttpGet("import/template")]
        public IActionResult DownloadImportTemplate()
        {
            var bytes = _importService.BuildTemplateWorkbook();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Student-Import-Template.xlsx");
        }

        // Processes every row independently — one bad row (a typo'd class
        // name, a malformed B-Form number) is reported against that row
        // and skipped, never aborts the other 999 good ones. Re-uploading
        // the same file after fixing the failed rows is safe: rows already
        // imported are detected by B-Form number and skipped, not
        // duplicated.
        [Authorize(Roles = "Admin")]
        [HttpPost("import")]
        [RequestSizeLimit(20_000_000)] // 20MB — generous for a spreadsheet of even several thousand rows
        public async Task<ActionResult<StudentImportResultDto>> ImportStudents(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            var extension = Path.GetExtension(file.FileName);
            if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Please upload an .xlsx file (use the template download for the right format)." });

            using var stream = file.OpenReadStream();
            var result = await _importService.ImportAsync(stream);
            return Ok(result);
        }


        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateStudentDto dto)
        {
            if (!Enum.TryParse<AdmissionStatus>(dto.AdmissionStatus, out _))
                return BadRequest(new { message = "Invalid AdmissionStatus. Must be Applied, Admitted, Withdrawn, Rejected, or Graduated." });
            var updated = await _studentService.UpdateStudentAsync(id, dto);
            if (!updated) return NotFound();
            return NoContent();
        }


        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var photoFileName = await _context.Students
                    .AsNoTracking()
                    .Where(s => s.StudentId == id)
                    .Select(s => s.PhotoFileName)
                    .FirstOrDefaultAsync();

                var deleted = await _studentService.DeleteStudentAsync(id);
                if (!deleted) return NotFound();

                // Don't leave a deleted student's photo behind on disk.
                if (!string.IsNullOrEmpty(photoFileName))
                {
                    try
                    {
                        var photoPath = Path.Combine(GetPhotoFolder(), photoFileName);
                        if (System.IO.File.Exists(photoPath)) System.IO.File.Delete(photoPath);
                    }
                    catch { /* orphaned file is harmless clutter, not worth failing the delete */ }
                }
                return NoContent();
            }
            catch (DbUpdateException)
            {
                // FeeLedger/StudentCharge cascade-delete with the student, but
                // Payment rows restrict deletion of the ledger/charge they
                // reference — so a student with payment history can't be
                // deleted outright. Surface that as a clear 409, not a 500.
                return Conflict(new
                {
                    message = "This student has payment history and can't be deleted. " +
                               "Set their admission status to Withdrawn instead, or remove their payment records first."
                });
            }
        }

        [HttpGet("by-class/{classId}")]
        public async Task<ActionResult<List<StudentDto>>> GetByClass(int classId)
        {
            var all = await _studentService.GetAllStudentsAsync();
            return Ok(all.Where(s => s.ClassId == classId).ToList());
        }


        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/discount")]
        public async Task<IActionResult> SetDiscount(int id, SetDiscountDto dto)
        {
            var updated = await _studentService.SetDiscountAsync(id, dto.MonthlyDiscountAmount, dto.Reason, dto.ApplyToRemainingMonthsThisYear);
            if (!updated) return NotFound();
            return NoContent();
        }
    }
}