import React from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import axios, { AxiosError } from "axios";

/**
 * Advanced API Integration with TanStack Query
 * Demonstrates proper error handling, caching, and data management
 */

// Types
interface User {
	id: string;
	name: string;
	email: string;
	role: "admin" | "user";
}

interface CreateUserRequest {
	name: string;
	email: string;
	role: string;
}

interface ApiError {
	statusCode: number;
	message: string;
	errors?: Record<string, string[]>;
}

// API Client Setup
const apiClient = axios.create({
	baseURL: import.meta.env.VITE_API_URL || "http://localhost:5000/api",
	timeout: 10000,
});

// Error handling interceptor
apiClient.interceptors.response.use(
	(response) => response,
	(error: AxiosError<ApiError>) => {
		if (error.response?.status === 401) {
			localStorage.removeItem("accessToken");
			window.location.href = "/login";
		}
		return Promise.reject(error);
	}
);

// API Service
const userService = {
	async getUsers(page: number, limit: number) {
		const response = await apiClient.get("/users", {
			params: { page, limit },
		});
		return response.data;
	},

	async getUser(id: string) {
		const response = await apiClient.get<User>(`/users/${id}`);
		return response.data;
	},

	async createUser(data: CreateUserRequest) {
		const response = await apiClient.post<User>("/users", data);
		return response.data;
	},

	async updateUser(id: string, data: Partial<CreateUserRequest>) {
		const response = await apiClient.put<User>(`/users/${id}`, data);
		return response.data;
	},

	async deleteUser(id: string) {
		await apiClient.delete(`/users/${id}`);
	},
};

// Custom Hooks
export function useUsers(page: number, limit: number) {
	return useQuery({
		queryKey: ["users", page, limit],
		queryFn: () => userService.getUsers(page, limit),
		staleTime: 5 * 60 * 1000,
		gcTime: 10 * 60 * 1000,
		retry: 3,
		retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000),
	});
}

export function useUser(id: string | null) {
	return useQuery({
		queryKey: ["users", id],
		queryFn: () => userService.getUser(id!),
		enabled: !!id,
	});
}

export function useCreateUser() {
	const queryClient = useQueryClient();

	return useMutation({
		mutationFn: (data: CreateUserRequest) => userService.createUser(data),
		onSuccess: (newUser) => {
			queryClient.invalidateQueries({ queryKey: ["users"] });
			queryClient.setQueryData(["users", newUser.id], newUser);
		},
	});
}

export function useUpdateUser() {
	const queryClient = useQueryClient();

	return useMutation({
		mutationFn: ({
			id,
			data,
		}: {
			id: string;
			data: Partial<CreateUserRequest>;
		}) => userService.updateUser(id, data),
		onSuccess: (updatedUser) => {
			queryClient.setQueryData(["users", updatedUser.id], updatedUser);
			queryClient.invalidateQueries({ queryKey: ["users"] });
		},
	});
}

export function useDeleteUser() {
	const queryClient = useQueryClient();

	return useMutation({
		mutationFn: (id: string) => userService.deleteUser(id),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["users"] });
		},
	});
}

// Component Example
export function UserTable() {
	const [page, setPage] = React.useState(1);
	const { data, isLoading, error } = useUsers(page, 10);
	const createUser = useCreateUser();
	const updateUser = useUpdateUser();
	const deleteUser = useDeleteUser();

	if (isLoading) return <div>Loading users...</div>;
	if (error)
		return (
			<div>
				Error loading users:{" "}
				{(error as AxiosError<ApiError>).response?.data.message}
			</div>
		);

	return (
		<div>
			<h2>Users</h2>
			<button
				onClick={() =>
					createUser.mutate({
						name: "New User",
						email: "newuser@example.com",
						role: "user",
					})
				}
				disabled={createUser.isPending}
			>
				{createUser.isPending ? "Creating..." : "Add User"}
			</button>

			{createUser.error && (
				<div className="text-red-500">
					Error:{" "}
					{(createUser.error as AxiosError<ApiError>).response?.data.message}
				</div>
			)}

			<table>
				<thead>
					<tr>
						<th>Name</th>
						<th>Email</th>
						<th>Role</th>
						<th>Actions</th>
					</tr>
				</thead>
				<tbody>
					{data?.data?.map((user) => (
						<tr key={user.id}>
							<td>{user.name}</td>
							<td>{user.email}</td>
							<td>{user.role}</td>
							<td>
								<button
									onClick={() => deleteUser.mutate(user.id)}
									disabled={deleteUser.isPending}
								>
									Delete
								</button>
							</td>
						</tr>
					))}
				</tbody>
			</table>

			<div>
				<button
					onClick={() => setPage((p) => Math.max(1, p - 1))}
					disabled={page === 1}
				>
					Previous
				</button>
				<span>Page {page}</span>
				<button onClick={() => setPage((p) => p + 1)}>Next</button>
			</div>
		</div>
	);
}
