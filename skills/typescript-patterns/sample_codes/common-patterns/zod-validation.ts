/**
 * Runtime Validation with Zod
 * Demonstrates schema definition, type derivation, and validation patterns
 */

import { z } from 'zod';

/**
 * Define schema and derive TypeScript type automatically
 */
const UserSchema = z.object({
  id: z.string().uuid(),
  email: z.string().email('Invalid email address'),
  name: z.string().min(1).max(100),
  age: z.number().int().min(0).max(150).optional(),
  role: z.enum(['user', 'admin', 'moderator']).default('user'),
  createdAt: z.string().datetime().transform((val) => new Date(val)),
});

// Derive TypeScript type (no duplication)
type User = z.infer<typeof UserSchema>;
// {
//   id: string;
//   email: string;
//   name: string;
//   age?: number;
//   role: "user" | "admin" | "moderator";
//   createdAt: Date;
// }

/**
 * API Response with discriminated union
 */
const ApiResponseSchema = z.union([
  z.object({
    success: z.literal(true),
    data: UserSchema,
  }),
  z.object({
    success: z.literal(false),
    error: z.object({
      code: z.enum(['NOT_FOUND', 'UNAUTHORIZED', 'VALIDATION_ERROR', 'SERVER_ERROR']),
      message: z.string(),
    }),
  }),
]);

type ApiResponse = z.infer<typeof ApiResponseSchema>;

/**
 * Create/Update DTO (different from full User type)
 */
const CreateUserSchema = UserSchema.pick({ email: true, name: true, age: true });
type CreateUserInput = z.infer<typeof CreateUserSchema>;

/**
 * Fetch with validation
 */
async function getUser(id: string): Promise<User> {
  const response = await fetch(`/api/users/${id}`);
  const json = await response.json();

  // Throws if validation fails
  return UserSchema.parse(json);
}

/**
 * Safe parsing with error handling
 */
async function getUserSafe(id: string): Promise<User | null> {
  const response = await fetch(`/api/users/${id}`);
  const json = await response.json();

  const result = UserSchema.safeParse(json);
  if (!result.success) {
    console.error('Validation errors:', result.error.errors);
    return null;
  }

  return result.data;
}

/**
 * Handle API responses
 */
async function fetchUserResponse(id: string): Promise<{ user: User } | { error: string }> {
  const response = await fetch(`/api/users/${id}`);
  const json = await response.json();

  const result = ApiResponseSchema.safeParse(json);
  if (!result.success) {
    return { error: 'Invalid API response' };
  }

  if (result.data.success) {
    return { user: result.data.data }; // Type-safe access
  } else {
    return { error: result.data.error.message };
  }
}

/**
 * Form validation
 */
const PasswordSchema = z
  .string()
  .min(8, 'Password must be at least 8 characters')
  .regex(/[A-Z]/, 'Must contain uppercase letter')
  .regex(/[0-9]/, 'Must contain number')
  .regex(/[!@#$%^&*]/, 'Must contain special character');

const SignupSchema = z
  .object({
    email: z.string().email(),
    password: PasswordSchema,
    passwordConfirm: z.string(),
  })
  .refine((data) => data.password === data.passwordConfirm, {
    message: 'Passwords do not match',
    path: ['passwordConfirm'], // Show error on this field
  });

type SignupInput = z.infer<typeof SignupSchema>;

function validateSignup(data: unknown): SignupInput | { errors: Record<string, string> } {
  const result = SignupSchema.safeParse(data);

  if (!result.success) {
    const errors: Record<string, string> = {};
    result.error.errors.forEach((err) => {
      const path = err.path.join('.');
      errors[path] = err.message;
    });
    return { errors };
  }

  return result.data;
}

/**
 * Nested structures
 */
const AddressSchema = z.object({
  street: z.string(),
  city: z.string(),
  zip: z.string().regex(/^\d{5}(-\d{4})?$/),
});

const CompanySchema = z.object({
  name: z.string(),
  headquarters: AddressSchema,
  employees: z.array(UserSchema),
});

type Company = z.infer<typeof CompanySchema>;

/**
 * Polymorphic data (discriminated unions)
 */
const EventSchema = z.union([
  z.object({
    type: z.literal('user_created'),
    userId: z.string(),
    timestamp: z.date(),
  }),
  z.object({
    type: z.literal('user_deleted'),
    userId: z.string(),
    timestamp: z.date(),
    reason: z.string().optional(),
  }),
  z.object({
    type: z.literal('password_changed'),
    userId: z.string(),
    timestamp: z.date(),
  }),
]);

type Event = z.infer<typeof EventSchema>;

function handleEvent(event: Event) {
  switch (event.type) {
    case 'user_created':
      console.log(`User ${event.userId} created`);
      break;
    case 'user_deleted':
      console.log(`User ${event.userId} deleted. Reason: ${event.reason}`);
      break;
    case 'password_changed':
      console.log(`User ${event.userId} changed password`);
      break;
  }
}

/**
 * Pre-processing and transformation
 */
const UserInputSchema = z
  .object({
    email: z.string().email(),
    name: z.string(),
  })
  .transform((data) => ({
    ...data,
    email: data.email.toLowerCase(),
    name: data.name.trim(),
  }));

export {
  UserSchema,
  User,
  ApiResponseSchema,
  ApiResponse,
  CreateUserSchema,
  CreateUserInput,
  SignupSchema,
  SignupInput,
  CompanySchema,
  Company,
  EventSchema,
  Event,
  getUser,
  getUserSafe,
  validateSignup,
  handleEvent,
};
