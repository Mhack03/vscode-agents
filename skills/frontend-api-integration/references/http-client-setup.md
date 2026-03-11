# HTTP Client Setup

## React with Axios

```typescript
import axios, { AxiosInstance, AxiosError } from "axios";

// Create HTTP client with base configuration
const createApiClient = (): AxiosInstance => {
	const client = axios.create({
		baseURL: import.meta.env.VITE_API_URL || "http://localhost:5000/api",
		timeout: 10000,
		headers: {
			"Content-Type": "application/json",
		},
	});

	// Request interceptor - add auth token
	client.interceptors.request.use(
		(config) => {
			const token = localStorage.getItem("accessToken");
			if (token) {
				config.headers.Authorization = `Bearer ${token}`;
			}
			return config;
		},
		(error) => Promise.reject(error)
	);

	// Response interceptor - handle errors and token refresh
	client.interceptors.response.use(
		(response) => response,
		async (error: AxiosError) => {
			const originalRequest = error.config as any;

			// Handle 401 Unauthorized - try token refresh
			if (error.response?.status === 401 && !originalRequest._retry) {
				originalRequest._retry = true;
				try {
					const refreshToken = localStorage.getItem("refreshToken");
					const response = await axios.post(
						`${import.meta.env.VITE_API_URL}/auth/refresh`,
						{ refreshToken }
					);
					const { accessToken } = response.data;
					localStorage.setItem("accessToken", accessToken);
					originalRequest.headers.Authorization = `Bearer ${accessToken}`;
					return client(originalRequest);
				} catch {
					// Refresh failed - redirect to login
					window.location.href = "/login";
					return Promise.reject(error);
				}
			}

			return Promise.reject(error);
		}
	);

	return client;
};

export const apiClient = createApiClient();
```

## Key Setup Steps

1. **Base Configuration**: Set API URL, timeout, and default headers
2. **Request Interceptor**: Automatically attach auth tokens to all requests
3. **Response Interceptor**: Handle 401 errors with automatic token refresh
4. **Error Recovery**: Redirect to login if token refresh fails
