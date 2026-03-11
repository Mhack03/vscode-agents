# Authentication & Authorization Integration

## JWT Token Management

```typescript
// services/authService.ts
interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
}

interface LoginRequest {
  email: string;
  password: string;
}

interface LoginResponse {
  tokens: AuthTokens;
  user: User;
}

const TOKEN_STORAGE_KEY = 'auth_tokens';
const TOKEN_REFRESH_BUFFER = 60; // seconds before expiry

export const authService = {
  async login(credentials: LoginRequest): Promise<LoginResponse> {
    const response = await axios.post<LoginResponse>('/auth/login', credentials);
    const { tokens, user } = response.data;

    this.storeTokens(tokens);
    this.scheduleTokenRefresh(tokens.expiresIn);

    return { tokens, user };
  },

  async logout(): Promise<void> {
    try {
      await axios.post('/auth/logout');
    } finally {
      this.clearTokens();
      this.cancelTokenRefresh();
    }
  },

  async refreshTokens(): Promise<AuthTokens> {
    const refreshToken = this.getRefreshToken();
    if (!refreshToken) throw new Error('No refresh token available');

    const response = await axios.post<{ tokens: AuthTokens }>(
      '/auth/refresh',
      { refreshToken }
    );

    const { tokens } = response.data;
    this.storeTokens(tokens);
    this.scheduleTokenRefresh(tokens.expiresIn);

    return tokens;
  },

  getAccessToken(): string | null {
    const tokens = this.getStoredTokens();
    return tokens?.accessToken || null;
  },

  getRefreshToken(): string | null {
    const tokens = this.getStoredTokens();
    return tokens?.refreshToken || null;
  },

  isTokenExpiringSoon(): boolean {
    const tokens = this.getStoredTokens();
    if (!tokens) return false;

    const decodedToken = jwtDecode<{ exp: number }>(tokens.accessToken);
    const expiresAt = decodedToken.exp * 1000;
    const now = Date.now();
    const bufferMs = TOKEN_REFRESH_BUFFER * 1000;

    return expiresAt - now < bufferMs;
  },

  private storeTokens(tokens: AuthTokens): void {
    localStorage.setItem(TOKEN_STORAGE_KEY, JSON.stringify(tokens));
  },

  private getStoredTokens(): AuthTokens | null {
    const stored = localStorage.getItem(TOKEN_STORAGE_KEY);
    return stored ? JSON.parse(stored) : null;
  },

  private clearTokens(): void {
    localStorage.removeItem(TOKEN_STORAGE_KEY);
  },

  private refreshTimerId?: NodeJS.Timeout;

  private scheduleTokenRefresh(expiresIn: number): void {
    this.cancelTokenRefresh();

    const refreshTime = (expiresIn - TOKEN_REFRESH_BUFFER) * 1000;
    this.refreshTimerId = setTimeout(() => {
      this.refreshTokens().catch(err => {
        console.error('Token refresh failed:', err);
        this.clearTokens();
        window.location.href = '/login';
      });
    }, refreshTime);
  },

  private cancelTokenRefresh(): void {
    if (this.refreshTimerId) {
      clearTimeout(this.refreshTimerId);
      this.refreshTimerId = undefined;
    }
  },
};
```

## Protected Route Component

```typescript
// components/ProtectedRoute.tsx
interface ProtectedRouteProps {
  children: React.ReactNode;
  requiredRoles?: string[];
}

export function ProtectedRoute({
  children,
  requiredRoles = [],
}: ProtectedRouteProps) {
  const [user, setUser] = React.useState<User | null>(null);
  const [isLoading, setIsLoading] = React.useState(true);
  const navigate = useNavigate();

  React.useEffect(() => {
    const checkAuth = async () => {
      const token = authService.getAccessToken();

      if (!token) {
        navigate('/login');
        return;
      }

      try {
        // Validate token with backend
        const response = await apiClient.get<User>('/auth/me');
        const userData = response.data;

        if (requiredRoles.length > 0) {
          if (!requiredRoles.includes(userData.role)) {
            navigate('/unauthorized');
            return;
          }
        }

        setUser(userData);
      } catch (error) {
        navigation.push('/login');
      } finally {
        setIsLoading(false);
      }
    };

    checkAuth();
  }, [requiredRoles, navigate]);

  if (isLoading) return <LoadingSpinner />;
  if (!user) return null;

  return <>{children}</>;
}

// Usage
<ProtectedRoute requiredRoles={['admin']}>
  <AdminDashboard />
</ProtectedRoute>
```

## CORS Backend Configuration (ASP.NET Core)

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policyBuilder =>
    {
        policyBuilder
            .WithOrigins(
                "http://localhost:3000",
                "http://localhost:3001",
                "https://example.com"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("X-Total-Count", "X-Page-Count");
    });
});

var app = builder.Build();

// Use CORS
app.UseCors("FrontendPolicy");

app.Run();
```
