using Microsoft.Extensions.Logging;
using SchoolManagement.WPF.Mvvm;

namespace SchoolManagement.WPF.ViewModels.Dialogs;

public class InvoiceEditViewModel : DialogViewModelBase
{
    public InvoiceEditViewModel(ILogger<InvoiceEditViewModel> logger) : base(logger)
    {
        Title = "Retired";
        IsEditable = false;
    }

    protected override Task<bool> SaveAsync() => Task.FromResult(true);
}
