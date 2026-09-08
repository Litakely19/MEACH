using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Domain.Interfaces;

public interface IPaymentTypeRepository : IRepository<PaymentType>
{
    Task<PaymentType?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentType>> ListActiveAsync(PaymentFrequency? frequency = null, CancellationToken cancellationToken = default);
}
