using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.DTOs.PaymentTypes;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Enums;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;

namespace SchoolManagement.WPF.ViewModels.Dialogs;

/// <summary>
/// Defines a billable item: its label, its default amount and how often it is due.
/// The frequency drives which billing screen can generate it.
/// </summary>
public class PaymentTypeEditViewModel : DialogViewModelBase
{
    private readonly IScopedExecutor _scopedExecutor;

    private int? _paymentTypeId;
    private string _name = string.Empty;
    private string? _description;
    private decimal _defaultAmount;
    private PaymentFrequency _frequency = PaymentFrequency.OneTime;
    private bool _isActive = true;

    public PaymentTypeEditViewModel(ILogger<PaymentTypeEditViewModel> logger, IScopedExecutor scopedExecutor)
        : base(logger)
    {
        _scopedExecutor = scopedExecutor;
        DialogWidth = 480;

        foreach (var frequency in Enum.GetValues<PaymentFrequency>())
        {
            Frequencies.Add(frequency);
        }
    }

    public ObservableCollection<PaymentFrequency> Frequencies { get; } = new();

    public bool IsNew => _paymentTypeId is null;

    public bool CanChangeActivity => !IsNew;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string? Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public decimal DefaultAmount
    {
        get => _defaultAmount;
        set => SetProperty(ref _defaultAmount, value);
    }

    public PaymentFrequency Frequency
    {
        get => _frequency;
        set => SetProperty(ref _frequency, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public void InitializeForCreate()
    {
        _paymentTypeId = null;
        Title = "New payment type";
        ConfirmButtonText = "Create";
        RaiseModeChanged();
    }

    public void InitializeForEdit(PaymentTypeListItem item)
    {
        _paymentTypeId = item.Id;
        Name = item.Name;
        Description = item.Description;
        DefaultAmount = item.DefaultAmount;
        Frequency = item.Frequency;
        IsActive = item.IsActive;

        Title = $"Payment type {item.Name}";
        ConfirmButtonText = "Save";
        RaiseModeChanged();
    }

    protected override async Task<bool> SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "The name is required.";
            return false;
        }

        if (DefaultAmount < 0)
        {
            ErrorMessage = "The default amount cannot be negative.";
            return false;
        }

        var description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();

        await _scopedExecutor.RunAsync(async provider =>
        {
            var service = provider.GetRequiredService<IPaymentTypeService>();

            if (IsNew)
            {
                await service.CreateAsync(new CreatePaymentTypeRequest(
                    Name.Trim(),
                    description,
                    DefaultAmount,
                    Frequency));
            }
            else
            {
                await service.UpdateAsync(new UpdatePaymentTypeRequest(
                    _paymentTypeId!.Value,
                    Name.Trim(),
                    description,
                    DefaultAmount,
                    Frequency,
                    IsActive));
            }
        });

        return true;
    }

    private void RaiseModeChanged()
    {
        OnPropertyChanged(nameof(IsNew));
        OnPropertyChanged(nameof(CanChangeActivity));
    }
}
