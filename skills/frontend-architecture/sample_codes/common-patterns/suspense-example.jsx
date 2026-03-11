import React, { Suspense } from "react";
import { useQuery } from "@tanstack/react-query";

function Profile() {
	const { data } = useQuery({
		queryKey: ["user"],
		queryFn: () => fetch("/api/user").then((r) => r.json()),
		suspense: true,
	});
	return <div>{data.name}</div>;
}

export default function SuspenseExample() {
	return (
		<Suspense fallback={<div>Loading...</div>}>
			<Profile />
		</Suspense>
	);
}
