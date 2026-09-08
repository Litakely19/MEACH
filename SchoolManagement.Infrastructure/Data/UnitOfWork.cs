using SchoolManagement.Domain.Interfaces;
using SchoolManagement.Infrastructure.Repositories;

namespace SchoolManagement.Infrastructure.Data;

/// <summary>
/// Groups every repository over one <see cref="SchoolDbContext"/> instance so that
/// a single <see cref="SaveChangesAsync"/> commits a whole business operation.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly SchoolDbContext _context;

    private IUserRepository? _users;
    private IRoleRepository? _roles;
    private IStudentRepository? _students;
    private IAcademicLevelRepository? _academicLevels;
    private IStudentGroupRepository? _studentGroups;
    private IClassScheduleRepository? _classSchedules;
    private IAttendanceRepository? _attendances;
    private ISchoolYearRepository? _schoolYears;
    private IPaymentTypeRepository? _paymentTypes;
    private IStudentFeeRepository? _studentFees;
    private IPaymentRepository? _payments;
    private IReceiptRepository? _receipts;
    private IAuditLogRepository? _auditLogs;
    private ISchoolSettingRepository? _schoolSettings;

    public UnitOfWork(SchoolDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _users ??= new UserRepository(_context);

    public IRoleRepository Roles => _roles ??= new RoleRepository(_context);

    public IStudentRepository Students => _students ??= new StudentRepository(_context);

    public IAcademicLevelRepository AcademicLevels => _academicLevels ??= new AcademicLevelRepository(_context);

    public IStudentGroupRepository StudentGroups => _studentGroups ??= new StudentGroupRepository(_context);

    public IClassScheduleRepository ClassSchedules => _classSchedules ??= new ClassScheduleRepository(_context);

    public IAttendanceRepository Attendances => _attendances ??= new AttendanceRepository(_context);

    public ISchoolYearRepository SchoolYears => _schoolYears ??= new SchoolYearRepository(_context);

    public IPaymentTypeRepository PaymentTypes => _paymentTypes ??= new PaymentTypeRepository(_context);

    public IStudentFeeRepository StudentFees => _studentFees ??= new StudentFeeRepository(_context);

    public IPaymentRepository Payments => _payments ??= new PaymentRepository(_context);

    public IReceiptRepository Receipts => _receipts ??= new ReceiptRepository(_context);

    public IAuditLogRepository AuditLogs => _auditLogs ??= new AuditLogRepository(_context);

    public ISchoolSettingRepository SchoolSettings => _schoolSettings ??= new SchoolSettingRepository(_context);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public async Task<ITransactionScope> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        return new EfTransactionScope(transaction);
    }
}
