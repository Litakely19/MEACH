using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.DTOs.Settings;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.WPF.Commands;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;

namespace SchoolManagement.WPF.ViewModels.Administration;

/// <summary>
/// School identity used on every printed document, billing defaults, and the
/// database backup. Changing the identity here changes all future receipts.
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    private readonly IScopedExecutor _scopedExecutor;
    private readonly IFileService _fileService;
    private readonly IDialogService _dialogService;

    private string _schoolName = string.Empty;
    private string? _address;
    private string? _phoneNumber;
    private string? _email;
    private string? _website;
    private string _currencyCode = "MGA";
    private string _currencySymbol = "Ar";
    private string? _receiptFooter;
    private string? _logoPath;
    private int _defaultDueDay = 5;
    private string _databasePath = string.Empty;

    public SettingsViewModel(
        ILogger<SettingsViewModel> logger,
        IScopedExecutor scopedExecutor,
        IFileService fileService,
        IDialogService dialogService) : base(logger)
    {
        _scopedExecutor = scopedExecutor;
        _fileService = fileService;
        _dialogService = dialogService;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        ReloadCommand = new AsyncRelayCommand(LoadAsync);
        BrowseLogoCommand = new RelayCommand(BrowseLogo);
        ClearLogoCommand = new RelayCommand(() => LogoPath = null);
        BackupCommand = new AsyncRelayCommand(BackupAsync);
        RefreshOverdueCommand = new AsyncRelayCommand(RefreshOverdueAsync);
    }

    public ICommand SaveCommand { get; }

    public ICommand ReloadCommand { get; }

    public ICommand BrowseLogoCommand { get; }

    public ICommand ClearLogoCommand { get; }

    public ICommand BackupCommand { get; }

    public ICommand RefreshOverdueCommand { get; }

    public string SchoolName
    {
        get => _schoolName;
        set => SetProperty(ref _schoolName, value);
    }

    public string? Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }

    public string? PhoneNumber
    {
        get => _phoneNumber;
        set => SetProperty(ref _phoneNumber, value);
    }

    public string? Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string? Website
    {
        get => _website;
        set => SetProperty(ref _website, value);
    }

    public string CurrencyCode
    {
        get => _currencyCode;
        set => SetProperty(ref _currencyCode, value);
    }

    public string CurrencySymbol
    {
        get => _currencySymbol;
        set => SetProperty(ref _currencySymbol, value);
    }

    public string? ReceiptFooter
    {
        get => _receiptFooter;
        set => SetProperty(ref _receiptFooter, value);
    }

    public string? LogoPath
    {
        get => _logoPath;
        set => SetProperty(ref _logoPath, value);
    }

    /// <summary>Day of the month used as due date when generating monthly fees.</summary>
    public int DefaultDueDay
    {
        get => _defaultDueDay;
        set => SetProperty(ref _defaultDueDay, value);
    }

    public string DatabasePath
    {
        get => _databasePath;
        private set => SetProperty(ref _databasePath, value);
    }

    public override Task LoadAsync() =>
        RunGuardedAsync(async () =>
        {
            var (settings, databasePath) = await _scopedExecutor.RunAsync(async provider =>
            {
                var dto = await provider.GetRequiredService<ISchoolSettingsService>().GetAsync();
                var path = provider.GetRequiredService<IDatabaseBackupService>().DatabaseFilePath;

                return (dto, path);
            });

            Apply(settings);
            DatabasePath = databasePath;
        });

    private void Apply(SchoolSettingsDto settings)
    {
        SchoolName = settings.SchoolName;
        Address = settings.Address;
        PhoneNumber = settings.PhoneNumber;
        Email = settings.Email;
        Website = settings.Website;
        CurrencyCode = settings.CurrencyCode;
        CurrencySymbol = settings.CurrencySymbol;
        ReceiptFooter = settings.ReceiptFooter;
        LogoPath = settings.LogoPath;
        DefaultDueDay = settings.DefaultDueDay;
    }

    private Task SaveAsync() =>
        RunGuardedAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(SchoolName))
            {
                ErrorMessage = "The school name is required; it appears on every receipt.";
                return false;
            }

            if (DefaultDueDay is < 1 or > 28)
            {
                ErrorMessage = "The default due day must be between 1 and 28.";
                return false;
            }

            await _scopedExecutor.RunAsync(provider =>
                provider.GetRequiredService<ISchoolSettingsService>().UpdateAsync(new UpdateSchoolSettingsRequest(
                    SchoolName.Trim(),
                    Trim(Address),
                    Trim(PhoneNumber),
                    Trim(Email),
                    Trim(Website),
                    string.IsNullOrWhiteSpace(CurrencyCode) ? "MGA" : CurrencyCode.Trim(),
                    string.IsNullOrWhiteSpace(CurrencySymbol) ? "Ar" : CurrencySymbol.Trim(),
                    Trim(ReceiptFooter),
                    Trim(LogoPath),
                    DefaultDueDay)));

            StatusMessage = "The settings have been saved.";
            return true;
        });

    private void BrowseLogo()
    {
        var path = _fileService.AskForFileToOpen(_fileService.ImageFilter);

        if (!string.IsNullOrWhiteSpace(path))
        {
            LogoPath = path;
        }
    }

    private Task BackupAsync() =>
        RunGuardedAsync(async () =>
        {
            var suggestedName = await _scopedExecutor.RunAsync(provider =>
                Task.FromResult(provider.GetRequiredService<IDatabaseBackupService>().BuildSuggestedFileName()));

            var destination = _fileService.AskForSavePath(suggestedName, _fileService.DatabaseFilter);

            if (string.IsNullOrWhiteSpace(destination))
            {
                return;
            }

            await _scopedExecutor.RunAsync(provider =>
                provider.GetRequiredService<IDatabaseBackupService>().BackupAsync(destination));

            StatusMessage = $"Backup written to {destination}";
        });

    private Task RefreshOverdueAsync() =>
        RunGuardedAsync(async () =>
        {
            if (!_dialogService.Confirm(
                    "Mark every unpaid item past its due date as overdue?",
                    "Refresh overdue items"))
            {
                return;
            }

            var affected = await _scopedExecutor.RunAsync(provider =>
                provider.GetRequiredService<IFeeService>().RefreshOverdueStatusesAsync());

            StatusMessage = affected == 0
                ? "No item needed to change status."
                : $"{affected} item(s) are now flagged as overdue.";
        });

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
