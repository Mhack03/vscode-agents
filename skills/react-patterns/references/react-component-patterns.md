# React Component Patterns

## Form Components with Validation

```typescript
interface Formfield {
	value: string;
	error: string | null;
	touched: boolean;
}

interface FormState {
	[key: string]: FormField;
}

interface UseFormReturn<T extends Record<string, FormField>> {
	values: T;
	errors: Record<string, string | null>;
	touched: Record<string, boolean>;
	handleChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
	handleBlur: (e: React.FocusEvent<HTMLInputElement>) => void;
	handleSubmit: (
		onSubmit: (values: any) => void
	) => (e: React.FormEvent) => void;
	reset: () => void;
}

export function useForm<T extends Record<string, FormField>>(
	initialValues: T,
	onSubmit: (values: any) => Promise<void>,
	validate?: (values: any) => Record<string, string>
): UseFormReturn<T> {
	const [values, setValues] = React.useState(initialValues);
	const [touched, setTouched] = React.useState(
		Object.keys(initialValues).reduce(
			(acc, key) => ({ ...acc, [key]: false }),
			{}
		)
	);
	const [errors, setErrors] = React.useState<Record<string, string | null>>({});

	const handleChange = React.useCallback(
		(e: React.ChangeEvent<HTMLInputElement>) => {
			const { name, value } = e.target;
			setValues((prev) => ({ ...prev, [name]: value }));
		},
		[]
	);

	const handleBlur = React.useCallback(
		(e: React.FocusEvent<HTMLInputElement>) => {
			const { name } = e.target;
			setTouched((prev) => ({ ...prev, [name]: true }));
			if (validate) {
				const newErrors = validate(values);
				setErrors(newErrors);
			}
		},
		[values, validate]
	);

	const handleSubmit = React.useCallback(
		(submitFn: (values: any) => Promise<void>) =>
			async (e: React.FormEvent) => {
				e.preventDefault();
				if (validate) {
					const newErrors = validate(values);
					setErrors(newErrors);
					if (Object.values(newErrors).some((e) => e !== null)) return;
				}
				await submitFn(values);
			},
		[values, validate]
	);

	const reset = React.useCallback(() => {
		setValues(initialValues);
		setTouched(
			Object.keys(initialValues).reduce(
				(acc, key) => ({ ...acc, [key]: false }),
				{}
			)
		);
		setErrors({});
	}, [initialValues]);

	return {
		values,
		errors,
		touched,
		handleChange,
		handleBlur,
		handleSubmit,
		reset,
	};
}
```

## Error Boundary Component

```typescript
interface ErrorBoundaryProps {
  children: React.ReactNode;
  fallback?: (error: Error, retry: () => void) => React.ReactNode;
}

interface ErrorBoundaryState {
  hasError: boolean;
  error: Error | null;
}

export class ErrorBoundary extends React.Component<ErrorBoundaryProps, ErrorBoundaryState> {
  constructor(props: ErrorBoundaryProps) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    console.error('Error caught by boundary:', error, errorInfo);
  }

  retry = () => {
    this.setState({ hasError: false, error: null });
  };

  render() {
    if (this.state.hasError && this.state.error) {
      return (
        this.props.fallback?.(this.state.error, this.retry) || (
          <div className="border-l-4 border-red-500 p-4 bg-red-50">
            <p className="text-red-800">Something went wrong</p>
            <button onClick={this.retry} className="mt-2 px-4 py-2 bg-red-600 text-white rounded">
              Try Again
            </button>
          </div>
        )
      );
    }

    return this.props.children;
  }
}
```

## Suspense with Code Splitting

```typescript
import React, { Suspense } from 'react';

// Lazy load components
const Dashboard = React.lazy(() => import('./Dashboard'));
const Settings = React.lazy(() => import('./Settings'));
const Analytics = React.lazy(() => import('./Analytics'));

interface RouteConfig {
  path: string;
  component: React.LazyExoticComponent<() => JSX.Element>;
  label: string;
}

const routes: RouteConfig[] = [
  { path: '/dashboard', component: Dashboard, label: 'Dashboard' },
  { path: '/settings', component: Settings, label: 'Settings' },
  { path: '/analytics', component: Analytics, label: 'Analytics' },
];

function LoadingFallback() {
  return (
    <div className="flex items-center justify-center h-screen">
      <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
    </div>
  );
}

export function AppRouter() {
  const [currentPath, setCurrentPath] = React.useState('/dashboard');
  const currentRoute = routes.find(r => r.path === currentPath);
  const Component = currentRoute?.component;

  return (
    <div className="flex">
      <nav className="w-64 bg-gray-900 text-white p-4">
        {routes.map(route => (
          <button
            key={route.path}
            onClick={() => setCurrentPath(route.path)}
            className={`w-full text-left px-4 py-2 ${currentPath === route.path ? 'bg-blue-600' : ''}`}
          >
            {route.label}
          </button>
        ))}
      </nav>
      <main className="flex-1">
        <Suspense fallback={<LoadingFallback />}>
          {Component && <Component />}
        </Suspense>
      </main>
    </div>
  );
}
```

## Compound Component Pattern

```typescript
interface TabsProps {
  children: React.ReactNode;
  defaultTab?: string;
}

interface TabsContextType {
  activeTab: string;
  setActiveTab: (tab: string) => void;
}

const TabsContext = React.createContext<TabsContextType | undefined>(undefined);

export function Tabs({ children, defaultTab = '0' }: TabsProps) {
  const [activeTab, setActiveTab] = React.useState(defaultTab);
  const value = React.useMemo(() => ({ activeTab, setActiveTab }), [activeTab]);

  return (
    <TabsContext.Provider value={value}>
      <div>{children}</div>
    </TabsContext.Provider>
  );
}

export function TabList({ children }: { children: React.ReactNode }) {
  return <div className="flex border-b">{children}</div>;
}

interface TabProps {
  id: string;
  children: React.ReactNode;
}

export function Tab({ id, children }: TabProps) {
  const context = React.useContext(TabsContext);
  if (!context) throw new Error('Tab must be used within Tabs');
  const { activeTab, setActiveTab } = context;

  return (
    <button
      onClick={() => setActiveTab(id)}
      className={`px-4 py-2 ${activeTab === id ? 'border-b-2 border-blue-600 font-bold' : ''}`}
    >
      {children}
    </button>
  );
}

export function TabPanel({ id, children }: TabProps) {
  const context = React.useContext(TabsContext);
  if (!context) throw new Error('TabPanel must be used within Tabs');
  const { activeTab } = context;

  return activeTab === id ? <div className="p-4">{children}</div> : null;
}

// Usage:
// <Tabs>
//   <TabList>
//     <Tab id="0">Tab 1</Tab>
//     <Tab id="1">Tab 2</Tab>
//   </TabList>
//   <TabPanel id="0">Content 1</TabPanel>
//   <TabPanel id="1">Content 2</TabPanel>
// </Tabs>
```
