# Migrating JavaScript to TypeScript

## Incremental Adoption Strategy

Migrating large JavaScript codebases to TypeScript is a process, not an event. Use these strategies to adopt TypeScript gradually.

## Phase 1: Prepare Your Project

### 1. Install TypeScript and Types

```bash
npm install --save-dev typescript @types/node

# Install types for frameworks used
npm install --save-dev @types/react @types/react-dom
npm install --save-dev @types/express
```

### 2. Configure tsconfig.json

Start with relaxed settings, gradually tighten:

```json
{
  "compilerOptions": {
    "allowJs": true,
    "checkJs": false,
    "noImplicitAny": false,
    "strict": false,
    "target": "ES2020",
    "module": "commonjs",
    "lib": ["ES2020", "DOM"],
    "outDir": "./dist",
    "rootDir": "./src"
  },
  "include": ["src/**/*"],
  "exclude": ["node_modules", "dist"]
}
```

### 3. Add Build Step

```json
{
  "scripts": {
    "build": "tsc",
    "start": "node dist/index.js"
  }
}
```

## Phase 2: Enable Checking

### Enable checkJs Gradually

```bash
# Enable checking on .js files with allowJs=true
# Update tsconfig.json: "checkJs": true
```

TypeScript will report errors in existing JavaScript. Ignore them initially with `// @ts-ignore`:

```javascript
const result = await someUntypedFunction(); // @ts-ignore
processData(result);
```

## Phase 3: Convert Files to TypeScript

Convert files in this order:

1. **Utility/Helper files** (no dependencies): `math.js` → `math.ts`
2. **Data models** (simple types): `User.js` → `User.ts`
3. **Business logic** (mid-level): `services/**`
4. **Entry points** (last): `index.js` → `index.ts`

### Conversion Example

**Before (math.js):**
```javascript
function add(a, b) {
  return a + b;
}

function multiply(a, b) {
  return a * b;
}

export { add, multiply };
```

**After (math.ts):**
```typescript
export function add(a: number, b: number): number {
  return a + b;
}

export function multiply(a: number, b: number): number {
  return a * b;
}
```

## Phase 4: Type Complex Code

### Intro to Basic Types

```typescript
interface User {
  id: string;
  name: string;
  email: string;
}

async function getUser(id: string): Promise<User> {
  const response = await fetch(`/api/users/${id}`);
  return await response.json();
}
```

### Handle External/Untrusted Data

Use `unknown` for external input and build narrowers:

```typescript
// External data from API/user input
function processUserInput(data: unknown): User {
  // Type guard
  if (!isValidUser(data)) {
    throw new Error('Invalid user data');
  }
  return data; // Now typed as User
}

// Type guard function
function isValidUser(data: unknown): data is User {
  return (
    typeof data === 'object' &&
    data !== null &&
    'id' in data &&
    'name' in data &&
    'email' in data &&
    typeof (data as any).id === 'string' &&
    typeof (data as any).name === 'string' &&
    typeof (data as any).email === 'string'
  );
}
```

Or use a validation library:

```typescript
import { z } from 'zod';

const UserSchema = z.object({
  id: z.string(),
  name: z.string(),
  email: z.string().email(),
});

function processUserInput(data: unknown): User {
  return UserSchema.parse(data);
}
```

## Phase 5: Tighten Configuration

Gradually enable stricter compiler options:

### Step 1: Enable noImplicitAny

```json
{
  "compilerOptions": {
    "noImplicitAny": true
  }
}
```

Requires all function parameters and variables to have explicit types or be inferred.

### Step 2: Enable strict Nullability

```json
{
  "compilerOptions": {
    "strictNullChecks": true
  }
}
```

Requires explicit handling of `null` and `undefined`.

```typescript
function greet(name: string | null) {
  if (name) {
    console.log(`Hello, ${name}`); // ✓ name is string here
  }
}
```

### Step 3: Enable Full Strict Mode

```json
{
  "compilerOptions": {
    "strict": true
  }
}
```

Enables all strict options at once:
- `noImplicitAny`
- `noImplicitThis`
- `alwaysStrict`
- `strictBindCallApply`
- `strictNullChecks`
- `strictFunctionTypes`
- `strictPropertyInitialization`

## Common Patterns

### Error Handling Conversion

**Before:**
```javascript
function getData(userId) {
  try {
    return fetch(`/api/users/${userId}`).then((r) => r.json());
  } catch (e) {
    console.error(e);
    return null;
  }
}
```

**After:**
```typescript
interface Result<T> {
  success: true;
  data: T;
}

interface ErrorResult {
  success: false;
  error: string;
}

async function getData(userId: string): Promise<Result<User> | ErrorResult> {
  try {
    const response = await fetch(`/api/users/${userId}`);
    if (!response.ok) {
      return { success: false, error: `HTTP ${response.status}` };
    }
    const data: unknown = await response.json();
    const user = UserSchema.parse(data);
    return { success: true, data: user };
  } catch (err) {
    return { success: false, error: String(err) };
  }
}
```

## Tips for Large Migrations

- **Set a pace**: Convert 5-10 files per sprint, not everything at once
- **Use `// @ts-expect-error`**: Document intentional type violations temporarily
- **Lean on inference**: TypeScript infers types from assignments—you don't need explicit types everywhere
- **Run tests**: Type safety doesn't replace tests; run suite frequently during migration
- **Pair noImplicitAny with `unknown`**: Catch misconfigured types early
- **Document decisions**: Leave comments explaining non-obvious types to help team members
