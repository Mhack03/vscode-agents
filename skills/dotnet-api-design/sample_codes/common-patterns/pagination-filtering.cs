// pagination-filtering.cs — Keyset pagination + dynamic filtering
// Demonstrates: offset vs keyset, ProductFilter, ApplyFilter

using System.Linq.Expressions;

// ─── Offset Pagination (simple, max ~10k pages) ───────────────────────────────
public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
}

public static class QueryableExtensions
{
    public static async Task<PagedResponse<T>> ToPagedAsync<T>(
        this IQueryable<T> query,
        int page, int pageSize,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResponse<T>(items, page, pageSize, total);
    }
}

// ─── Keyset / Cursor Pagination (performant for deep pages) ──────────────────
public record CursorPage<T>(
    IReadOnlyList<T> Items,
    string? NextCursor,
    bool HasMore);

public static class KeysetExtensions
{
    public static async Task<CursorPage<Product>> ToKeysetPageAsync(
        this IQueryable<Product> query,
        string? cursor, int pageSize,
        CancellationToken ct = default)
    {
        if (cursor is not null)
        {
            var (lastId, lastName) = DecodeCursor(cursor);
            query = query.Where(p =>
                string.Compare(p.Name, lastName, StringComparison.Ordinal) > 0
                || (p.Name == lastName && p.Id > lastId));
        }

        // Fetch one extra to determine if there are more pages
        var items = await query
            .OrderBy(p => p.Name).ThenBy(p => p.Id)
            .Take(pageSize + 1)
            .ToListAsync(ct);

        var hasMore = items.Count > pageSize;
        if (hasMore) items.RemoveAt(items.Count - 1);

        var nextCursor = hasMore
            ? EncodeCursor(items[^1].Id, items[^1].Name)
            : null;

        return new CursorPage<Product>(items, nextCursor, hasMore);
    }

    private static string EncodeCursor(int id, string name) =>
        Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"{id}|{name}"));

    private static (int Id, string Name) DecodeCursor(string cursor)
    {
        var decoded = System.Text.Encoding.UTF8.GetString(
            Convert.FromBase64String(cursor));
        var parts = decoded.Split('|', 2);
        return (int.Parse(parts[0]), parts[1]);
    }
}

// ─── Filtering ────────────────────────────────────────────────────────────────
public record ProductFilter(
    [FromQuery] string? Name = null,
    [FromQuery] string? Category = null,
    [FromQuery] decimal? MinPrice = null,
    [FromQuery] decimal? MaxPrice = null,
    [FromQuery] bool? InStock = null,
    [FromQuery] string Sort = "name_asc",
    [FromQuery] int Page = 1,
    [FromQuery] int PageSize = 25);

public static class ProductQueryExtensions
{
    public static IQueryable<Product> ApplyFilter(
        this IQueryable<Product> query,
        ProductFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Name))
            query = query.Where(p =>
                p.Name.Contains(filter.Name, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(filter.Category))
            query = query.Where(p => p.Category.Name == filter.Category);

        if (filter.MinPrice.HasValue)
            query = query.Where(p => p.Price >= filter.MinPrice.Value);

        if (filter.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= filter.MaxPrice.Value);

        if (filter.InStock.HasValue)
            query = query.Where(p => filter.InStock.Value
                ? p.StockQuantity > 0
                : p.StockQuantity == 0);

        query = filter.Sort switch
        {
            "name_asc" => query.OrderBy(p => p.Name),
            "name_desc" => query.OrderByDescending(p => p.Name),
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            _ => query.OrderBy(p => p.Name),
        };

        return query;
    }
}

// ─── Endpoint Usage ───────────────────────────────────────────────────────────
/*
app.MapGet("/api/products", async (
    [AsParameters] ProductFilter filter,
    AppDbContext db,
    CancellationToken ct) =>
{
    var query  = db.Products.Include(p => p.Category).ApplyFilter(filter);
    var result = await query.ToPagedAsync(filter.Page, filter.PageSize, ct);
    return TypedResults.Ok(result);
});
*/
