// Example manualChunks config for Vite/Rollup
export default {
	build: {
		rollupOptions: {
			output: {
				manualChunks: {
					vendor: ["react", "react-dom"],
					ui: ["@radix-ui/react-dialog"],
				},
			},
		},
	},
};
