using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.WPF.Services;
using SchoolManagement.WPF.ViewModels;
using SchoolManagement.WPF.ViewModels.Administration;
using SchoolManagement.WPF.ViewModels.Billing;
using SchoolManagement.WPF.ViewModels.Dialogs;
using SchoolManagement.WPF.ViewModels.Overview;
using SchoolManagement.WPF.ViewModels.Payments;
using SchoolManagement.WPF.ViewModels.People;
using SchoolManagement.WPF.ViewModels.Reports;
using SchoolManagement.WPF.ViewModels.Shell;
using SchoolManagement.WPF.Views;

namespace SchoolManagement.WPF;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the presentation layer. View models are transient so every
    /// navigation starts from a clean state, and they reach the business services
    /// through <see cref="IScopedExecutor"/> rather than by capturing a scope.
    /// </summary>
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddSingleton<IScopedExecutor, ScopedExecutor>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IDocumentService, DocumentService>();
        services.AddSingleton<INavigationService, NavigationService>();

        // Both are transient: a new session must rebuild the menu for the role that
        // just signed in rather than reuse the previous operator's shell.
        services.AddTransient<MainViewModel>();
        services.AddTransient<LoginViewModel>();

        services.AddTransient<LoginWindow>();
        services.AddTransient<MainWindow>();

        AddScreens(services);
        AddDialogs(services);

        return services;
    }

    private static void AddScreens(IServiceCollection services)
    {
        services.AddTransient<DashboardViewModel>();

        services.AddTransient<StudentListViewModel>();
        services.AddTransient<StudentDetailViewModel>();
        services.AddTransient<AcademicLevelListViewModel>();
        services.AddTransient<SchoolClassListViewModel>();
        services.AddTransient<AttendanceSheetViewModel>();
        services.AddTransient<AttendanceReportViewModel>();
        services.AddTransient<SchoolYearListViewModel>();

        services.AddTransient<MonthlyFeeViewModel>();
        services.AddTransient<DroitFeeViewModel>();
        services.AddTransient<LivreFeeViewModel>();
        services.AddTransient<MockExamFeeViewModel>();
        services.AddTransient<OfficialExamFeeViewModel>();
        services.AddTransient<CustomOneTimeFeeViewModel>();
        services.AddTransient<InvoiceListViewModel>();
        services.AddTransient<UnpaidFeeViewModel>();

        services.AddTransient<PaymentHistoryViewModel>();
        services.AddTransient<ReceiptListViewModel>();

        services.AddTransient<ReportsViewModel>();

        services.AddTransient<UserListViewModel>();
        services.AddTransient<PaymentTypeListViewModel>();
        services.AddTransient<AuditLogViewModel>();
        services.AddTransient<SettingsViewModel>();
    }

    private static void AddDialogs(IServiceCollection services)
    {
        services.AddTransient<ChangePasswordViewModel>();
        services.AddTransient<StudentEditViewModel>();
        services.AddTransient<StudentTransferViewModel>();
        services.AddTransient<AcademicLevelEditViewModel>();
        services.AddTransient<SchoolClassEditViewModel>();
        services.AddTransient<ClassScheduleEditViewModel>();
        services.AddTransient<SchoolYearEditViewModel>();
        services.AddTransient<PaymentTypeEditViewModel>();
        services.AddTransient<InvoiceEditViewModel>();
        services.AddTransient<InvoiceDetailViewModel>();
        services.AddTransient<MonthlyFeeGenerationViewModel>();
        services.AddTransient<OneTimeFeeGenerationViewModel>();
        services.AddTransient<CombinedBillAndPayViewModel>();
        services.AddTransient<PaymentDialogViewModel>();
        services.AddTransient<PaymentDetailViewModel>();
        services.AddTransient<UserEditViewModel>();
        services.AddTransient<ResetPasswordViewModel>();
    }
}
