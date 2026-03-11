import React from "react";
import { Link } from "react-router-dom";

export default function PrefetchLink({ to, children, prefetch }) {
	return (
		<Link
			to={to}
			onMouseEnter={() => {
				if (prefetch) prefetch();
			}}
		>
			{children}
		</Link>
	);
}
