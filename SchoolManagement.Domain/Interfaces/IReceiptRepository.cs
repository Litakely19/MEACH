using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Domain.Interfaces;

public interface IReceiptRepository : IRepository<Receipt>
{
    Task<Receipt?> GetByPaymentIdAsync(int paymentId, bool trackChanges = false, CancellationToken cancellationToken = default);

    Task<Receipt?> GetDetailAsync(int id, CancellationToken cancellationToken = default);

    Task<string?> GetLastReceiptNumberAsync(string prefix, CancellationToken cancellationToken = default);
}
