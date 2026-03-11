# Vite Optimization

## Bundle Analysis

```typescript
// vite.config.ts
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { visualizer } from "rollup-plugin-visualizer";

export default defineConfig({
	plugins: [
		react(),
		visualizer({
			open: true,
			gzipSize: true,
			brotliSize: true,
		}),
	],
});
```

## Code Splitting Strategies

### Route-Based Splitting

```typescript
// Automatically split by route
export default defineConfig({
	build: {
		rollupOptions: {
			output: {
				manualChunks(id) {
					if (id.includes("node_modules")) {
						return "vendor";
					}
					if (id.includes("src/features/auth")) {
						return "auth";
					}
					if (id.includes("src/features/dashboard")) {
						return "dashboard";
					}
				},
			},
		},
	},
});
```

### Vendor Splitting

```typescript
manualChunks: {
  'vendor-react': ['react', 'react-dom', 'react-router-dom'],
  'vendor-ui': ['@mui/material', '@mui/icons-material'],
  'vendor-utils': ['lodash', 'date-fns', 'axios'],
}
```

## Minification Configuration

```typescript
export default defineConfig({
	build: {
		// Use terser for minification
		minify: "terser",
		terserOptions: {
			compress: {
				drop_console: true,
				dead_code: true,
			},
			output: {
				comments: false,
			},
		},
		// Or use esbuild (faster)
		minify: "esbuild",
	},
});
```

## Preload and Prefetch

```typescript
// In generated HTML
<link rel="preload" href="/chunk.js" as="script">
<link rel="prefetch" href="/future-page.js">

// Vite generates automatically for:
// - Entry point dependencies
// - Critical chunks
```

## Dynamic Import Optimization

```typescript
// Instead of:
const Module = lazy(() => import('./HeavyModule'));

// Use named imports when possible:
const { Component } = lazy(() => import('./Module'));

// Or with retry logic:
const lazy Retry = (importFunc, retries = 3) => {
  return lazy(() =>
    importFunc().catch((error) => {
      if (retries > 0) {
        return new Promise((resolve) => {
          setTimeout(() => resolve(lazyRetry(importFunc, retries - 1)), 1000);
        });
      }
      throw error;
    })
  );
};
```

## Performance Metrics

```typescript
// Measure build time
export default defineConfig({
	plugins: [
		{
			name: "build-time-plugin",
			apply: "build",
			async generateBundle() {
				const startTime = performance.now();
				// ... bundling happens
				const endTime = performance.now();
				console.log(`Build completed in ${endTime - startTime}ms`);
			},
		},
	],
});

// Chrome DevTools - Lighthouse integration
// Measure Core Web Vitals
export function measureCWV() {
	new PerformanceObserver((list) => {
		for (const entry of list.getEntries()) {
			console.log(`${entry.name}: ${entry.value}`);
		}
	}).observe({
		type: "largest-contentful-paint",
		buffered: true,
	});
}
```
