# TypeScript with React

## Component Type Patterns

### Functional Component with Props

```typescript
interface UserCardProps {
  userId: number;
  name: string;
  email: string;
  onDelete?: (id: number) => void;
  role?: 'admin' | 'user' | 'guest';
}

export const UserCard: React.FC<UserCardProps> = ({
  userId,
  name,
  email,
  onDelete,
  role = 'user',
}) => {
  return (
    <div className="border rounded p-4">
      <h3>{name}</h3>
      <p>{email}</p>
      <span className="badge">{role}</span>
      {onDelete && (
        <button onClick={() => onDelete(userId)}>Delete</button>
      )}
    </div>
  );
};
```

### Generic Component

```typescript
interface ListProps<T> {
  items: T[];
  renderItem: (item: T, index: number) => React.ReactNode;
  keyExtractor: (item: T, index: number) => string | number;
  emptyMessage?: string;
  className?: string;
}

export function List<T,>({
  items,
  renderItem,
  keyExtractor,
  emptyMessage = 'No items',
  className = '',
}: ListProps<T>) {
  if (items.length === 0) {
    return <p>{emptyMessage}</p>;
  }

  return (
    <ul className={className}>
      {items.map((item, index) => (
        <li key={keyExtractor(item, index)}>
          {renderItem(item, index)}
        </li>
      ))}
    </ul>
  );
}

// Usage:
// <List<User>
//   items={users}
//   renderItem={(user) => <UserCard {...user} />}
//   keyExtractor={(user) => user.id}
// />
```

## Hook Type Patterns

### useReducer with TypeScript

```typescript
type State = {
	count: number;
	error: string | null;
};

type Action =
	| { type: "INCREMENT"; payload: number }
	| { type: "DECREMENT"; payload: number }
	| { type: "RESET" }
	| { type: "SET_ERROR"; payload: string };

function reducer(state: State, action: Action): State {
	switch (action.type) {
		case "INCREMENT":
			return { ...state, count: state.count + action.payload, error: null };
		case "DECREMENT":
			return { ...state, count: state.count - action.payload, error: null };
		case "RESET":
			return { ...state, count: 0 };
		case "SET_ERROR":
			return { ...state, error: action.payload };
		default:
			return state;
	}
}

export function useCounter() {
	const [state, dispatch] = React.useReducer(reducer, {
		count: 0,
		error: null,
	});
	return { state, dispatch };
}
```

### useCallback with Types

```typescript
interface User {
	id: number;
	name: string;
	email: string;
}

interface UseUserManagerReturn {
	users: User[];
	addUser: (user: User) => void;
	removeUser: (id: number) => void;
	updateUser: (id: number, updates: Partial<User>) => void;
}

export function useUserManager(): UseUserManagerReturn {
	const [users, setUsers] = React.useState<User[]>([]);

	const addUser = React.useCallback((user: User) => {
		setUsers((prev) => [...prev, user]);
	}, []);

	const removeUser = React.useCallback((id: number) => {
		setUsers((prev) => prev.filter((u) => u.id !== id));
	}, []);

	const updateUser = React.useCallback((id: number, updates: Partial<User>) => {
		setUsers((prev) =>
			prev.map((u) => (u.id === id ? { ...u, ...updates } : u))
		);
	}, []);

	return { users, addUser, removeUser, updateUser };
}
```

## Event Handler Types

```typescript
// Simple click handler
const handleClick: React.MouseEventHandler<HTMLButtonElement> = (e) => {
	console.log(e.currentTarget.name);
};

// Form input change
const handleInputChange: React.ChangeEventHandler<HTMLInputElement> = (e) => {
	const { value, name } = e.currentTarget;
	setFormData((prev) => ({ ...prev, [name]: value }));
};

// Form submission
const handleSubmit: React.FormEventHandler<HTMLFormElement> = (e) => {
	e.preventDefault();
	const formData = new FormData(e.currentTarget);
	submitForm(Object.fromEntries(formData));
};

// Select change
const handleSelectChange: React.ChangeEventHandler<HTMLSelectElement> = (e) => {
	setSelectedOption(e.target.value);
};
```

## Ref Types

```typescript
// DOM element ref
const inputRef = React.useRef<HTMLInputElement>(null);

function focus() {
  inputRef.current?.focus();
}

// Custom object ref
interface TimerHandle {
  start: () => void;
  stop: () => void;
}

export const Timer = React.forwardRef<TimerHandle>((props, ref) => {
  const [time, setTime] = React.useState(0);
  const intervalRef = React.useRef<NodeJS.Timeout | null>(null);

  React.useImperativeHandle(ref, () => ({
    start: () => {
      intervalRef.current = setInterval(() => setTime(t => t + 1), 100);
    },
    stop: () => {
      if (intervalRef.current) clearInterval(intervalRef.current);
    },
  }));

  return <div>{time}</div>;
});
```

## Context with TypeScript

```typescript
interface Theme {
  primary: string;
  secondary: string;
  background: string;
}

interface ThemeContextType {
  theme: Theme;
  setTheme: (theme: Theme) => void;
  toggleDarkMode: () => void;
}

const ThemeContext = React.createContext<ThemeContextType | undefined>(undefined);

export function ThemeProvider({ children }: { children: React.ReactNode }) {
  const [isDark, setIsDark] = React.useState(false);

  const theme: Theme = isDark
    ? { primary: '#333', secondary: '#666', background: '#000' }
    : { primary: '#0066cc', secondary: '#0099ff', background: '#fff' };

  const value = React.useMemo<ThemeContextType>(
    () => ({
      theme,
      setTheme: (newTheme) => console.log('Setting theme...'),
      toggleDarkMode: () => setIsDark(prev => !prev),
    }),
    [theme]
  );

  return (
    <ThemeContext.Provider value={value}>
      {children}
    </ThemeContext.Provider>
  );
}

export function useTheme(): ThemeContextType {
  const context = React.useContext(ThemeContext);
  if (context === undefined) {
    throw new Error('useTheme must be used within ThemeProvider');
  }
  return context;
}
```
