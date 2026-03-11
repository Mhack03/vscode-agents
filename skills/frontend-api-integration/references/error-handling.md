# Error Handling Strategies

## Structured Error Handling

```typescript
// types/errors.ts
export class ApiError extends Error {
	constructor(
		public statusCode: number,
		public message: string,
		public validationErrors?: Record<string, string[]>,
		public timestamp?: Date
	) {
		super(message);
		this.name = "ApiError";
	}
}

export class NetworkError extends Error {
	constructor(message: string) {
		super(message);
		this.name = "NetworkError";
	}
}

export class TimeoutError extends Error {
	constructor(message: string) {
		super(message);
		this.name = "TimeoutError";
	}
}

// Error severity levels
export enum ErrorSeverity {
	INFO = "info",
	WARNING = "warning",
	ERROR = "error",
	CRITICAL = "critical",
}

// Error service
export const errorService = {
	getErrorMessage(error: unknown): string {
		if (error instanceof ApiError) {
			return error.message;
		}
		if (error instanceof NetworkError) {
			return "Network connection error. Please check your internet.";
		}
		if (error instanceof TimeoutError) {
			return "Request timeout. Please try again.";
		}
		if (error instanceof Error) {
			return error.message;
		}
		return "An unknown error occurred";
	},

	getSeverity(error: unknown): ErrorSeverity {
		if (error instanceof ApiError) {
			if (error.statusCode >= 500) return ErrorSeverity.CRITICAL;
			if (error.statusCode >= 400) return ErrorSeverity.ERROR;
		}
		if (error instanceof NetworkError) return ErrorSeverity.ERROR;
		if (error instanceof TimeoutError) return ErrorSeverity.WARNING;
		return ErrorSeverity.ERROR;
	},

	getValidationErrors(error: ApiError): Record<string, string[]> | null {
		return error.validationErrors || null;
	},
};
```

## Response Error Interceptor

```typescript
// api/errorInterceptor.ts
import axios from "axios";

export function setupErrorHandling(client: typeof axios) {
	client.interceptors.response.use(
		(response) => response,
		async (error) => {
			if (error.response) {
				// Server responded with error
				const { status, data } = error.response;

				throw new ApiError(
					status,
					data.message || getDefaultErrorMessage(status),
					data.validationErrors || data.errors,
					new Date(data.timestamp)
				);
			}

			if (error.code === "ECONNABORTED") {
				throw new TimeoutError("Request timed out");
			}

			if (!error.response) {
				throw new NetworkError("Network connection failed");
			}

			throw error;
		}
	);
}

function getDefaultErrorMessage(statusCode: number): string {
	const messages: Record<number, string> = {
		400: "Invalid request",
		401: "Unauthorized",
		403: "Access denied",
		404: "Resource not found",
		409: "Conflict",
		429: "Too many requests",
		500: "Server error",
		503: "Service unavailable",
	};
	return messages[statusCode] || "An error occurred";
}
```

## Error Notification Component

```typescript
// components/ErrorNotification.tsx
import React from 'react';
import { errorService, ErrorSeverity } from '@/services/errorService';

interface ErrorNotificationProps {
  error: unknown;
  onDismiss?: () => void;
  autoClose?: boolean;
  autoCloseDuration?: number;
}

export function ErrorNotification({
  error,
  onDismiss,
  autoClose = true,
  autoCloseDuration = 5000,
}: ErrorNotificationProps) {
  const [isVisible, setIsVisible] = React.useState(true);

  React.useEffect(() => {
    if (autoClose) {
      const timer = setTimeout(() => setIsVisible(false), autoCloseDuration);
      return () => clearTimeout(timer);
    }
  }, [autoClose, autoCloseDuration]);

  const message = errorService.getErrorMessage(error);
  const severity = errorService.getSeverity(error);
  const validationErrors = error instanceof ApiError
    ? errorService.getValidationErrors(error)
    : null;

  const severityStyles: Record<ErrorSeverity, string> = {
    [ErrorSeverity.INFO]: 'bg-blue-100 text-blue-800 border-blue-300',
    [ErrorSeverity.WARNING]: 'bg-yellow-100 text-yellow-800 border-yellow-300',
    [ErrorSeverity.ERROR]: 'bg-red-100 text-red-800 border-red-300',
    [ErrorSeverity.CRITICAL]: 'bg-red-200 text-red-900 border-red-500',
  };

  if (!isVisible) return null;

  return (
    <div className={`border-l-4 p-4 mb-4 ${severityStyles[severity]}`}>
      <div className="flex justify-between items-start">
        <div>
          <p className="font-semibold">{message}</p>
          {validationErrors && (
            <ul className="mt-2 text-sm list-disc list-inside">
              {Object.entries(validationErrors).map(([field, errors]) => (
                <li key={field}>{field}: {errors.join(', ')}</li>
              ))}
            </ul>
          )}
        </div>
        {onDismiss && (
          <button
            onClick={() => {
              setIsVisible(false);
              onDismiss();
            }}
            className="text-xl"
          >
            ×
          </button>
        )}
      </div>
    </div>
  );
}
```

## Error Boundary with API Errors

```typescript
// components/ApiErrorBoundary.tsx
interface ApiErrorBoundaryState {
  error: unknown | null;
  hasError: boolean;
}

export class ApiErrorBoundary extends React.Component<
  { children: React.ReactNode },
  ApiErrorBoundaryState
> {
  constructor(props: { children: React.ReactNode }) {
    super(props);
    this.state = { error: null, hasError: false };
  }

  static getDerivedStateFromError(error: unknown): ApiErrorBoundaryState {
    return { error, hasError: true };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    console.error('API Error caught:', error, errorInfo);
  }

  retry = () => {
    this.setState({ error: null, hasError: false });
  };

  render() {
    if (this.state.hasError) {
      return (
        <div className="p-4 border border-red-300 rounded bg-red-50">
          <ErrorNotification
            error={this.state.error}
            onDismiss={this.retry}
          />
          <button
            onClick={this.retry}
            className="mt-4 px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700"
          >
            Try Again
          </button>
        </div>
      );
    }

    return this.props.children;
  }
}
```
