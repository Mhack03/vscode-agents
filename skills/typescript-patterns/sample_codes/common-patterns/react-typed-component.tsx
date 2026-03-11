/**
 * React Component with TypeScript
 * Demonstrates proper typing for functional components, generics, hooks, and refs
 */

import {
    FC,
    ReactNode,
    useState,
    useCallback,
    useRef,
    forwardRef,
    InputHTMLAttributes,
    useContext,
    createContext,
} from 'react';

/**
 * Basic typed component with proper prop interface
 */
interface ButtonProps {
    label: string;
    onClick: () => void;
    disabled?: boolean;
    variant?: 'primary' | 'secondary';
    children?: ReactNode;
}

const Button = ({ label, onClick, disabled = false, variant = 'primary' }: ButtonProps) => {
    return (
        <button
            className={`btn btn-${variant}`}
            onClick={onClick}
            disabled={disabled}
        >
            {label}
        </button>
    );
};

/**
 * Generic component that accepts a render prop
 */
interface CardProps<T> {
    title: string;
    data: T;
    render: (item: T) => ReactNode;
    onSelect?: (item: T) => void;
}

function Card<T>({ title, data, render, onSelect }: CardProps<T>) {
    return (
        <div className="card">
            <h2>{title}</h2>
            <div onClick={() => onSelect?.(data)}>{render(data)}</div>
        </div>
    );
}

/**
 * Controlled input component with forwardRef
 */
interface TextInputProps extends InputHTMLAttributes<HTMLInputElement> {
    label?: string;
    error?: string;
}

const TextInput = forwardRef<HTMLInputElement, TextInputProps>(
    ({ label, error, className = '', ...props }, ref) => {
        return (
            <div>
                {label && <label>{label}</label>}
                <input
                    ref={ref}
                    className={`input ${error ? 'input-error' : ''} ${className}`}
                    {...props}
                />
                {error && <span className="error-message">{error}</span>}
            </div>
        );
    }
);

TextInput.displayName = 'TextInput';

/**
 * Custom hook: useAsync
 * Manages async operations with proper type inference
 */
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

    const callback = useCallback(() => {
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

        if (immediate) {
            execute();
        }

        return () => {
            isMounted = false;
        };
    }, [asyncFunction, immediate]);

    // Effect would be called here in real component
    callback();

    return state;
}

/**
 * Form component demonstrating state management
 */
interface FormData {
    email: string;
    name: string;
}

interface FormProps {
    onSubmit: (data: FormData) => Promise<void>;
}

function UserForm({ onSubmit }: FormProps) {
    const [data, setData] = useState<FormData>({ email: '', name: '' });
    const [errors, setErrors] = useState<Partial<FormData>>({});
    const [isLoading, setIsLoading] = useState(false);

    const handleChange = useCallback(
        (field: keyof FormData) => (e: React.ChangeEvent<HTMLInputElement>) => {
            setData((prev) => ({ ...prev, [field]: e.target.value }));
            setErrors((prev) => ({ ...prev, [field]: '' })); // Clear error on change
        },
        []
    );

    const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        setIsLoading(true);

        try {
            await onSubmit(data);
            setData({ email: '', name: '' });
        } catch (err) {
            setErrors({ email: String(err) });
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <form onSubmit={handleSubmit}>
            <TextInput
                label="Name"
                value={data.name}
                onChange={handleChange('name')}
                error={errors.name}
                required
            />
            <TextInput
                label="Email"
                type="email"
                value={data.email}
                onChange={handleChange('email')}
                error={errors.email}
                required
            />
            <Button label="Submit" onClick={() => { }} disabled={isLoading} />
        </form>
    );
}

export { Button, Card, TextInput, useAsync, UserForm, type FormData };
