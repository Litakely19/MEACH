using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Domain.Interfaces;

public interface IStudentRepository : IRepository<Student>
{
    Task<Student?> GetDetailAsync(int id, CancellationToken cancellationToken = default);

    Task<Student?> GetByStudentNumberAsync(string studentNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Highest sequence already used for the given student number prefix, used to
    /// allocate the next number without scanning the whole table client side.
    /// </summary>
    Task<string?> GetLastStudentNumberAsync(string prefix, CancellationToken cancellationToken = default);
}
