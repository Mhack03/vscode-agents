import React from "react";

// Presentational component - focused on rendering
export function UserProfile({ user }) {
	return (
		<div className="user-profile">
			<h2>{user.name}</h2>
			<p>{user.email}</p>
		</div>
	);
}

// Container component - orchestrates data fetching and state
export function UserDashboard({ userId }) {
	const [user, setUser] = React.useState(null);

	React.useEffect(() => {
		let mounted = true;
		fetch(`/api/users/${userId}`)
			.then((r) => r.json())
			.then((data) => mounted && setUser(data));
		return () => (mounted = false);
	}, [userId]);

	if (!user) return <div>Loading...</div>;
	return (
		<div>
			<UserProfile user={user} />
		</div>
	);
}

export default UserDashboard;
