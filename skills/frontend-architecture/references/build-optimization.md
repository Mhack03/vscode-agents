# Build & Bundle Optimization

Tips for build-time optimizations and bundle size reduction:

- Prefer tree-shakable packages (lodash-es), use ESM where possible.
- Configure manualChunks to split vendor and heavy libraries.
- Use bundle analyzers and sourcemaps to identify large modules.

Example config snippets are provided in `../sample_codes/common-patterns/bundle-config.js`.
