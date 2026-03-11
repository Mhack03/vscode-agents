/**
 * Advanced Generic Patterns
 * Demonstrates reusable type-safe utilities using TypeScript generics
 */

/**
 * Repository pattern with generics
 */
interface Entity {
  id: string;
}

interface Repository<T extends Entity> {
  findById(id: string): Promise<T | null>;
  findAll(): Promise<T[]>;
  save(entity: T): Promise<T>;
  delete(id: string): Promise<void>;
}

class InMemoryRepository<T extends Entity> implements Repository<T> {
  private items: Map<string, T> = new Map();

  async findById(id: string): Promise<T | null> {
    return this.items.get(id) ?? null;
  }

  async findAll(): Promise<T[]> {
    return Array.from(this.items.values());
  }

  async save(entity: T): Promise<T> {
    this.items.set(entity.id, entity);
    return entity;
  }

  async delete(id: string): Promise<void> {
    this.items.delete(id);
  }
}

/**
 * Query builder pattern
 */
interface Query<T> {
  where<K extends keyof T>(field: K, value: T[K]): Query<T>;
  select<K extends keyof T>(...fields: K[]): Query<Pick<T, K>>;
  limit(n: number): Query<T>;
  execute(): Promise<T[]>;
}

class TypesafeQueryBuilder<T> implements Query<T> {
  private filters: Array<{ field: keyof T; value: unknown }> = [];
  private selectedFields?: (keyof T)[];
  private limitValue?: number;

  where<K extends keyof T>(field: K, value: T[K]): Query<T> {
    this.filters.push({ field, value });
    return this;
  }

  select<K extends keyof T>(...fields: K[]): Query<Pick<T, K>> {
    this.selectedFields = fields as (keyof T)[];
    return this as any;
  }

  limit(n: number): Query<T> {
    this.limitValue = n;
    return this;
  }

  async execute(): Promise<T[]> {
    // Mock implementation
    return [];
  }
}

/**
 * Event emitter with type safety
 */
type EventMap = Record<string, any>;
type EventCallback<T> = (data: T) => void;

class TypedEventEmitter<Events extends EventMap> {
  private listeners: Map<keyof Events, Set<EventCallback<any>>> = new Map();

  on<K extends keyof Events>(event: K, callback: EventCallback<Events[K]>): void {
    if (!this.listeners.has(event)) {
      this.listeners.set(event, new Set());
    }
    this.listeners.get(event)?.add(callback);
  }

  emit<K extends keyof Events>(event: K, data: Events[K]): void {
    this.listeners.get(event)?.forEach((callback) => callback(data));
  }

  off<K extends keyof Events>(event: K, callback: EventCallback<Events[K]>): void {
    this.listeners.get(event)?.delete(callback);
  }
}

// Usage
interface UserEvents {
  created: { id: string; email: string };
  updated: { id: string; name: string };
  deleted: { id: string };
}

const emitter = new TypedEventEmitter<UserEvents>();

emitter.on('created', (data) => {
  // data is typed as { id: string; email: string }
  console.log(`User created: ${data.email}`);
});

emitter.emit('created', { id: '1', email: 'user@example.com' });

/**
 * Middleware pipeline pattern
 */
type Middleware<Ctx> = (ctx: Ctx, next: () => Promise<void>) => Promise<void>;

class Pipeline<Ctx> {
  private middlewares: Middleware<Ctx>[] = [];

  use(middleware: Middleware<Ctx>): this {
    this.middlewares.push(middleware);
    return this;
  }

  async execute(ctx: Ctx): Promise<void> {
    let index = -1;

    const dispatch = async (i: number): Promise<void> => {
      if (i <= index) return;
      index = i;

      const middleware = this.middlewares[i];
      if (middleware) {
        await middleware(ctx, () => dispatch(i + 1));
      }
    };

    await dispatch(0);
  }
}

// Usage
interface RequestContext {
  path: string;
  method: 'GET' | 'POST' | 'PUT' | 'DELETE';
  headers: Record<string, string>;
}

const pipeline = new Pipeline<RequestContext>();

pipeline
  .use(async (ctx, next) => {
    console.log(`${ctx.method} ${ctx.path}`);
    await next();
  })
  .use(async (ctx, next) => {
    const start = Date.now();
    await next();
    console.log(`Took ${Date.now() - start}ms`);
  });

/**
 * Service locator / dependency injection
 */
class ServiceContainer {
  private services: Map<string, () => any> = new Map();
  private singletons: Map<string, any> = new Map();

  register<T>(name: string, factory: () => T): void {
    this.services.set(name, factory);
  }

  resolve<T>(name: string): T {
    if (this.singletons.has(name)) {
      return this.singletons.get(name);
    }

    const factory = this.services.get(name);
    if (!factory) {
      throw new Error(`Service '${name}' not registered`);
    }

    const instance = factory();
    this.singletons.set(name, instance);
    return instance;
  }
}

/**
 * Utility type: DeepPartial
 */
type DeepPartial<T> = T extends object
  ? {
      [K in keyof T]?: DeepPartial<T[K]>;
    }
  : T;

interface DeepConfig {
  api: {
    baseUrl: string;
    timeout: number;
    retries: number;
  };
  cache: {
    ttl: number;
    enabled: boolean;
  };
}

type PartialConfig = DeepPartial<DeepConfig>;

const config: PartialConfig = {
  api: {
    timeout: 5000, // baseUrl can be omitted
  },
};

/**
 * Constrain generic to ensure type safety
 */
function mergeObjects<T extends object, U extends object>(
  a: T,
  b: U
): T & U {
  return { ...a, ...b };
}

/**
 * Extract types from promises and arrays
 */
type Awaited<T> = T extends Promise<infer U> ? U : T;
type Flatten<T> = T extends Array<infer U> ? U : T;

type PromiseUser = Promise<{ id: string }>;
type User = Awaited<PromiseUser>; // { id: string }

type UserArray = Array<{ id: string; name: string }>;
type SingleUser = Flatten<UserArray>; // { id: string; name: string }

export {
  Repository,
  InMemoryRepository,
  TypedEventEmitter,
  Pipeline,
  ServiceContainer,
  DeepPartial,
  type UserEvents,
  type RequestContext,
  type PartialConfig,
};
