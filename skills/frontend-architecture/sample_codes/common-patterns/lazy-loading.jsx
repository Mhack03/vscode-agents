import React, { lazy, Suspense } from "react";

const HeavyChart = lazy(() => import("./HeavyChart"));

export default function Analytics({ data }) {
	return (
		<div>
			<h1>Analytics</h1>
			<Suspense fallback={<div>Loading chart...</div>}>
				<HeavyChart data={data} />
			</Suspense>
		</div>
	);
}
