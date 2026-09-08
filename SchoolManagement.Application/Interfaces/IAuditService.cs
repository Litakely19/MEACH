using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs.Audit;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.Interfaces;

public interface IAuditService
{
    /// <summary>
    /// Stages an audit entry on the current unit of work. It is persisted by the
    /// caller's SaveChanges, so an audited operation and its trace commit together.
    /// </summary>
    Task RecordAsync(
        AuditAction action,
        string entityName,
        object? entityId,
        string description,
        int? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Writes an audit entry immediately, for events outside a business transaction (logins).</summary>
    Task RecordAndSaveAsync(
        AuditAction action,
        string entityName,
        object? entityId,
        string description,
        int? userId = null,
        CancellationToken cancellationToken = default);

    Task<PagedResult<AuditLogListItem>> ListAsync(AuditLogFilter filter, CancellationToken cancellationToken = default);
}
