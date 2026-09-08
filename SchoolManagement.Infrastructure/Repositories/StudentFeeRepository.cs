using Microsoft.EntityFrameworkCore;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Domain.Interfaces;
using SchoolManagement.Infrastructure.Data;

namespace SchoolManagement.Infrastructure.Repositories;

public class StudentFeeRepository : Repository<StudentFee>, IStudentFeeRepository
{
    public StudentFeeRepository(SchoolDbContext context) : base(context)
    {
    }

    public Task<StudentFee?> GetDetailAsync(int id, CancellationToken cancellationToken = default) =>
        Query()
            .Include(fee => fee.Student)
                .ThenInclude(student => student.AcademicLevel)
            .Include(fee => fee.Student)
                .ThenInclude(student => student.StudentGroup)
            .Include(fee => fee.PaymentType)
            .Include(fee => fee.Payments)
            .FirstOrDefaultAsync(fee => fee.Id == id, cancellationToken);

    public Task<bool> MonthlyExistsAsync(
        int studentId,
        int paymentTypeId,
        int month,
        int year,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = Query().Where(fee =>
            fee.StudentId == studentId
            && fee.PaymentTypeId == paymentTypeId
            && fee.Month == month
            && fee.Year == year
            && fee.Status != FeeStatus.Cancelled);

        if (excludeId.HasValue)
        {
            query = query.Where(fee => fee.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> OneTimeExistsAsync(
        int studentId,
        int paymentTypeId,
        int? schoolYearId = null,
        int? excludeId = null,
        string? notes = null,
        bool matchNotes = false,
        CancellationToken cancellationToken = default)
    {
        var query = Query().Where(fee =>
            fee.StudentId == studentId
            && fee.PaymentTypeId == paymentTypeId
            && fee.Month == null
            && fee.Status != FeeStatus.Cancelled);

        if (schoolYearId.HasValue)
        {
            query = query.Where(fee => fee.SchoolYearId == schoolYearId.Value);
        }

        if (excludeId.HasValue)
        {
            query = query.Where(fee => fee.Id != excludeId.Value);
        }

        if (matchNotes)
        {
            var normalized = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
            query = normalized is null
                ? query.Where(fee => fee.Notes == null || fee.Notes == string.Empty)
                : query.Where(fee => fee.Notes == normalized);
        }

        return query.AnyAsync(cancellationToken);
    }
}
