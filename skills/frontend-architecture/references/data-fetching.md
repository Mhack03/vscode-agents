# Data Fetching Patterns

Recommendations for server vs client state and common libraries:

- Use React Query / SWR for server state: caching, background refresh, retries, and pagination.
- Keep pagination and filtering server-driven where possible; use optimistic updates for UX.
- Centralize fetch logic into small adapter functions to simplify testing and retry behavior.

See `../sample_codes/common-patterns/virtualized-list.jsx` for combining server paging with virtualization.
