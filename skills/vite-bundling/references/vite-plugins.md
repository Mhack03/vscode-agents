# Vite Plugin Development

## Creating Custom Plugins

```typescript
// plugin-example.ts
import { Plugin, ViteDevServer } from "vite";

export function myCustomPlugin(): Plugin {
	let config: ResolvedConfig;

	return {
		name: "my-custom-plugin",
		configResolved(resolvedConfig) {
			config = resolvedConfig;
		},
		resolveId(id) {
			if (id === "virtual-module") {
				return this.resolve(id);
			}
		},
		load(id) {
			if (id === "virtual-module") {
				return `export const msg = "Message from virtual module"`;
			}
		},
		transform(code, id) {
			if (id.endsWith(".custom")) {
				return {
					code: transformCustomFile(code),
					map: null,
				};
			}
		},
		transformIndexHtml(html) {
			return html.replace(
				/<title>(.*?)<\/title>/,
				`<title>${config.env.VITE_TITLE || "$1"}</title>`
			);
		},
		handleHotUpdate(ctx) {
			if (ctx.file.endsWith(".custom")) {
				console.log("Custom file updated:", ctx.file);
				ctx.server.ws.send({
					type: "custom",
					event: "special",
					data: null,
				});
				return [];
			}
		},
	};
}
```

## Common Plugins

```typescript
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import vue from "@vitejs/plugin-vue";
import compress from "vite-plugin-compression";
import { visualizer } from "rollup-plugin-visualizer";

export default defineConfig({
	plugins: [
		react(),
		compress({
			verbose: true,
			disable: false,
			threshold: 10240,
			algorithm: "gzip",
			ext: ".gz",
		}),
		visualizer({
			open: false,
			gzipSize: true,
		}),
	],
});
```

## Virtual Modules

```typescript
// virtual-module-plugin.ts
export function virtualModulePlugin() {
	const virtualModuleId = "virtual-env";
	const resolvedVirtualModuleId = "\0" + virtualModuleId;

	return {
		name: "virtual-env",
		resolveId(id) {
			if (id === virtualModuleId) {
				return resolvedVirtualModuleId;
			}
		},
		load(id) {
			if (id === resolvedVirtualModuleId) {
				return `export const env = ${JSON.stringify(process.env)}`;
			}
		},
	};
}

// Usage in code
import { env } from "virtual-env";
console.log(env.NODE_ENV);
```

## Framework-Specific Plugins

### React Plugin Options

```typescript
export default defineConfig({
	plugins: [
		react({
			// Use Babel for JSX transformation
			babel: {
				plugins: [["@babel/plugin-proposal-decorators", { legacy: true }]],
			},
			// Fast refresh configuration
			fastRefresh: true,
			// JSX is available globally
			jsxImportSource: "react",
		}),
	],
});
```

### Vue Plugin Options

```typescript
export default defineConfig({
	plugins: [
		vue({
			template: {
				compilerOptions: {
					whitespace: "condense",
				},
			},
		}),
	],
});
```
