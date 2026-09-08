using System.Windows.Input;

namespace SchoolManagement.WPF.Commands;

/// <summary>
/// Command backed by an asynchronous handler. The command reports itself as
/// unavailable while the handler runs, which stops a double click from starting a
/// second payment registration or export.
/// </summary>
public class AsyncRelayCommand : ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Func<object?, bool>? _canExecute;
    private bool _isRunning;

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute is null ? null : _ => canExecute())
    {
    }

    public AsyncRelayCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (_isRunning == value)
            {
                return;
            }

            _isRunning = value;
            RaiseCanExecuteChanged();
        }
    }

    public bool CanExecute(object? parameter) => !IsRunning && (_canExecute?.Invoke(parameter) ?? true);

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        IsRunning = true;
        try
        {
            await _execute(parameter);
        }
        finally
        {
            IsRunning = false;
        }
    }

    public void RaiseCanExecuteChanged() =>
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
