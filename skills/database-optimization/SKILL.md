---
name: database-optimization
description: Database performance optimization for SQL and NoSQL databases. Use when writing complex SQL queries, optimizing slow queries, designing database schemas, implementing indexing strategies, scaling database operations, troubleshooting N+1 query problems, working with ORMs efficiently, or implementing connection pooling. Covers B-tree indexes, composite indexes, partial indexes, full-text search, query optimization with EXPLAIN, JOIN strategies, pagination, and database monitoring.
license: Complete terms in LICENSE.txt
---

# Database Optimization

## Overview

Best practices for designing efficient database schemas, writing optimized queries, and scaling database operations for high-performance applications.

## When to Use This Skill

- Writing complex SQL queries
- Optimizing slow queries
- Designing database schemas
- Implementing indexing strategies
- Scaling database operations
- Troubleshooting N+1 query problems
- Working with ORMs efficiently

## Indexing Strategies

### When to Add Indexes

- Columns used in WHERE clauses
- Foreign keys for JOIN operations
- Columns used in ORDER BY, GROUP BY
- Columns frequently searched

### Index Types

**B-Tree Index (Default)**

```sql
-- Single column index
CREATE INDEX idx_users_email ON users(email);

-- Composite index (order matters!)
CREATE INDEX idx_users_status_created ON users(status, created_at);

-- Use composite index for queries like:
SELECT * FROM users WHERE status = 'active' ORDER BY created_at;
```

**Unique Index**

```sql
CREATE UNIQUE INDEX idx_users_email_unique ON users(email);
-- Enforces uniqueness and improves lookup performance
```

**Partial/Filtered Index**

```sql
-- PostgreSQL
CREATE INDEX idx_active_users ON users(created_at) WHERE status = 'active';

-- Only indexes active users, smaller and faster
```

**Full-Text Search Index**

```sql
-- PostgreSQL
CREATE INDEX idx_products_search ON products USING GIN(to_tsvector('english', name || ' ' || description));

-- Search query
SELECT * FROM products
WHERE to_tsvector('english', name || ' ' || description) @@ to_tsquery('laptop');
```

### Index Trade-offs

**Pros:**

- Faster SELECT queries
- Faster JOIN operations
- Enforces uniqueness

**Cons:**

- Slower INSERT/UPDATE/DELETE
- Takes up disk space
- Maintenance overhead

**Best Practice:** Index frequently read columns, be selective for write-heavy tables

## Query Optimization

### 1. Use EXPLAIN to Analyze Queries

```sql
EXPLAIN ANALYZE
SELECT u.name, o.total
FROM users u
JOIN orders o ON u.id = o.user_id
WHERE u.status = 'active'
AND o.created_at > '2026-01-01';

-- Look for:
-- - Seq Scan (bad) vs Index Scan (good)
-- - High cost values
-- - Nested loops on large datasets
```

### 2. Avoid SELECT \*

```sql
-- ❌ Bad - Retrieves unnecessary data
SELECT * FROM users WHERE id = 123;

-- ✅ Good - Only fetch needed columns
SELECT id, name, email FROM users WHERE id = 123;
```

### 3. Use Appropriate JOIN Types

```sql
-- INNER JOIN - Only matching records
SELECT u.name, o.total
FROM users u
INNER JOIN orders o ON u.id = o.user_id;

-- LEFT JOIN - All from left table, matching from right
SELECT u.name, COUNT(o.id) as order_count
FROM users u
LEFT JOIN orders o ON u.id = o.user_id
GROUP BY u.id, u.name;

-- ⚠️ Avoid unnecessary JOINs when possible
```

### 4. Optimize WHERE Clauses

```sql
-- ✅ Good - Uses index on status
SELECT * FROM users WHERE status = 'active';

-- ❌ Bad - Function prevents index usage
SELECT * FROM users WHERE UPPER(status) = 'ACTIVE';

-- ✅ Good - Store normalized data
SELECT * FROM users WHERE status = 'active'; -- Ensure data is lowercase

-- ❌ Bad - Leading wildcard prevents index
SELECT * FROM users WHERE email LIKE '%@example.com';

-- ✅ Good - Can use index
SELECT * FROM users WHERE email LIKE 'john%';
```

### 5. Use LIMIT for Large Result Sets

```sql
-- ❌ Bad - Returns all results
SELECT * FROM orders ORDER BY created_at DESC;

-- ✅ Good - Pagination
SELECT * FROM orders
ORDER BY created_at DESC
LIMIT 20 OFFSET 40; -- Page 3

-- ✅ Better - Cursor-based pagination (for large offsets)
SELECT * FROM orders
WHERE created_at < '2026-02-01'
ORDER BY created_at DESC
LIMIT 20;
```

## N+1 Query Problem

### The Problem

```javascript
// ❌ Bad - N+1 queries
const users = await User.findAll(); // 1 query

for (const user of users) {
	const orders = await Order.findAll({ where: { userId: user.id } }); // N queries
	user.orders = orders;
}
// Total: 1 + N queries
```

### Solutions

**1. Eager Loading (ORM)**

```javascript
// ✅ Good - 2 queries total
const users = await User.findAll({
	include: [
		{
			model: Order,
			as: "orders",
		},
	],
});

// Or with Prisma
const users = await prisma.user.findMany({
	include: {
		orders: true,
	},
});
```

**2. Manual JOIN**

```sql
-- ✅ Good - Single query
SELECT
  u.id, u.name, u.email,
  o.id as order_id, o.total, o.created_at as order_created_at
FROM users u
LEFT JOIN orders o ON u.id = o.user_id;
```

**3. Batch Loading (DataLoader)**

```javascript
const DataLoader = require("dataloader");

const orderLoader = new DataLoader(async (userIds) => {
	const orders = await Order.findAll({
		where: { userId: userIds },
	});

	// Group orders by userId
	const ordersByUser = userIds.map((id) =>
		orders.filter((o) => o.userId === id)
	);

	return ordersByUser;
});

// Usage - automatically batches requests
const user1Orders = await orderLoader.load(user1.id);
const user2Orders = await orderLoader.load(user2.id);
// Only 1 query executed
```

## Database Schema Design

### Normalization

**1NF (First Normal Form)**

- Atomic values (no arrays in cells)
- Each row is unique

**2NF (Second Normal Form)**

- 1NF + No partial dependencies
- All non-key attributes depend on entire primary key

**3NF (Third Normal Form)**

- 2NF + No transitive dependencies
- Non-key attributes don't depend on other non-key attributes

### When to Denormalize

```sql
-- Normalized (3NF)
CREATE TABLE orders (
  id INT PRIMARY KEY,
  user_id INT,
  created_at TIMESTAMP
);

CREATE TABLE order_items (
  id INT PRIMARY KEY,
  order_id INT,
  product_id INT,
  quantity INT,
  price DECIMAL
);

-- Calculating total requires aggregation
SELECT o.id, SUM(oi.quantity * oi.price) as total
FROM orders o
JOIN order_items oi ON o.id = oi.order_id
GROUP BY o.id;

-- Denormalized - add total column
CREATE TABLE orders (
  id INT PRIMARY KEY,
  user_id INT,
  total DECIMAL, -- Denormalized for performance
  created_at TIMESTAMP
);

-- Update total when items change (use triggers or application logic)
```

**When to Denormalize:**

- Read-heavy operations
- Expensive aggregations
- Reporting/analytics tables
- Caching computed values

**When NOT to Denormalize:**

- Write-heavy tables
- Data frequently changes
- Storage is a concern

### Appropriate Data Types

```sql
-- ✅ Good - Right-sized types
CREATE TABLE users (
  id BIGINT PRIMARY KEY,           -- For large tables
  email VARCHAR(255) NOT NULL,     -- Reasonable max length
  age SMALLINT,                    -- 0-255 is enough
  balance DECIMAL(10,2),           -- Precise for money
  is_active BOOLEAN,               -- Not INT
  created_at TIMESTAMP DEFAULT NOW()
);

-- ❌ Bad - Oversized types
CREATE TABLE users (
  id VARCHAR(1000),                -- Waste of space
  email TEXT,                      -- No max length validation
  age INT,                         -- Unnecessarily large
  balance FLOAT,                   -- Precision issues for money
  is_active VARCHAR(10),           -- Use BOOLEAN
  created_at VARCHAR(50)           -- Use TIMESTAMP
);
```

## Transactions

### ACID Properties

- **Atomicity**: All or nothing
- **Consistency**: Valid state transitions
- **Isolation**: Concurrent transactions don't interfere
- **Durability**: Committed data persists

### Transaction Usage

```javascript
// ✅ Good - Use transactions for related operations
await db.transaction(async (trx) => {
  const order = await trx('orders').insert({
    user_id: userId,
    total: 100
  }).returning('*');

  await trx('order_items').insert([
    { order_id: order.id, product_id: 1, quantity: 2 },
    { order_id: order.id, product_id: 2, quantity: 1 }
  ]);

  await trx('users').where({ id: userId }).decrement('balance', 100);

  // All succeed or all fail together
});

// ❌ Bad - No transaction
const order = await Order.create({ userId, total: 100 });
await OrderItem.insert([...]); // Could fail, leaving orphaned order
await User.update({ balance: balance - 100 }); // Could fail, inconsistent state
```

### Isolation Levels

```sql
-- Read Uncommitted (lowest isolation, highest performance)
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

-- Read Committed (default in PostgreSQL)
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

-- Repeatable Read
SET TRANSACTION ISOLATION LEVEL REPEATABLE READ;

-- Serializable (highest isolation, lowest performance)
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
```

## Advanced Topics

For detailed patterns on these topics, see [advanced-patterns.md](references/advanced-patterns.md):

- Connection pooling configuration
- Application-level caching with Redis
- Query result caching with materialized views
- Database partitioning (range, list)
- Safe database migrations
- ORM best practices (Prisma, TypeORM)
- Monitoring, slow query logging, VACUUM

## Database Optimization Checklist

- [ ] Indexes on frequently queried columns
- [ ] Composite indexes for multi-column queries
- [ ] No unused indexes (check pg_stat_user_indexes)
- [ ] Proper data types (no oversized columns)
- [ ] Foreign key constraints with indexes
- [ ] Normalized schema (unless denormalization justified)
- [ ] Connection pooling configured
- [ ] Query optimization (use EXPLAIN ANALYZE)
- [ ] No N+1 query problems
- [ ] Batch operations for bulk inserts/updates
- [ ] Transactions for related operations
- [ ] Caching for expensive queries
- [ ] Slow query logging enabled
- [ ] Regular VACUUM and ANALYZE
- [ ] Monitoring query performance
- [ ] Prepared statements (SQL injection prevention)
- [ ] Database backups automated
- [ ] Partitioning for large tables
- [ ] Read replicas for scaling reads
- [ ] Regular security updates
