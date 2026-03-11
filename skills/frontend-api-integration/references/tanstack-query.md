# TanStack Query (React Query) Integration

## Query Hooks Setup

```typescript
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { userService } from "@/services/userService";

export function useUsers(pageNumber: number, pageSize: number) {
	return useQuery({
		queryKey: ["users", pageNumber, pageSize],
		queryFn: () => userService.getUsers(pageNumber, pageSize),
		staleTime: 5 * 60 * 1000, // 5 minutes
		gcTime: 10 * 60 * 1000, // 10 minutes (formerly cacheTime)
	});
}

export function useUser(id: string) {
	return useQuery({
		queryKey: ["users", id],
		queryFn: () => userService.getUser(id),
		enabled: !!id, // Only fetch if id exists
	});
}

export function useCreateUser() {
	const queryClient = useQueryClient();

	return useMutation({
		mutationFn: (data) => userService.createUser(data),
		onSuccess: (newUser) => {
			// Invalidate users list to refetch
			queryClient.invalidateQueries({ queryKey: ["users"] });
			// Add new user to cache
			queryClient.setQueryData(["users", newUser.id], newUser);
		},
		onError: (error) => {
			console.error("Failed to create user:", error);
		},
	});
}
```

## Component Usage

```typescript
export function UsersList() {
  const [page, setPage] = React.useState(1);
  const { data, isLoading, error } = useUsers(page, 10);
  const createUser = useCreateUser();

  if (isLoading) return <div>Loading...</div>;
  if (error) return <div>Error: {error.message}</div>;

  return (
    <div>
      {data?.items.map(user => (
        <div key={user.id}>{user.name}</div>
      ))}
    </div>
  );
}
```

## Key Features

- **Automatic caching**: Deduplicates requests and caches results
- **Stale while revalidate**: Serve cached data while refreshing in background
- **Garbage collection**: Configurable cleanup of unused cache
- **Optimistic updates**: Update UI before server confirms
- **Background refetching**: Keep data fresh without user interaction
