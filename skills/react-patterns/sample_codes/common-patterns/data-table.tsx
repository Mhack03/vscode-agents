import React from "react";

/**
 * Reusable Data Table Component
 * Demonstrates:
 * - Generic component with TypeScript
 * - Complex state management
 * - Sorting and pagination
 * - Search/filter functionality
 * - Memoization for performance
 */

export interface Column<T> {
	key: keyof T;
	label: string;
	sortable?: boolean;
	render?: (value: T[keyof T], row: T) => React.ReactNode;
}

interface DataTableProps<T extends Record<string, any>> {
	data: T[];
	columns: Column<T>[];
	pageSize?: number;
	onRowClick?: (row: T) => void;
	searchableFields?: (keyof T)[];
}

interface SortState {
	field: string;
	direction: "asc" | "desc";
}

export function DataTable<T extends Record<string, any>>({
	data,
	columns,
	pageSize = 10,
	onRowClick,
	searchableFields = [],
}: DataTableProps<T>): JSX.Element {
	const [currentPage, setCurrentPage] = React.useState(1);
	const [sortState, setSortState] = React.useState<SortState | null>(null);
	const [searchTerm, setSearchTerm] = React.useState("");

	// Filtered data
	const filteredData = React.useMemo(() => {
		if (!searchTerm || searchableFields.length === 0) return data;

		return data.filter((row) =>
			searchableFields.some((field) => {
				const value = String(row[field]).toLowerCase();
				return value.includes(searchTerm.toLowerCase());
			})
		);
	}, [data, searchTerm, searchableFields]);

	// Sorted data
	const sortedData = React.useMemo(() => {
		if (!sortState) return filteredData;

		return [...filteredData].sort((a, b) => {
			const aValue = a[sortState.field as keyof T];
			const bValue = b[sortState.field as keyof T];

			if (aValue < bValue) return sortState.direction === "asc" ? -1 : 1;
			if (aValue > bValue) return sortState.direction === "asc" ? 1 : -1;
			return 0;
		});
	}, [filteredData, sortState]);

	// Paginated data
	const paginatedData = React.useMemo(() => {
		const startIndex = (currentPage - 1) * pageSize;
		return sortedData.slice(startIndex, startIndex + pageSize);
	}, [sortedData, currentPage, pageSize]);

	const totalPages = Math.ceil(sortedData.length / pageSize);

	const handleSort = React.useCallback((field: string) => {
		setSortState((prev) => {
			if (prev?.field === field) {
				return {
					field,
					direction: prev.direction === "asc" ? "desc" : "asc",
				};
			}
			return { field, direction: "asc" };
		});
		setCurrentPage(1);
	}, []);

	const handleSearch = React.useCallback((term: string) => {
		setSearchTerm(term);
		setCurrentPage(1);
	}, []);

	return (
		<div className="w-full">
			{/* Search bar */}
			{searchableFields.length > 0 && (
				<div className="mb-4">
					<input
						type="text"
						placeholder="Search..."
						value={searchTerm}
						onChange={(e) => handleSearch(e.target.value)}
						className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
					/>
				</div>
			)}

			{/* Table */}
			<div className="overflow-x-auto border border-gray-300 rounded-lg">
				<table className="w-full">
					<thead className="bg-gray-100 border-b border-gray-300">
						<tr>
							{columns.map((column) => (
								<th
									key={String(column.key)}
									onClick={() =>
										column.sortable && handleSort(String(column.key))
									}
									className={`px-6 py-3 text-left text-sm font-semibold text-gray-700 ${
										column.sortable ? "cursor-pointer hover:bg-gray-200" : ""
									}`}
								>
									<div className="flex items-center gap-2">
										{column.label}
										{column.sortable &&
											sortState?.field === String(column.key) && (
												<span className="text-xs">
													{sortState.direction === "asc" ? "↑" : "↓"}
												</span>
											)}
									</div>
								</th>
							))}
						</tr>
					</thead>
					<tbody>
						{paginatedData.length === 0 ? (
							<tr>
								<td
									colSpan={columns.length}
									className="px-6 py-8 text-center text-gray-500"
								>
									No data found
								</td>
							</tr>
						) : (
							paginatedData.map((row, rowIndex) => (
								<tr
									key={rowIndex}
									onClick={() => onRowClick?.(row)}
									className={`border-b border-gray-200 ${
										onRowClick ? "cursor-pointer hover:bg-gray-50" : ""
									}`}
								>
									{columns.map((column) => (
										<td
											key={String(column.key)}
											className="px-6 py-4 text-sm text-gray-700"
										>
											{column.render
												? column.render(row[column.key], row)
												: String(row[column.key])}
										</td>
									))}
								</tr>
							))
						)}
					</tbody>
				</table>
			</div>

			{/* Pagination */}
			{totalPages > 1 && (
				<div className="mt-4 flex items-center justify-between">
					<button
						onClick={() => setCurrentPage((prev) => Math.max(1, prev - 1))}
						disabled={currentPage === 1}
						className="px-4 py-2 bg-blue-500 text-white rounded disabled:bg-gray-300"
					>
						Previous
					</button>
					<span className="text-sm text-gray-600">
						Page {currentPage} of {totalPages}
					</span>
					<button
						onClick={() =>
							setCurrentPage((prev) => Math.min(totalPages, prev + 1))
						}
						disabled={currentPage === totalPages}
						className="px-4 py-2 bg-blue-500 text-white rounded disabled:bg-gray-300"
					>
						Next
					</button>
				</div>
			)}
		</div>
	);
}

// Example usage
interface User {
	id: number;
	name: string;
	email: string;
	department: string;
	joinDate: string;
}

export function UserTableExample() {
	const users: User[] = [
		{
			id: 1,
			name: "John Doe",
			email: "john@example.com",
			department: "Engineering",
			joinDate: "2023-01-15",
		},
		{
			id: 2,
			name: "Jane Smith",
			email: "jane@example.com",
			department: "Product",
			joinDate: "2023-02-20",
		},
		{
			id: 3,
			name: "Bob Wilson",
			email: "bob@example.com",
			department: "Engineering",
			joinDate: "2023-03-10",
		},
		// Add more users...
	];

	const columns: Column<User>[] = [
		{ key: "name", label: "Name", sortable: true },
		{ key: "email", label: "Email", sortable: true },
		{ key: "department", label: "Department", sortable: true },
		{ key: "joinDate", label: "Join Date", sortable: true },
	];

	return (
		<DataTable<User>
			data={users}
			columns={columns}
			pageSize={5}
			searchableFields={["name", "email", "department"]}
			onRowClick={(user) => console.log("Selected user:", user)}
		/>
	);
}
