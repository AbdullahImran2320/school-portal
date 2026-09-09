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

        private static byte[] ToBytes(XLWorkbook workbook)
        {
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
