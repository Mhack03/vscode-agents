# Advanced TypeScript Utility Types

## Utility Types Overview

TypeScript provides built-in utility types for common type transformations. Use these to reduce duplication and maintain maintainability.

| Type | Purpose | Example |
|------|---------|---------|
| `Pick<T, K>` | Extract subset of properties | `Pick<User, 'id' \| 'name'>` |
| `Omit<T, K>` | Remove properties | `Omit<User, 'password'>` |
| `Partial<T>` | Make all properties optional | `Partial<User>` |
| `Required<T>` | Make all properties required | `Required<Partial<User>>` |
| `Record<K, T>` | Create object with key-value pairs | `Record<'light' \| 'dark', Theme>` |
| `Exclude<T, U>` | Remove union members | `Exclude<'a' \| 'b' \| 'c', 'a'>` → `'b' \| 'c'` |
| `Extract<T, U>` | Keep only matching union members | `Extract<'a' \| 'b' \| 'c', 'a' \| 'c'>` → `'a' \| 'c'` |
| `ReturnType<T>` | Extract function return type | `ReturnType<() => string>` → `string` |
| `Parameters<T>` | Extract function parameters | `Parameters<(a: string) => void>` → `[a: string]` |

## Essential Patterns

### Pick and Omit

```typescript
interface User {
  id: string;
  email: string;
  password: string;
  createdAt: Date;
}

// Public API response (exclude sensitive fields)
type PublicUser = Omit<User, 'password'>;

// User creation DTO (only required fields)
type CreateUserInput = Pick<User, 'email' | 'password'>;

// Usage
const sendUserEmail = (user: PublicUser) => {
  // Can access: id, email, createdAt
  // Cannot access: password (prevents accidental leaks)
};
```

### Partial and Required

```typescript
interface Config {
  apiUrl: string;
  timeout: number;
  retries: number;
}

// Patch endpoint (all fields optional)
type UpdateConfig = Partial<Config>;

async function updateConfig(updates: UpdateConfig) {
  const merged = { ...defaultConfig, ...updates };
}

// Ensure all required fields present
function validateConfig(config: Required<Config>) {
  // All properties guaranteed to exist and not be undefined
}
```

### Record for Maps

```typescript
type Theme = 'light' | 'dark';

// Instead of: { light: Colors; dark: Colors; }
type ThemeVariants = Record<Theme, Colors>;

const themes: ThemeVariants = {
  light: { bg: '#fff', text: '#000' },
  dark: { bg: '#000', text: '#fff' },
};

// Keys must be exhaustive (TypeScript error if missing 'dark')
```

## Advanced Patterns

### Conditional Types

```typescript
// Return string type if T is primitive, otherwise return object type name
type Flatten<T> = T extends Array<infer U> ? U : T;

type Str = Flatten<string[]>; // string
type Num = Flatten<number>;   // number
```

### Mapped Types

```typescript
// Convert all properties to getters
type Getters<T> = {
  [K in keyof T as `get${Capitalize<string & K>}`]: () => T[K];
};

interface Person {
  name: string;
  age: number;
}

type PersonGetters = Getters<Person>;
// {
//   getName: () => string;
//   getAge: () => number;
// }
```

### DeepPartial for Nested Objects

```typescript
type DeepPartial<T> = T extends object
  ? {
      [K in keyof T]?: DeepPartial<T[K]>;
    }
  : T;

interface Config {
  api: {
    baseUrl: string;
    timeout: number;
  };
  cache: {
    ttl: number;
  };
}

type PartialConfig = DeepPartial<Config>;
// All nested properties are optional
const partial: PartialConfig = {
  api: {
    timeout: 5000, // ✓ baseUrl can be omitted
  },
};
```

### Return Type Extraction

```typescript
function getUserById(id: string): Promise<User | null> {
  // implementation
}

type Result = ReturnType<typeof getUserById>;
// type Result = Promise<User | null>

type AwaitedResult = Awaited<Result>;
// type AwaitedResult = User | null
```

### Function Overloads with Utility Types

```typescript
interface ApiCall {
  <T>(url: string, method: 'GET'): Promise<T>;
  <T>(url: string, method: 'POST', body: object): Promise<T>;
}

// Extract parameter types
type ApiMethods = Parameters<ApiCall>[1];
// 'GET' | 'POST'
```

## Custom Utility Types

### StartsWith and EndsWith

```typescript
type StartsWith<T, U> = T extends `${U & string}${infer _}` ? true : false;

type A = StartsWith<'admin-user', 'admin'>;  // true
type B = StartsWith<'user-admin', 'admin'>;  // false
```

### HasProperty

```typescript
type HasProperty<T, K extends PropertyKey> = K extends keyof T ? true : false;

type A = HasProperty<User, 'email'>;  // true
type B = HasProperty<User, 'phone'>;  // false
```

### Ensure Readonly

```typescript
type Readonly<T> = {
  readonly [K in keyof T]: T[K];
};

const config: Readonly<Config> = { /* ... */ };
config.apiUrl = 'https://api.example.com'; // ✗ Error: cannot assign to readonly
```

## Best Practices

- **Prefer Omit over Partial**: `Omit` is more explicit and type-safe than using `Partial`
- **Use Pick for DTOs**: Separate types for request/response using `Pick` to avoid coupling
- **Record for finite sets**: Use `Record<T, V>` when mapping over a finite set of keys (not unbounded)
- **Test complex types**: Complex utility types can hide bugs; add tests to ensure inference works correctly
- **Document custom utilities**: Comments explain why the type exists and when to use it
- **Avoid deeply nested conditionals**: Complex conditional types are hard to debug; split into simpler steps
