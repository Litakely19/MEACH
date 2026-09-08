namespace SchoolManagement.Domain.Interfaces;

public interface IUnitOfWork
{
    IUserRepository Users { get; }

    IRoleRepository Roles { get; }

    IStudentRepository Students { get; }

    IAcademicLevelRepository AcademicLevels { get; }

    IStudentGroupRepository StudentGroups { get; }

    IClassScheduleRepository ClassSchedules { get; }

    IAttendanceRepository Attendances { get; }

    ISchoolYearRepository SchoolYears { get; }

    IPaymentTypeRepository PaymentTypes { get; }

    IStudentFeeRepository StudentFees { get; }

    IPaymentRepository Payments { get; }

    IReceiptRepository Receipts { get; }

    IAuditLogRepository AuditLogs { get; }

    ISchoolSettingRepository SchoolSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<ITransactionScope> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
