using Microsoft.EntityFrameworkCore;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Interfaces;
using SchoolManagement.Infrastructure.Data;

namespace SchoolManagement.Infrastructure.Repositories;

public class StudentRepository : SoftDeletableRepository<Student>, IStudentRepository
{
    public StudentRepository(SchoolDbContext context) : base(context)
    {
    }

    public Task<Student?> GetDetailAsync(int id, CancellationToken cancellationToken = default) =>
        Query()
            .Include(student => student.AcademicLevel)
            .Include(student => student.StudentGroup)
                .ThenInclude(group => group.Schedules)
            .Include(student => student.SchoolYear)
            .FirstOrDefaultAsync(student => student.Id == id, cancellationToken);

    public Task<Student?> GetByStudentNumberAsync(string studentNumber, CancellationToken cancellationToken = default) =>
        Query()
            .Include(student => student.AcademicLevel)
            .Include(student => student.StudentGroup)
            .FirstOrDefaultAsync(student => student.StudentNumber == studentNumber, cancellationToken);

    public Task<string?> GetLastStudentNumberAsync(string prefix, CancellationToken cancellationToken = default) =>
        QueryIncludingDeleted()
            .Where(student => student.StudentNumber.StartsWith(prefix))
            .OrderByDescending(student => student.StudentNumber)
            .Select(student => student.StudentNumber)
            .FirstOrDefaultAsync(cancellationToken);
}
