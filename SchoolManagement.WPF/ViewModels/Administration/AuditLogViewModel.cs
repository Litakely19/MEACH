using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs.Audit;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Enums;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;

namespace SchoolManagement.WPF.ViewModels.Administration;

/// <summary>
/// Read only trail of the sensitive actions: sign in, billing, payments,
/// cancellations and account changes.
/// </summary>
public class AuditLogViewModel : PagedListViewModel<AuditLogListItem>
{
    private readonly IScopedExecutor _scopedExecutor;

    private FilterOption<AuditAction>? _selectedAction;
    private FilterOption<int>? _selectedUser;
    private DateTime? _from = DateTime.Today.AddDays(-7);
    private DateTime? _to = DateTime.Today;

    public AuditLogViewModel(ILogger<AuditLogViewModel> logger, IScopedExecutor scopedExecutor)
        : base(logger)
    {
        _scopedExecutor = scopedExecutor;
        PageSize = 50;

        foreach (var option in FilterOption.ForEnum<AuditAction>("All actions"))
        {
            ActionOptions.Add(option);
        }

        _selectedAction = ActionOptions.FirstOrDefault();
    }

    public ObservableCollection<FilterOption<AuditAction>> ActionOptions { get; } = new();

    public ObservableCollection<FilterOption<int>> UserOptions { get; } = new();

    public FilterOption<AuditAction>? SelectedAction
    {
        get => _selectedAction;
        set => SetFilter(ref _selectedAction, value);
    }

    public FilterOption<int>? SelectedUser
    {
        get => _selectedUser;
        set => SetFilter(ref _selectedUser, value);
    }

    public DateTime? From
    {
        get => _from;
        set => SetFilter(ref _from, value);
    }

    public DateTime? To
    {
        get => _to;
        set => SetFilter(ref _to, value);
    }

    public override async Task LoadAsync()
    {
        await LoadUsersAsync();
        await base.LoadAsync();
    }

    protected override Task<PagedResult<AuditLogListItem>> FetchAsync(int page) =>
        _scopedExecutor.RunAsync(provider => provider.GetRequiredService<IAuditService>().ListAsync(
            new AuditLogFilter(
                SearchTerm,
                SelectedUser?.Value,
                SelectedAction?.Value,
                EntityName: null,
                From,
                // The filter is inclusive of the whole closing day.
                To?.Date.AddDays(1).AddTicks(-1),
                page,
                PageSize)));

    private Task LoadUsersAsync() =>
        RunGuardedAsync(async () =>
        {
            var users = await _scopedExecutor.RunAsync(provider =>
                provider.GetRequiredService<IUserService>().ListAsync());

            UserOptions.Clear();
            foreach (var option in FilterOption.ForItems("All users", users, user => user.Id, user => user.FullName))
            {
                UserOptions.Add(option);
            }

            SetFilterSilently(ref _selectedUser, UserOptions.FirstOrDefault(), nameof(SelectedUser));
        });
}
