import React from "react";

/**
 * Simple Counter App - Getting Started with React
 * Demonstrates:
 * - useState hook for state management
 * - Event handling with onClick
 * - Conditional rendering
 * - Basic component structure with TypeScript
 */

interface CounterProps {
	initialValue?: number;
	onMaxReached?: (value: number) => void;
	maxValue?: number;
}

export function Counter({
	initialValue = 0,
	onMaxReached,
	maxValue = 100,
}: CounterProps): JSX.Element {
	const [count, setCount] = React.useState(initialValue);
	const [history, setHistory] = React.useState<number[]>([initialValue]);

	const handleIncrement = () => {
		const newCount = count + 1;
		setCount(newCount);
		setHistory((prev) => [...prev, newCount]);

		if (newCount === maxValue) {
			onMaxReached?.(newCount);
		}
	};

	const handleDecrement = () => {
		const newCount = Math.max(0, count - 1);
		setCount(newCount);
		setHistory((prev) => [...prev, newCount]);
	};

	const handleReset = () => {
		setCount(initialValue);
		setHistory([initialValue]);
	};

	const isAtMax = count === maxValue;
	const isAtMin = count === 0;

	return (
		<div className="flex flex-col items-center justify-center min-h-screen bg-gray-100">
			<div className="bg-white rounded-lg shadow-lg p-8 w-96">
				<h1 className="text-3xl font-bold text-center mb-8">Counter App</h1>

				{/* Display current count */}
				<div className="bg-blue-50 rounded-lg p-6 mb-6">
					<p className="text-gray-600 text-center font-semibold">
						Current Count
					</p>
					<p className="text-5xl font-bold text-center text-blue-600">
						{count}
					</p>
					{isAtMax && (
						<p className="text-center text-red-500 font-semibold mt-2">
							Maximum reached!
						</p>
					)}
				</div>

				{/* Control buttons */}
				<div className="flex gap-4 mb-6">
					<button
						onClick={handleDecrement}
						disabled={isAtMin}
						className="flex-1 bg-red-500 hover:bg-red-600 disabled:bg-gray-300 text-white font-bold py-2 px-4 rounded transition"
					>
						−
					</button>
					<button
						onClick={handleReset}
						className="flex-1 bg-gray-500 hover:bg-gray-600 text-white font-bold py-2 px-4 rounded transition"
					>
						Reset
					</button>
					<button
						onClick={handleIncrement}
						disabled={isAtMax}
						className="flex-1 bg-green-500 hover:bg-green-600 disabled:bg-gray-300 text-white font-bold py-2 px-4 rounded transition"
					>
						+
					</button>
				</div>

				{/* History */}
				<div className="border-t pt-4">
					<h3 className="font-semibold text-gray-700 mb-2">History</h3>
					<div className="flex flex-wrap gap-2 max-h-20 overflow-y-auto">
						{history.map((value, index) => (
							<span
								key={index}
								className="bg-gray-200 px-3 py-1 rounded text-sm text-gray-700"
							>
								{value}
							</span>
						))}
					</div>
				</div>
			</div>
		</div>
	);
}

// Example usage
export default function App() {
	const handleMaxReached = (value: number) => {
		alert(`Counter reached maximum: ${value}`);
	};

	return (
		<Counter initialValue={0} maxValue={50} onMaxReached={handleMaxReached} />
	);
}
