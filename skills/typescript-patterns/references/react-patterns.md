# React TypeScript Patterns

## Component Typing

### Basic Functional Component

```typescript
import { FC, ReactNode } from 'react';

interface ButtonProps {
  label: string;
  onClick: () => void;
  disabled?: boolean;
  children?: ReactNode;
}

// Modern approach: explicit function type (preferred over React.FC)
const Button = ({ label, onClick, disabled = false }: ButtonProps) => {
  return (
    <button onClick={onClick} disabled={disabled}>
      {label}
    </button>
  );
};

export default Button;
```

### Generic Component

```typescript
import { ReactNode } from 'react';

interface CardProps<T> {
  title: string;
  data: T;
  render: (item: T) => ReactNode;
}

// Generic component maintains type through render prop
function Card<T>({ title, data, render }: CardProps<T>) {
  return (
    <div className="card">
      <h2>{title}</h2>
      <div>{render(data)}</div>
    </div>
  );
}

// Usage
<Card<User>
  title="User Info"
  data={user}
  render={(u) => `${u.name} (${u.email})`}  // u is inferred as User
/>
```

## Hook Typing

### useAsync Generic Hook

```typescript
import { useEffect, useState } from 'react';

interface UseAsyncState<T> {
  status: 'idle' | 'loading' | 'success' | 'error';
  data: T | null;
  error: Error | null;
}

function useAsync<T>(
  asyncFunction: () => Promise<T>,
  immediate = true
): UseAsyncState<T> {
  const [state, setState] = useState<UseAsyncState<T>>({
    status: 'idle',
    data: null,
    error: null,
  });

  useEffect(() => {
    if (!immediate) return;

    let isMounted = true;

    const execute = async () => {
      setState({ status: 'loading', data: null, error: null });
      try {
        const response = await asyncFunction();
        if (isMounted) {
          setState({ status: 'success', data: response, error: null });
        }
      } catch (error) {
        if (isMounted) {
          setState({ status: 'error', data: null, error: error as Error });
        }
      }
    };

    execute();
    return () => {
      isMounted = false;
    };
  }, [asyncFunction, immediate]);

  return state;
}

// Usage
const { status, data, error } = useAsync(() => fetchUsers(), true);
// status is 'idle' | 'loading' | 'success' | 'error'
// data is User[] | null
// error is Error | null
```

### useLocalStorage Generic Hook

```typescript
function useLocalStorage<T>(key: string, initialValue: T): [T, (value: T) => void] {
  const [storedValue, setStoredValue] = useState<T>(() => {
    try {
      const item = window.localStorage.getItem(key);
      return item ? JSON.parse(item) : initialValue;
    } catch {
      return initialValue;
    }
  });

  const setValue = (value: T) => {
    try {
      setStoredValue(value);
      window.localStorage.setItem(key, JSON.stringify(value));
    } catch (error) {
      console.error(error);
    }
  };

  return [storedValue, setValue];
}

// Usage
const [theme, setTheme] = useLocalStorage<'light' | 'dark'>('theme', 'light');
// theme is 'light' | 'dark'
```

## Context with TypeScript

```typescript
import { createContext, useContext, ReactNode } from 'react';

interface User {
  id: string;
  name: string;
}

interface AuthContextType {
  user: User | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

// Create context with default undefined to enforce useAuth() calls within provider
const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  // Implementation...
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

// Custom hook prevents usage outside provider
export function useAuth(): AuthContextType {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
}

// Usage — TypeScript ensures user is User | null (not undefined)
function Profile() {
  const { user } = useAuth();
  return <div>{user?.name}</div>;
}
```

## forwardRef with TypeScript

```typescript
import { forwardRef, InputHTMLAttributes } from 'react';

interface TextInputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
}

// Preserve ref type: HTMLInputElement
const TextInput = forwardRef<HTMLInputElement, TextInputProps>(
  ({ label, ...props }, ref) => {
    return (
      <div>
        {label && <label>{label}</label>}
        <input ref={ref} {...props} />
      </div>
    );
  }
);

TextInput.displayName = 'TextInput';

// Usage
const inputRef = useRef<HTMLInputElement>(null);
<TextInput ref={inputRef} label="Name" />
// inputRef.current is HTMLInputElement | null
```

## Best Practices

- **Avoid `React.FC`**: Use explicit function types (TypeScript infers return type automatically)
- **Generic components**: Use `<T>` syntax for render props and reusable hooks
- **Strict context checking**: Set initial value to `undefined` and enforce provider usage
- **forwardRef types**: Always specify DOM element type (`HTMLInputElement`, `HTMLDivElement`, etc.)
- **Event handlers**: Type with React event types (`React.ChangeEvent<HTMLInputElement>`)
