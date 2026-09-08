using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs.Fees;
using SchoolManagement.Application.DTOs.Students;

namespace SchoolManagement.Application.Interfaces;

public interface IStudentService
{
    Task<PagedResult<StudentListItem>> ListAsync(StudentFilter filter, CancellationToken cancellationToken = default);

    Task<StudentDetail> GetDetailAsync(int studentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MonthlyFeeCell>> GetMonthlyFeeTimelineAsync(
        int studentId,
        int year,
        CancellationToken cancellationToken = default);

    Task<int> CreateAsync(CreateStudentRequest request, CancellationToken cancellationToken = default);

    Task UpdateAsync(UpdateStudentRequest request, CancellationToken cancellationToken = default);

    Task TransferAsync(TransferStudentRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int studentId, CancellationToken cancellationToken = default);

    Task<string> PeekNextStudentNumberAsync(CancellationToken cancellationToken = default);
}
