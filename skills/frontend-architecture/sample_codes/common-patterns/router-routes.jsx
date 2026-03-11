import React, { lazy } from "react";
import { createBrowserRouter } from "react-router-dom";

const Home = lazy(() => import("./pages/Home"));
const Products = lazy(() => import("./pages/Products"));
const ProductDetail = lazy(() => import("./pages/ProductDetail"));

export const router = createBrowserRouter([
	{ path: "/", element: <Home /> },
	{
		path: "/products",
		element: <Products />,
		loader: async () => fetch("/api/products").then((r) => r.json()),
	},
	{
		path: "/products/:id",
		element: <ProductDetail />,
		loader: async ({ params }) =>
			fetch(`/api/products/${params.id}`).then((r) => r.json()),
	},
]);

export default router;
