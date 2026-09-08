using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<User?> GetWithRoleAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> UsernameExistsAsync(string username, int? excludeUserId = null, CancellationToken cancellationToken = default);
}
