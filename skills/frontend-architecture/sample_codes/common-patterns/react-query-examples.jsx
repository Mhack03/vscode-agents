import React from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";

export function UserProfile({ userId }) {
	const {
		data: user,
		isLoading,
		error,
	} = useQuery({
		queryKey: ["user", userId],
		queryFn: () => fetch(`/api/users/${userId}`).then((r) => r.json()),
	});

	if (isLoading) return <div>Loading...</div>;
	if (error) return <div>Error: {String(error)}</div>;

	return <div>{user.name}</div>;
}

export function UpdateProfileForm({ userId }) {
	const queryClient = useQueryClient();

	const mutation = useMutation({
		mutationFn: (data) =>
			fetch(`/api/users/${userId}`, {
				method: "POST",
				body: JSON.stringify(data),
			}),
		onSuccess: () =>
			queryClient.invalidateQueries({ queryKey: ["user", userId] }),
	});

	const handleSubmit = (data) => mutation.mutate(data);

	return (
		<form
			onSubmit={(e) => {
				e.preventDefault();
				handleSubmit({ name: "New" });
			}}
		>
			...
		</form>
	);
}

export default UserProfile;
