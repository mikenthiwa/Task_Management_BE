namespace Application.Common.Models;

public class PaginatedList<T>(IReadOnlyCollection<T> items, int count, int pageNumber, int pageSize)
{
    public int PageNumber { get; } = pageNumber;
    public int PageSize { get; } = pageSize;
    public int Count { get; } = count;
    public int TotalPage { get; } = (int)Math.Ceiling(count / (double)pageSize);
    public IReadOnlyCollection<T> Items { get; set; } = items;

    public bool HasPreviousPage = pageNumber > 1;
    public bool HasNextPage = pageNumber < count;

    public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageNumber, int pageSize)
    {
        var count = await source.CountAsync();
        var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PaginatedList<T>(items, count, pageNumber, pageSize);
    }
}
