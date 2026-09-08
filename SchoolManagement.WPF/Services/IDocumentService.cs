using SchoolManagement.Application.DTOs.Reports;

namespace SchoolManagement.WPF.Services;

public enum ExportFormat
{
    Pdf = 0,
    Excel = 1
}

/// <summary>
/// Turns application data into documents on disk: report exports, invoice copies,
/// receipts and their printing. Screens describe the content, this service handles
/// rendering, the save dialog and the external viewer.
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// Completes a report with the school identity, the generation date and the
    /// operator, so every export carries the same header.
    /// </summary>
    Task<TabularReport> BuildReportAsync(
        string title,
        string? subtitle,
        IReadOnlyList<ReportColumn> columns,
        IReadOnlyList<IReadOnlyList<string>> rows,
        IReadOnlyList<string>? totalsRow = null,
        bool landscape = false);

    /// <summary>Renders and saves the report, then opens it. Returns the path, or null when cancelled.</summary>
    Task<string?> ExportReportAsync(TabularReport report, ExportFormat format);

        Task<string?> ExportReceiptByPaymentAsync(int paymentId);

    /// <summary>Registers a print, renders the receipt and hands it to the printing dialog.</summary>
    Task<bool> PrintReceiptAsync(int receiptId);

    Task<string?> ExportReceiptAsync(int receiptId);
}
