---
name: frontend-api-integration
description: Patterns for integrating React and Blazor frontends with .NET Web API backends. Use when consuming REST APIs from React or Blazor, implementing error handling, request/response interceptors, authentication token management, CORS configuration, type-safe API clients, caching strategies, or real-time updates. Covers HTTP clients, type generation, and API contract management.
license: Complete terms in LICENSE.txt
---

# Frontend API Integration

Master patterns and best practices for integrating React and Blazor frontends with .NET Web API backends.

## When to Use This Skill

- Setting up React/Blazor to consume .NET Web API
- Implementing HTTP client configuration and interceptors
- Managing authentication tokens and session handling
- Handling API errors and timeout scenarios
- Type-safe API client generation from OpenAPI/Swagger
- CORS and security configuration
- Request/response logging and debugging
- Caching API responses
- Real-time communication with SignalR
- File upload/download handling

## Core Concepts

| Concept            | Role                                                         |
| ------------------ | ------------------------------------------------------------ |
| **REST API**       | GET, POST, PUT, DELETE operations with proper HTTP semantics |
| **HTTP Clients**   | Axios (React), HttpClient (Blazor), or Fetch API             |
| **Authentication** | Bearer tokens, refresh tokens, token storage strategies      |
| **Type Safety**    | Shared DTOs, TypeScript generics for response wrappers       |
| **Caching**        | Response caching, stale-while-revalidate, cache invalidation |
| **Error Handling** | HTTP errors, network failures, validation errors, timeouts   |
| **Real-Time**      | SignalR, WebSockets, server-sent events, event streaming     |

## Prerequisites

- .NET Web API backend (ASP.NET Core 8+)
- React with TypeScript or Blazor frontend
- Understanding of HTTP protocols
- Familiarity with async/await patterns
- Basic CORS knowledge

## Quick Start

### React Integration

Use [http-client-setup.md](references/http-client-setup.md) to:

- Set up Axios with base configuration
- Implement request/response interceptors
- Handle authentication token injection
- Implement automatic token refresh on 401 errors

### Type-Safe Services

Use [type-safe-services.md](references/type-safe-services.md) to:

- Define API DTOs and response contracts
- Create service layers for API calls
- Organize services by domain
- Implement pagination and filtering

### Data Fetching with React

Use [tanstack-query.md](references/tanstack-query.md) for:

- Query hook patterns with TanStack Query
- Automatic caching and deduplication
- Mutation handling and cache invalidation
- Optimistic updates pattern

### Blazor Integration

Use [blazor-http-client.md](references/blazor-http-client.md) for:

- ApiClient service in C#
- Component usage examples
- Dependency injection setup
- Built-in token refresh handling

## Best Practices

1. **Use typed DTOs** for all API contracts between frontend and backend
2. **Implement proper error handling** with user-friendly messages
3. **Use interceptors** for cross-cutting concerns (auth, logging, error handling)
4. **Cache API responses** to reduce redundant server calls
5. **Validate API responses** before using in UI render logic
6. **Use HTTP-only cookies** for storing sensitive authentication tokens
7. **Keep base URLs configurable** per environment (dev, staging, prod)
8. **Implement request debouncing** for search/autocomplete operations
9. **Handle network timeouts** gracefully with retry logic
10. **Log API requests/responses** for debugging production issues

## CORS Configuration

See [cors-setup.md](references/cors-setup.md) for:

- Configuring CORS policies in ASP.NET Core
- Handling preflight requests
- Managing credentials in cross-origin requests
- Security considerations

## Troubleshooting

| Issue                             | Solution                                                             |
| --------------------------------- | -------------------------------------------------------------------- |
| 401 Unauthorized on every request | Check token refresh logic in interceptors; verify token storage      |
| CORS errors in browser            | Ensure backend CORS policy allows frontend origin; check credentials |
| Stale data in UI                  | Implement cache invalidation on mutations or use staleTime properly  |
| Memory leaks in React             | Cancel pending requests on component unmount; clean up subscriptions |
| Type errors with API responses    | Update types when backend API contract changes; regenerate types     |

## References

- [HTTP Client Setup](references/http-client-setup.md)
- [Type-Safe Services](references/type-safe-services.md)
- [TanStack Query Integration](references/tanstack-query.md)
- [Blazor HTTP Client](references/blazor-http-client.md)
- [CORS Setup Guide](references/cors-setup.md)
- [Real-Time Communication](references/signalr-integration.md)
- [Error Handling Strategies](references/error-handling.md)
- [Authentication & Authorization](references/auth-integration.md)
