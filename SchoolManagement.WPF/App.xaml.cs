using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Infrastructure;
using SchoolManagement.WPF.Services;
using SchoolManagement.WPF.Views;
using Serilog;

namespace SchoolManagement.WPF;

// Qualified because SchoolManagement.Application shadows the WPF Application type here.
public partial class App : System.Windows.Application
{
    private IHost? _host;
    private ILogger<App>? _logger;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // The session loop below owns the window lifetime, so closing the login
        // window must not be interpreted as "last window closed, quit".
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        try
        {
            _host = BuildHost();
            await _host.StartAsync();

            _logger = _host.Services.GetRequiredService<ILogger<App>>();
            _logger.LogInformation("Application starting");

            await InitializeDatabaseAsync();
            await RunSessionsAsync();
        }
        catch (Exception exception)
        {
            _logger?.LogCritical(exception, "The application could not start");
            MessageBox.Show(
                $"The application could not start.\n\n{exception.Message}",
                "School Management",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }

        await Log.CloseAndFlushAsync();

        base.OnExit(e);
    }

    private static IHost BuildHost() =>
        Host.CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.Sources.Clear();
                configuration
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                    .AddEnvironmentVariables("SCHOOLMANAGEMENT_");
            })
            .UseSerilog((context, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .WriteTo.File(
                    Path.Combine(LogDirectory(), "school-management-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14,
                    shared: true))
            .ConfigureServices((context, services) => services
                .AddInfrastructure(context.Configuration)
                .AddApplication()
                .AddPresentation())
            .Build();

    private static string LogDirectory()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SchoolManagement",
            "logs");

        Directory.CreateDirectory(directory);
        return directory;
    }

    /// <summary>
    /// Applies pending migrations and seeds the roles, the administrator account,
    /// the payment types, the school settings and demo roster data before any window is shown.
    /// </summary>
    private async Task InitializeDatabaseAsync()
    {
        await using var scope = _host!.Services.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>().InitializeAsync();

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        if (configuration.GetValue("Application:RefreshOverdueFeesAtStartup", true))
        {
            // Overdue is a function of today's date, so it is recomputed once per run
            // instead of being trusted from the last session.
            var updated = await scope.ServiceProvider.GetRequiredService<IFeeService>()
                .RefreshOverdueStatusesAsync();

            _logger!.LogInformation("Marked {Count} fee lines as overdue at startup", updated);
        }
    }

    /// <summary>
    /// Login window, then shell, then login window again after a sign out. The
    /// loop ends when the operator closes a window instead of signing in or out.
    /// </summary>
    private async Task RunSessionsAsync()
    {
        var services = _host!.Services;

        while (true)
        {
            var loginWindow = services.GetRequiredService<LoginWindow>();

            if (loginWindow.ShowDialog() != true)
            {
                break;
            }

            var signOutRequested = await ShowShellAsync();

            services.GetRequiredService<INavigationService>().Reset();

            if (!signOutRequested)
            {
                break;
            }

            await using var scope = services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IAuthenticationService>().LogoutAsync();
        }

        Shutdown();
    }

    /// <summary>Returns true when the shell was closed because of a sign out.</summary>
    private Task<bool> ShowShellAsync()
    {
        var completion = new TaskCompletionSource<bool>();
        var shell = _host!.Services.GetRequiredService<MainWindow>();
        var signOutRequested = false;

        shell.ViewModel.LogoutRequested += (_, _) =>
        {
            signOutRequested = true;
            shell.Close();
        };

        shell.Closed += (_, _) =>
        {
            MainWindow = null;
            completion.TrySetResult(signOutRequested);
        };

        MainWindow = shell;
        shell.Show();

        return completion.Task;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.LogError(e.Exception, "Unhandled exception on the user interface thread");

        MessageBox.Show(
            $"An unexpected error occurred.\n\n{e.Exception.Message}",
            "School Management",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        // Keeping the process alive is safer than losing the operator's context: the
        // failed operation is already rolled back by its own unit of work.
        e.Handled = true;
    }
}
