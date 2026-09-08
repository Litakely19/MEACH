namespace SchoolManagement.Application.Common;

/// <summary>
/// One page of a longer list, together with the totals the UI needs to render pager controls.
/// </summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public static PagedResult<T> Empty(int pageSize = 25) =>
        new(Array.Empty<T>(), 0, 1, pageSize);

    public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;

    public int FirstItemNumber => TotalCount == 0 ? 0 : ((Page - 1) * PageSize) + 1;

    public int LastItemNumber => Math.Min(Page * PageSize, TotalCount);
}
