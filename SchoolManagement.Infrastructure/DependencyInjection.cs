using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Interfaces;
using SchoolManagement.Infrastructure.Data;
using SchoolManagement.Infrastructure.Repositories;
using SchoolManagement.Infrastructure.Security;
using SchoolManagement.Infrastructure.Services;

namespace SchoolManagement.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = DatabaseLocation.Resolve(configuration.GetConnectionString(ConnectionStringName));

        services.AddDbContext<SchoolDbContext>(options =>
        {
            options.UseSqlite(connectionString, sqlite => sqlite.CommandTimeout(30));

            options.ConfigureWarnings(warnings => warnings.Ignore(
                CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<IAcademicLevelRepository, AcademicLevelRepository>();
        services.AddScoped<IStudentGroupRepository, StudentGroupRepository>();
        services.AddScoped<IClassScheduleRepository, ClassScheduleRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<ISchoolYearRepository, SchoolYearRepository>();
        services.AddScoped<IPaymentTypeRepository, PaymentTypeRepository>();
        services.AddScoped<IStudentFeeRepository, StudentFeeRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IReceiptRepository, ReceiptRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<ISchoolSettingRepository, SchoolSettingRepository>();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
        services.AddScoped<IDatabaseBackupService, SqliteBackupService>();

        services.AddSingleton<IPdfExportService, PdfExportService>();
        services.AddSingleton<IExcelExportService, ExcelExportService>();

        return services;
    }
}
