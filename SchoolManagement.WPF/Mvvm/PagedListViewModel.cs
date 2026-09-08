using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Common;
using SchoolManagement.WPF.Commands;

namespace SchoolManagement.WPF.Mvvm;

/// <summary>
/// Shared behaviour of every long list in the application: one page at a time, a
/// search box that restarts at page one, and a pager. Screens only supply the
/// query through <see cref="FetchAsync"/>.
/// </summary>
public abstract class PagedListViewModel<TItem> : ViewModelBase
{
    private string? _searchTerm;
    private int _page = 1;
    private int _pageSize = 25;
    private int _totalCount;
    private int _totalPages = 1;
    private TItem? _selectedItem;

    protected PagedListViewModel(ILogger logger) : base(logger)
    {
        SearchCommand = new AsyncRelayCommand(() => GoToPageAsync(1));
        RefreshCommand = new AsyncRelayCommand(() => GoToPageAsync(Page));
        PreviousPageCommand = new AsyncRelayCommand(() => GoToPageAsync(Page - 1), () => HasPreviousPage);
        NextPageCommand = new AsyncRelayCommand(() => GoToPageAsync(Page + 1), () => HasNextPage);
    }

    public ObservableCollection<TItem> Items { get; } = new();

    public ICommand SearchCommand { get; }

    public ICommand RefreshCommand { get; }

    public ICommand PreviousPageCommand { get; }

    public ICommand NextPageCommand { get; }

    public string? SearchTerm
    {
        get => _searchTerm;
        set => SetProperty(ref _searchTerm, value);
    }

    public TItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                OnSelectionChanged();
            }
        }
    }

    public int Page
    {
        get => _page;
        private set => SetProperty(ref _page, value);
    }

    public int PageSize
    {
        get => _pageSize;
        protected set => SetProperty(ref _pageSize, value);
    }

    public int TotalCount
    {
        get => _totalCount;
        private set => SetProperty(ref _totalCount, value);
    }

    public int TotalPages
    {
        get => _totalPages;
        private set => SetProperty(ref _totalPages, value);
    }

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;

    /// <summary>Reads as "1 - 25 of 348" under the grid.</summary>
    public string PageSummary => TotalCount == 0
        ? "No result"
        : $"{((Page - 1) * PageSize) + 1} - {Math.Min(Page * PageSize, TotalCount)} of {TotalCount}";

    public bool IsEmpty => Items.Count == 0;

    /// <summary>
    /// False until the first load completed, so that filling the filter lists does
    /// not trigger one query per default value.
    /// </summary>
    protected bool IsInitialized { get; private set; }

    public override async Task LoadAsync()
    {
        await GoToPageAsync(Page);
        IsInitialized = true;
    }

    /// <summary>
    /// Assigns a filter and requeries from the first page, since a narrower filter
    /// usually leaves fewer pages than the current position.
    /// </summary>
    protected void SetFilter<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);

        if (IsInitialized)
        {
            _ = ReloadFirstPageAsync();
        }
    }

    /// <summary>Sets a filter field without requerying, used while building the default state.</summary>
    protected void SetFilterSilently<T>(ref T field, T value, string propertyName)
    {
        field = value;
        OnPropertyChanged(propertyName);
    }

    protected abstract Task<PagedResult<TItem>> FetchAsync(int page);

    /// <summary>Hook for screens that also need lookup lists or totals on every query.</summary>
    protected virtual Task OnPageLoadedAsync() => Task.CompletedTask;

    /// <summary>Called when the highlighted row changes, to refresh command availability.</summary>
    protected virtual void OnSelectionChanged()
    {
    }

    protected Task ReloadFirstPageAsync() => GoToPageAsync(1);

    protected Task ReloadCurrentPageAsync() => GoToPageAsync(Page);

    /// <summary>
    /// Requeries without the busy guard, for callers already running inside
    /// <see cref="ViewModelBase.RunGuardedAsync(Func{Task}, string?)"/>, where a
    /// nested guarded call would be skipped.
    /// </summary>
    protected Task ReloadCurrentPageCoreAsync() => LoadPageCoreAsync(Page);

    private Task GoToPageAsync(int page) => RunGuardedAsync(() => LoadPageCoreAsync(page));

    private async Task LoadPageCoreAsync(int page)
    {
        var requested = Math.Max(1, page);
        var result = await FetchAsync(requested);

        Items.Clear();
        foreach (var item in result.Items)
        {
            Items.Add(item);
        }

        Page = result.Page;
        PageSize = result.PageSize;
        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages;

        RaisePagingChanged();

        await OnPageLoadedAsync();
    }

    private void RaisePagingChanged()
    {
        OnPropertyChanged(nameof(HasPreviousPage));
        OnPropertyChanged(nameof(HasNextPage));
        OnPropertyChanged(nameof(PageSummary));
        OnPropertyChanged(nameof(IsEmpty));

        (PreviousPageCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (NextPageCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }
}
