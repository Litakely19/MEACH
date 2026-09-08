using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.DTOs.Attendance;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Domain.Exceptions;
using SchoolManagement.Domain.Interfaces;

namespace SchoolManagement.Application.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public AttendanceService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task<AttendanceSheet> GetSheetAsync(
        int studentGroupId,
        DateTime date,
        int? classScheduleId = null,
        CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ViewAttendance);

        var group = await _unitOfWork.StudentGroups.Query()
            .Include(item => item.AcademicLevel)
            .FirstOrDefaultAsync(item => item.Id == studentGroupId, cancellationToken)
            ?? throw new NotFoundException(nameof(StudentGroup), studentGroupId);

        var day = date.Date;
        var schedules = (await _unitOfWork.ClassSchedules.Query()
            .Where(schedule => schedule.StudentGroupId == studentGroupId && schedule.DayOfWeek == day.DayOfWeek)
            .ToListAsync(cancellationToken))
            .OrderBy(schedule => schedule.StartTime)
            .ToList();

        ClassSchedule? session = null;
        if (classScheduleId.HasValue)
        {
            session = schedules.FirstOrDefault(schedule => schedule.Id == classScheduleId.Value)
                ?? await _unitOfWork.ClassSchedules.Query()
                    .FirstOrDefaultAsync(
                        schedule => schedule.Id == classScheduleId.Value && schedule.StudentGroupId == studentGroupId,
                        cancellationToken);
        }
        else
        {
            session = schedules.FirstOrDefault();
        }

        var students = await _unitOfWork.Students.Query()
            .Where(student => student.StudentGroupId == studentGroupId && student.Status == StudentStatus.Active)
            .OrderBy(student => student.LastName)
            .ThenBy(student => student.FirstName)
            .Select(student => new { student.Id, student.StudentNumber, student.FirstName, student.LastName })
            .ToListAsync(cancellationToken);

        var existing = session is null
            ? new Dictionary<int, Attendance>()
            : await _unitOfWork.Attendances.Query()
                .Where(attendance => attendance.ClassScheduleId == session.Id && attendance.AttendanceDate == day)
                .ToDictionaryAsync(attendance => attendance.StudentId, cancellationToken);

        var rows = students
            .Select(student =>
            {
                existing.TryGetValue(student.Id, out var record);
                return new AttendanceSheetStudent(
                    student.Id,
                    student.StudentNumber,
                    $"{student.FirstName} {student.LastName}".Trim(),
                    record?.Id,
                    record?.Status ?? AttendanceStatus.Present,
                    record?.Remarks);
            })
            .ToList();

        return new AttendanceSheet(
            group.AcademicLevelId,
            group.AcademicLevel.Name,
            group.Id,
            group.Name,
            day,
            session is null
                ? null
                : new ClassScheduleSession(
                    session.Id,
                    session.DayOfWeek,
                    session.StartTime,
                    session.EndTime,
                    session.SessionLabel),
            rows);
    }

    public async Task SaveAsync(SaveAttendanceRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManageAttendance);

        var group = await _unitOfWork.StudentGroups.GetByIdAsync(request.StudentGroupId, cancellationToken)
            ?? throw new NotFoundException(nameof(StudentGroup), request.StudentGroupId);

        var schedule = await _unitOfWork.ClassSchedules.GetByIdAsync(request.ClassScheduleId, cancellationToken)
            ?? throw new NotFoundException(nameof(ClassSchedule), request.ClassScheduleId);

        if (schedule.StudentGroupId != group.Id)
        {
            throw new DomainException("The selected session does not belong to this student group.");
        }

        var day = request.Date.Date;
        var studentIds = request.Entries.Select(entry => entry.StudentId).Distinct().ToList();

        var groupStudentIds = await _unitOfWork.Students.Query()
            .Where(student => student.StudentGroupId == group.Id && studentIds.Contains(student.Id))
            .Select(student => student.Id)
            .ToListAsync(cancellationToken);

        if (groupStudentIds.Count != studentIds.Count)
        {
            throw new DomainException("Attendance can only be recorded for students who belong to the selected group.");
        }

        var userId = _currentUser.RequireUserId();
        var now = DateTime.Now;

        foreach (var entry in request.Entries)
        {
            var existing = await _unitOfWork.Attendances.GetAsync(
                entry.StudentId,
                schedule.Id,
                day,
                cancellationToken);

            if (existing is null)
            {
                await _unitOfWork.Attendances.AddAsync(new Attendance
                {
                    StudentId = entry.StudentId,
                    ClassScheduleId = schedule.Id,
                    AttendanceDate = day,
                    Status = entry.Status,
                    Remarks = string.IsNullOrWhiteSpace(entry.Remarks) ? null : entry.Remarks.Trim(),
                    RecordedByUserId = userId,
                    CreatedAt = now
                }, cancellationToken);
            }
            else
            {
                existing.Status = entry.Status;
                existing.Remarks = string.IsNullOrWhiteSpace(entry.Remarks) ? null : entry.Remarks.Trim();
                existing.RecordedByUserId = userId;
            }
        }

        await _auditService.RecordAsync(
            AuditAction.AttendanceRecorded,
            nameof(Attendance),
            group.Id,
            $"Attendance recorded for '{group.Name}' on {day:dd/MM/yyyy} ({schedule.SessionLabel}).",
            cancellationToken: cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AttendanceReportRow>> GetReportAsync(
        AttendanceReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ViewAttendance);

        var from = filter.From?.Date;
        var to = filter.To?.Date;

        if (filter.Month is > 0 and <= 12)
        {
            var year = filter.Year ?? DateTime.Today.Year;
            from = new DateTime(year, filter.Month.Value, 1);
            to = from.Value.AddMonths(1).AddDays(-1);
        }

        var studentsQuery = _unitOfWork.Students.Query();

        if (filter.StudentId.HasValue)
        {
            studentsQuery = studentsQuery.Where(student => student.Id == filter.StudentId);
        }

        if (filter.AcademicLevelId.HasValue)
        {
            studentsQuery = studentsQuery.Where(student => student.AcademicLevelId == filter.AcademicLevelId);
        }

        if (filter.StudentGroupId.HasValue)
        {
            studentsQuery = studentsQuery.Where(student => student.StudentGroupId == filter.StudentGroupId);
        }

        var students = await studentsQuery
            .OrderBy(student => student.LastName)
            .ThenBy(student => student.FirstName)
            .Select(student => new
            {
                student.Id,
                student.StudentNumber,
                student.FirstName,
                student.LastName,
                LevelName = student.AcademicLevel.Name,
                GroupName = student.StudentGroup.Name
            })
            .ToListAsync(cancellationToken);

        var studentIds = students.Select(student => student.Id).ToList();
        if (studentIds.Count == 0)
        {
            return Array.Empty<AttendanceReportRow>();
        }

        var attendanceQuery = _unitOfWork.Attendances.Query()
            .Where(attendance => studentIds.Contains(attendance.StudentId));

        if (from.HasValue)
        {
            attendanceQuery = attendanceQuery.Where(attendance => attendance.AttendanceDate >= from.Value);
        }

        if (to.HasValue)
        {
            attendanceQuery = attendanceQuery.Where(attendance => attendance.AttendanceDate <= to.Value);
        }

        var records = await attendanceQuery
            .Select(attendance => new { attendance.StudentId, attendance.Status })
            .ToListAsync(cancellationToken);

        var grouped = records.GroupBy(record => record.StudentId).ToDictionary(group => group.Key, group => group.ToList());

        return students
            .Select(student =>
            {
                grouped.TryGetValue(student.Id, out var rows);
                rows ??= [];

                var present = rows.Count(row => row.Status == AttendanceStatus.Present);
                var absent = rows.Count(row => row.Status == AttendanceStatus.Absent);
                var late = rows.Count(row => row.Status == AttendanceStatus.Late);
                var excused = rows.Count(row => row.Status == AttendanceStatus.Excused);
                var total = rows.Count;
                var percentage = total == 0 ? 0m : Math.Round(present / (decimal)total * 100m, 1);

                return new AttendanceReportRow(
                    student.Id,
                    student.StudentNumber,
                    $"{student.FirstName} {student.LastName}".Trim(),
                    student.LevelName,
                    student.GroupName,
                    total,
                    present,
                    absent,
                    late,
                    excused,
                    percentage);
            })
            .ToList();
    }
}
