using Microsoft.Extensions.Logging;
using SchoolManagement.WPF.Mvvm;

namespace SchoolManagement.WPF.ViewModels.Billing;

public class InvoiceListViewModel : ViewModelBase
{
    public InvoiceListViewModel(ILogger<InvoiceListViewModel> logger) : base(logger)
    {
    }
}
