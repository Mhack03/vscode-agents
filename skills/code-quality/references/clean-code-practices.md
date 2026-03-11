# Clean Code Practices

Core practices for writing readable, maintainable, and professional code.

## Naming Conventions

**Good names reveal intent and make code self-documenting.**

### ❌ Bad names:

```typescript
function f(x: any) {
	return x * 2;
}

const d = new Date();
const temp = getUserData();
let flag = true;
const arr = [1, 2, 3];
const data = { n: "John", a: 25 };
```

### ✅ Good names:

```typescript
function doubleValue(value: number): number {
	return value * 2;
}

const currentDate = new Date();
const userData = getUserData();
const isUserActive = true;
const userIds = [1, 2, 3];
const user = { name: "John", age: 25 };
```

### Naming Guidelines

**Functions - Use verbs or verb phrases:**

```typescript
function calculateTotal() {}
function fetchUserData() {}
function isValidEmail() {}
function handleSubmit() {}
function validateInput() {}
function subscribe() {}
```

**Booleans - Ask a question:**

```typescript
const isLoading = false;
const hasPermission = true;
const canEdit = false;
const shouldRetry = true;
const isAuthenticated = false;
const hasError = false;
```

**Classes - Use nouns:**

```typescript
class UserRepository {}
class PaymentProcessor {}
class EmailValidator {}
class OrderService {}
class DatabaseConnection {}
```

**Constants - UPPER_SNAKE_CASE:**

```typescript
const MAX_RETRY_ATTEMPTS = 3;
const DEFAULT_TIMEOUT_MS = 5000;
const API_BASE_URL = "https://api.example.com";
const DISCOUNT_THRESHOLD = 100;
```

---

## Function Design

### Size & Scope

**❌ Bad - Large function doing everything:**

```typescript
function registerUser(email: string, password: string, name: string) {
	// Validation (20 lines)
	// Database operations (15 lines)
	// Email sending (10 lines)
	// Analytics (5 lines)
	// 50+ lines total
}
```

**✅ Good - Small, focused functions:**

```typescript
async function registerUser(
	email: string,
	password: string,
	name: string
): Promise<User> {
	validateRegistrationData(email, password, name);
	const user = await createUserInDatabase(email, password, name);
	await sendWelcomeEmail(user);
	trackUserRegistration(user.id);
	return user;
}

function validateRegistrationData(
	email: string,
	password: string,
	name: string
): void {
	if (!isValidEmail(email)) throw new Error("Invalid email");
	if (!isStrongPassword(password)) throw new Error("Weak password");
	if (!name || name.length < 2) throw new Error("Name too short");
}

// Each function: single responsibility, easy to test, easy to reuse
```

### Function Parameters

**Best practices:**

- Keep parameters under 3 (consider object parameter for > 3)
- Use object destructuring for related parameters
- Default parameters for optional values

```typescript
// ❌ Bad - Too many parameters
function createOrder(id, status, items, total, customer, date, notes) {}

// ✅ Good - Use object parameter
interface CreateOrderRequest {
	id: string;
	status: "pending" | "confirmed" | "shipped";
	items: OrderItem[];
	total: number;
	customer: Customer;
	date?: Date;
	notes?: string;
}

function createOrder(request: CreateOrderRequest): Order {}
```

---

## Comments

**Good comments explain WHY, not WHAT. Code describes WHAT.**

### ❌ Bad comments:

```typescript
// Loop through users
for (const user of users) {
	// Check if user is active
	if (user.isActive) {
		// Send email
		sendEmail(user);
	}
}

// Increment i
i++;

// TODO: Fix this later (written 2 years ago)
// HACK: Don't touch this!!!
```

### ✅ Good comments:

```typescript
// User must be active for at least 30 days before they can post reviews
// This prevents spam from newly created bot accounts
if (user.isActive && daysSinceRegistration(user) >= 30) {
	allowReviewPosting(user);
}

/**
 * Calculates compound interest using formula: A = P(1 + r/n)^(nt)
 *
 * @param principal Initial investment amount
 * @param rate Annual interest rate (e.g., 0.05 for 5%)
 * @param years Investment period in years
 * @param compoundFrequency Times interest compounds per year
 * @returns Final amount after compound interest
 *
 * Example: $1000 at 5% for 10 years, compounded monthly = $1645.31
 */
function calculateCompoundInterest(
	principal: number,
	rate: number,
	years: number,
	compoundFrequency: number = 12
): number {
	return (
		principal *
		Math.pow(1 + rate / compoundFrequency, compoundFrequency * years)
	);
}

// WORKAROUND: API returns null instead of empty array before v2.0
// Remove this check after migrating to API v2.0 (scheduled for Q2 2026)
const items = response.items ?? [];

// Self-documenting code needs fewer comments
function isEligibleForPremiumDiscount(user: User): boolean {
	const hasActiveSubscription = user.subscription?.status === "active";
	const isLongTermCustomer = user.memberSince < oneYearAgo();
	return hasActiveSubscription && isLongTermCustomer;
}
```

---

## Error Handling

### Custom Error Types

```typescript
class ValidationError extends Error {
	constructor(
		message: string,
		public field: string,
		public value: any
	) {
		super(message);
		this.name = "ValidationError";
	}
}

class PaymentError extends Error {
	constructor(
		message: string,
		public transactionId: string,
		public amount: number
	) {
		super(message);
		this.name = "PaymentError";
	}
}

// Catch and handle specific errors
async function processOrder(order: Order): Promise<OrderResult> {
	try {
		const paymentResult = await processPayment(order.total);
		return {
			success: true,
			orderId: order.id,
			transactionId: paymentResult.id,
		};
	} catch (error) {
		if (error instanceof ValidationError) {
			logger.warn(`Validation failed for field ${error.field}`);
			return { success: false, error: "Invalid order data" };
		}

		if (error instanceof PaymentError) {
			logger.error(`Payment failed for transaction ${error.transactionId}`);
			return { success: false, error: "Payment processing failed" };
		}

		logger.error("Unexpected error processing order", error);
		throw error;
	}
}
```

### Result Type Pattern (Alternative)

```typescript
type Result<T, E = Error> =
	| { success: true; value: T }
	| { success: false; error: E };

async function createUser(data: UserData): Promise<Result<User, string>> {
	if (!isValidEmail(data.email)) {
		return { success: false, error: "Invalid email format" };
	}

	try {
		const user = await db.users.create(data);
		return { success: true, value: user };
	} catch (error) {
		return { success: false, error: "Database error" };
	}
}

// Usage
const result = await createUser(userData);
if (result.success) {
	console.log("User created:", result.value);
} else {
	console.error("Failed:", result.error);
}
```

---

## DRY, KISS, YAGNI

### DRY (Don't Repeat Yourself)

```typescript
// ❌ Bad - Repetition
const fullName1 = `${user.firstName} ${user.lastName}`;
const fullName2 = `${author.firstName} ${author.lastName}`;
const fullName3 = `${admin.firstName} ${admin.lastName}`;

// ✅ Good - Extract into function
function getFullName(person: { firstName: string; lastName: string }): string {
	return `${person.firstName} ${person.lastName}`;
}

const fullName1 = getFullName(user);
const fullName2 = getFullName(author);
const fullName3 = getFullName(admin);
```

### KISS (Keep It Simple, Stupid)

```typescript
// ❌ Bad - Overly complex
function isEven(n: number): boolean {
	return n % 2 === 0 ? true : n % 2 === 1 ? false : true;
}

// ✅ Good - Simple and clear
function isEven(n: number): boolean {
	return n % 2 === 0;
}
```

### YAGNI (You Aren't Gonna Need It)

```typescript
// ❌ Bad - Building features you don't need yet
class User {
	constructor(
		public id: string,
		public name: string,
		public email: string,
		public customFields?: Record<string, any>,
		public tags?: string[],
		public metadata?: Record<string, unknown>,
		public preferences?: UserPreferences
	) {}
}

// ✅ Good - Start simple
class User {
	constructor(
		public id: string,
		public name: string,
		public email: string
	) {}
}

// Add properties when actually needed
```

---

## Type Safety (TypeScript)

```typescript
// ❌ Bad - Too permissive
function processData(data: any): any {
	return data.value * 2;
}

// ✅ Good - Specific types
interface NumberData {
	value: number;
}

function processData(data: NumberData): number {
	return data.value * 2;
}

// ✅ Even better - Generics for flexibility
function processData<T extends { value: number }>(data: T): number {
	return data.value * 2;
}
```

**Type Safety Benefits:**

- Catch errors at compile time
- IDE autocomplete and refactoring
- Self-documenting code
- Safer refactoring
