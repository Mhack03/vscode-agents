# MassTransit, Outbox Pattern & Sagas

---

## Outbox Pattern — Why It's Essential

```
Problem:
  DbContext.SaveChangesAsync()  ✅ Order saved
  IPublishEndpoint.Publish()    ❌ Broker down → message lost

Outbox Solution:
  SaveChangesAsync()  →  saves Order + OutboxMessage in ONE transaction ✅
  Background worker   →  reads OutboxMessage, publishes to broker, marks sent ✅
  Result: at-least-once delivery, no lost messages
```

---

## MassTransit Outbox Setup (EF Core)

```csharp
// Program.cs
builder.Services.AddMassTransit(x =>
{
    // Outbox — messages saved to DB, delivered by background worker
    x.AddEntityFrameworkOutbox<AppDbContext>(o =>
    {
        o.UseSqlServer();
        o.UseBusOutbox();                      // Enable bus-level outbox
        o.QueryDelay         = TimeSpan.FromSeconds(5);
        o.QueryTimeout       = TimeSpan.FromSeconds(30);
    });

    x.AddConsumers(typeof(Program).Assembly);

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMq")!);
        cfg.UseMessageRetry(r => r.Exponential(5,
            TimeSpan.FromMilliseconds(200),
            TimeSpan.FromSeconds(60),
            TimeSpan.FromMilliseconds(200)));
        cfg.ConfigureEndpoints(ctx);
    });
});
```

```csharp
// Add outbox tables to DbContext
public class AppDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
```

---

## Saga State Machine (Order Workflow)

```csharp
// State machine — orchestrates the order lifecycle
public class OrderStateMachine : MassTransitStateMachine<OrderSagaState>
{
    public State Submitted  { get; set; } = null!;
    public State Paid       { get; set; } = null!;
    public State Shipped    { get; set; } = null!;
    public State Cancelled  { get; set; } = null!;

    public Event<OrderSubmitted>  OrderSubmitted  { get; set; } = null!;
    public Event<PaymentReceived> PaymentReceived { get; set; } = null!;
    public Event<OrderShipped>    OrderShipped    { get; set; } = null!;
    public Event<OrderCancelled>  OrderCancelled  { get; set; } = null!;

    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => OrderSubmitted,  e => e.CorrelateById(m => m.Message.OrderId));
        Event(() => PaymentReceived, e => e.CorrelateById(m => m.Message.OrderId));
        Event(() => OrderShipped,    e => e.CorrelateById(m => m.Message.OrderId));
        Event(() => OrderCancelled,  e => e.CorrelateById(m => m.Message.OrderId));

        Initially(
            When(OrderSubmitted)
                .Then(ctx =>
                {
                    ctx.Saga.OrderId    = ctx.Message.OrderId;
                    ctx.Saga.CustomerId = ctx.Message.CustomerId;
                    ctx.Saga.SubmittedAt = DateTime.UtcNow;
                })
                .Publish(ctx => new SendOrderConfirmationEmail(ctx.Saga.OrderId))
                .TransitionTo(Submitted));

        During(Submitted,
            When(PaymentReceived)
                .Then(ctx => ctx.Saga.PaidAt = DateTime.UtcNow)
                .Publish(ctx => new TriggerFulfillment(ctx.Saga.OrderId))
                .TransitionTo(Paid),
            When(OrderCancelled)
                .TransitionTo(Cancelled)
                .Finalize());

        During(Paid,
            When(OrderShipped)
                .Then(ctx => ctx.Saga.ShippedAt = DateTime.UtcNow)
                .TransitionTo(Shipped)
                .Finalize());

        SetCompletedWhenFinalized();
    }
}

public class OrderSagaState : SagaStateMachineInstance
{
    public Guid     CorrelationId { get; set; }
    public string   CurrentState  { get; set; } = string.Empty;
    public Guid     OrderId       { get; set; }
    public string   CustomerId    { get; set; } = string.Empty;
    public DateTime SubmittedAt   { get; set; }
    public DateTime? PaidAt       { get; set; }
    public DateTime? ShippedAt    { get; set; }
}
```

---

## Request/Response (RPC over Message Bus)

```csharp
// Consumer that handles a request and sends a response
public class GetProductConsumer : IConsumer<GetProductRequest>
{
    public async Task Consume(ConsumeContext<GetProductRequest> ctx)
    {
        var product = await productService.GetByIdAsync(ctx.Message.ProductId);
        await ctx.RespondAsync(product is not null
            ? new GetProductResponse(product)
            : new GetProductResponse(null) { NotFound = true });
    }
}

// Caller
var client = context.CreateRequestClient<GetProductRequest>();
var response = await client.GetResponse<GetProductResponse>(
    new GetProductRequest(productId));
```

---

## Testing with InMemory Transport

```csharp
// Integration test setup — no real broker needed
builder.ConfigureTestServices(services =>
{
    services.AddMassTransitTestHarness(x =>
    {
        x.AddConsumer<OrderCreatedConsumer>();
    });
});

// In test
var harness = serviceProvider.GetRequiredService<ITestHarness>();
await harness.Start();

await publishEndpoint.Publish(new OrderCreated(orderId, customerId, 99.99m, DateTime.UtcNow));

// Assert consumer received and processed the message
Assert.True(await harness.Consumed.Any<OrderCreated>());
var consumerHarness = harness.GetConsumerHarness<OrderCreatedConsumer>();
Assert.True(await consumerHarness.Consumed.Any<OrderCreated>());
```

---

## Learn More

| Topic | Query |
|-------|-------|
| MassTransit outbox | `microsoft_docs_search(query="MassTransit EntityFramework outbox transactional messaging")` |
| Sagas | `microsoft_docs_search(query="MassTransit saga state machine stateful workflow")` |
| Test harness | `microsoft_docs_search(query="MassTransit test harness ITestHarness unit testing consumers")` |
