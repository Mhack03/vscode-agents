# Runtime Validation with Zod

## Why Runtime Validation?

TypeScript types exist only at compile time. API responses, user input, and external data must be validated at runtime to ensure type safety.

Zod provides schema-based validation with automatic TypeScript type inference:

```typescript
import { z } from 'zod';

// Define schema
const UserSchema = z.object({
  id: z.string().uuid(),
  email: z.string().email(),
  name: z.string().min(1).max(100),
  age: z.number().int().min(0).max(150).optional(),
});

// Derive TypeScript type from schema (no duplication)
type User = z.infer<typeof UserSchema>;

// Now User type is automatically:
// {
//   id: string;
//   email: string;
//   name: string;
//   age?: number;
// }
```

## Common Patterns

### Parse External Data

```typescript
const rawData = await fetch('/api/user').then((r) => r.json());

// Throws if validation fails
const user: User = UserSchema.parse(rawData);

// Or use safeParse to handle errors explicitly
const result = UserSchema.safeParse(rawData);
if (result.success) {
  console.log(result.data); // User
} else {
  console.log(result.error.errors); // List of validation errors
}
```

### Nested Schemas

```typescript
const AddressSchema = z.object({
  street: z.string(),
  city: z.string(),
  zip: z.string(),
});

const CompanySchema = z.object({
  name: z.string(),
  headquarters: AddressSchema,
});

type Company = z.infer<typeof CompanySchema>;
// {
//   name: string;
//   headquarters: {
//     street: string;
//     city: string;
//     zip: string;
//   };
// }
```

### Arrays and Unions

```typescript
// Array of items
const UsersSchema = z.array(UserSchema);
type Users = z.infer<typeof UsersSchema>; // User[]

// Union types (discriminated)
const ResponseSchema = z.union([
  z.object({ success: z.literal(true), data: UserSchema }),
  z.object({ success: z.literal(false), error: z.string() }),
]);

type Response = z.infer<typeof ResponseSchema>;
// {
//   success: true;
//   data: User;
// } | {
//   success: false;
//   error: string;
// }
```

### Transform Data During Validation

```typescript
const UserSchema = z
  .object({
    name: z.string(),
    email: z.string().email(),
    createdAt: z.string(), // ISO date string
  })
  .transform((data) => ({
    ...data,
    createdAt: new Date(data.createdAt), // Convert to Date
  }));

type User = z.infer<typeof UserSchema>;
// {
//   name: string;
//   email: string;
//   createdAt: Date;
// }
```

## API Response Validation

```typescript
import { z } from 'zod';

const ProductSchema = z.object({
  id: z.number(),
  name: z.string(),
  price: z.number().positive(),
  tags: z.array(z.string()).optional(),
});

const ApiResponseSchema = z.object({
  success: z.literal(true),
  data: z.array(ProductSchema),
  total: z.number(),
});

type ApiResponse = z.infer<typeof ApiResponseSchema>;

async function getProducts(): Promise<ApiResponse> {
  const response = await fetch('/api/products');
  const json = await response.json();
  
  // Throws if doesn't match schema
  return ApiResponseSchema.parse(json);
}
```

## Error Handling

```typescript
const SafeUserSchem = UserSchema;

const result = SafeUserSchem.safeParse(unknownData);

if (!result.success) {
  // result.error.errors contains detailed error info
  result.error.errors.forEach((err) => {
    console.log(`Field: ${err.path.join('.')}`);
    console.log(`Issue: ${err.code}`); // 'invalid_type', 'too_small', etc.
    console.log(`Message: ${err.message}`);
  });
} else {
  // result.data is fully typed as User
  console.log(result.data);
}
```

## Custom Validations

```typescript
const PasswordSchema = z
  .string()
  .min(8, 'Password must be at least 8 characters')
  .regex(/[A-Z]/, 'Must contain uppercase letter')
  .regex(/[0-9]/, 'Must contain number');

const SignupSchema = z
  .object({
    email: z.string().email(),
    password: PasswordSchema,
    passwordConfirm: z.string(),
  })
  .refine((data) => data.password === data.passwordConfirm, {
    message: 'Passwords do not match',
    path: ['passwordConfirm'], // Set which field shows the error
  });

type Signup = z.infer<typeof SignupSchema>;
```

## Best Practices

- **Co-locate schemas with types**: Define Zod schema in same file as exported type
- **Reuse schemas**: Build schemas from smaller composable pieces
- **Separate request/response**: API request and response DTOs often differ
- **Transform at validation**: Use `.transform()` to normalize data (lowercase emails, parse dates, etc.)
- **Explicit error handling**: Use `safeParse()` for user input, `parse()` for internal trusted data
