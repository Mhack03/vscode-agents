# API Design with TypeScript

## Discriminated Unions (Result Types)

Instead of throwing errors for expected failures, use discriminated unions to make error paths explicit and type-safe.

### Pattern: Success | Error

```typescript
type Result<T> = 
  | { success: true; data: T }
  | { success: false; error: string };

async function fetchUser(id: string): Promise<Result<User>> {
  try {
    const response = await fetch(`/api/users/${id}`);
    if (!response.ok) {
      return { success: false, error: `HTTP ${response.status}` };
    }
    const data = await response.json();
    return { success: true, data };
  } catch (err) {
    return { success: false, error: String(err) };
  }
}

// Usage with type narrowing
const result = await fetchUser("123");
if (result.success) {
  console.log(result.data.email); // ✓ result.data is User
} else {
  console.error(result.error);    // ✓ result.error is string
}
```

### Pattern: Ok<T> | Err<E>

For more control, use generic error types:

```typescript
type Ok<T> = { kind: 'ok'; value: T };
type Err<E> = { kind: 'error'; error: E };
type Result<T, E> = Ok<T> | Err<E>;

// Helper functions
const ok = <T>(value: T): Result<T, never> => ({ kind: 'ok', value });
const err = <E>(error: E): Result<never, E> => ({ kind: 'error', error });

// Usage
function divide(a: number, b: number): Result<number, string> {
  if (b === 0) return err('Division by zero');
  return ok(a / b);
}

const result = divide(10, 2);
if (result.kind === 'ok') {
  console.log(result.value); // 5
}
```

### Pattern: Chaining with Helpers

Build utilities for cleaner workflows:

```typescript
type Result<T, E> = 
  | { kind: 'ok'; value: T }
  | { kind: 'error'; error: E };

function isOk<T, E>(r: Result<T, E>): r is Ok<T> {
  return r.kind === 'ok';
}

function isErr<T, E>(r: Result<T, E>): r is Err<E> {
  return r.kind === 'error';
}

// Chaining
async function loadUserWithPosts(userId: string) {
  const userResult = await fetchUser(userId);
  if (!isOk(userResult)) return userResult;

  const postsResult = await fetchPosts(userResult.value.id);
  if (!isOk(postsResult)) return postsResult;

  return ok({ user: userResult.value, posts: postsResult.value });
}
```

## Type-Safe HTTP Endpoints

### Request/Response Separation

```typescript
// Request DTO (Data Transfer Object)
interface CreateUserRequest {
  email: string;
  name: string;
}

// Response DTO
interface CreateUserResponse {
  id: string;
  email: string;
  name: string;
  createdAt: string;
}

// Type-safe handler
type Handler<Req, Res> = (req: Req) => Promise<Result<Res, ApiError>>;

const createUser: Handler<CreateUserRequest, CreateUserResponse> = async (req) => {
  // TypeScript ensures req has email and name
  // And return type must be Res | ApiError
};
```

## Best Practices

- **Discriminant fields**: Use `success`, `kind`, or `type` fields to distinguish union members
- **Avoid `any`**: Use `unknown` for external input, then narrow with type guards
- **Co-locate types and validation**: Keep DTOs near their validation schemas
- **Exhaustiveness checking**: TypeScript's control flow analysis ensures all branches are handled
