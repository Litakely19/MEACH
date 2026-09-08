using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.DTOs.Schedules;
using SchoolManagement.Application.DTOs.StudentGroups;
using SchoolManagement.Application.DTOs.Students;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Application.Services.Internal;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Domain.Exceptions;
using SchoolManagement.Domain.Interfaces;

namespace SchoolManagement.Application.Services;

public class StudentGroupService : IStudentGroupService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public StudentGroupService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task<IReadOnlyList<StudentGroupListItem>> ListAsync(
        StudentGroupFilter filter,
        CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ViewStudentGroups);

        var query = _unitOfWork.StudentGroups.Query();

        if (filter.AcademicLevelId.HasValue)
        {
            query = query.Where(group => group.AcademicLevelId == filter.AcademicLevelId);
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(group => group.IsActive == filter.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim();
            query = query.Where(group =>
                group.Name.Contains(term)
                || group.AcademicLevel.Name.Contains(term)
                || (group.Description != null && group.Description.Contains(term)));
        }

        return await query
            .OrderBy(group => group.AcademicLevel.Name)
            .ThenBy(group => group.Name)
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

    public async Task<IReadOnlyList<StudentGroupOption>> ListOptionsAsync(
        int? academicLevelId = null,
        bool onlyActive = true,
        CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.StudentGroups.Query();

        if (academicLevelId.HasValue)
        {
            query = query.Where(group => group.AcademicLevelId == academicLevelId);
        }

        if (onlyActive)
        {
            query = query.Where(group => group.IsActive);
        }

        return await query
            .OrderBy(group => group.AcademicLevel.Name)
            .ThenBy(group => group.Name)
            .Select(group => new StudentGroupOption(
                group.Id,
                group.Name,
                group.AcademicLevelId,
                group.AcademicLevel.Name,
                group.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StudentListItem>> ListStudentsAsync(
        int studentGroupId,
        CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ViewStudents);

        var students = await _unitOfWork.Students.Query()
            .Where(student => student.StudentGroupId == studentGroupId)
            .OrderBy(student => student.LastName)
            .ThenBy(student => student.FirstName)
            .Select(student => new
            {
                student.Id,
                student.StudentNumber,
                student.FirstName,
                student.LastName,
                student.Gender,
                student.DateOfBirth,
                LevelName = student.AcademicLevel.Name,
                GroupName = student.StudentGroup.Name,
                student.PhoneNumber,
                student.Status,
                student.EnrollmentDate
            })
            .ToListAsync(cancellationToken);

        var balances = await StudentBalanceLookup.GetOutstandingAsync(
            _unitOfWork,
            students.Select(student => student.Id).ToList(),
            cancellationToken);

        return students
            .Select(student => new StudentListItem(
                student.Id,
                student.StudentNumber,
                $"{student.FirstName} {student.LastName}",
                student.Gender,
                student.DateOfBirth,
                student.LevelName,
                student.GroupName,
                student.PhoneNumber,
                student.Status,
                student.EnrollmentDate,
                balances.GetValueOrDefault(student.Id)))
            .ToList();
    }

    public async Task<IReadOnlyList<ClassScheduleDto>> ListSchedulesAsync(
        int studentGroupId,
        CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ViewSchedules);

        var rows = (await _unitOfWork.ClassSchedules.Query()
            .Include(schedule => schedule.StudentGroup)
            .Where(schedule => schedule.StudentGroupId == studentGroupId)
            .ToListAsync(cancellationToken))
            .OrderBy(schedule => schedule.DayOfWeek)
            .ThenBy(schedule => schedule.StartTime)
            .ToList();

        return rows.Select(MapSchedule).ToList();
    }

    private static ClassScheduleDto MapSchedule(ClassSchedule schedule) =>
        new(
            schedule.Id,
            schedule.StudentGroupId,
            schedule.StudentGroup?.Name ?? string.Empty,
            schedule.DayOfWeek,
            schedule.StartTime,
            schedule.EndTime,
            schedule.SessionLabel);

    public async Task<int> CreateAsync(CreateStudentGroupRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManageStudentGroups);

        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("The student group name is required.");
        }

        _ = await _unitOfWork.AcademicLevels.GetByIdAsync(request.AcademicLevelId, cancellationToken)
            ?? throw new NotFoundException(nameof(AcademicLevel), request.AcademicLevelId);

        if (await _unitOfWork.StudentGroups.NameExistsAsync(name, request.AcademicLevelId, cancellationToken: cancellationToken))
        {
            throw new DomainException($"A group named '{name}' already exists for this academic level.");
        }

        var group = new StudentGroup
        {
            AcademicLevelId = request.AcademicLevelId,
            Name = name,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        await _unitOfWork.StudentGroups.AddAsync(group, cancellationToken);
        await _auditService.RecordAsync(
            AuditAction.Created,
            nameof(StudentGroup),
            null,
            $"Student group '{name}' created.",
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return group.Id;
    }

    public async Task UpdateAsync(UpdateStudentGroupRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManageStudentGroups);

        var group = await _unitOfWork.StudentGroups.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(StudentGroup), request.Id);

        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("The student group name is required.");
        }

        _ = await _unitOfWork.AcademicLevels.GetByIdAsync(request.AcademicLevelId, cancellationToken)
            ?? throw new NotFoundException(nameof(AcademicLevel), request.AcademicLevelId);

        if (await _unitOfWork.StudentGroups.NameExistsAsync(name, request.AcademicLevelId, request.Id, cancellationToken))
        {
            throw new DomainException($"A group named '{name}' already exists for this academic level.");
        }

        group.AcademicLevelId = request.AcademicLevelId;
        group.Name = name;
        group.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        group.IsActive = request.IsActive;
        group.UpdatedAt = DateTime.Now;

        await _auditService.RecordAsync(
            AuditAction.Updated,
            nameof(StudentGroup),
            group.Id,
            $"Student group '{name}' updated.",
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetActiveAsync(int studentGroupId, bool isActive, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManageStudentGroups);

        var group = await _unitOfWork.StudentGroups.GetByIdAsync(studentGroupId, cancellationToken)
            ?? throw new NotFoundException(nameof(StudentGroup), studentGroupId);

        group.IsActive = isActive;
        group.UpdatedAt = DateTime.Now;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int studentGroupId, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManageStudentGroups);

        var group = await _unitOfWork.StudentGroups.GetByIdAsync(studentGroupId, cancellationToken)
            ?? throw new NotFoundException(nameof(StudentGroup), studentGroupId);

        var studentCount = await _unitOfWork.StudentGroups.CountStudentsAsync(studentGroupId, cancellationToken);
        if (studentCount > 0)
        {
            throw new DomainException(
                $"The group '{group.Name}' still has {studentCount} student(s). Transfer them before archiving the group.");
        }

        _unitOfWork.StudentGroups.Remove(group);
        await _auditService.RecordAsync(
            AuditAction.Deleted,
            nameof(StudentGroup),
            group.Id,
            $"Student group '{group.Name}' archived.",
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
