using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.DTOs.Schedules;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Domain.Exceptions;
using SchoolManagement.Domain.Interfaces;

namespace SchoolManagement.Application.Services;

public class ClassScheduleService : IClassScheduleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public ClassScheduleService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task<IReadOnlyList<ClassScheduleDto>> ListByGroupAsync(
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

        return rows.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<ClassScheduleDto>> ListByStudentAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ViewSchedules);

        var student = await _unitOfWork.Students.GetByIdAsync(studentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Student), studentId);

        return await ListByGroupAsync(student.StudentGroupId, cancellationToken);
    }

    public async Task<int> CreateAsync(CreateClassScheduleRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManageSchedules);
        EnsureValidTimes(request.StartTime, request.EndTime);

        var group = await _unitOfWork.StudentGroups.GetByIdAsync(request.StudentGroupId, cancellationToken)
            ?? throw new NotFoundException(nameof(StudentGroup), request.StudentGroupId);

        if (await _unitOfWork.ClassSchedules.OverlapsAsync(
                request.StudentGroupId,
                request.DayOfWeek,
                request.StartTime,
                request.EndTime,
                cancellationToken: cancellationToken))
        {
            throw new DomainException("This schedule overlaps another session for the same group on that day.");
        }

        var schedule = new ClassSchedule
        {
            StudentGroupId = group.Id,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            CreatedAt = DateTime.Now
        };

        await _unitOfWork.ClassSchedules.AddAsync(schedule, cancellationToken);
        await _auditService.RecordAsync(
            AuditAction.Created,
            nameof(ClassSchedule),
            null,
            $"Schedule {request.DayOfWeek} {request.StartTime:hh\\:mm}-{request.EndTime:hh\\:mm} added to '{group.Name}'.",
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return schedule.Id;
    }

    public async Task UpdateAsync(UpdateClassScheduleRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManageSchedules);
        EnsureValidTimes(request.StartTime, request.EndTime);

        var schedule = await _unitOfWork.ClassSchedules.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ClassSchedule), request.Id);

        if (await _unitOfWork.ClassSchedules.OverlapsAsync(
                schedule.StudentGroupId,
                request.DayOfWeek,
                request.StartTime,
                request.EndTime,
                request.Id,
                cancellationToken))
        {
            throw new DomainException("This schedule overlaps another session for the same group on that day.");
        }

        schedule.DayOfWeek = request.DayOfWeek;
        schedule.StartTime = request.StartTime;
        schedule.EndTime = request.EndTime;
        schedule.UpdatedAt = DateTime.Now;

        await _auditService.RecordAsync(
            AuditAction.Updated,
            nameof(ClassSchedule),
            schedule.Id,
            $"Schedule {request.DayOfWeek} {request.StartTime:hh\\:mm}-{request.EndTime:hh\\:mm} updated.",
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int scheduleId, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManageSchedules);

        var schedule = await _unitOfWork.ClassSchedules.GetByIdAsync(scheduleId, cancellationToken)
            ?? throw new NotFoundException(nameof(ClassSchedule), scheduleId);

        var hasAttendance = await _unitOfWork.Attendances.Query()
            .AnyAsync(attendance => attendance.ClassScheduleId == scheduleId, cancellationToken);

        if (hasAttendance)
        {
            throw new DomainException("This schedule already has attendance records and cannot be deleted.");
        }

        _unitOfWork.ClassSchedules.Remove(schedule);
        await _auditService.RecordAsync(
            AuditAction.Deleted,
            nameof(ClassSchedule),
            schedule.Id,
            $"Schedule {schedule.DayOfWeek} {schedule.SessionLabel} removed.",
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void EnsureValidTimes(TimeSpan startTime, TimeSpan endTime)
    {
        if (endTime <= startTime)
        {
            throw new DomainException("The session end time must be after the start time.");
        }
    }

    private static ClassScheduleDto Map(ClassSchedule schedule) =>
        new(
            schedule.Id,
            schedule.StudentGroupId,
            schedule.StudentGroup?.Name ?? string.Empty,
            schedule.DayOfWeek,
            schedule.StartTime,
            schedule.EndTime,
            schedule.SessionLabel);
}
