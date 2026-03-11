import React from "react";
import { FixedSizeList as List } from "react-window";

export default function LargeList({ items }) {
	const Row = ({ index, style }) => (
		<div style={style}>{items[index].name}</div>
	);

	return (
		<List height={600} itemCount={items.length} itemSize={50} width="100%">
			{Row}
		</List>
	);
}
