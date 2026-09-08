using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs.Receipts;

namespace SchoolManagement.Application.Interfaces;

public interface IReceiptService
{
    Task<PagedResult<ReceiptListItem>> ListAsync(ReceiptFilter filter, CancellationToken cancellationToken = default);

    Task<ReceiptDocument> GetDocumentAsync(int receiptId, CancellationToken cancellationToken = default);

    Task<ReceiptDocument> GetDocumentByPaymentAsync(int paymentId, CancellationToken cancellationToken = default);

    /// <summary>Records a print or reprint and returns the document to render.</summary>
    Task<ReceiptDocument> RegisterPrintAsync(int receiptId, CancellationToken cancellationToken = default);
}
