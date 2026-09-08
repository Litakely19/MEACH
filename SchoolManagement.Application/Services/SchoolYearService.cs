using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.DTOs.SchoolYears;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Application.Validators;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Domain.Exceptions;
using SchoolManagement.Domain.Interfaces;

namespace SchoolManagement.Application.Services;

public class SchoolYearService : ISchoolYearService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public SchoolYearService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task<IReadOnlyList<SchoolYearListItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        var years = await _unitOfWork.SchoolYears.Query()
            .OrderByDescending(year => year.StartDate)
            .Select(year => new
            {
                year.Id,
                year.Name,
                year.StartDate,
                year.EndDate,
                year.IsCurrent,
                year.IsClosed,
                year.ClosedAt,
                GroupCount = 0,
                StudentCount = year.Students.Count(student => !student.IsDeleted)
            })
            .ToListAsync(cancellationToken);

        return years
            .Select(year => new SchoolYearListItem(
                year.Id,
                year.Name,
                year.StartDate,
                year.EndDate,
                year.IsCurrent,
                year.IsClosed,
                year.ClosedAt,
                year.GroupCount,
                year.StudentCount))
            .ToList();
    }

    public async Task<IReadOnlyList<SchoolYearOption>> ListOptionsAsync(CancellationToken cancellationToken = default)
    {
        var years = await _unitOfWork.SchoolYears.Query()
            .OrderByDescending(year => year.StartDate)
            .ToListAsync(cancellationToken);

        return years
            .Select(year => new SchoolYearOption(
                year.Id,
                year.Name,
                year.IsCurrent,
                year.IsClosed,
                year.StartDate,
                year.EndDate))
            .ToList();
    }

    public async Task<SchoolYearOption?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var year = await _unitOfWork.SchoolYears.GetCurrentAsync(cancellationToken);

        return year is null
            ? null
            : new SchoolYearOption(year.Id, year.Name, year.IsCurrent, year.IsClosed, year.StartDate, year.EndDate);
    }

    public async Task<int> CreateAsync(CreateSchoolYearRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManageSchoolYears);
        RequestValidation.Ensure(request, new CreateSchoolYearRequestValidator());

        var name = request.Name.Trim();

        if (await _unitOfWork.SchoolYears.NameExistsAsync(name, cancellationToken: cancellationToken))
        {
            throw new DomainException($"A school year named '{name}' already exists.");
        }

        var schoolYear = new SchoolYear
        {
            Name = name,
            StartDate = request.StartDate.Date,
            EndDate = request.EndDate.Date,
            IsCurrent = request.SetAsCurrent,
            CreatedAt = DateTime.Now
        };

        await _unitOfWork.SchoolYears.AddAsync(schoolYear, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (request.SetAsCurrent)
        {
            await _unitOfWork.SchoolYears.ClearCurrentFlagAsync(schoolYear.Id, cancellationToken);
        }

        await _auditService.RecordAsync(
            AuditAction.Created,
            nameof(SchoolYear),
            schoolYear.Id,
            $"School year '{name}' created.",
            cancellationToken: cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return schoolYear.Id;
    }

    public async Task UpdateAsync(UpdateSchoolYearRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManageSchoolYears);

        var schoolYear = await _unitOfWork.SchoolYears.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(SchoolYear), request.Id);

        RequestValidation.Ensure(request, new UpdateSchoolYearRequestValidator());

        var name = request.Name.Trim();

        if (await _unitOfWork.SchoolYears.NameExistsAsync(name, request.Id, cancellationToken))
        {
            throw new DomainException($"A school year named '{name}' already exists.");
        }

        schoolYear.Name = name;
        schoolYear.StartDate = request.StartDate.Date;
        schoolYear.EndDate = request.EndDate.Date;

        if (request.SetAsCurrent)
        {
            schoolYear.IsCurrent = true;
            await _unitOfWork.SchoolYears.ClearCurrentFlagAsync(schoolYear.Id, cancellationToken);
        }

        await _auditService.RecordAsync(
            AuditAction.Updated,
            nameof(SchoolYear),
            schoolYear.Id,
            $"School year '{name}' updated.",
            cancellationToken: cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetCurrentAsync(int schoolYearId, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManageSchoolYears);

        var schoolYear = await _unitOfWork.SchoolYears.GetByIdAsync(schoolYearId, cancellationToken)
            ?? throw new NotFoundException(nameof(SchoolYear), schoolYearId);

        schoolYear.IsCurrent = true;
        await _unitOfWork.SchoolYears.ClearCurrentFlagAsync(schoolYearId, cancellationToken);

        await _auditService.RecordAsync(
            AuditAction.Updated,
            nameof(SchoolYear),
            schoolYear.Id,
            $"School year '{schoolYear.Name}' set as the current year.",
            cancellationToken: cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CloseAsync(int schoolYearId, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsurePermission(Permission.ManageSchoolYears);

        var schoolYear = await _unitOfWork.SchoolYears.GetByIdAsync(schoolYearId, cancellationToken)
            ?? throw new NotFoundException(nameof(SchoolYear), schoolYearId);

        if (schoolYear.IsClosed)
        {
            return;
        }

        schoolYear.IsClosed = true;
        schoolYear.ClosedAt = DateTime.Now;

        await _auditService.RecordAsync(
            AuditAction.SchoolYearClosed,
            nameof(SchoolYear),
            schoolYear.Id,
            $"School year '{schoolYear.Name}' closed; new financial operations are blocked.",
            cancellationToken: cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ReopenAsync(int schoolYearId, CancellationToken cancellationToken = default)
    {
        // Reopening a closed financial period is an administrator only action.
        _currentUser.EnsurePermission(Permission.ManageSchoolYears);
        _currentUser.EnsurePermission(Permission.ManageSettings);

        var schoolYear = await _unitOfWork.SchoolYears.GetByIdAsync(schoolYearId, cancellationToken)
            ?? throw new NotFoundException(nameof(SchoolYear), schoolYearId);

        if (!schoolYear.IsClosed)
        {
            return;
        }

        schoolYear.IsClosed = false;
        schoolYear.ClosedAt = null;

        await _auditService.RecordAsync(
            AuditAction.SchoolYearReopened,
            nameof(SchoolYear),
            schoolYear.Id,
            $"School year '{schoolYear.Name}' reopened.",
            cancellationToken: cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task EnsureOpenForTransactionsAsync(int schoolYearId, CancellationToken cancellationToken = default)
    {
        var schoolYear = await _unitOfWork.SchoolYears.Query()
            .FirstOrDefaultAsync(year => year.Id == schoolYearId, cancellationToken)
            ?? throw new NotFoundException(nameof(SchoolYear), schoolYearId);

        if (schoolYear.IsClosed)
        {
            throw new DomainException(
                $"The school year '{schoolYear.Name}' is closed. An administrator must reopen it before new financial operations can be recorded.");
        }
    }
}
