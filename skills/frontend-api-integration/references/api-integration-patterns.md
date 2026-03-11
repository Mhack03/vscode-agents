# API Integration Patterns

## Request/Response Logging Middleware

```typescript
// api/interceptors.ts
import axios, { AxiosInstance } from "axios";

export function setupLoggingInterceptors(client: AxiosInstance) {
	client.interceptors.request.use((config) => {
		console.log(`[API] ${config.method?.toUpperCase()} ${config.url}`, {
			headers: config.headers,
			data: config.data,
		});
		return config;
	});

	client.interceptors.response.use(
		(response) => {
			console.log(`[API] Response ${response.status}`, {
				url: response.config.url,
				data: response.data,
			});
			return response;
		},
		(error) => {
			console.error(`[API] Error ${error.response?.status}`, {
				url: error.config?.url,
				message: error.message,
				response: error.response?.data,
			});
			return Promise.reject(error);
		}
	);
}
```

## File Upload/Download

```typescript
// services/fileService.ts
export const fileService = {
	// Upload file
	async uploadFile(file: File, onProgress?: (progress: number) => void) {
		const formData = new FormData();
		formData.append("file", file);

		const response = await apiClient.post<{ fileId: string }>(
			"/files/upload",
			formData,
			{
				headers: {
					"Content-Type": "multipart/form-data",
				},
				onUploadProgress: (progressEvent) => {
					if (progressEvent.total) {
						const progress = (progressEvent.loaded / progressEvent.total) * 100;
						onProgress?.(progress);
					}
				},
			}
		);

		return response.data;
	},

	// Download file
	async downloadFile(fileId: string, filename: string) {
		const response = await apiClient.get(`/files/${fileId}/download`, {
			responseType: "blob",
		});

		const url = window.URL.createObjectURL(new Blob([response.data]));
		const link = document.createElement("a");
		link.href = url;
		link.setAttribute("download", filename);
		document.body.appendChild(link);
		link.click();
		link.parentNode?.removeChild(link);
	},
};
```

## Request Deduplication

```typescript
// api/deduplication.ts
const pendingRequests = new Map<string, Promise<any>>();

export function setupDeduplicationInterceptor(client: AxiosInstance) {
	client.interceptors.request.use((config) => {
		const key = `${config.method}:${config.url}`;
		config.headers["x-request-key"] = key;
		return config;
	});

	client.interceptors.response.use(
		(response) => {
			const key = response.config.headers["x-request-key"];
			pendingRequests.delete(key as string);
			return response;
		},
		(error) => {
			const key = error.config?.headers["x-request-key"];
			if (key) pendingRequests.delete(key);
			return Promise.reject(error);
		}
	);
}

// Wrapper for deduplication
export async function dedupedFetch<T>(
	fetchFn: () => Promise<T>,
	key: string
): Promise<T> {
	if (pendingRequests.has(key)) {
		return pendingRequests.get(key);
	}

	const promise = fetchFn();
	pendingRequests.set(key, promise);

	try {
		const result = await promise;
		return result;
	} finally {
		pendingRequests.delete(key);
	}
}
```

## Polling and Long Polling

```typescript
// hooks/useApi.ts
export function usePolling<T>(
	fetchFn: () => Promise<T>,
	interval: number = 5000,
	enabled: boolean = true
) {
	const [data, setData] = React.useState<T | null>(null);
	const [error, setError] = React.useState<Error | null>(null);
	const intervalRef = React.useRef<NodeJS.Timeout>();

	const poll = React.useCallback(async () => {
		try {
			const result = await fetchFn();
			setData(result);
			setError(null);
		} catch (err) {
			setError(err instanceof Error ? err : new Error(String(err)));
		}
	}, [fetchFn]);

	React.useEffect(() => {
		if (!enabled) return;

		poll(); // Fetch immediately
		intervalRef.current = setInterval(poll, interval);

		return () => {
			if (intervalRef.current) clearInterval(intervalRef.current);
		};
	}, [poll, interval, enabled]);

	return { data, error, refetch: poll };
}

// Usage
const { data: messages } = usePolling(() => messageService.getMessages(), 3000);
```

## Exponential Backoff Retry

```typescript
// api/retry.ts
export async function retryWithBackoff<T>(
	fn: () => Promise<T>,
	maxRetries: number = 3,
	initialDelay: number = 1000
): Promise<T> {
	let lastError: Error | null = null;

	for (let attempt = 0; attempt < maxRetries; attempt++) {
		try {
			return await fn();
		} catch (error) {
			lastError = error as Error;
			if (attempt < maxRetries - 1) {
				const delay = initialDelay * Math.pow(2, attempt);
				const jitter = Math.random() * delay * 0.1;
				await new Promise((resolve) => setTimeout(resolve, delay + jitter));
			}
		}
	}

	throw lastError;
}

// Setup in client
export const apiClientWithRetry = axios.create();
apiClientWithRetry.interceptors.response.use(
	(response) => response,
	async (error) => {
		if (error.response?.status >= 500) {
			return retryWithBackoff(() => apiClientWithRetry(error.config));
		}
		return Promise.reject(error);
	}
);
```
