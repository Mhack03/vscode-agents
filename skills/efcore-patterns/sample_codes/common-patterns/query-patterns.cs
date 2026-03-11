// efcore-patterns/sample_codes/common-patterns/query-patterns.cs
// All key EF Core query patterns demonstrated in one file

using Microsoft.EntityFrameworkCore;

namespace MyApp.Infrastructure.Repositories;

public class ProductRepository(AppDbContext context)
{
    // ── Read Patterns ──────────────────────────────────────────────────────────

    // Basic: no tracking, projected DTO — most efficient for reads
    public async Task<IReadOnlyList<ProductSummaryDto>> GetSummariesAsync(
        CancellationToken ct = default) =>
        await context.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new ProductSummaryDto(p.Id, p.Name, p.Price,
                p.Category.Name))
            .ToListAsync(ct);

    // Pagination — efficient cursor via Skip/Take
    public async Task<PagedResult<ProductDto>> GetPagedAsync(
        int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = context.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search) ||
                                     p.Description.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductDto(p.Id, p.Name, p.Price))
            .ToListAsync(ct);

        return new PagedResult<ProductDto>(items, total, page, pageSize);
    }

    // Eager loading with split query — avoids Cartesian explosion
    public async Task<Order?> GetOrderWithDetailsAsync(
        int orderId, CancellationToken ct = default) =>
        await context.Orders
            .Include(o => o.Lines).ThenInclude(l => l.Product)
            .Include(o => o.Customer)
            .Include(o => o.Payments)
            .AsSplitQuery()    // 4 separate SQL SELECT statements — no duplication
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

    // Tracking query — use when you'll modify the entity
    public async Task<Product?> GetForUpdateAsync(int id, CancellationToken ct = default) =>
        await context.Products
            .FirstOrDefaultAsync(p => p.Id == id, ct);  // No AsNoTracking

    // ── Compiled Queries — fastest option for frequently-used queries ──────────

    private static readonly Func<AppDbContext, int, Task<Product?>> GetByIdCompiled =
        EF.CompileAsyncQuery((AppDbContext ctx, int id) =>
            ctx.Products.AsNoTracking()
               .FirstOrDefault(p => p.Id == id && p.IsActive));

    private static readonly Func<AppDbContext, int, decimal, IAsyncEnumerable<ProductDto>>
        GetByCategoryAndMaxPrice =
            EF.CompileAsyncQuery((AppDbContext ctx, int categoryId, decimal maxPrice) =>
                ctx.Products
                   .AsNoTracking()
                   .Where(p => p.CategoryId == categoryId && p.Price <= maxPrice)
                   .OrderBy(p => p.Price)
                   .Select(p => new ProductDto(p.Id, p.Name, p.Price)));

    public Task<Product?> GetByIdAsync(int id) =>
        GetByIdCompiled(context, id);  // No CancellationToken in compiled query

    // ── Bulk Operations (EF Core 7+) — no entity loading ──────────────────────

    // Bulk update — single UPDATE SQL, never loads entities
    public Task<int> MarkCategoryActiveAsync(int categoryId, CancellationToken ct = default) =>
        context.Products
            .Where(p => p.CategoryId == categoryId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.IsActive, true)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
            ct);

    // Bulk delete — single DELETE SQL
    public Task<int> DeleteDiscontinuedAsync(CancellationToken ct = default) =>
        context.Products
            .Where(p => p.Status == ProductStatus.Discontinued && !p.HasPendingOrders)
            .ExecuteDeleteAsync(ct);

    // ── Raw SQL (escape hatch) ─────────────────────────────────────────────────

    // Safe parameterized raw SQL query
    public async Task<IReadOnlyList<Product>> SearchByNameRawAsync(
        string searchTerm, CancellationToken ct = default) =>
        await context.Products
            .FromSql($"SELECT * FROM Products WHERE CONTAINS(Name, {searchTerm})")
            .AsNoTracking()
            .ToListAsync(ct);

    // Execute raw non-query SQL
    public Task RebuildSearchIndexAsync(CancellationToken ct = default) =>
        context.Database.ExecuteSqlRawAsync(
            "EXEC dbo.RebuildProductSearchIndex", ct);

    // ── Streaming — for large result sets (avoids loading all into memory) ─────

    public async IAsyncEnumerable<ProductDto> StreamAllAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var product in context.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Id)
            .Select(p => new ProductDto(p.Id, p.Name, p.Price))
            .AsAsyncEnumerable()
            .WithCancellation(ct))
        {
            yield return product;
        }
    }

    // ── Write Operations ───────────────────────────────────────────────────────

    public async Task<Product> AddAsync(Product product, CancellationToken ct = default)
    {
        context.Products.Add(product);  // Starts tracking in Added state
        await context.SaveChangesAsync(ct);
        return product;
    }

    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        // entity already tracked from GetForUpdateAsync — just save
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Product product, CancellationToken ct = default)
    {
        context.Products.Remove(product);  // SoftDeleteInterceptor converts to Update
        await context.SaveChangesAsync(ct);
    }
}
