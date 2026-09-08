using SchoolManagement.Application.DTOs.SchoolYears;

namespace SchoolManagement.Application.Interfaces;

public interface ISchoolYearService
{
    Task<IReadOnlyList<SchoolYearListItem>> ListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchoolYearOption>> ListOptionsAsync(CancellationToken cancellationToken = default);

    Task<SchoolYearOption?> GetCurrentAsync(CancellationToken cancellationToken = default);

    Task<int> CreateAsync(CreateSchoolYearRequest request, CancellationToken cancellationToken = default);

    Task UpdateAsync(UpdateSchoolYearRequest request, CancellationToken cancellationToken = default);

    Task SetCurrentAsync(int schoolYearId, CancellationToken cancellationToken = default);

    /// <summary>Closing a year blocks new invoices and payments against it.</summary>
    Task CloseAsync(int schoolYearId, CancellationToken cancellationToken = default);

    Task ReopenAsync(int schoolYearId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Throws when the year rejects financial operations, used by the invoice and payment services.
    /// </summary>
    Task EnsureOpenForTransactionsAsync(int schoolYearId, CancellationToken cancellationToken = default);
}
