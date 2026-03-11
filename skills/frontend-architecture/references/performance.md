# Performance

High-level performance guidance:

- Measure before optimizing (Lighthouse, Web Vitals).
- Optimize critical path: prioritize HTML/CSS, defer non-critical JS.
- Use code-splitting, tree-shaking, and lazy loading for large components.
- Cache and debounce expensive operations; memoize heavy render computations.

See `../sample_codes/common-patterns/lazy-loading.jsx` and `memoization.jsx` for examples.
