using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Domain.Interfaces;

public interface IRoleRepository : IRepository<Role>
{
    Task<Role?> GetByNameAsync(RoleName name, CancellationToken cancellationToken = default);
}
