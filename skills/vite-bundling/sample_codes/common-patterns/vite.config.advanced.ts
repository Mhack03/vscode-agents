import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import path from "path";

/**
 * Advanced Vite Configuration
 * Production-ready setup with optimization and monitoring
 */

export default defineConfig(({ command, mode }) => {
	const isDevelopment = command === "serve";
	const isProduction = mode === "production";
	const isStaging = mode === "staging";

	return {
		plugins: [react()],

		resolve: {
			alias: {
				"@": path.resolve(__dirname, "./src"),
				"@components": path.resolve(__dirname, "./src/components"),
				"@pages": path.resolve(__dirname, "./src/pages"),
				"@hooks": path.resolve(__dirname, "./src/hooks"),
				"@services": path.resolve(__dirname, "./src/services"),
				"@utils": path.resolve(__dirname, "./src/utils"),
				"@types": path.resolve(__dirname, "./src/types"),
				"@constants": path.resolve(__dirname, "./src/constants"),
			},
		},

		define: {
			__APP_VERSION__: JSON.stringify("1.0.0"),
			__BUILD_TIME__: JSON.stringify(new Date().toISOString()),
			__BUILD_MODE__: JSON.stringify(mode),
		},

		server: {
			port: 3000,
			strictPort: false,
			open: true,
			proxy: {
				"/api": {
					target: "http://localhost:5000",
					changeOrigin: true,
					rewrite: (path) => path.replace(/^\/api/, "/api/v1"),
				},
			},
			middlewareMode: false,
		},

		build: {
			target: "esnext",
			outDir: "dist",
			assetsDir: "assets",
			assetsInlineLimit: 4096,
			sourcemap: isStaging ? "hidden" : false,
			minify: isProduction ? "terser" : false,
			reportCompressedSize: true,
			chunkSizeWarningLimit: 500,

			rollupOptions: {
				output: {
					manualChunks: {
						vendor: ["react", "react-dom", "react-router-dom"],
						ui: ["@mui/material", "@mui/icons-material"],
					},
					entryFileNames: "js/[name]-[hash].js",
					chunkFileNames: "js/[name]-[hash].js",
					assetFileNames: "assets/[name]-[hash][extname]",
				},
			},

			terserOptions: {
				compress: {
					drop_console: isProduction,
					dead_code: true,
				},
			},
		},

		css: {
			preprocessorOptions: {
				scss: {
					additionalData: '@import "src/styles/variables.scss";',
				},
			},
		},

		ssr: {
			noExternal: ["some-ssr-library"],
		},
	};
});
