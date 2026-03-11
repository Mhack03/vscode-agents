# SignalR Integration

## Blazor with SignalR

```csharp
// Startup configuration
builder.Services.AddSignalR();

var app = builder.Build();
app.MapHub<ChatHub>("/hubs/chat");
```

## React with SignalR

```typescript
// services/signalRService.ts
import * as signalR from "@microsoft/signalr";

class SignalRService {
	private connection: signalR.HubConnection | null = null;

	async start(hubUrl: string): Promise<void> {
		if (this.connection?.state === signalR.HubConnectionState.Connected) {
			return;
		}

		this.connection = new signalR.HubConnectionBuilder()
			.withUrl(hubUrl, {
				accessTokenFactory: () => authService.getAccessToken() ?? "",
				skipNegotiation: false,
				transport:
					signalR.HttpTransportType.WebSockets |
					signalR.HttpTransportType.LongPolling,
			})
			.withAutomaticReconnect([0, 100, 1000, 5000, 10000])
			.withHubProtocol(new signalR.JsonHubProtocol())
			.build();

		await this.connection.start();
	}

	on<T>(methodName: string, callback: (data: T) => void): void {
		if (!this.connection) throw new Error("Connection not started");
		this.connection.on(methodName, callback);
	}

	send<T>(methodName: string, ...args: any[]): Promise<T> {
		if (!this.connection) throw new Error("Connection not started");
		return this.connection.invoke(methodName, ...args);
	}

	stop(): Promise<void> {
		return this.connection?.stop() ?? Promise.resolve();
	}
}

export const signalRService = new SignalRService();
```

## Real-Time Notifications Component

```typescript
// hooks/useSignalR.ts
export function useSignalR(hubUrl: string) {
  const [isConnected, setIsConnected] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);

  React.useEffect(() => {
    const connect = async () => {
      try {
        await signalRService.start(hubUrl);
        setIsConnected(true);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Connection failed');
      }
    };

    connect();

    return () => {
      signalRService.stop();
    };
  }, [hubUrl]);

  return { isConnected, error };
}

// Component usage
interface Message {
  id: string;
  user: string;
  content: string;
  timestamp: Date;
}

export function ChatComponent() {
  const [messages, setMessages] = React.useState<Message[]>([]);
  const { isConnected, error } = useSignalR('http://localhost:5000/hubs/chat');

  React.useEffect(() => {
    if (!isConnected) return;

    signalRService.on<Message>('ReceiveMessage', (message) => {
      setMessages(prev => [...prev, message]);
    });

    return () => {
      signalRService.stop();
    };
  }, [isConnected]);

  return (
    <div>
      {error && <div className="text-red-500">{error}</div>}
      {!isConnected && <div>Connecting...</div>}
      <div>
        {messages.map(msg => (
          <div key={msg.id}>{msg.user}: {msg.content}</div>
        ))}
      </div>
    </div>
  );
}
```
