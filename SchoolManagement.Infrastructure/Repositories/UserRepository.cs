using Microsoft.EntityFrameworkCore;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Interfaces;
using SchoolManagement.Infrastructure.Data;

namespace SchoolManagement.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(SchoolDbContext context) : base(context)
    {
    }

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        Query(trackChanges: true)
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user => user.Username == username, cancellationToken);

    public Task<User?> GetWithRoleAsync(int id, CancellationToken cancellationToken = default) =>
        Query(trackChanges: true)
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<bool> UsernameExistsAsync(string username, int? excludeUserId = null, CancellationToken cancellationToken = default) =>
        Query().AnyAsync(
            user => user.Username == username && (excludeUserId == null || user.Id != excludeUserId),
            cancellationToken);
}
