using System.Windows.Input;
using Microsoft.Extensions.Logging;
using SchoolManagement.WPF.Commands;

namespace SchoolManagement.WPF.Mvvm;

/// <summary>
/// Base class for the content of a modal dialog. The hosting window owns the
/// buttons and closes itself when <see cref="RequestClose"/> is raised, so a
/// dialog view model never touches a WPF window.
/// </summary>
public abstract class DialogViewModelBase : ViewModelBase
{
    private string _title = string.Empty;
    private string _confirmButtonText = "Save";

    protected DialogViewModelBase(ILogger logger) : base(logger)
    {
        ConfirmCommand = new AsyncRelayCommand(ConfirmAsync);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, false));
    }

    public event EventHandler<bool>? RequestClose;

    public string Title
    {
        get => _title;
        protected set => SetProperty(ref _title, value);
    }

    public string ConfirmButtonText
    {
        get => _confirmButtonText;
        protected set => SetProperty(ref _confirmButtonText, value);
    }

    /// <summary>Set to false by read only dialogs so the host shows a single Close button.</summary>
    public bool IsEditable { get; protected set; } = true;

    public string CancelButtonText => IsEditable ? "Cancel" : "Close";

    public double DialogWidth { get; protected set; } = 560;

    public ICommand ConfirmCommand { get; }

    public ICommand CancelCommand { get; }

    /// <summary>
    /// Persists the dialog content. Returning false keeps the dialog open, which is
    /// what validation failures do.
    /// </summary>
    protected abstract Task<bool> SaveAsync();

    private async Task ConfirmAsync()
    {
        // SaveAsync reports its own validation problems and returns false, which
        // keeps the dialog open on the offending field.
        if (await RunGuardedAsync(SaveAsync))
        {
            RequestClose?.Invoke(this, true);
        }
    }
}
