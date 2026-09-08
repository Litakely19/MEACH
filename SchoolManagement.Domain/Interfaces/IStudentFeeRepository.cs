using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Domain.Interfaces;

public interface IStudentFeeRepository : IRepository<StudentFee>
{
    Task<StudentFee?> GetDetailAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> MonthlyExistsAsync(
        int studentId,
        int paymentTypeId,
        int month,
        int year,
        int? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<bool> OneTimeExistsAsync(
        int studentId,
        int paymentTypeId,
        int? schoolYearId = null,
        int? excludeId = null,
        string? notes = null,
        bool matchNotes = false,
        CancellationToken cancellationToken = default);
}
