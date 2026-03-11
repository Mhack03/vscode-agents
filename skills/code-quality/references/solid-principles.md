# SOLID Principles

Master the five SOLID principles: Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, and Dependency Inversion. These form the foundation of maintainable, extensible code.

## Single Responsibility Principle (SRP)

_A class/module should have one, and only one, reason to change._

**❌ Bad - Multiple responsibilities:**

```typescript
class UserManager {
	async createUser(userData: UserData) {
		// Validation, DB operations, email sending, logging, analytics
		// All mixed together - 50+ lines of tangled logic
	}
}
```

**✅ Good - Separated responsibilities:**

```typescript
class UserValidator {
	validate(userData: UserData) {}
}
class UserRepository {
	async create(userData: UserData) {}
}
class EmailService {
	async sendWelcomeEmail(user: User) {}
}
class UserAnalytics {
	track(event: string, properties: any) {}
}

class UserService {
	constructor(
		private validator: UserValidator,
		private repository: UserRepository,
		private emailService: EmailService,
		private analytics: UserAnalytics
	) {}

	async createUser(userData: UserData): Promise<User> {
		this.validator.validate(userData);
		const user = await this.repository.create(userData);
		await this.emailService.sendWelcomeEmail(user);
		this.analytics.track("user_created", { userId: user.id });
		return user;
	}
}
```

**Key Benefits:**

- Each class has one reason to change
- Easier to test in isolation
- Highly reusable components
- Better readability

---

## Open/Closed Principle (OCP)

_Software entities should be open for extension, but closed for modification._

**❌ Bad - Must modify class to add new behavior:**

```typescript
class PaymentProcessor {
	processPayment(amount: number, type: string) {
		if (type === "credit_card") return this.processCreditCard(amount);
		else if (type === "paypal") return this.processPayPal(amount);
		else if (type === "stripe") return this.processStripe(amount);
		// Need to modify this class every time we add a new payment method!
	}
}
```

**✅ Good - Open for extension via abstraction:**

```typescript
interface PaymentMethod {
	process(amount: number): Promise<PaymentResult>;
	validate(amount: number): boolean;
}

class CreditCardPayment implements PaymentMethod {}
class PayPalPayment implements PaymentMethod {}
class CryptoPayment implements PaymentMethod {}

class PaymentProcessor {
	constructor(private paymentMethod: PaymentMethod) {}

	async processPayment(amount: number): Promise<PaymentResult> {
		if (!this.paymentMethod.validate(amount)) throw new Error("Invalid amount");
		return await this.paymentMethod.process(amount);
	}

	setPaymentMethod(method: PaymentMethod): void {
		this.paymentMethod = method;
	}
}

// Usage - easily extensible
const processor = new PaymentProcessor(new CreditCardPayment());
processor.setPaymentMethod(new CryptoPayment()); // No class modification needed!
```

**Key Benefits:**

- Add new behaviors without modifying existing code
- Reduces risk of breaking existing functionality
- Enables plugin architectures

---

## Liskov Substitution Principle (LSP)

_Objects of a superclass should be replaceable with objects of its subclasses without breaking the application._

**❌ Bad - Subclass violates superclass contract:**

```typescript
class Bird {
	fly(): void {
		console.log("Flying...");
	}
}
class Penguin extends Bird {
	fly(): void {
		throw new Error("Penguins cannot fly!");
	} // ❌ LSP violation
}
function makeBirdFly(bird: Bird) {
	bird.fly();
}
makeBirdFly(new Penguin()); // Crashes!
```

**✅ Good - Proper abstraction respecting capabilities:**

```typescript
interface Animal {
	move(): void;
	makeSound(): void;
}
interface Flyable {
	fly(): void;
}

class Bird implements Animal {
	move(): void {
		console.log("Moving...");
	}
	makeSound(): void {
		console.log("Chirp!");
	}
}

class Sparrow extends Bird implements Flyable {
	fly(): void {
		console.log("Sparrow flying...");
	}
}

class Penguin extends Bird {
	move(): void {
		console.log("Penguin waddling...");
	}
	swim(): void {
		console.log("Penguin swimming...");
	}
}

function makeAnimalMove(animal: Animal) {
	animal.move();
} // Works for all
function makeFlyableObjectFly(flyable: Flyable) {
	flyable.fly();
} // Only flyables

makeAnimalMove(new Sparrow()); // ✅ Works
makeAnimalMove(new Penguin()); // ✅ Works
makeFlyableObjectFly(new Sparrow()); // ✅ Works
```

**Key Benefits:**

- Prevents runtime errors from unexpected behavior
- Enables safe polymorphism
- Clear contract definitions

---

## Interface Segregation Principle (ISP)

_Clients should not be forced to depend on interfaces they don't use._

**❌ Bad - Fat interface with unneeded methods:**

```typescript
interface Worker {
	work(): void;
	eat(): void;
	sleep(): void;
	getPaid(): void;
}

class RobotWorker implements Worker {
	work(): void {
		console.log("Working 24/7...");
	}
	eat(): void {
		throw new Error("Robots don't eat!");
	} // Forced!
	sleep(): void {
		throw new Error("Robots don't sleep!");
	} // Forced!
	getPaid(): void {
		throw new Error("Robots don't get paid!");
	} // Forced!
}
```

**✅ Good - Segregated interfaces:**

```typescript
interface Workable {
	work(): void;
}
interface Eatable {
	eat(): void;
}
interface Sleepable {
	sleep(): void;
}
interface Payable {
	getPaid(): void;
}

class HumanWorker implements Workable, Eatable, Sleepable, Payable {}
class RobotWorker implements Workable {
	work(): void {}
}
class Contractor implements Workable, Payable {}

// Functions only depend on what they need
function scheduleWork(worker: Workable) {
	worker.work();
}
function processPayroll(employee: Payable) {
	employee.getPaid();
}
```

**Key Benefits:**

- Minimal interface contracts
- Flexible class implementations
- No forced dummy implementations

---

## Dependency Inversion Principle (DIP)

_Depend upon abstractions, not concretions. High-level modules should not depend on low-level modules._

**❌ Bad - Tight coupling to concrete implementations:**

```typescript
class UserService {
	private db: MySQLDatabase; // Tightly coupled!

	constructor() {
		this.db = new MySQLDatabase();
	}

	async getUser(id: string) {
		return this.db.query(`SELECT * FROM users WHERE id = ${id}`);
	}
}
```

**✅ Good - Depend on abstractions:**

```typescript
interface Database {
	connect(): Promise<void>;
	query<T>(query: string, params?: any[]): Promise<T>;
}

class UserService {
	constructor(private db: Database) {} // Injected abstraction

	async getUser(id: string): Promise<User> {
		return this.db.query<User>("SELECT * FROM users WHERE id = ?", [id]);
	}
}

// Easy to swap implementations
const mysqlDb = new MySQLDatabase();
const postgresDb = new PostgreSQLDatabase();
const mockDb = new MockDatabase(); // For testing

const prodService = new UserService(mysqlDb);
const testService = new UserService(mockDb);
```

**Key Benefits:**

- Easy to test with mocks
- Flexible component swapping
- Loose coupling throughout codebase
- Infrastructure independence
