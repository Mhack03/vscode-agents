import React, { memo, useMemo, useCallback, useState } from "react";

const ExpensiveComponent = memo(function ExpensiveComponent({ value }) {
	const derived = useMemo(() => heavyComputation(value), [value]);
	return <div>{derived}</div>;
});

function heavyComputation(v) {
	// placeholder for expensive work
	let s = 0;
	for (let i = 0; i < 100000; i++) s += (i * v) % 7;
	return s;
}

export function ProductList({ products, filter }) {
	const filtered = useMemo(
		() => products.filter((p) => p.category === filter),
		[products, filter]
	);
	const [count, setCount] = useState(0);

	const handleInc = useCallback(() => setCount((c) => c + 1), []);

	return (
		<div>
			<button onClick={handleInc}>Inc {count}</button>
			{filtered.map((p) => (
				<ExpensiveComponent key={p.id} value={p.id} />
			))}
		</div>
	);
}
