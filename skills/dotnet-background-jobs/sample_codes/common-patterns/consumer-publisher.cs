// consumer-publisher.cs — MassTransit consumer + outbox publisher
// Demonstrates: IConsumer<T>, ConsumerDefinition, outbox publish

using MassTransit;
using Microsoft.EntityFrameworkCore;

// ─── Message Contracts ────────────────────────────────────────────────────────
public record OrderCreated(
    Guid OrderId,
    string CustomerId,
    decimal TotalAmount,
    DateTime CreatedAt);

public record OrderShipped(
    Guid OrderId,
    string TrackingNumber,
    DateTime ShippedAt);

public record SendOrderConfirmation(
    Guid OrderId,
    string Email);

// ─── Publisher (via outbox) ───────────────────────────────────────────────────
// Publish is atomic with SaveChangesAsync — guaranteed delivery
public class OrderService(
    AppDbContext db,
    IPublishEndpoint publisher,
    ILogger<OrderService> logger)
{
    public async Task<OrderDto> CreateAsync(CreateOrderRequest req, CancellationToken ct)
    {
        var order = new Order
        {
            CustomerId = req.CustomerId,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
        };

        db.Orders.Add(order);

        // Publish is saved to outbox table inside THIS transaction
        await publisher.Publish(new OrderCreated(
            order.Id, req.CustomerId, req.TotalAmount, order.CreatedAt), ct);

        // Both order + outbox message commit atomically
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Order {OrderId} created and event published via outbox",
            order.Id);

        return order.ToDto();
    }
}

// ─── Consumer — handles OrderCreated ─────────────────────────────────────────
public class OrderCreatedConsumer(
    IEmailService email,
    ILogger<OrderCreatedConsumer> logger)
    : IConsumer<OrderCreated>
{
    public async Task Consume(ConsumeContext<OrderCreated> ctx)
    {
        var msg = ctx.Message;
        logger.LogInformation(
            "Processing OrderCreated for order {OrderId}, customer {CustomerId}",
            msg.OrderId, msg.CustomerId);

        await email.SendOrderConfirmationAsync(
            msg.CustomerId, msg.OrderId, msg.TotalAmount,
            ctx.CancellationToken);

        // Optionally publish a chained event
        await ctx.Publish(new SendOrderConfirmation(
            msg.OrderId, msg.CustomerId));
    }
}

// ─── Consumer Definition (retry + concurrency) ────────────────────────────────
public class OrderCreatedConsumerDefinition : ConsumerDefinition<OrderCreatedConsumer>
{
    public OrderCreatedConsumerDefinition()
    {
        // Max concurrent messages per instance
        ConcurrentMessageLimit = 10;

        // Dead-letter queue suffix
        EndpointName = "order-created";
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpoint,
        IConsumerConfigurator<OrderCreatedConsumer> consumer,
        IRegistrationContext context)
    {
        endpoint.UseMessageRetry(r => r
            .Incremental(3,
                initialInterval: TimeSpan.FromSeconds(5),
                intervalIncrement: TimeSpan.FromSeconds(5)));

        endpoint.UseInMemoryOutbox(context);  // Prevent duplicate side effects on retry
    }
}

// ─── Consumer — handles OrderShipped ─────────────────────────────────────────
public class OrderShippedConsumer(
    AppDbContext db,
    ILogger<OrderShippedConsumer> logger)
    : IConsumer<OrderShipped>
{
    public async Task Consume(ConsumeContext<OrderShipped> ctx)
    {
        var msg = ctx.Message;
        var order = await db.Orders.FirstOrDefaultAsync(
            o => o.Id == msg.OrderId, ctx.CancellationToken);

        if (order is null)
        {
            logger.LogWarning("Order {OrderId} not found for shipment update", msg.OrderId);
            return;
        }

        order.Status = "Shipped";
        order.TrackingNumber = msg.TrackingNumber;
        order.ShippedAt = msg.ShippedAt;
        await db.SaveChangesAsync(ctx.CancellationToken);

        logger.LogInformation("Order {OrderId} marked as shipped ({Tracking})",
            msg.OrderId, msg.TrackingNumber);
    }
}
