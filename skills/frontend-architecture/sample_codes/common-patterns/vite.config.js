import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { visualizer } from "rollup-plugin-visualizer";

export default defineConfig({
	plugins: [react(), visualizer({ open: false })],
	build: {
		rollupOptions: {
			output: {
				manualChunks(id) {
					if (id.includes("node_modules")) {
						if (id.includes("react")) return "vendor-react";
						return "vendor";
					}
				},
			},
		},
	},
});
