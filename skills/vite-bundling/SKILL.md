---
name: vite-bundling
description: Vite build optimization, configuration, and bundling strategies for React, Vue, and TypeScript projects. Use when setting up Vite projects, optimizing build performance, configuring dev server, managing code splitting, implementing Hot Module Replacement (HMR), or handling environment-specific builds. Covers plugins, rollup configuration, and production optimization.
license: Complete terms in LICENSE.txt
---

# Vite Bundling

Master Vite configuration, build optimization, and bundling strategies for high-performance modern web development.

## When to Use This Skill

- Setting up new Vite projects for React, Vue, or TypeScript
- Optimizing build performance and bundle size
- Configuring development server with HMR
- Implementing code splitting strategies
- Managing environment-specific builds and configurations
- Using Vite plugins for asset handling
- Optimizing production builds
- Debugging build issues and performance
- Configuring path aliases and imports
- CSS/SCSS/PostCSS configuration

## Key Concepts

### 1. Build Optimization

- **Tree Shaking**: Removing unused code
- **Lazy Loading**: Code splitting and dynamic imports
- **Minification**: Optimizing JavaScript and CSS
- **Asset Handling**: Images, fonts, and static files
- **Source Maps**: Debugging production builds

### 2. Configuration

- **Vite Config File**: vite.config.ts
- **Environment Setup**: .env files
- **Rollup Options**: Advanced bundling control
- **Plugin System**: Extending Vite functionality

### 3. Development Experience

- **HMR (Hot Module Replacement)**: Fast refresh
- **Dev Server**: Development server configuration
- **TypeScript Support**: Out-of-the-box
- **CSS Support**: Native CSS/SCSS/PostCSS

### 4. Production Optimization

- **Bundle Analysis**: Understanding bundle size
- **Preload/Prefetch**: Optimization hints
- **Compression**: GZIP and Brotli
- **Module Federation**: Sharing code between apps

### 5. Performance Monitoring

- **Build Speed**: Measuring and improving
- **Bundle Size**: Tracking and reducing
- **Load Time**: Analyzing page load performance

## Prerequisites

- Node.js 18.0.0+
- npm or yarn
- Basic understanding of JavaScript bundling
- TypeScript knowledge (recommended)

## Step-by-Step Workflows

### Setting Up a React + TypeScript Vite Project

```bash
# Create new project
npm create vite@latest my-app -- --template react-ts

# Install dependencies
cd my-app
npm install

# Start development server
npm run dev

# Build for production
npm run build
```

### Basic Vite Configuration

```typescript
// vite.config.ts
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import path from "path";

export default defineConfig({
	plugins: [react()],

	// Path alias configuration
	resolve: {
		alias: {
			"@": path.resolve(__dirname, "./src"),
			"@components": path.resolve(__dirname, "./src/components"),
			"@hooks": path.resolve(__dirname, "./src/hooks"),
			"@types": path.resolve(__dirname, "./src/types"),
		},
	},

	// Development server configuration
	server: {
		port: 3000,
		strictPort: false,
		open: true,
		proxy: {
			"/api": {
				target: "http://localhost:5000",
				changeOrigin: true,
				rewrite: (path) => path.replace(/^\/api/, ""),
			},
		},
	},

	// Build optimization
	build: {
		target: "esnext",
		minify: "terser",
		sourcemap: false,
		rollupOptions: {
			output: {
				manualChunks: {
					vendor: ["react", "react-dom"],
					ui: ["@mui/material"],
				},
			},
		},
	},

	// Environment variables
	define: {
		__APP_VERSION__: JSON.stringify("1.0.0"),
		__BUILD_TIME__: JSON.stringify(new Date().toISOString()),
	},
});
```

### Code Splitting Strategy

```typescript
// vite.config.ts - Advanced chunk configuration
export default defineConfig({
	build: {
		rollupOptions: {
			output: {
				manualChunks: {
					// Vendor libraries
					"vendor-react": ["react", "react-dom"],
					"vendor-ui": ["@mui/material", "@mui/icons-material"],

					// Feature-based chunks
					"feature-auth": [
						"./src/features/auth/pages",
						"./src/features/auth/hooks",
					],
					"feature-dashboard": [
						"./src/features/dashboard/pages",
						"./src/features/dashboard/components",
					],
				},
			},
		},
	},
});

// In components - lazy load routes
import { lazy, Suspense } from "react";

const Dashboard = lazy(() => import("./pages/Dashboard"));
const Settings = lazy(() => import("./pages/Settings"));
const Analytics = lazy(() => import("./pages/Analytics"));

// Dynamic chunk names in build output
const LazyComponent = lazy(
	() => import(/* @vite-ignore */ `./features/${featureName}.tsx`)
);
```

### Environment Configuration

```typescript
// vite.config.ts
export default defineConfig({
  // Environment-specific configuration
  server: {
    proxy: {
      '/api': {
        target: import.meta.env.VITE_API_URL,
        changeOrigin: true,
      },
    },
  },
});

// .env
VITE_API_URL=http://localhost:5000
VITE_APP_NAME=MyApp

// .env.production
VITE_API_URL=https://api.example.com
VITE_APP_NAME=MyApp Production

// Usage in application
console.log(import.meta.env.VITE_API_URL);
console.log(import.meta.env.MODE); // 'development' or 'production'
```

### Custom Plugin Creation

```typescript
// plugins/vite-plugin-compress.ts
import { Plugin } from "vite";
import zlib from "zlib";
import fs from "fs";
import path from "path";

export function vitePluginCompress(): Plugin {
	return {
		name: "vite-plugin-compress",

		apply: "build",

		async generateBundle(options, bundle) {
			// Compress output files
			for (const [fileName, asset] of Object.entries(bundle)) {
				if (typeof asset.source === "string" || Buffer.isBuffer(asset.source)) {
					const source = asset.source;
					const compressed = zlib.gzipSync(source);
					const gzipFileName = fileName + ".gz";

					this.emitFile({
						type: "asset",
						fileName: gzipFileName,
						source: compressed,
					});
				}
			}
		},
	};
}

// vite.config.ts
import { vitePluginCompress } from "./plugins/vite-plugin-compress";

export default defineConfig({
	plugins: [react(), vitePluginCompress()],
});
```

## Best Practices

1. **Use dynamic imports** for code splitting:

   ```typescript
   const Component = lazy(() => import("./Component"));
   ```

2. **Optimize vendor bundles** to cache across builds:

   ```typescript
   manualChunks: {
   	vendor: ["react", "react-dom"];
   }
   ```

3. **Enable source maps only in development**:

   ```typescript
   sourcemap: process.env.NODE_ENV === "development";
   ```

4. **Use path aliases** to reduce import verbosity:

   ```typescript
   alias: { '@': path.resolve(__dirname, './src') }
   ```

5. **Monitor bundle size** with `npm run build -- --report`

6. **Preload critical assets**:

   ```html
   <link rel="preload" href="/chunk.js" as="script" />
   ```

7. **Use modulePreload** for dependency discovery

8. **Configure proxy** for API development:

   ```typescript
   proxy: { '/api': { target: 'http://localhost:5000' } }
   ```

9. **Lazy load heavy libraries**:

   ```typescript
   const Editor = lazy(() => import("heavy-editor-lib"));
   ```

10. **Use proper rollup external** for library builds

## Common Configuration Patterns

See [vite-patterns.md](references/vite-patterns.md) for:

- Environment-specific configuration
- Advanced plugin development
- Build optimization techniques
- Performance monitoring setup
- Troubleshooting common issues

## Troubleshooting

| Issue                         | Solution                                                          |
| ----------------------------- | ----------------------------------------------------------------- |
| Build is too slow             | Enable `cacheDir`, use `manualChunks`, check plugin count         |
| Bundle size too large         | Analyze with `rollup-plugin-visualizer`, implement code splitting |
| HMR not working               | Check proxy config, verify port, clear browser cache              |
| Missing imports in production | Check `treeshake` config, verify external dependencies            |
| Alias path not resolving      | Check `resolve.alias` config, restart dev server                  |
| CSS not loading               | Verify CSS imports, check plugin order, ensure CSS file exists    |

## References

- [Official Vite Documentation](https://vitejs.dev)
- [Rollup Configuration](https://rollupjs.org/configuration-options/)
- [Vite Configuration Patterns](references/vite-patterns.md)
- [Performance Optimization Guide](references/vite-optimization.md)
- [Plugin Development](references/vite-plugins.md)
