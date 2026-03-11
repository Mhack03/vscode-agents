// ============================================================
// Repository Pattern with Entity Framework Core
// ============================================================

using Microsoft.EntityFrameworkCore;

// ── Generic Read-Only Repository ─────────────────────────────

public interface IReadRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}

// ── Write Repository (Unit of Work embedded) ─────────────────

public interface IRepository<T> : IReadRepository<T> where T : class
{
    void Add(T entity);
    void AddRange(IEnumerable<T> entities);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
}

// ── Unit of Work ─────────────────────────────────────────────

public interface IUnitOfWork : IAsyncDisposable
{
    IRepository<Order> Orders { get; }
    IRepository<Customer> Customers { get; }

    Task<int> CommitAsync(CancellationToken ct = default);
}

// ── Base EF Core Implementation ───────────────────────────────

public abstract class EfRepositoryBase<T>(AppDbContext db) : IRepository<T>
    where T : class
{
    protected DbSet<T> Set => db.Set<T>();

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await Set.FindAsync([id], ct);

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default) =>
        await Set.AsNoTracking().ToListAsync(ct);

    public virtual async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) =>
        await Set.AnyAsync(e => EF.Property<Guid>(e, "Id") == id, ct);

    public void Add(T entity) => Set.Add(entity);
    public void AddRange(IEnumerable<T> entities) => Set.AddRange(entities);
    public void Remove(T entity) => Set.Remove(entity);
    public void RemoveRange(IEnumerable<T> entities) => Set.RemoveRange(entities);
}

// ── Concrete Order Repository with Custom Queries ─────────────

public interface IOrderRepository : IRepository<Order>
{
    Task<IReadOnlyList<Order>> GetByCustomerIdAsync(
        Guid customerId, CancellationToken ct = default);

    Task<Order?> GetWithLinesAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Order>> GetPendingAsync(CancellationToken ct = default);
}

public class OrderRepository(AppDbContext db)
    : EfRepositoryBase<Order>(db), IOrderRepository
{
    // Include navigation properties when needed
    public override async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    // Query with AsNoTracking for read-only projections
    public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(
        Guid customerId, CancellationToken ct = default) =>
        await db.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

    // Eager loading
    public Task<Order?> GetWithLinesAsync(Guid id, CancellationToken ct = default) =>
        db.Orders
            .Include(o => o.Lines)
                .ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    // Filtered query
    public async Task<IReadOnlyList<Order>> GetPendingAsync(CancellationToken ct = default) =>
        await db.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Pending)
            .ToListAsync(ct);
}

// ── Unit of Work Implementation ───────────────────────────────

public class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    // Lazy initialisation — only create the repo when first accessed
    private IRepository<Order>? _orders;
    private IRepository<Customer>? _customers;

    public IRepository<Order> Orders => _orders ??= new OrderRepository(db);
    public IRepository<Customer> Customers => _customers ??= new EfRepository<Customer>(db);

    public Task<int> CommitAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);

    public ValueTask DisposeAsync() => db.DisposeAsync();
}

// Concrete generic for types that don't need custom queries
public class EfRepository<T>(AppDbContext db) : EfRepositoryBase<T>(db) where T : class;

// ── Service using Unit of Work ────────────────────────────────

public class OrderService(IUnitOfWork uow, ILogger<OrderService> logger)
{
    public async Task<Order> CreateOrderAsync(
        CreateOrderRequest req, CancellationToken ct = default)
    {
        var customer = await uow.Customers.GetByIdAsync(req.CustomerId, ct)
            ?? throw new KeyNotFoundException($"Customer {req.CustomerId} not found.");

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Lines = req.Lines.Select(l => new OrderLine
            {
                ProductId = l.ProductId,
                Quantity = l.Quantity
            }).ToList()
        };

        uow.Orders.Add(order);
        await uow.CommitAsync(ct);

        logger.LogInformation("Created order {OrderId} for customer {CustomerId}",
            order.Id, customer.Id);

        return order;
    }
}

// ── Domain Models (simplified) ────────────────────────────────

public class Order
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; init; }
    public List<OrderLine> Lines { get; init; } = [];
}

public class OrderLine
{
    public Guid ProductId { get; init; }
    public int Quantity { get; set; }
    public Product? Product { get; init; }
}

public class Customer
{
    public Guid Id { get; init; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class Product
{
    public Guid Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public enum OrderStatus { Pending, Processing, Shipped, Completed, Cancelled }

// ── DTOs ──────────────────────────────────────────────────────
public record CreateOrderRequest(
    Guid CustomerId,
    List<OrderLineRequest> Lines);

public record OrderLineRequest(Guid ProductId, int Quantity);

// ── AppDbContext (abbreviated) ─────────────────────────────────
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
}
