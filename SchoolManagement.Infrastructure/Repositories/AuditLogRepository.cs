using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Interfaces;
using SchoolManagement.Infrastructure.Data;

namespace SchoolManagement.Infrastructure.Repositories;

public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(SchoolDbContext context) : base(context)
    {
    }
}
