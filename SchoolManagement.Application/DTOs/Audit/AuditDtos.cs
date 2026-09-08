using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.DTOs.Audit;

public record AuditLogListItem(
    int Id,
    DateTime CreatedAt,
    int? UserId,
    string UserName,
    AuditAction Action,
    string EntityName,
    string? EntityId,
    string Description);

public record AuditLogFilter(
    string? SearchTerm = null,
    int? UserId = null,
    AuditAction? Action = null,
    string? EntityName = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 50);
