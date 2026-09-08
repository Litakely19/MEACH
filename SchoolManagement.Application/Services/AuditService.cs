using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs.Audit;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Domain.Interfaces;

namespace SchoolManagement.Application.Services;

public class AuditService : IAuditService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public AuditService(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task RecordAsync(
        AuditAction action,
        string entityName,
        object? entityId,
        string description,
        int? userId = null,
        CancellationToken cancellationToken = default)
    {
        var entry = new AuditLog
        {
            UserId = userId ?? _currentUser.User?.Id,
            Action = action,
            EntityName = entityName,
            EntityId = entityId?.ToString(),
            Description = Truncate(description, 1024),
            CreatedAt = DateTime.Now
        };

        await _unitOfWork.AuditLogs.AddAsync(entry, cancellationToken);
    }

    public async Task RecordAndSaveAsync(
        AuditAction action,
        string entityName,
        object? entityId,
        string description,
        int? userId = null,
        CancellationToken cancellationToken = default)
    {
        await RecordAsync(action, entityName, entityId, description, userId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<AuditLogListItem>> ListAsync(
        AuditLogFilter filter,
        CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManageSettings);

        var query = _unitOfWork.AuditLogs.Query().Include(log => log.User).AsQueryable();

        if (filter.UserId.HasValue)
        {
            query = query.Where(log => log.UserId == filter.UserId);
        }

        if (filter.Action.HasValue)
        {
            query = query.Where(log => log.Action == filter.Action);
        }

        if (!string.IsNullOrWhiteSpace(filter.EntityName))
        {
            query = query.Where(log => log.EntityName == filter.EntityName);
        }

        if (filter.From.HasValue)
        {
            var from = filter.From.Value.Date;
            query = query.Where(log => log.CreatedAt >= from);
        }

        if (filter.To.HasValue)
        {
            var to = filter.To.Value.Date.AddDays(1);
            query = query.Where(log => log.CreatedAt < to);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim();
            query = query.Where(log => log.Description.Contains(term) || log.EntityName.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var pageSize = Math.Max(1, filter.PageSize);
        var page = Math.Max(1, filter.Page);

        var logs = await query
            .OrderByDescending(log => log.CreatedAt)
            .ThenByDescending(log => log.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = logs
            .Select(log => new AuditLogListItem(
                log.Id,
                log.CreatedAt,
                log.UserId,
                log.User is null ? "System" : log.User.FullName,
                log.Action,
                log.EntityName,
                log.EntityId,
                log.Description))
            .ToList();

        return new PagedResult<AuditLogListItem>(items, totalCount, page, pageSize);
    }

    private static string Truncate(string value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];
}
