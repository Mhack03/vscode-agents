# Advanced Database Patterns

Detailed patterns for connection pooling, caching, partitioning, migrations, ORM optimization, and monitoring.

## Connection Pooling

### Configuration

```javascript
const { Pool } = require('pg');

const pool = new Pool({
  host: 'localhost',
  port: 5432,
  database: 'mydb',
  user: 'user',
  password: 'password',
  max: 20,          // Maximum connections
  min: 5,           // Minimum connections
  idleTimeoutMillis: 30000,
  connectionTimeoutMillis: 2000,
});

// ✅ Good - Use pool
app.get('/api/users', async (req, res) => {
  const client = await pool.connect();
  try {
    const result = await client.query('SELECT * FROM users');
    res.json(result.rows);
  } finally {
    client.release(); // Important!
  }
});

// ❌ Bad - Creating new connection each time
app.get('/api/users', async (req, res) => {
  const client = new Client({ ... });
  await client.connect();
  const result = await client.query('SELECT * FROM users');
  await client.end();
  res.json(result.rows);
});
```

## Caching Strategies

### Application-Level Caching

```javascript
const Redis = require("ioredis");
const redis = new Redis();

async function getUserById(id) {
	// Try cache first
	const cached = await redis.get(`user:${id}`);
	if (cached) {
		return JSON.parse(cached);
	}

	// Cache miss - query database
	const user = await User.findById(id);

	// Store in cache (TTL: 1 hour)
	await redis.setex(`user:${id}`, 3600, JSON.stringify(user));

	return user;
}

// Invalidate cache on update
async function updateUser(id, data) {
	const user = await User.update(id, data);
	await redis.del(`user:${id}`); // Invalidate cache
	return user;
}
```

### Query Result Caching

```sql
-- PostgreSQL - Materialized views
CREATE MATERIALIZED VIEW popular_products AS
SELECT p.id, p.name, COUNT(oi.id) as order_count
FROM products p
JOIN order_items oi ON p.id = oi.product_id
GROUP BY p.id, p.name
ORDER BY order_count DESC;

-- Refresh periodically
REFRESH MATERIALIZED VIEW popular_products;

-- Query the cached view
SELECT * FROM popular_products LIMIT 10;
```

## Database Partitioning

### Range Partitioning

```sql
-- PostgreSQL - Partition by date
CREATE TABLE orders (
  id BIGINT,
  user_id INT,
  total DECIMAL,
  created_at TIMESTAMP
) PARTITION BY RANGE (created_at);

CREATE TABLE orders_2025 PARTITION OF orders
FOR VALUES FROM ('2025-01-01') TO ('2026-01-01');

CREATE TABLE orders_2026 PARTITION OF orders
FOR VALUES FROM ('2026-01-01') TO ('2027-01-01');

-- Queries automatically use correct partition
SELECT * FROM orders WHERE created_at > '2026-01-01';
```

### List Partitioning

```sql
-- Partition by region
CREATE TABLE users (
  id BIGINT,
  name VARCHAR(255),
  region VARCHAR(10)
) PARTITION BY LIST (region);

CREATE TABLE users_us PARTITION OF users FOR VALUES IN ('US');
CREATE TABLE users_eu PARTITION OF users FOR VALUES IN ('EU', 'UK');
CREATE TABLE users_asia PARTITION OF users FOR VALUES IN ('CN', 'JP', 'IN');
```

## Database Migrations

### Best Practices

```javascript
// Migration: add_email_index.js

exports.up = async function (knex) {
	// Check if index exists before creating
	const hasIndex = await knex.schema.hasColumn("users", "email");
	if (hasIndex) {
		await knex.schema.alterTable("users", (table) => {
			table.index("email", "idx_users_email");
		});
	}
};

exports.down = async function (knex) {
	await knex.schema.alterTable("users", (table) => {
		table.dropIndex("email", "idx_users_email");
	});
};

// ✅ Always provide rollback (down)
// ✅ Make migrations idempotent
// ✅ Test migrations on staging first
// ⚠️ Be careful with data migrations on large tables
```

### Safe Schema Changes

```sql
-- ✅ Safe - Add nullable column
ALTER TABLE users ADD COLUMN phone VARCHAR(20);

-- ✅ Safe - Add column with default (PostgreSQL 11+)
ALTER TABLE users ADD COLUMN status VARCHAR(20) DEFAULT 'active';

-- ⚠️ Risky - Add NOT NULL column without default
ALTER TABLE users ADD COLUMN phone VARCHAR(20) NOT NULL;
-- Better: Add nullable first, populate, then add constraint

-- Step 1: Add nullable column
ALTER TABLE users ADD COLUMN phone VARCHAR(20);

-- Step 2: Populate with default value
UPDATE users SET phone = '' WHERE phone IS NULL;

-- Step 3: Add NOT NULL constraint
ALTER TABLE users ALTER COLUMN phone SET NOT NULL;
```

## ORM Best Practices

### Efficient Queries with Prisma

```javascript
// ✅ Good - Select only needed fields
const users = await prisma.user.findMany({
	select: {
		id: true,
		name: true,
		email: true,
	},
});

// ✅ Good - Use findUnique for single records
const user = await prisma.user.findUnique({
	where: { id: 123 },
});

// ✅ Good - Batch operations
await prisma.order.createMany({
	data: orders, // Array of orders
	skipDuplicates: true,
});

// ❌ Bad - Multiple individual creates
for (const order of orders) {
	await prisma.order.create({ data: order });
}
```

### TypeORM Optimization

```typescript
// ✅ Good - Use QueryBuilder for complex queries
const users = await userRepository
	.createQueryBuilder("user")
	.leftJoinAndSelect("user.orders", "order")
	.where("user.status = :status", { status: "active" })
	.andWhere("order.total > :minTotal", { minTotal: 100 })
	.getMany();

// ❌ Bad - Loading all entities
const users = await userRepository.find({
	relations: ["orders"], // Loads ALL orders
});
```

## Monitoring & Maintenance

### Slow Query Log

```sql
-- PostgreSQL - Enable slow query logging
ALTER SYSTEM SET log_min_duration_statement = 1000; -- Log queries > 1s
SELECT pg_reload_conf();

-- MySQL
SET GLOBAL slow_query_log = 1;
SET GLOBAL long_query_time = 1;
```

### Database Statistics

```sql
-- PostgreSQL - Check table sizes
SELECT
  schemaname,
  tablename,
  pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
FROM pg_tables
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC
LIMIT 10;

-- Check index usage
SELECT
  schemaname,
  tablename,
  indexname,
  idx_scan,
  idx_tup_read,
  idx_tup_fetch
FROM pg_stat_user_indexes
WHERE idx_scan = 0 -- Unused indexes
ORDER BY pg_relation_size(indexrelid) DESC;
```

### VACUUM and ANALYZE

```sql
-- PostgreSQL - Reclaim space and update statistics
VACUUM ANALYZE users;

-- Full vacuum (locks table)
VACUUM FULL users;

-- Auto-vacuum configuration
ALTER TABLE users SET (autovacuum_vacuum_scale_factor = 0.1);
```
