# Design Patterns

Common patterns for creating objects, structuring code, and handling behavior changes.

## Creational Patterns

### Factory Pattern

_Creates objects without specifying the exact class to create._

```typescript
interface Product {
	id: string;
	name: string;
	getDescription(): string;
}

class Book implements Product {
	constructor(
		public id: string,
		public name: string,
		public author: string,
		public pages: number
	) {}
	getDescription(): string {
		return `${this.name} by ${this.author}`;
	}
}

class ProductFactory {
	static createProduct(type: string, data: any): Product {
		switch (type) {
			case "book":
				return new Book(data.id, data.name, data.author, data.pages);
			case "electronics":
				return new Electronics(data.id, data.name, data.warranty);
			default:
				throw new Error("Unknown product type");
		}
	}
}

// Usage - cleaner than scattered `new` calls
const products = items.map((item) =>
	ProductFactory.createProduct(item.type, item)
);
```

**When to use:** Complex object creation logic, multiple implementations of same interface, centralized creation rules.

### Builder Pattern

_Constructs complex objects step by step._

```typescript
class HttpRequest {
	constructor(
		public url: string,
		public method: string = "GET",
		public headers: Record<string, string> = {},
		public body?: any,
		public timeout: number = 30000
	) {}
}

class HttpRequestBuilder {
	private url: string = "";
	private method: string = "GET";
	private headers: Record<string, string> = {};
	private body?: any;
	private timeout: number = 30000;

	setUrl(url: string): HttpRequestBuilder {
		this.url = url;
		return this;
	}

	setMethod(method: string): HttpRequestBuilder {
		this.method = method;
		return this;
	}

	setHeader(key: string, value: string): HttpRequestBuilder {
		this.headers[key] = value;
		return this;
	}

	setBody(body: any): HttpRequestBuilder {
		this.body = body;
		return this;
	}

	setTimeout(timeout: number): HttpRequestBuilder {
		this.timeout = timeout;
		return this;
	}

	build(): HttpRequest {
		if (!this.url) throw new Error("URL is required");
		return new HttpRequest(
			this.url,
			this.method,
			this.headers,
			this.body,
			this.timeout
		);
	}
}

// Usage - readable and flexible
const request = new HttpRequestBuilder()
	.setUrl("https://api.example.com/users")
	.setMethod("POST")
	.setHeader("Content-Type", "application/json")
	.setBody({ name: "John" })
	.setTimeout(5000)
	.build();
```

**When to use:** Complex objects with many optional parameters, step-by-step construction logic, immutable objects.

---

## Structural Patterns

### Adapter Pattern

_Allows incompatible interfaces to work together._

```typescript
interface PaymentGateway {
	processPayment(details: PaymentDetails): Promise<PaymentResult>;
}

// Legacy system with incompatible interface
class LegacyPaymentGateway {
	makePayment(accountNumber: string, amount: number): boolean {
		// Old implementation
		return true;
	}
}

// Adapter bridges the gap
class LegacyPaymentAdapter implements PaymentGateway {
	constructor(private legacyGateway: LegacyPaymentGateway) {}

	async processPayment(details: PaymentDetails): Promise<PaymentResult> {
		const success = this.legacyGateway.makePayment(
			details.source,
			details.amount
		);
		return {
			success,
			transactionId: success ? "txn_" + Date.now() : "",
		};
	}
}

// Easy to switch implementations
const legacyService = new CheckoutService(
	new LegacyPaymentAdapter(new LegacyPaymentGateway())
);
const modernService = new CheckoutService(new StripePaymentGateway());
```

**When to use:** Integrating third-party libraries, legacy code integration, API incompatibilities.

### Decorator Pattern

_Adds new functionality to objects dynamically without inheritance explosion._

```typescript
interface Beverage {
	cost(): number;
	description(): string;
}

class Coffee implements Beverage {
	cost(): number {
		return 2.0;
	}
	description(): string {
		return "Simple coffee";
	}
}

abstract class BeverageDecorator implements Beverage {
	constructor(protected beverage: Beverage) {}
	abstract cost(): number;
	abstract description(): string;
}

class MilkDecorator extends BeverageDecorator {
	cost(): number {
		return this.beverage.cost() + 0.5;
	}
	description(): string {
		return this.beverage.description() + ", milk";
	}
}

class SugarDecorator extends BeverageDecorator {
	cost(): number {
		return this.beverage.cost() + 0.25;
	}
	description(): string {
		return this.beverage.description() + ", sugar";
	}
}

// Compose dynamically without creating new classes for every combination
let beverage: Beverage = new Coffee();
beverage = new MilkDecorator(beverage);
beverage = new SugarDecorator(beverage);
console.log(`${beverage.description()} = $${beverage.cost()}`);
// "Simple coffee, milk, sugar = $2.75"
```

**When to use:** Adding features to objects at runtime, avoiding inheritance explosion, flexible feature composition.

### Facade Pattern

_Provides a simplified interface to a complex subsystem._

```typescript
class OrderFacade {
	private inventory = new InventorySystem();
	private payment = new PaymentSystem();
	private shipping = new ShippingSystem();
	private notification = new NotificationService();

	async createOrder(orderDetails: OrderDetails): Promise<OrderResult> {
		// Check inventory
		const available = await this.inventory.checkAvailability(
			orderDetails.items
		);
		if (!available) throw new Error("Items not in stock");

		// Reserve items
		await this.inventory.reserveItems(orderDetails.items);

		try {
			// Process payment
			const paymentResult = await this.payment.process(orderDetails.payment);
			if (!paymentResult.success) {
				await this.inventory.releaseItems(orderDetails.items);
				throw new Error("Payment failed");
			}

			// Create shipment
			const shipment = await this.shipping.createShipment(
				orderDetails.address,
				orderDetails.items
			);

			// Send confirmation
			await this.notification.sendConfirmation(
				orderDetails.email,
				shipment.trackingNumber
			);

			return { success: true, orderId: "ord_" + Date.now() };
		} catch (error) {
			await this.inventory.releaseItems(orderDetails.items);
			throw error;
		}
	}
}

// Controller is now much simpler
class OrderController {
	constructor(private orderFacade: OrderFacade) {}

	async createOrder(req: Request, res: Response) {
		try {
			const result = await this.orderFacade.createOrder(req.body);
			res.json(result);
		} catch (error) {
			res.status(400).json({ error: error.message });
		}
	}
}
```

**When to use:** Complex subsystems, simplifying client interactions, decoupling client from implementation details.

---

## Behavioral Patterns

### Strategy Pattern

_Defines a family of algorithms and makes them interchangeable._

```typescript
interface PricingStrategy {
	calculatePrice(basePrice: number): number;
}

class RegularPricing implements PricingStrategy {
	calculatePrice(basePrice: number): number {
		return basePrice;
	}
}

class PremiumPricing implements PricingStrategy {
	calculatePrice(basePrice: number): number {
		return basePrice * 0.8; // 20% discount
	}
}

class VIPPricing implements PricingStrategy {
	calculatePrice(basePrice: number): number {
		return basePrice * 0.7; // 30% discount
	}
}

class PriceCalculator {
	constructor(private strategy: PricingStrategy) {}

	setStrategy(strategy: PricingStrategy): void {
		this.strategy = strategy;
	}

	calculatePrice(basePrice: number): number {
		return this.strategy.calculatePrice(basePrice);
	}
}

// Usage - easily switch algorithms
const calculator = new PriceCalculator(new RegularPricing());
console.log(calculator.calculatePrice(100)); // 100

calculator.setStrategy(new VIPPricing());
console.log(calculator.calculatePrice(100)); // 70
```

**When to use:** Multiple algorithms for same task, runtime algorithm selection, eliminating conditional logic.

### Observer Pattern

_Defines a subscription mechanism to notify multiple objects about events._

```typescript
interface Observer {
	update(data: any): void;
}

class StockTracker {
	private observers: Observer[] = [];
	private price: number = 0;

	attach(observer: Observer): void {
		this.observers.push(observer);
	}

	detach(observer: Observer): void {
		this.observers = this.observers.filter((o) => o !== observer);
	}

	notify(): void {
		this.observers.forEach((observer) =>
			observer.update({ price: this.price })
		);
	}

	setPrice(price: number): void {
		this.price = price;
		this.notify();
	}
}

class EmailAlertObserver implements Observer {
	constructor(
		private email: string,
		private threshold: number
	) {}

	update(data: any): void {
		if (data.price > this.threshold) {
			console.log(`Email to ${this.email}: Price exceeded $${this.threshold}`);
		}
	}
}

const stock = new StockTracker();
stock.attach(new EmailAlertObserver("investor@example.com", 150));
stock.setPrice(155); // Triggers email alert
```

**When to use:** Event-driven systems, decoupling event producers from consumers, publish-subscribe patterns.

### Command Pattern

_Encapsulates a request as an object, allowing parameterization and queuing._

```typescript
interface Command {
	execute(): void;
	undo(): void;
}

class TextDocument {
	private content: string = "";

	insert(text: string, position: number): void {
		this.content =
			this.content.slice(0, position) + text + this.content.slice(position);
	}

	getContent(): string {
		return this.content;
	}
}

class InsertTextCommand implements Command {
	private previousContent: string;

	constructor(
		private document: TextDocument,
		private text: string,
		private position: number
	) {
		this.previousContent = document.getContent();
	}

	execute(): void {
		this.document.insert(this.text, this.position);
	}

	undo(): void {
		this.document = { ...this.document };
	}
}

class CommandHistory {
	private history: Command[] = [];
	private currentIndex: number = -1;

	execute(command: Command): void {
		command.execute();
		this.history = this.history.slice(0, this.currentIndex + 1);
		this.history.push(command);
		this.currentIndex++;
	}

	undo(): void {
		if (this.currentIndex >= 0) {
			this.history[this.currentIndex].undo();
			this.currentIndex--;
		}
	}

	redo(): void {
		if (this.currentIndex < this.history.length - 1) {
			this.currentIndex++;
			this.history[this.currentIndex].execute();
		}
	}
}
```

**When to use:** Undo/redo functionality, command queuing, macro recording, operation history.
