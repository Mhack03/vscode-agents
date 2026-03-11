import { render, screen, fireEvent } from "@testing-library/react";
import React from "react";

function Counter() {
	const [n, setN] = React.useState(0);
	return <button onClick={() => setN(n + 1)}>Count {n}</button>;
}

test("increments counter", () => {
	render(<Counter />);
	const btn = screen.getByText(/Count 0/);
	fireEvent.click(btn);
	expect(screen.getByText(/Count 1/)).toBeInTheDocument();
});
