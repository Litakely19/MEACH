using Microsoft.EntityFrameworkCore;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Interfaces;
using SchoolManagement.Infrastructure.Data;

namespace SchoolManagement.Infrastructure.Repositories;

public class StudentGroupRepository : SoftDeletableRepository<StudentGroup>, IStudentGroupRepository
{
    public StudentGroupRepository(SchoolDbContext context) : base(context)
    {
    }

    public Task<StudentGroup?> GetDetailAsync(int id, CancellationToken cancellationToken = default) =>
        Query()
            .Include(group => group.AcademicLevel)
            .Include(group => group.Schedules)
            .FirstOrDefaultAsync(group => group.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(
        string name,
        int academicLevelId,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = Query().Where(group => group.AcademicLevelId == academicLevelId && group.Name == name);
        if (excludeId.HasValue)
        {
            query = query.Where(group => group.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<int> CountStudentsAsync(int studentGroupId, CancellationToken cancellationToken = default) =>
        Context.Students.CountAsync(
            student => student.StudentGroupId == studentGroupId && !student.IsDeleted,
            cancellationToken);
}
