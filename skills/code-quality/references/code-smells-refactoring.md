# Code Smells & Refactoring Techniques

Identify common code smells and learn how to refactor them for better maintainability.

## Long Functions/Methods

**Problem:** Functions doing too much, hard to understand and test.

**❌ Bad - 50+ line function:**

```typescript
async function processOrder(orderId: string) {
	const order = await db.orders.findById(orderId);
	if (!order) throw new Error("Order not found");
	if (!order.items || order.items.length === 0) throw new Error("No items");
	if (order.status !== "pending") throw new Error("Invalid status");

	let subtotal = 0;
	for (const item of order.items) {
		subtotal += item.price * item.quantity;
	}
	const tax = subtotal * 0.0825;
	const shipping = subtotal > 50 ? 0 : 5.99;
	const total = subtotal + tax + shipping;

	for (const item of order.items) {
		const stock = await db.inventory.findById(item.productId);
		if (stock.quantity < item.quantity) throw new Error("Out of stock");
	}

	for (const item of order.items) {
		await db.inventory.update(item.productId, { quantity: -item.quantity });
	}

	const payment = await paymentGateway.charge({
		amount: total,
		source: order.paymentMethod,
	});
	if (!payment.success) throw new Error("Payment failed");

	await db.orders.update(orderId, {
		status: "confirmed",
		total: total,
		paymentId: payment.id,
	});

	await emailService.send({
		to: order.email,
		subject: `Order ${orderId} confirmed`,
		body: `Your order has been confirmed!`,
	});

	return order;
}
```

**✅ Good - Extract into focused functions:**

```typescript
async function processOrder(orderId: string): Promise<Order> {
	const order = await fetchAndValidateOrder(orderId);
	const totals = calculateOrderTotals(order);

	await validateInventory(order.items);
	await reserveInventory(order.items);

	try {
		const payment = await processPayment(totals.total, order.paymentMethod);
		await updateOrderStatus(orderId, "confirmed", totals.total, payment.id);
		await sendOrderConfirmation(order.email, orderId);
		return order;
	} catch (error) {
		await rollbackInventory(order.items);
		throw error;
	}
}

// Each function has a single purpose
async function fetchAndValidateOrder(orderId: string): Promise<Order> {
	const order = await db.orders.findById(orderId);
	if (!order) throw new Error("Order not found");
	if (!order.items?.length) throw new Error("No items");
	if (order.status !== "pending") throw new Error("Invalid status");
	return order;
}

interface OrderTotals {
	subtotal: number;
	tax: number;
	shipping: number;
	total: number;
}

function calculateOrderTotals(order: Order): OrderTotals {
	const subtotal = order.items.reduce(
		(sum, item) => sum + item.price * item.quantity,
		0
	);
	return {
		subtotal,
		tax: subtotal * 0.0825,
		shipping: subtotal > 50 ? 0 : 5.99,
		total: subtotal + tax + shipping,
	};
}

async function validateInventory(items: OrderItem[]): Promise<void> {
	for (const item of items) {
		const stock = await db.inventory.findById(item.productId);
		if (stock.quantity < item.quantity) throw new Error("Out of stock");
	}
}
```

**Refactoring techniques:**

- Extract smaller functions with descriptive names
- Use helper functions for sub-tasks
- Aim for functions under 20 lines

---

## Deep Nesting

**Problem:** Excessive indentation makes code hard to follow.

**❌ Bad - Deep nesting:**

```typescript
function processUserData(user: any) {
	if (user) {
		if (user.isActive) {
			if (user.subscription) {
				if (user.subscription.plan === "premium") {
					if (user.subscription.expiresAt > Date.now()) {
						if (user.preferences?.notifications) {
							return sendPremiumNotification(user);
						}
					}
				}
			}
		}
	}
	return null;
}
```

**✅ Good - Early returns and guard clauses:**

```typescript
function processUserData(user: any) {
	if (!user) return null;
	if (!user.isActive) return null;
	if (!user.subscription) return null;
	if (user.subscription.plan !== "premium") return null;
	if (user.subscription.expiresAt <= Date.now()) return null;
	if (!user.preferences?.notifications) return null;

	return sendPremiumNotification(user);
}

// Or extract validation
function isPremiumUserWithNotifications(user: any): boolean {
	return !!(
		user?.isActive &&
		user?.subscription?.plan === "premium" &&
		user?.subscription?.expiresAt > Date.now() &&
		user?.preferences?.notifications
	);
}

function processUserData(user: any) {
	if (!isPremiumUserWithNotifications(user)) return null;
	return sendPremiumNotification(user);
}
```

**Refactoring techniques:**

- Use guard clauses (early returns)
- Extract complex conditions into named functions
- Max 2-3 nesting levels

---

## Duplicate Code (DRY Violations)

**Problem:** Same code repeated in multiple places - hard to maintain, bug-prone.

**❌ Bad - Repetition:**

```typescript
function createPost(data: PostData) {
	if (!data.title || data.title.length < 5) throw new Error("Title too short");
	if (!data.content || data.content.length < 20)
		throw new Error("Content too short");
	return db.posts.create(data);
}

function updatePost(id: string, data: PostData) {
	if (!data.title || data.title.length < 5) throw new Error("Title too short");
	if (!data.content || data.content.length < 20)
		throw new Error("Content too short");
	return db.posts.update(id, data);
}

function createComment(data: CommentData) {
	if (!data.content || data.content.length < 20)
		throw new Error("Content too short");
	return db.comments.create(data);
}
```

**✅ Good - Extract common logic:**

```typescript
function validateMinLength(
	value: string,
	minLength: number,
	fieldName: string
): void {
	if (!value || value.length < minLength) {
		throw new Error(`${fieldName} must be at least ${minLength} characters`);
	}
}

function validatePostData(data: PostData): void {
	validateMinLength(data.title, 5, "Title");
	validateMinLength(data.content, 20, "Content");
}

function createPost(data: PostData) {
	validatePostData(data);
	return db.posts.create(data);
}

function updatePost(id: string, data: PostData) {
	validatePostData(data);
	return db.posts.update(id, data);
}

function createComment(data: CommentData) {
	validateMinLength(data.content, 20, "Content");
	return db.comments.create(data);
}
```

**Refactoring techniques:**

- Extract repeated logic into reusable functions
- Use helper classes for shared behavior
- Apply Template Method pattern for similar workflows

---

## God Objects/Classes

**Problem:** Class does too many things - violates SRP, hard to test, difficult to maintain.

**❌ Bad - Everything in one class:**

```typescript
class UserManager {
	// Authentication
	login(email: string, password: string) {}
	logout(userId: string) {}
	resetPassword(email: string) {}

	// User CRUD
	createUser(data: any) {}
	updateUser(id: string, data: any) {}
	deleteUser(id: string) {}

	// Permissions
	checkPermission(userId: string, action: string) {}
	grantPermission(userId: string, permission: string) {}

	// Profile
	updateProfile(userId: string, profile: any) {}
	uploadAvatar(userId: string, file: File) {}

	// Notifications
	sendNotification(userId: string, message: string) {}
	getNotifications(userId: string) {}

	// 50+ methods...
}
```

**✅ Good - Split into focused classes:**

```typescript
class AuthenticationService {
	login(email: string, password: string): Promise<Session> {}
	logout(userId: string): Promise<void> {}
	resetPassword(email: string): Promise<void> {}
	verifyToken(token: string): Promise<boolean> {}
}

class UserRepository {
	create(data: UserData): Promise<User> {}
	findById(id: string): Promise<User | null> {}
	update(id: string, data: Partial<UserData>): Promise<User> {}
	delete(id: string): Promise<void> {}
}

class PermissionService {
	check(userId: string, action: string): Promise<boolean> {}
	grant(userId: string, permission: string): Promise<void> {}
	revoke(userId: string, permission: string): Promise<void> {}
}

class UserProfileService {
	update(userId: string, profile: ProfileData): Promise<Profile> {}
	uploadAvatar(userId: string, file: File): Promise<string> {}
}

class NotificationService {
	send(userId: string, message: string): Promise<void> {}
	getForUser(userId: string): Promise<Notification[]> {}
	markAsRead(notificationId: string): Promise<void> {}
}
```

**Refactoring techniques:**

- Identify distinct responsibilities
- Create separate classes/services for each responsibility
- Inject dependencies through constructor

---

## Magic Numbers

**Problem:** Unexplained numbers scattered in code - unclear intent, hard to maintain.

**❌ Bad - Magic numbers everywhere:**

```typescript
function calculateDiscount(price: number, customerType: string): number {
	if (customerType === "vip") return price * 0.8;
	if (price > 100) return price * 0.95;
	return price;
}

function isValidPassword(password: string): boolean {
	return (
		password.length >= 8 && /[A-Z]/.test(password) && /[0-9]/.test(password)
	);
}

setTimeout(() => checkStatus(), 300000); // What's 300000??
```

**✅ Good - Named constants:**

```typescript
const DISCOUNT = {
	VIP: 0.2, // 20% discount for VIP customers
	BULK_ORDER: 0.05, // 5% discount for orders over threshold
} as const;

const ORDER_THRESHOLD = {
	BULK_ORDER_MINIMUM: 100,
	FREE_SHIPPING: 50,
} as const;

function calculateDiscount(price: number, customerType: string): number {
	if (customerType === "vip") return price * (1 - DISCOUNT.VIP);
	if (price > ORDER_THRESHOLD.BULK_ORDER_MINIMUM) {
		return price * (1 - DISCOUNT.BULK_ORDER);
	}
	return price;
}

const PASSWORD_REQUIREMENTS = {
	MIN_LENGTH: 8,
	REQUIRES_UPPERCASE: true,
	REQUIRES_NUMBER: true,
} as const;

function isValidPassword(password: string): boolean {
	return (
		password.length >= PASSWORD_REQUIREMENTS.MIN_LENGTH &&
		PASSWORD_REQUIREMENTS.REQUIRES_UPPERCASE &&
		/[A-Z]/.test(password) &&
		PASSWORD_REQUIREMENTS.REQUIRES_NUMBER &&
		/[0-9]/.test(password)
	);
}

const STATUS_CHECK_INTERVAL_MS = 5 * 60 * 1000; // 5 minutes - clear intent!

setTimeout(() => checkStatus(), STATUS_CHECK_INTERVAL_MS);
```

**Benefits:**

- Clear intent and rationale
- Centralized configuration
- Easy to update values
