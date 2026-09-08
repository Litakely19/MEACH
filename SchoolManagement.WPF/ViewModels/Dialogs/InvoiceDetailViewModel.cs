using Microsoft.Extensions.Logging;
using SchoolManagement.WPF.Mvvm;

namespace SchoolManagement.WPF.ViewModels.Dialogs;

public class InvoiceDetailViewModel : DialogViewModelBase
{
    public InvoiceDetailViewModel(ILogger<InvoiceDetailViewModel> logger) : base(logger)
    {
        Title = "Retired";
        IsEditable = false;
    }

    protected override Task<bool> SaveAsync() => Task.FromResult(true);
}
