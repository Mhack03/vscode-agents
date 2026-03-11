# Clean Architecture

Build scalable, maintainable systems with proper separation of concerns.

## Layered Architecture

### The Three-Layer Model

```
┌─────────────────────────────────┐
│    Presentation Layer (UI)      │ - React, Vue, Angular, Web Forms
├─────────────────────────────────┤
│    Application Layer            │ - Services, Controllers, API Routes
├─────────────────────────────────┤
│    Data Layer                   │ - Repositories, Database, External APIs
├─────────────────────────────────┤
│    Domain Layer                 │ - Business Logic, Entities (core)
└─────────────────────────────────┘
```

### Layer Responsibilities

**Domain Layer (Innermost):**

- Pure business logic
- Entities and value objects
- Business rules and invariants
- No framework dependencies
- No database concerns

**Data Layer:**

- Database access (repositories)
- External API integration
- Data transformation
- Query optimization

**Application Layer:**

- Use cases / workflows
- Service coordination
- Input validation
- Error handling
- Transaction management

**Presentation Layer (Outermost):**

- User interface
- API endpoints
- HTTP request/response handling
- User input collection

### Example: User Registration

```typescript
// Domain Layer - Pure business logic
interface User {
	id: string;
	email: string;
	password: string; // In production: hash only!
	createdAt: Date;
}

function validateEmailFormat(email: string): boolean {
	return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

function isStrongPassword(password: string): boolean {
	return (
		password.length >= 8 && /[A-Z]/.test(password) && /[0-9]/.test(password)
	);
}

class InvalidUserDataError extends Error {
	constructor(message: string) {
		super(message);
		this.name = "InvalidUserDataError";
	}
}

// Data Layer - Repository interface
interface UserRepository {
	findByEmail(email: string): Promise<User | null>;
	create(user: User): Promise<User>;
}

// Service Layer - Use case coordination
class UserRegistrationService {
	constructor(private userRepository: UserRepository) {}

	async register(email: string, password: string): Promise<User> {
		// Validate input
		if (!validateEmailFormat(email)) {
			throw new InvalidUserDataError("Invalid email format");
		}

		if (!isStrongPassword(password)) {
			throw new InvalidUserDataError(
				"Password must be at least 8 characters with uppercase and number"
			);
		}

		// Check if user already exists
		const existingUser = await this.userRepository.findByEmail(email);
		if (existingUser) {
			throw new InvalidUserDataError("User with this email already exists");
		}

		// Create new user
		const newUser: User = {
			id: generateId(),
			email,
			password: hashPassword(password), // Domain responsibility
			createdAt: new Date(),
		};

		return await this.userRepository.create(newUser);
	}
}

// Presentation Layer - API endpoint
export async function register(req: Request, res: Response) {
	try {
		const { email, password } = req.body;
		const service = new UserRegistrationService(userRepository);
		const user = await service.register(email, password);

		res.status(201).json({
			id: user.id,
			email: user.email,
			createdAt: user.createdAt,
		});
	} catch (error) {
		if (error instanceof InvalidUserDataError) {
			res.status(400).json({ error: error.message });
		} else {
			res.status(500).json({ error: "Internal server error" });
		}
	}
}
```

---

## Dependency Flow

**Cardinal rule: Dependencies point inward (toward domain layer).**

### ❌ Bad - Layers depend on outer layers:

```typescript
// Domain depends on framework
import { Request, Response } from "express";

class User {
	handleRequest(req: Request): void {
		// Domain knows about HTTP
	}
}

// Service depends on specific implementation
class OrderService {
	private db = new MySQLDatabase(); // Tightly coupled
}
```

### ✅ Good - Inversion of Control:

```typescript
// Domain layer (no external dependencies)
interface UserRepository {
	findById(id: string): Promise<User | null>;
	save(user: User): Promise<void>;
}

class UserService {
	constructor(private repository: UserRepository) {} // Inject abstraction

	async getUser(id: string): Promise<User> {
		const user = await this.repository.findById(id);
		if (!user) throw new Error("User not found");
		return user;
	}
}

// Data layer implements the interface
class MySQLUserRepository implements UserRepository {
	async findById(id: string): Promise<User | null> {
		return await db.query("SELECT * FROM users WHERE id = ?", [id]);
	}

	async save(user: User): Promise<void> {
		await db.query("INSERT INTO users VALUES (?)", [user]);
	}
}

// Presentation layer wires everything together
const repository = new MySQLUserRepository();
const userService = new UserService(repository);

app.get("/users/:id", async (req, res) => {
	try {
		const user = await userService.getUser(req.params.id);
		res.json(user);
	} catch (error) {
		res.status(404).json({ error: error.message });
	}
});
```

---

## React Example: Clean Architecture

```typescript
// Domain Layer - Business logic (domain/userService.ts)
export interface User {
  id: string;
  name: string;
  email: string;
}

export interface UserService {
  getUser(id: string): Promise<User>;
  updateUser(id: string, data: Partial<User>): Promise<User>;
}

// Data Layer - API communication (data/userRepository.ts)
import axios from "axios";

export class ApiUserRepository implements UserService {
  private client = axios.create({ baseURL: process.env.REACT_APP_API_URL });

  async getUser(id: string): Promise<User> {
    const response = await this.client.get<User>(`/users/${id}`);
    return response.data;
  }

  async updateUser(id: string, data: Partial<User>): Promise<User> {
    const response = await this.client.put<User>(`/users/${id}`, data);
    return response.data;
  }
}

// Application Layer - Use cases (hooks/useUser.ts)
import { useQuery, useMutation } from "@tanstack/react-query";

export function useUser(userService: UserService, id: string) {
  return useQuery({
    queryKey: ["user", id],
    queryFn: () => userService.getUser(id),
  });
}

export function useUpdateUser(userService: UserService) {
  return useMutation({
    mutationFn: (data: { id: string; updates: Partial<User> }) =>
      userService.updateUser(data.id, data.updates),
  });
}

// Presentation Layer - UI Components (pages/UserProfile.tsx)
import React from "react";

interface UserProfileProps {
  userId: string;
  userService: UserService;
}

export function UserProfile({ userId, userService }: UserProfileProps) {
  const { data: user, isLoading } = useUser(userService, userId);
  const updateUser = useUpdateUser(userService);

  if (isLoading) return <div>Loading...</div>;
  if (!user) return <div>User not found</div>;

  return (
    <div>
      <h1>{user.name}</h1>
      <p>{user.email}</p>
      <button onClick={() => updateUser.mutate({ id: userId, updates: { name: "New Name" } })}>
        Update Name
      </button>
    </div>
  );
}

// Composition Root (App.tsx)
import { ApiUserRepository } from "./data/userRepository";

export function App() {
  const userService = new ApiUserRepository();

  return (
    <Routes>
      <Route path="/users/:id" element={<UserProfile userService={userService} />} />
    </Routes>
  );
}
```

---

## Benefits of Clean Architecture

✅ **Testability** - Inject mocks at any layer

✅ **Flexibility** - Swap implementations easily (MySQL ↔ PostgreSQL, REST ↔ GraphQL)

✅ **Maintainability** - Each layer has single responsibility

✅ **Scalability** - Add features without breaking existing code

✅ **Readability** - Clear dependencies and data flow

✅ **Framework Independence** - Core logic doesn't depend on specific libraries

---

## Anti-Patterns to Avoid

### ❌ Layered Anarchy

Every layer talks to every layer - no structure.

### ❌ Layer Skipping

Presentation layer directly accesses data layer, bypassing services.

```typescript
// Bad
function UserList() {
	const [users, setUsers] = useState([]);

	useEffect(() => {
		// Direct database/API call in component
		fetch("/api/users")
			.then((res) => res.json())
			.then(setUsers);
	}, []);
}
```

### ❌ Circular Dependencies

Layer A depends on B, B depends on C, C depends on A.

### ❌ Data Leakage

Domain objects exposed directly to presentation layer; changes in domain break UI.
