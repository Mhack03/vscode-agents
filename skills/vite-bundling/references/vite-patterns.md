# Vite Configuration Patterns

## Environment-Specific Configuration

```typescript
// vite.config.ts
import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";
import path from "path";

export default defineConfig(({ command, mode }) => {
	// Load environment variables
	const env = loadEnv(mode, process.cwd(), "");

	const config = {
		plugins: [react()],
		resolve: {
			alias: {
				"@": path.resolve(__dirname, "./src"),
			},
		},
	};

	// Development configuration
	if (command === "serve") {
		return {
			...config,
			server: {
				port: parseInt(env.VITE_DEV_PORT) || 3000,
				proxy: {
					"/api": {
						target: env.VITE_API_URL,
						changeOrigin: true,
						rewrite: (path) => path.replace(/^\/api/, ""),
					},
				},
				hmr: {
					protocol: "ws",
					host: "localhost",
					port: 5173,
				},
			},
		};
	}

	// Production configuration
	return {
		...config,
		build: {
			target: "esnext",
			minify: "terser",
			sourcemap: mode === "staging",
			outDir: "dist",
			reportCompressedSize: true,
			rollupOptions: {
				output: {
					manualChunks: {
						vendor: ["react", "react-dom"],
					},
				},
			},
		},
	};
});
```

## Monorepo Configuration

```typescript
// Root vite.config.ts for monorepo
const packages = ["core", "ui", "utils"];

export default defineConfig({
	build: {
		rollupOptions: {
			input: packages.reduce((acc, pkg) => {
				acc[pkg] = path.resolve(__dirname, `packages/${pkg}/src/index.ts`);
				return acc;
			}, {}),
			output: {
				dir: "dist",
				format: "es",
			},
			external: ["react", "react-dom"],
		},
	},
});
```

## Library Build Configuration

```typescript
// vite.config.ts for library
export default defineConfig({
	build: {
		lib: {
			entry: path.resolve(__dirname, "src/index.ts"),
			name: "MyLibrary",
			fileName: (format) => `my-library.${format}.js`,
		},
		rollupOptions: {
			external: ["react", "react-dom"],
			output: [
				{
					format: "es",
					entryFileNames: "[name].mjs",
					dir: "dist/es",
				},
				{
					format: "cjs",
					entryFileNames: "[name].cjs",
					dir: "dist/cjs",
				},
				{
					format: "umd",
					name: "MyLibrary",
					entryFileNames: "[name].umd.js",
					dir: "dist/umd",
					globals: {
						react: "React",
						"react-dom": "ReactDOM",
					},
				},
			],
		},
	},
});
```

## Advanced Proxy Configuration

```typescript
export default defineConfig({
	server: {
		proxy: {
			// Simple proxy
			"/api": "http://localhost:5000",

			// Proxy with rewrite
			"/auth": {
				target: "http://auth-server:3001",
				changeOrigin: true,
				rewrite: (path) => path.replace(/^\/auth/, "/api"),
			},

			// Proxy with custom logic
			"/graphql": {
				target: "http://graphql:4000",
				changeOrigin: true,
				configure(proxy, options) {
					proxy.on("error", (err, req, res) => {
						console.log("Proxy error:", err);
						res.writeHead(500, { "Content-Type": "text/plain" });
						res.end("Proxy error:" + err);
					});
				},
			},

			// WebSocket proxy
			"/ws": {
				target: "ws://localhost:8080",
				changeOrigin: true,
				ws: true,
			},
		},
	},
});
```

## CSS/SASS Processing

```typescript
export default defineConfig({
	css: {
		preprocessorOptions: {
			scss: {
				additionalData: `
          @import "src/variables.scss";
          @import "src/mixins.scss";
        `,
				quietDeps: true,
			},
			less: {
				math: "parens-division",
			},
		},
		postcss: {
			plugins: [
				require("autoprefixer"),
				require("postcss-preset-env")({
					stage: 4,
				}),
			],
		},
		modules: {
			localsConvention: "camelCase",
		},
	},
});
```

## Asset Handling Configuration

```typescript
export default defineConfig({
	build: {
		assetsDir: "assets",
		assetsInlineLimit: 4096,
		rollupOptions: {
			output: {
				assetFileNames: (assetInfo) => {
					if (assetInfo.name && assetInfo.name.endsWith(".css")) {
						return "styles/[name]-[hash][extname]";
					} else if (
						assetInfo.name &&
						/\.(png|jpg|jpeg|svg|gif|webp)$/.test(assetInfo.name)
					) {
						return "images/[name]-[hash][extname]";
					} else if (
						assetInfo.name &&
						/\.(woff|woff2|eot|ttf|otf)$/.test(assetInfo.name)
					) {
						return "fonts/[name]-[hash][extname]";
					}
					return "assets/[name]-[hash][extname]";
				},
			},
		},
	},
});
```
