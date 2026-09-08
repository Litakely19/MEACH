using System.IO;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Application.DTOs.Reports;
using SchoolManagement.Application.Interfaces;

namespace SchoolManagement.WPF.Services;

public class DocumentService : IDocumentService
{
    private readonly IScopedExecutor _scopedExecutor;
    private readonly IFileService _fileService;
    private readonly ICurrentUserService _currentUser;

    public DocumentService(
        IScopedExecutor scopedExecutor,
        IFileService fileService,
        ICurrentUserService currentUser)
    {
        _scopedExecutor = scopedExecutor;
        _fileService = fileService;
        _currentUser = currentUser;
    }

    public async Task<TabularReport> BuildReportAsync(
        string title,
        string? subtitle,
        IReadOnlyList<ReportColumn> columns,
        IReadOnlyList<IReadOnlyList<string>> rows,
        IReadOnlyList<string>? totalsRow = null,
        bool landscape = false)
    {
        var settings = await _scopedExecutor.RunAsync(provider =>
            provider.GetRequiredService<ISchoolSettingsService>().GetAsync());

        return new TabularReport(
            title,
            subtitle,
            columns,
            rows,
            totalsRow,
            settings.SchoolName,
            settings.Address,
            DateTime.Now,
            _currentUser.User?.FullName ?? "System",
            landscape);
    }

    public async Task<string?> ExportReportAsync(TabularReport report, ExportFormat format)
    {
        var isPdf = format == ExportFormat.Pdf;

        var content = await _scopedExecutor.RunAsync(provider => Task.FromResult(isPdf
            ? provider.GetRequiredService<IPdfExportService>().RenderReport(report)
            : provider.GetRequiredService<IExcelExportService>().RenderReport(report)));

        var fileName = $"{Sanitize(report.Title)}-{DateTime.Now:yyyyMMdd-HHmm}{(isPdf ? ".pdf" : ".xlsx")}";

        return await _fileService.SaveAndOpenAsync(
            content,
            fileName,
            isPdf ? _fileService.PdfFilter : _fileService.ExcelFilter);
    }

    public async Task<string?> ExportReceiptByPaymentAsync(int paymentId)
    {
        var (content, receiptNumber) = await _scopedExecutor.RunAsync(async provider =>
        {
            var receipt = await provider.GetRequiredService<IReceiptService>().GetDocumentByPaymentAsync(paymentId);
            return (provider.GetRequiredService<IPdfExportService>().RenderReceipt(receipt), receipt.ReceiptNumber);
        });

        return await _fileService.SaveAndOpenAsync(
            content,
            $"{Sanitize(receiptNumber)}.pdf",
            _fileService.PdfFilter);
    }

    public async Task<string?> ExportReceiptAsync(int receiptId)
    {
        var (content, receiptNumber) = await _scopedExecutor.RunAsync(async provider =>
        {
            var receipt = await provider.GetRequiredService<IReceiptService>().GetDocumentAsync(receiptId);
            return (provider.GetRequiredService<IPdfExportService>().RenderReceipt(receipt), receipt.ReceiptNumber);
        });

        return await _fileService.SaveAndOpenAsync(
            content,
            $"{Sanitize(receiptNumber)}.pdf",
            _fileService.PdfFilter);
    }

    public async Task<bool> PrintReceiptAsync(int receiptId)
    {
        // The print counter is incremented by the service, so a reprint is visible
        // on the document itself and in the receipt list.
        var (content, receiptNumber) = await _scopedExecutor.RunAsync(async provider =>
        {
            var receipt = await provider.GetRequiredService<IReceiptService>().RegisterPrintAsync(receiptId);
            return (provider.GetRequiredService<IPdfExportService>().RenderReceipt(receipt), receipt.ReceiptNumber);
        });

        // Printing goes through a temporary file: the viewer registered for PDF
        // owns the printer selection dialog.
        var path = Path.Combine(Path.GetTempPath(), $"{Sanitize(receiptNumber)}-{Guid.NewGuid():N}.pdf");

        await _fileService.SaveAsync(path, content);
        _fileService.Print(path);

        return true;
    }

    private static string? BuildContactLine(string? phoneNumber, string? email)
    {
        var parts = new[] { phoneNumber, email }.Where(part => !string.IsNullOrWhiteSpace(part));
        var line = string.Join(" - ", parts);

        return string.IsNullOrWhiteSpace(line) ? null : line;
    }

    private static string Sanitize(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Select(character => invalid.Contains(character) ? '-' : character).ToArray());

        return string.IsNullOrWhiteSpace(cleaned) ? "document" : cleaned.Replace(' ', '-');
    }
}
