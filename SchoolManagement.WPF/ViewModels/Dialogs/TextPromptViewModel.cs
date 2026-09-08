using Microsoft.Extensions.Logging.Abstractions;
using SchoolManagement.WPF.Mvvm;

namespace SchoolManagement.WPF.ViewModels.Dialogs;

/// <summary>
/// Single line prompt used where a free text reason is required, such as
/// cancelling a payment or an invoice.
/// </summary>
public class TextPromptViewModel : DialogViewModelBase
{
    private string _value;

    public TextPromptViewModel(string message, string title, string? initialValue)
        : base(NullLogger.Instance)
    {
        Message = message;
        Title = title;
        _value = initialValue ?? string.Empty;
        ConfirmButtonText = "OK";
        DialogWidth = 480;
    }

    public string Message { get; }

    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    protected override Task<bool> SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Value))
        {
            ErrorMessage = "This field is required.";
            return Task.FromResult(false);
        }

        Value = Value.Trim();
        return Task.FromResult(true);
    }
}
