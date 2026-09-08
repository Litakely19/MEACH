using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Domain.Interfaces;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<Payment?> GetDetailAsync(int id, CancellationToken cancellationToken = default);

    Task<string?> GetLastPaymentNumberAsync(string prefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects a payment with the same student, amount and method registered within
    /// the given window, so the cashier can be warned about a double entry.
    /// </summary>
    Task<Payment?> FindPossibleDuplicateAsync(
        int studentId,
        decimal amount,
        DateTime paymentDate,
        TimeSpan window,
        CancellationToken cancellationToken = default);
}
