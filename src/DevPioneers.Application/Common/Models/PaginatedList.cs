// ============================================
// File: DevPioneers.Application/Common/Models/PaginatedList.cs
// ============================================
using Microsoft.EntityFrameworkCore;

namespace DevPioneers.Application.Common.Models;

/// <summary>
/// Generic paginated list for query results
/// </summary>
public class PaginatedList<T>
{
    public List<T> Items { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages { get; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PaginatedList(List<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
    }

    public static async Task<PaginatedList<T>> CreateAsync(
        IQueryable<T> source,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await source.CountAsync(cancellationToken);
        var items = await source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<T>(items, totalCount, pageNumber, pageSize);
    }

    public ApiMetadata ToMetadata() => new()
    {
        TotalCount = TotalCount,
        PageNumber = PageNumber,
        PageSize = PageSize,
        TotalPages = TotalPages,
        HasPreviousPage = HasPreviousPage,
        HasNextPage = HasNextPage,
        Timestamp = DateTime.UtcNow
    };
}
