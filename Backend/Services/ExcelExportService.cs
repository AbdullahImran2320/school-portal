// Services/ExcelExportService.cs
using ClosedXML.Excel;
using SchoolPortal.API.DTOs;

namespace SchoolPortal.API.Services
{
    public static class ExcelExportService
    {
        public static byte[] BuildDefaultersWorkbook(List<DefaulterDto> defaulters)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Defaulters");

            var headers = new[] { "Student", "Class", "Father's Mobile", "Overdue Months", "Total Outstanding (Rs)" };
            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];
            ws.Row(1).Style.Font.Bold = true;
            ws.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");

            var row = 2;
            foreach (var d in defaulters)
            {
                ws.Cell(row, 1).Value = d.StudentName;
                ws.Cell(row, 2).Value = d.ClassName;
                ws.Cell(row, 3).Value = d.FatherMobile;
                ws.Cell(row, 4).Value = d.OverdueMonthsCount;
                ws.Cell(row, 5).Value = d.TotalOutstanding;
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
                row++;
            }

            if (defaulters.Count > 0)
            {
                ws.Cell(row, 1).Value = "Total";
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 5).FormulaA1 = $"SUM(E2:E{row - 1})";
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 5).Style.Font.Bold = true;
            }

            ws.Columns().AdjustToContents();
            return ToBytes(workbook);
        }

        public static byte[] BuildCollectionSummaryWorkbook(CollectionSummaryDto summary)
        {
            using var workbook = new XLWorkbook();
            var monthName = new DateTime(summary.Year, summary.Month, 1).ToString("MMMM yyyy");
            var ws = workbook.Worksheets.Add(monthName.Length <= 31 ? monthName : "Collection Summary");

            ws.Cell(1, 1).Value = $"Fee Collection Summary — {monthName}";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 13;
            ws.Range(1, 1, 1, 5).Merge();

            var headers = new[] { "Class", "Tuition Collected (Rs)", "Other Charges Collected (Rs)", "Total (Rs)", "Payments" };
            for (int i = 0; i < headers.Length; i++)
                ws.Cell(3, i + 1).Value = headers[i];
            ws.Row(3).Style.Font.Bold = true;
            ws.Row(3).Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");

            var row = 4;
            foreach (var c in summary.ByClass)
            {
                ws.Cell(row, 1).Value = c.ClassName;
                ws.Cell(row, 2).Value = c.TuitionCollected;
                ws.Cell(row, 3).Value = c.OtherChargesCollected;
                ws.Cell(row, 4).Value = c.Total;
                ws.Cell(row, 5).Value = c.PaymentsCount;
                ws.Range(row, 2, row, 4).Style.NumberFormat.Format = "#,##0";
                row++;
            }

            ws.Cell(row, 1).Value = "Grand Total";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 4).Value = summary.GrandTotal;
            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 4).Style.Font.Bold = true;
            ws.Cell(row, 5).Value = summary.TotalPaymentsCount;
            ws.Cell(row, 5).Style.Font.Bold = true;

            ws.Columns().AdjustToContents();
            return ToBytes(workbook);
        }

        // One row per student, one column per calendar day — P/A/L/L
        // (Present/Absent/Leave/Late) letter codes so the whole month fits
        // legibly across a page, matching how a physical attendance
        // register is actually laid out. Summary columns at the end give
        // the month's totals per student without needing a calculator.
        public static byte[] BuildAttendanceRegisterWorkbook(AttendanceRegisterDto register)
        {
            using var workbook = new XLWorkbook();
            var monthName = new DateTime(register.Year, register.Month, 1).ToString("MMMM yyyy");
            var sheetName = $"{register.ClassName} {monthName}";
            var ws = workbook.Worksheets.Add(sheetName.Length <= 31 ? sheetName : "Register");

            ws.Cell(1, 1).Value = $"Attendance Register — {register.ClassName} — {monthName}";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 13;

            var dayColStart = 3;
            var summaryColStart = dayColStart + register.DaysInMonth;

            ws.Cell(3, 1).Value = "Roll No";
            ws.Cell(3, 2).Value = "Student";
            for (int d = 1; d <= register.DaysInMonth; d++)
                ws.Cell(3, dayColStart + d - 1).Value = d;
            ws.Cell(3, summaryColStart).Value = "Present";
            ws.Cell(3, summaryColStart + 1).Value = "Absent";
            ws.Cell(3, summaryColStart + 2).Value = "Leave";
            ws.Cell(3, summaryColStart + 3).Value = "Late";
            ws.Row(3).Style.Font.Bold = true;
            ws.Row(3).Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");

            var row = 4;
            foreach (var student in register.Students)
            {
                ws.Cell(row, 1).Value = student.RollNumber?.ToString() ?? "";
                ws.Cell(row, 2).Value = student.StudentName;

                foreach (var day in student.Days)
                {
                    var cell = ws.Cell(row, dayColStart + day.Day - 1);
                    cell.Value = day.Status switch
                    {
                        "Present" => "P",
                        "Absent" => "A",
                        "Leave" => "L",
                        "Late" => "T",
                        _ => ""
                    };
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                ws.Cell(row, summaryColStart).Value = student.PresentCount;
                ws.Cell(row, summaryColStart + 1).Value = student.AbsentCount;
                ws.Cell(row, summaryColStart + 2).Value = student.LeaveCount;
                ws.Cell(row, summaryColStart + 3).Value = student.LateCount;
                row++;
            }

            ws.Cell(row + 1, 1).Value = "P = Present, A = Absent, L = Leave, T = Late. Blank = not marked.";
            ws.Cell(row + 1, 1).Style.Font.Italic = true;
            ws.Cell(row + 1, 1).Style.Font.FontSize = 9;

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeColumns(2);
            ws.SheetView.FreezeRows(3);
            return ToBytes(workbook);
        }

        private static byte[] ToBytes(XLWorkbook workbook)
        {
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
