using SchoolManagement.Application.DTOs.PaymentTypes;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.Interfaces;

public interface IPaymentTypeService
{
    Task<IReadOnlyList<PaymentTypeListItem>> ListAsync(
        bool includeInactive = true,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentTypeOption>> ListOptionsAsync(
        PaymentFrequency? frequency = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentTypeOption>> ListOptionsAsync(
        PaymentTypeScope scope,
        CancellationToken cancellationToken = default);

    Task<int> CreateAsync(CreatePaymentTypeRequest request, CancellationToken cancellationToken = default);

    Task UpdateAsync(UpdatePaymentTypeRequest request, CancellationToken cancellationToken = default);

    Task SetActiveAsync(int paymentTypeId, bool isActive, CancellationToken cancellationToken = default);
}
