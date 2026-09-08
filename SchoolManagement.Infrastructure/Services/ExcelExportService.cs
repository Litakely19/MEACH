using ClosedXML.Excel;
using SchoolManagement.Application.DTOs.Reports;
using SchoolManagement.Application.Interfaces;

namespace SchoolManagement.Infrastructure.Services;

/// <summary>
/// Writes a <see cref="TabularReport"/> to a real .xlsx workbook with ClosedXML.
/// </summary>
/// <remarks>
/// Cells that hold a number are written as numbers rather than text, so the
/// accountant can sum and filter the export instead of retyping it.
/// </remarks>
public class ExcelExportService : IExcelExportService
{
    private const string AccentColor = "#1B5E9C";

    public byte[] RenderReport(TabularReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(SanitizeSheetName(report.Title));

        var columnCount = Math.Max(1, report.Columns.Count);
        var currentRow = 1;

        sheet.Cell(currentRow, 1).Value = report.SchoolName;
        sheet.Range(currentRow, 1, currentRow, columnCount).Merge();
        sheet.Cell(currentRow, 1).Style.Font.SetBold().Font.SetFontSize(14);
        currentRow++;

        if (!string.IsNullOrWhiteSpace(report.SchoolAddress))
        {
            sheet.Cell(currentRow, 1).Value = report.SchoolAddress;
            sheet.Range(currentRow, 1, currentRow, columnCount).Merge();
            currentRow++;
        }

        sheet.Cell(currentRow, 1).Value = report.Title;
        sheet.Range(currentRow, 1, currentRow, columnCount).Merge();
        sheet.Cell(currentRow, 1).Style.Font.SetBold().Font.SetFontSize(12);
        currentRow++;

        if (!string.IsNullOrWhiteSpace(report.Subtitle))
        {
            sheet.Cell(currentRow, 1).Value = report.Subtitle;
            sheet.Range(currentRow, 1, currentRow, columnCount).Merge();
            currentRow++;
        }

        sheet.Cell(currentRow, 1).Value = $"Generated on {report.GeneratedAt:dd/MM/yyyy HH:mm} by {report.GeneratedBy}";
        sheet.Range(currentRow, 1, currentRow, columnCount).Merge();
        sheet.Cell(currentRow, 1).Style.Font.SetItalic().Font.SetFontSize(9);
        currentRow += 2;

        var headerRow = currentRow;
        for (var index = 0; index < report.Columns.Count; index++)
        {
            var cell = sheet.Cell(headerRow, index + 1);
            cell.Value = report.Columns[index].Header;
            cell.Style.Font.SetBold().Font.SetFontColor(XLColor.White);
            cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml(AccentColor));
            cell.Style.Alignment.SetHorizontal(ToExcelAlignment(report.Columns[index].Alignment));
        }

        currentRow++;

        foreach (var row in report.Rows)
        {
            for (var index = 0; index < report.Columns.Count; index++)
            {
                var value = index < row.Count ? row[index] : string.Empty;
                var cell = sheet.Cell(currentRow, index + 1);

                SetCellValue(cell, value);
                cell.Style.Alignment.SetHorizontal(ToExcelAlignment(report.Columns[index].Alignment));
            }

            currentRow++;
        }

        if (report.TotalsRow is { Count: > 0 })
        {
            for (var index = 0; index < report.Columns.Count; index++)
            {
                var value = index < report.TotalsRow.Count ? report.TotalsRow[index] : string.Empty;
                var cell = sheet.Cell(currentRow, index + 1);

                SetCellValue(cell, value);
                cell.Style.Font.SetBold();
                cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#EDF2F7"));
                cell.Style.Alignment.SetHorizontal(ToExcelAlignment(report.Columns[index].Alignment));
            }

            currentRow++;
        }

        if (report.Rows.Count > 0)
        {
            sheet.Range(headerRow, 1, headerRow + report.Rows.Count, columnCount).SetAutoFilter();
        }

        sheet.SheetView.FreezeRows(headerRow);
        sheet.Columns().AdjustToContents(headerRow, currentRow, 8d, 45d);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Amounts arrive already formatted for display ("1 250 000 Ar"); the digits are
    /// recovered so the workbook keeps them numeric.
    /// </summary>
    private static void SetCellValue(IXLCell cell, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            cell.Value = string.Empty;
            return;
        }

        if (TryParseNumber(value, out var number))
        {
            cell.Value = number;
            cell.Style.NumberFormat.SetFormat("#,##0.##");
            return;
        }

        if (DateTime.TryParse(value, out var date))
        {
            cell.Value = date;
            cell.Style.DateFormat.SetFormat("dd/MM/yyyy");
            return;
        }

        cell.Value = value;
    }

    private static bool TryParseNumber(string value, out double number)
    {
        number = 0d;

        var cleaned = value
            .Replace("Ar", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("MGA", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("%", string.Empty)
            .Replace("\u00A0", string.Empty)
            .Replace(" ", string.Empty)
            .Trim();

        if (cleaned.Length == 0 || !cleaned.All(character => char.IsDigit(character)
                || character is ',' or '.' or '-'))
        {
            return false;
        }

        cleaned = cleaned.Replace(',', '.');

        return double.TryParse(
            cleaned,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out number);
    }

    private static XLAlignmentHorizontalValues ToExcelAlignment(ReportColumnAlignment alignment) => alignment switch
    {
        ReportColumnAlignment.Right => XLAlignmentHorizontalValues.Right,
        ReportColumnAlignment.Center => XLAlignmentHorizontalValues.Center,
        _ => XLAlignmentHorizontalValues.Left
    };

    private static string SanitizeSheetName(string title)
    {
        var invalid = new[] { '\\', '/', '*', '?', ':', '[', ']' };
        var cleaned = new string(title.Where(character => !invalid.Contains(character)).ToArray()).Trim();

        if (string.IsNullOrWhiteSpace(cleaned))
        {
            cleaned = "Report";
        }

        return cleaned.Length > 31 ? cleaned[..31] : cleaned;
    }
}
