using Microsoft.EntityFrameworkCore;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Domain.Interfaces;
using SchoolManagement.Infrastructure.Data;

namespace SchoolManagement.Infrastructure.Repositories;

public class RoleRepository : Repository<Role>, IRoleRepository
{
    public RoleRepository(SchoolDbContext context) : base(context)
    {
    }

    public Task<Role?> GetByNameAsync(RoleName name, CancellationToken cancellationToken = default) =>
        Query(trackChanges: true).FirstOrDefaultAsync(role => role.Name == name, cancellationToken);
}
