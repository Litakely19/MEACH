using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Application.Services;

namespace SchoolManagement.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the business services. They are scoped because they share the unit
    /// of work of the surrounding operation; the signed in identity is the only
    /// singleton, since it belongs to the desktop session rather than to an operation.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<ICurrentUserService, CurrentUserService>();

        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ISchoolSettingsService, SchoolSettingsService>();
        services.AddScoped<ISchoolYearService, SchoolYearService>();
        services.AddScoped<IAcademicLevelService, AcademicLevelService>();
        services.AddScoped<IStudentGroupService, StudentGroupService>();
        services.AddScoped<IClassScheduleService, ClassScheduleService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<IPaymentTypeService, PaymentTypeService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IReceiptService, ReceiptService>();
        services.AddScoped<IFeeService, FeeService>();
        services.AddScoped<IReportService, ReportService>();

        return services;
    }
}
