using Microsoft.Extensions.Logging;
using SchoolManagement.Domain.Exceptions;

namespace SchoolManagement.WPF.Mvvm;

public abstract class ViewModelBase : ObservableObject
{
    private bool _isBusy;
    private string? _errorMessage;
    private string? _statusMessage;

    protected ViewModelBase(ILogger logger)
    {
        Logger = logger;
    }

    protected ILogger Logger { get; }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsIdle));
            }
        }
    }

    public bool IsIdle => !_isBusy;

    public string? ErrorMessage
    {
        get => _errorMessage;
        protected set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(_errorMessage);

    public string? StatusMessage
    {
        get => _statusMessage;
        protected set
        {
            if (SetProperty(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(HasStatus));
            }
        }
    }

    public bool HasStatus => !string.IsNullOrWhiteSpace(_statusMessage);

    /// <summary>Called by the shell the first time the screen is shown.</summary>
    public virtual Task LoadAsync() => Task.CompletedTask;

    public void ClearMessages()
    {
        ErrorMessage = null;
        StatusMessage = null;
    }

    /// <summary>
    /// Runs an operation with the busy indicator on and a single error handling
    /// policy: business rule violations are shown as they are written, anything
    /// unexpected is logged and reported without leaking a stack trace to the user.
    /// </summary>
    protected Task<bool> RunGuardedAsync(Func<Task> operation, string? successMessage = null) =>
        RunGuardedAsync(
            async () =>
            {
                await operation();
                return true;
            },
            successMessage);

    /// <summary>
    /// Same policy as <see cref="RunGuardedAsync(Func{Task}, string?)"/> for
    /// operations that decide themselves whether they succeeded, such as a dialog
    /// that fails its own validation.
    /// </summary>
    protected async Task<bool> RunGuardedAsync(Func<Task<bool>> operation, string? successMessage = null)
    {
        if (IsBusy)
        {
            return false;
        }

        ClearMessages();
        IsBusy = true;

        try
        {
            var succeeded = await operation();

            if (succeeded && !string.IsNullOrWhiteSpace(successMessage))
            {
                StatusMessage = successMessage;
            }

            return succeeded;
        }
        catch (DomainException exception)
        {
            ErrorMessage = exception.Message;
            Logger.LogInformation(exception, "Operation rejected by a business rule");
            return false;
        }
        catch (Exception exception)
        {
            ErrorMessage = "The operation failed. Check the log file for details.";
            Logger.LogError(exception, "Unhandled error in {ViewModel}", GetType().Name);
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
