namespace MyLibrary.Components.Shared;

public class TableState<T>
{
    public string SearchText { get; set; } = string.Empty;
    public Dictionary<string, string> ColumnFilters { get; } = new();
    public string SortColumn { get; set; } = string.Empty;
    public bool SortAscending { get; set; } = true;
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public IEnumerable<T> Apply(IEnumerable<T> source, Dictionary<string, Func<T, string?>> columnSelectors)
    {
        var query = source;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(item => columnSelectors.Values.Any(selector =>
                (selector(item) ?? string.Empty).Contains(SearchText, StringComparison.OrdinalIgnoreCase)));
        }

        foreach (var (column, filter) in ColumnFilters)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                continue;
            }

            if (columnSelectors.TryGetValue(column, out var selector))
            {
                query = query.Where(item =>
                    (selector(item) ?? string.Empty).Contains(filter, StringComparison.OrdinalIgnoreCase));
            }
        }

        if (!string.IsNullOrWhiteSpace(SortColumn) && columnSelectors.TryGetValue(SortColumn, out var sortSelector))
        {
            query = SortAscending
                ? query.OrderBy(item => sortSelector(item))
                : query.OrderByDescending(item => sortSelector(item));
        }

        return query;
    }

    public IEnumerable<T> ApplyPaging(IEnumerable<T> source)
    {
        return source.Skip((PageIndex - 1) * PageSize).Take(PageSize);
    }

    public int TotalPages(IEnumerable<T> source)
    {
        var count = source.Count();
        return Math.Max(1, (int)Math.Ceiling(count / (double)PageSize));
    }

    public void EnsureValidPage(int totalPages)
    {
        if (PageIndex > totalPages)
        {
            PageIndex = totalPages;
        }
        if (PageIndex < 1)
        {
            PageIndex = 1;
        }
    }
}
