# State Management

When to use local state, Context, or a state library:

- Local: `useState` for UI-only concerns.
- Context: lightweight cross-tree values (theme, auth) — avoid large mutable objects here.
- Library: Zustand/Redux for complex global state / cross-cutting actions.

Patterns: normalized state, selectors, and keep server-state in a dedicated cache (React Query).

See `../sample_codes/common-patterns/state-store.js` for a minimal Zustand pattern.
