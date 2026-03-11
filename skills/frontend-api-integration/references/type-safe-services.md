# Type-Safe API Services

## Types Definition

```typescript
// types/api.ts
export interface User {
	id: string;
	email: string;
	name: string;
	role: "admin" | "user" | "guest";
	createdAt: string;
}

export interface CreateUserRequest {
	email: string;
	name: string;
	role: string;
}

export interface ApiResponse<T> {
	data: T;
	statusCode: number;
	message: string;
}

export interface PaginatedResponse<T> {
	items: T[];
	totalCount: number;
	pageNumber: number;
	pageSize: number;
}
```

## Service Layer Implementation

```typescript
// services/userService.ts
import { apiClient } from "@/api/client";

export const userService = {
	// Get all users with pagination
	async getUsers(pageNumber: number, pageSize: number) {
		const response = await apiClient.get<PaginatedResponse<User>>("/users", {
			params: { pageNumber, pageSize },
		});
		return response.data;
	},

	// Get single user
	async getUser(id: string) {
		const response = await apiClient.get<ApiResponse<User>>(`/users/${id}`);
		return response.data.data;
	},

	// Create user
	async createUser(data: CreateUserRequest) {
		const response = await apiClient.post<ApiResponse<User>>("/users", data);
		return response.data.data;
	},

	// Update user
	async updateUser(id: string, data: Partial<CreateUserRequest>) {
		const response = await apiClient.put<ApiResponse<User>>(
			`/users/${id}`,
			data
		);
		return response.data.data;
	},

	// Delete user
	async deleteUser(id: string) {
		await apiClient.delete(`/users/${id}`);
	},
};
```

## Best Practices

- Define shared DTOs matching backend models
- Use TypeScript generics for reusable response wrappers
- Organize services by domain (userService, productService, etc.)
- Keep service layer thin — business logic goes in components or state management
