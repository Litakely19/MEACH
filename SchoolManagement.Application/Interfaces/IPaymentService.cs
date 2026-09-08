using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs.Payments;

namespace SchoolManagement.Application.Interfaces;

public interface IPaymentService
{
    Task<PagedResult<PaymentListItem>> ListAsync(PaymentFilter filter, CancellationToken cancellationToken = default);

    Task<PaymentDetail> GetDetailAsync(int paymentId, CancellationToken cancellationToken = default);

    Task<RegisterPaymentResult> RegisterAsync(
        RegisterPaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<DuplicatePaymentWarning?> CheckForDuplicateAsync(
        RegisterPaymentRequest request,
        CancellationToken cancellationToken = default);

    Task CancelAsync(CancelPaymentRequest request, CancellationToken cancellationToken = default);
}
