---
name: react-patterns
description: Advanced React component architecture, hooks patterns, and state management strategies. Use when building React components, implementing custom hooks, managing component state with Context API or Redux, optimizing performance, handling side effects, or designing scalable component hierarchies. Covers functional components, hooks best practices, render optimization, and TypeScript integration.
license: Complete terms in LICENSE.txt
---

# React Patterns

Master React component architecture, modern hooks patterns, and state management strategies for building scalable, maintainable applications.

## When to Use This Skill

- Creating reusable React components with TypeScript
- Implementing custom hooks for shared logic
- Managing application state (Context API, Redux patterns)
- Optimizing component performance and re-renders
- Handling side effects and data fetching
- Designing component hierarchies and composition patterns
- Building form components with validation
- Implementing error boundaries and suspense patterns
- Testing React components with isolation

## Key Concepts

### 1. Component Patterns

- **Functional Components**: Modern React with hooks (no class components)
- **Composition**: Props, children, render props, component injection
- **Controlled vs Uncontrolled**: Form handling patterns

### 2. Hooks Fundamentals

- **useState**: Local state management
- **useEffect**: Side effects and lifecycle management
- **useContext**: Consuming context without prop drilling
- **useReducer**: Complex state logic management
- **useCallback & useMemo**: Performance optimization
- **Custom Hooks**: Extracting reusable logic

### 3. State Management

- **Local State**: Component-level with useState
- **Context API**: Global state without libraries
- **Side Effects**: useEffect dependency arrays and cleanup
- **Derived State**: Computing values from props/state

### 4. Performance Optimization

- **Memoization**: React.memo, useMemo, useCallback
- **Code Splitting**: Lazy loading with React.lazy & Suspense
- **List Rendering**: Key prop and key generation strategies
- **Re-render Tracking**: Understanding when components update

### 5. TypeScript with React

- **Component Props**: Proper typing with interface/type
- **Event Handlers**: Typed event callbacks
- **Refs**: useRef with proper typing
- **Generic Components**: TypeScript generics for flexible components

## Prerequisites

- React 18+
- Node.js and npm/yarn
- TypeScript understanding (recommended)
- Vite or similar build tool configured

## Step-by-Step Workflows

### Creating a Reusable Component

```typescript
// Step 1: Define props interface
interface ButtonProps {
  label: string;
  onClick: (event: React.MouseEvent<HTMLButtonElement>) => void;
  variant?: 'primary' | 'secondary' | 'danger';
  disabled?: boolean;
  className?: string;
}

// Step 2: Create component with proper types
export const Button: React.FC<ButtonProps> = ({
  label,
  onClick,
  variant = 'primary',
  disabled = false,
  className = '',
}) => {
  const variantClasses = {
    primary: 'bg-blue-600 text-white',
    secondary: 'bg-gray-200 text-gray-800',
    danger: 'bg-red-600 text-white',
  };

  return (
    <button
      onClick={onClick}
      disabled={disabled}
      className={`px-4 py-2 rounded ${variantClasses[variant]} ${className}`}
    >
      {label}
    </button>
  );
};
```

### Implementing Custom Hooks

```typescript
// Custom hook for data fetching
interface UseFetchState<T> {
	data: T | null;
	loading: boolean;
	error: Error | null;
}

export function useFetch<T>(url: string): UseFetchState<T> {
	const [state, setState] = React.useState<UseFetchState<T>>({
		data: null,
		loading: true,
		error: null,
	});

	React.useEffect(() => {
		let isMounted = true;

		const fetchData = async () => {
			try {
				const response = await fetch(url);
				if (!response.ok) throw new Error("Failed to fetch");
				const json = await response.json();
				if (isMounted) {
					setState({ data: json, loading: false, error: null });
				}
			} catch (error) {
				if (isMounted) {
					setState({ data: null, loading: false, error: error as Error });
				}
			}
		};

		fetchData();
		return () => {
			isMounted = false;
		};
	}, [url]);

	return state;
}
```

### Managing State with Context API

```typescript
interface AppContextType {
  user: User | null;
  setUser: (user: User | null) => void;
  loading: boolean;
}

const AppContext = React.createContext<AppContextType | undefined>(undefined);

export function AppProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = React.useState<User | null>(null);
  const [loading, setLoading] = React.useState(false);

  const value = React.useMemo(() => ({ user, setUser, loading }), [user, loading]);

  return (
    <AppContext.Provider value={value}>
      {children}
    </AppContext.Provider>
  );
}

export function useApp() {
  const context = React.useContext(AppContext);
  if (context === undefined) {
    throw new Error('useApp must be used within AppProvider');
  }
  return context;
}
```

## Best Practices

1. **Always memoize callbacks** passed to child components
2. **Specify dependency arrays** in useEffect; use ESLint rules
3. **Avoid prop drilling** - use Context API or custom hooks instead
4. **Type everything** in TypeScript for better IDE support
5. **Extract custom hooks** when logic is reused across components
6. **Use React.memo** for expensive child components
7. **Handle loading and error states** explicitly
8. **Clean up side effects** in useEffect return function
9. **Use keys properly** in lists (unique, stable, not index)
10. **Test component behavior** in isolation with React Testing Library

## Common Patterns Reference

See [react-component-patterns.md](references/react-component-patterns.md) for:

- Form handling patterns
- Error boundary implementation
- Suspense integration
- Code splitting strategies
- Advanced custom hooks

## Troubleshooting

| Issue                                       | Solution                                                          |
| ------------------------------------------- | ----------------------------------------------------------------- |
| Component re-renders too often              | Use React.memo, useMemo, useCallback; check dependency arrays     |
| useEffect runs infinitely                   | Add missing dependencies; move dependencies outside if possible   |
| Prop drilling with deeply nested components | Extract custom hook or use Context API                            |
| Memory leaks with data fetching             | Add cleanup function in useEffect; track mounted state            |
| State updates don't reflect immediately     | React batches updates; use state from callback or next render     |
| Custom hook dependencies not working        | Ensure dependency array includes all external values used in hook |

## References

- [React Official Documentation](https://react.dev)
- [React Hooks Rules](https://react.dev/reference/rules)
- [React Component Patterns Guide](references/react-component-patterns.md)
- [TypeScript with React](references/typescript-react.md)
- [State Management Strategies](references/state-management.md)
