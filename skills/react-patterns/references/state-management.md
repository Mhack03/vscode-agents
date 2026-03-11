# State Management Strategies

## Context API for Global State

Use Context API when:

- State is shared across multiple components
- Avoiding prop drilling
- No complex state logic needed
- Bundle size is a concern

```typescript
interface AppState {
  user: User | null;
  notifications: Notification[];
  isLoading: boolean;
}

type AppAction =
  | { type: 'LOGIN'; payload: User }
  | { type: 'LOGOUT' }
  | { type: 'ADD_NOTIFICATION'; payload: Notification }
  | { type: 'CLEAR_NOTIFICATIONS' }
  | { type: 'SET_LOADING'; payload: boolean };

function appReducer(state: AppState, action: AppAction): AppState {
  switch (action.type) {
    case 'LOGIN':
      return { ...state, user: action.payload };
    case 'LOGOUT':
      return { ...state, user: null };
    case 'ADD_NOTIFICATION':
      return { ...state, notifications: [...state.notifications, action.payload] };
    case 'CLEAR_NOTIFICATIONS':
      return { ...state, notifications: [] };
    case 'SET_LOADING':
      return { ...state, isLoading: action.payload };
    default:
      return state;
  }
}

const AppContext = React.createContext<{
  state: AppState;
  dispatch: React.Dispatch<AppAction>;
} | undefined>(undefined);

export function AppProvider({ children }: { children: React.ReactNode }) {
  const [state, dispatch] = React.useReducer(appReducer, {
    user: null,
    notifications: [],
    isLoading: false,
  });

  const value = React.useMemo(() => ({ state, dispatch }), [state]);

  return (
    <AppContext.Provider value={value}>
      {children}
    </AppContext.Provider>
  );
}

export function useApp() {
  const context = React.useContext(AppContext);
  if (!context) throw new Error('useApp must be used within AppProvider');
  return context;
}
```

## Local State with useState

Use useState when:

- State is only used in one component
- State changes don't affect other components
- Simple state logic

```typescript
interface FormData {
  name: string;
  email: string;
  message: string;
}

export function ContactForm() {
  const [formData, setFormData] = React.useState<FormData>({
    name: '',
    email: '',
    message: '',
  });

  const [submitted, setSubmitted] = React.useState(false);
  const [errors, setErrors] = React.useState<Partial<FormData>>({});

  const handleChange: React.ChangeEventHandler<
    HTMLInputElement | HTMLTextAreaElement
  > = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
  };

  const handleSubmit: React.FormEventHandler = (e) => {
    e.preventDefault();
    if (validate()) {
      setSubmitted(true);
      // Send data to API
    }
  };

  const validate = () => {
    const newErrors: Partial<FormData> = {};
    if (!formData.name) newErrors.name = 'Name is required';
    if (!formData.email) newErrors.email = 'Email is required';
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  return (
    <form onSubmit={handleSubmit}>
      {/* Form fields */}
    </form>
  );
}
```

## useReducer for Complex State

Use useReducer when:

- Multiple state variables are related
- Complex state transitions
- Need to optimize performance
- Multiple state updates happen together

```typescript
interface ShoppingCart {
	items: CartItem[];
	total: number;
	taxRate: number;
}

interface CartItem {
	productId: string;
	name: string;
	quantity: number;
	price: number;
}

type CartAction =
	| { type: "ADD_ITEM"; payload: CartItem }
	| { type: "REMOVE_ITEM"; payload: string }
	| {
			type: "UPDATE_QUANTITY";
			payload: { productId: string; quantity: number };
	  }
	| { type: "CLEAR_CART" }
	| { type: "SET_TAX_RATE"; payload: number };

function cartReducer(state: ShoppingCart, action: CartAction): ShoppingCart {
	let items = state.items;
	let total = 0;

	switch (action.type) {
		case "ADD_ITEM": {
			const existing = items.find(
				(i) => i.productId === action.payload.productId
			);
			if (existing) {
				existing.quantity += action.payload.quantity;
			} else {
				items = [...items, action.payload];
			}
			break;
		}
		case "REMOVE_ITEM":
			items = items.filter((i) => i.productId !== action.payload);
			break;
		case "UPDATE_QUANTITY":
			items = items.map((i) =>
				i.productId === action.payload.productId
					? { ...i, quantity: action.payload.quantity }
					: i
			);
			break;
		case "CLEAR_CART":
			items = [];
			break;
		default:
			return state;
	}

	// Calculate total
	total = items.reduce((sum, item) => sum + item.price * item.quantity, 0);
	total *= 1 + state.taxRate;

	return { ...state, items, total };
}

export function useShoppingCart() {
	return React.useReducer(cartReducer, {
		items: [],
		total: 0,
		taxRate: 0.1,
	});
}
```

## Comparison Table

| Pattern                  | Best For              | Complexity | Performance |
| ------------------------ | --------------------- | ---------- | ----------- |
| useState                 | Local simple state    | Low        | Excellent   |
| useReducer               | Complex related state | Medium     | Excellent   |
| Context + useReducer     | Global state          | Medium     | Good        |
| External library (Redux) | Large complex apps    | High       | Excellent   |

## Performance Optimization with State

```typescript
// ✅ Good: Split state to prevent unnecessary re-renders
const [user, setUser] = React.useState<User | null>(null);
const [notifications, setNotifications] = React.useState<Notification[]>([]);
const [filters, setFilters] = React.useState<FilterState>(initialFilters);

// ❌ Avoid: Monolithic state that causes re-renders when unrelated parts change
const [appState, setAppState] = React.useState({
	user: null,
	notifications: [],
	filters: initialFilters,
	ui: { sidebarOpen: true, theme: "light" },
});

// ✅ Good: Memoize expensive computations
const userPreferences = React.useMemo(() => {
	return computeUserPreferences(user, notifications);
}, [user, notifications]);

// ✅ Good: Memoize callbacks that are passed to children
const handleUserUpdate = React.useCallback((updatedUser: User) => {
	setUser(updatedUser);
}, []);
```
