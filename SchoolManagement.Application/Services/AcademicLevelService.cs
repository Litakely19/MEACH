using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.DTOs.AcademicLevels;
using SchoolManagement.Application.DTOs.StudentGroups;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Domain.Exceptions;
using SchoolManagement.Domain.Interfaces;

namespace SchoolManagement.Application.Services;

public class AcademicLevelService : IAcademicLevelService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public AcademicLevelService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task<IReadOnlyList<AcademicLevelListItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ViewAcademicLevels);

        return await _unitOfWork.AcademicLevels.Query()
            .OrderBy(level => level.Name)
            .Select(level => new AcademicLevelListItem(
                level.Id,
                level.Name,
                level.Description,
                level.IsActive,
                level.StudentGroups.Count(group => !group.IsDeleted),
                level.Students.Count(student => !student.IsDeleted)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AcademicLevelOption>> ListOptionsAsync(
        bool onlyActive = true,
        CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.AcademicLevels.Query();
        if (onlyActive)
        {
            query = query.Where(level => level.IsActive);
        }

        return await query
            .OrderBy(level => level.Name)
            .Select(level => new AcademicLevelOption(level.Id, level.Name, level.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StudentGroupListItem>> ListGroupsAsync(
        int academicLevelId,
        CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ViewStudentGroups);

        return await _unitOfWork.StudentGroups.Query()
            .Where(group => group.AcademicLevelId == academicLevelId)
            .OrderBy(group => group.Name)
            .Select(group => new StudentGroupListItem(
                group.Id,
                group.Name,
                group.Description,
                group.AcademicLevelId,
                group.AcademicLevel.Name,
                group.IsActive,
                group.Students.Count(student => !student.IsDeleted),
                group.Schedules.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CreateAsync(CreateAcademicLevelRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManageAcademicLevels);

        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("The academic level name is required.");
        }

        if (await _unitOfWork.AcademicLevels.NameExistsAsync(name, cancellationToken: cancellationToken))
        {
            throw new DomainException($"An academic level named '{name}' already exists.");
        }

        var level = new AcademicLevel
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        await _unitOfWork.AcademicLevels.AddAsync(level, cancellationToken);
        await _auditService.RecordAsync(
            AuditAction.Created,
            nameof(AcademicLevel),
            null,
            $"Academic level '{name}' created.",
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return level.Id;
    }

    public async Task UpdateAsync(UpdateAcademicLevelRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManageAcademicLevels);

        var level = await _unitOfWork.AcademicLevels.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(AcademicLevel), request.Id);

        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("The academic level name is required.");
        }

        if (await _unitOfWork.AcademicLevels.NameExistsAsync(name, request.Id, cancellationToken))
        {
            throw new DomainException($"An academic level named '{name}' already exists.");
        }

        level.Name = name;
        level.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        level.IsActive = request.IsActive;
        level.UpdatedAt = DateTime.Now;

        await _auditService.RecordAsync(
            AuditAction.Updated,
            nameof(AcademicLevel),
            level.Id,
            $"Academic level '{name}' updated.",
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetActiveAsync(int academicLevelId, bool isActive, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManageAcademicLevels);

        var level = await _unitOfWork.AcademicLevels.GetByIdAsync(academicLevelId, cancellationToken)
            ?? throw new NotFoundException(nameof(AcademicLevel), academicLevelId);

        level.IsActive = isActive;
        level.UpdatedAt = DateTime.Now;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
