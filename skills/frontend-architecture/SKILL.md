---
name: frontend-architecture
description: Build scalable React web applications with component architecture, state management, performance optimization, and Core Web Vitals. Use when designing application structure, optimizing bundle size, choosing state management, improving performance metrics (LCP/FID/CLS), lazy loading, code splitting, or debugging rendering issues. Covers composition, memoization, React Query, virtualization, image optimization, and accessibility patterns.
license: Complete terms in LICENSE.txt
---

# Frontend Architecture & Performance

Best practices for building scalable React applications. Key topics: component composition, state management (useState, Context, React Query, Zustand), code splitting, memoization, virtualization, image optimization, Core Web Vitals, and accessibility.

## Prerequisites

Node.js 16+, npm/yarn, a build tool (Vite/Webpack/Next.js), knowledge of React and HTTP APIs.

**Quick install:** `npm install react react-dom vite @tanstack/react-query zustand`

## Core Patterns

**1. Component Architecture** — Favor composition, keep components small (<200 lines), single responsibility. See [component-architecture.md](./references/component-architecture.md)

**2. State Management** — Match tool to scale: Small = `useState`; Medium = Context/Zustand; Large = React Query + Redux. See [state-management.md](./references/state-management.md)

**3. Performance** — Prioritize: code splitting → image optimization → memoization → virtualization → bundle analysis. See [performance.md](./references/performance.md) and [build-optimization.md](./references/build-optimization.md)

**4. Data Fetching** — Use React Query/SWR for automatic caching, background refresh, optimistic updates, error handling. See [data-fetching.md](./references/data-fetching.md)

**5. Accessibility** — Semantic HTML, ARIA attributes, keyboard navigation. See [WCAG 2.1](https://www.w3.org/WAI/WCAG21/quickref/) and [React docs](https://react.dev/learn/accessibility)

## Troubleshooting

| Problem                                      | Root Cause                                         | Solution                                                                 |
| -------------------------------------------- | -------------------------------------------------- | ------------------------------------------------------------------------ |
| **Large initial bundle**                     | Missing code splitting; vendor bloat               | Analyze with `source-map-explorer`, split routes, extract vendor chunks  |
| **Slow LCP (Largest Contentful Paint)**      | Unoptimized images; render-blocking JS             | Use `<picture>`, WebP, lazy loading; code split above-fold content       |
| **Frequent re-renders / janky interactions** | Missing memoization; re-creating objects in render | Apply `memo()`, `useMemo()`, `useCallback()`; lift state appropriately   |
| **Large lists slow or freeze**               | DOM rendering every item                           | Use virtualization: `react-window` or `react-virtual`                    |
| **Hydration mismatch**                       | Server and client render different HTML            | Ensure consistent date/time handling, no browser-only code in first pass |
| **Memory leaks / stale queries**             | Uncleared event listeners; old query cache         | Clean up in `useEffect` return; reset React Query cache on unmount       |

## Code Examples

Runnable code and templates in `sample_codes/`:

- [setup.md](./sample_codes/getting-started/setup.md) — Project initialization
- [component-structure.jsx](./sample_codes/common-patterns/component-structure.jsx) — Component patterns
- [state-store.js](./sample_codes/common-patterns/state-store.js) — State management
- [lazy-loading.jsx](./sample_codes/common-patterns/lazy-loading.jsx) — Code splitting
- [memoization.jsx](./sample_codes/common-patterns/memoization.jsx) — Performance
- [react-query-examples.jsx](./sample_codes/common-patterns/react-query-examples.jsx) — Server state
- [error-boundary.jsx](./sample_codes/common-patterns/error-boundary.jsx) — Error handling
- [vite.config.js](./sample_codes/common-patterns/vite.config.js) — Build config
- [core-web-vitals.md](./sample_codes/common-patterns/core-web-vitals.md) — Performance metrics
