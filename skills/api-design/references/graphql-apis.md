# GraphQL API Design

## Schema Design

### Basic Schema

```graphql
type User {
	id: ID!
	email: String!
	name: String!
	role: Role!
	createdAt: DateTime!
	orders: [Order!]!
	profile: Profile
}

type Profile {
	bio: String
	avatar: String
	updatedAt: DateTime!
}

type Order {
	id: ID!
	userId: ID!
	total: Float!
	status: OrderStatus!
	items: [OrderItem!]!
	user: User!
	createdAt: DateTime!
}

type OrderItem {
	id: ID!
	productId: ID!
	quantity: Int!
	price: Float!
	product: Product!
}

type Product {
	id: ID!
	name: String!
	description: String
	price: Float!
	stock: Int!
}

enum Role {
	USER
	ADMIN
	MODERATOR
}

enum OrderStatus {
	PENDING
	PROCESSING
	COMPLETED
	CANCELLED
}

type UserConnection {
	edges: [UserEdge!]!
	pageInfo: PageInfo!
}

type UserEdge {
	node: User!
	cursor: String!
}

type PageInfo {
	hasNextPage: Boolean!
	hasPreviousPage: Boolean!
	startCursor: String
	endCursor: String
}

type Query {
	user(id: ID!): User
	users(first: Int, after: String, filter: UserFilter): UserConnection!
	order(id: ID!): Order
	orders(userId: ID!): [Order!]!
	product(id: ID!): Product
	products(filter: ProductFilter): [Product!]!
}

type Mutation {
	createUser(input: CreateUserInput!): CreateUserPayload!
	updateUser(id: ID!, input: UpdateUserInput!): UpdateUserPayload!
	deleteUser(id: ID!): DeleteUserPayload!

	createOrder(input: CreateOrderInput!): CreateOrderPayload!
	updateOrderStatus(
		orderId: ID!
		status: OrderStatus!
	): UpdateOrderStatusPayload!
}

type Subscription {
	orderUpdated(orderId: ID!): Order!
	userStatusChanged(userId: ID!): User!
}

# Input types
input UserFilter {
	role: Role
	search: String
}

input ProductFilter {
	minPrice: Float
	maxPrice: Float
	search: String
}

input CreateUserInput {
	email: String!
	name: String!
	role: Role
}

input UpdateUserInput {
	name: String
	email: String
	profile: ProfileInput
}

input ProfileInput {
	bio: String
	avatar: String
}

input CreateOrderInput {
	items: [OrderItemInput!]!
}

input OrderItemInput {
	productId: ID!
	quantity: Int!
}

# Response types
type CreateUserPayload {
	success: Boolean!
	user: User
	errors: [Error!]
}

type UpdateUserPayload {
	success: Boolean!
	user: User
	errors: [Error!]
}

type DeleteUserPayload {
	success: Boolean!
	errors: [Error!]
}

type CreateOrderPayload {
	success: Boolean!
	order: Order
	errors: [Error!]
}

type UpdateOrderStatusPayload {
	success: Boolean!
	order: Order
	errors: [Error!]
}

type Error {
	message: String!
	code: String!
	field: String
}

scalar DateTime
```

## Resolver Implementation

### Query Resolvers

```javascript
const resolvers = {
	Query: {
		user: async (_, { id }, { dataloaders, user }) => {
			if (!user) throw new Error("Unauthorized");
			return dataloaders.userLoader.load(id);
		},

		users: async (_, { first = 20, after, filter }, { dataloaders, user }) => {
			if (!user) throw new Error("Unauthorized");

			const query = {};
			if (filter?.role) query.role = filter.role;
			if (filter?.search) {
				query.$or = [
					{ name: { $regex: filter.search, $options: "i" } },
					{ email: { $regex: filter.search, $options: "i" } },
				];
			}

			const cursor = after ? Buffer.from(after, "base64").toString() : 0;
			const users = await User.find(query)
				.skip(cursor)
				.limit(first + 1)
				.lean();

			const hasNextPage = users.length > first;
			const edges = users.slice(0, first).map((u) => ({
				node: u,
				cursor: Buffer.from(u._id.toString()).toString("base64"),
			}));

			return {
				edges,
				pageInfo: {
					hasNextPage,
					endCursor: edges[edges.length - 1]?.cursor,
				},
			};
		},

		order: async (_, { id }, { dataloaders }) => {
			return dataloaders.orderLoader.load(id);
		},
	},
};
```

### Mutation Resolvers

```javascript
Mutation: {
  createUser: async (_, { input }, { user, pubsub }) => {
    if (!user || user.role !== 'ADMIN') {
      return {
        success: false,
        errors: [{ code: 'FORBIDDEN', message: 'Only admins can create users' }]
      };
    }

    try {
      const newUser = await User.create({
        email: input.email,
        name: input.name,
        role: input.role || 'USER'
      });

      // Publish subscription event
      await pubsub.publish('USER_CREATED', { userCreated: newUser });

      return {
        success: true,
        user: newUser
      };
    } catch (error) {
      return {
        success: false,
        errors: [{
          code: error.code || 'INTERNAL_ERROR',
          message: error.message
        }]
      };
    }
  },

  updateUser: async (_, { id, input }, { user }) => {
    if (!user || (user.id !== id && user.role !== 'ADMIN')) {
      return {
        success: false,
        errors: [{ code: 'FORBIDDEN', message: 'Cannot update other users' }]
      };
    }

    try {
      const updated = await User.findByIdAndUpdate(id, input, { new: true });
      return { success: true, user: updated };
    } catch (error) {
      return {
        success: false,
        errors: [{ code: 'UPDATE_ERROR', message: error.message }]
      };
    }
  }
}
```

### Field Resolvers

```javascript
User: {
  orders: async (user, _, { dataloaders }) => {
    return dataloaders.ordersByUserIdLoader.load(user.id);
  },

  profile: async (user, _, { dataloaders }) => {
    return dataloaders.profileLoader.load(user.id);
  }
},

Order: {
  user: async (order, _, { dataloaders }) => {
    return dataloaders.userLoader.load(order.userId);
  },

  items: async (order, _, { dataloaders }) => {
    return dataloaders.orderItemsByOrderIdLoader.load(order.id);
  }
},

OrderItem: {
  product: async (item, _, { dataloaders }) => {
    return dataloaders.productLoader.load(item.productId);
  }
}
```

## DataLoader for N+1 Prevention

```javascript
const DataLoader = require("dataloader");

function createDataloaders(db) {
	return {
		userLoader: new DataLoader(async (userIds) => {
			const users = await db.User.find({ _id: { $in: userIds } });

			// Return in same order as input
			return userIds.map((id) =>
				users.find((u) => u._id.toString() === id.toString())
			);
		}),

		orderLoader: new DataLoader(async (orderIds) => {
			const orders = await db.Order.find({ _id: { $in: orderIds } });
			return orderIds.map((id) =>
				orders.find((o) => o._id.toString() === id.toString())
			);
		}),

		productLoader: new DataLoader(async (productIds) => {
			const products = await db.Product.find({ _id: { $in: productIds } });
			return productIds.map((id) =>
				products.find((p) => p._id.toString() === id.toString())
			);
		}),

		ordersByUserIdLoader: new DataLoader(async (userIds) => {
			const orders = await db.Order.find({ userId: { $in: userIds } });
			return userIds.map((userId) =>
				orders.filter((o) => o.userId.toString() === userId.toString())
			);
		}),
	};
}

// Use in Apollo Server context
const server = new ApolloServer({
	typeDefs,
	resolvers,
	context: async ({ req }) => {
		const token = req.headers.authorization?.replace("Bearer ", "");
		const user = token ? verifyToken(token) : null;

		return {
			user,
			dataloaders: createDataloaders(db),
		};
	},
});
```

## Subscriptions

### WebSocket Setup

```javascript
const { WebSocketServer } = require("ws");
const { useServer } = require("graphql-ws/lib/use/ws");

const wsServer = new WebSocketServer({ server, path: "/graphql" });

useServer({ schema }, wsServer);
```

### Subscription Resolvers

```javascript
Subscription: {
  orderUpdated: {
    subscribe: (_, { orderId }, { pubsub }) => {
      return pubsub.subscribe(`ORDER_UPDATED:${orderId}`);
    },

    resolve: (payload) => payload.order
  },

  userStatusChanged: {
    subscribe: (_, { userId }, { pubsub }) => {
      return pubsub.subscribe(`USER_STATUS_CHANGED:${userId}`);
    },

    resolve: (payload) => payload.user
  }
}
```

### Publishing Subscription Events

```javascript
async function updateOrderStatus(orderId, status) {
	const order = await Order.findByIdAndUpdate(
		orderId,
		{ status },
		{ new: true }
	);

	// Publish event to all subscribers
	await pubsub.publish(`ORDER_UPDATED:${orderId}`, { order });
}
```

## Error Handling

```javascript
// Custom error class
class GraphQLError extends Error {
	constructor(message, code, statusCode = 400) {
		super(message);
		this.code = code;
		this.statusCode = statusCode;
	}
}

// Format errors
server.formatError = (error) => {
	const originalError = error.originalError || error;

	return {
		message: error.message,
		code: originalError.code || "INTERNAL_ERROR",
		extensions: {
			statusCode: originalError.statusCode || 500,
		},
	};
};
```

## Direction and Complexity Analysis

```javascript
const { createComplexityLimitRule } = require("graphql-shield");
const { getComplexity, simpleEstimator } = require("graphql-query-complexity");

// Control query complexity
app.use(
	"/graphql",
	graphqlHTTP(async (req) => ({
		schema,
		rootValue: resolvers,
		validationRules: [
			createComplexityLimitRule({
				maxScore: 1000,
				variables: req.variables,
				defaultComplexity: 1,
			}),
		],
	}))
);
```

## Security Considerations

1. **Authentication** - Verify token in context
2. **Authorization** - Check user permissions in resolvers
3. **Rate limiting** - Limit queries per user
4. **Query depth** - Prevent deeply nested queries
5. **Query complexity** - Calculate and limit
6. **Input validation** - Validate all inputs
7. **Directive-based auth** - Use @auth, @requiresRole directives

## Best Practices

1. **Use DataLoaders** - Prevent N+1 queries
2. **Consistent naming** - Use consistent field names
3. **Return objects** - For mutations, return result + errors
4. **Implement relay** - For pagination/connections
5. **Document schema** - Use descriptions
6. **Version APIs** - Plan for evolution
7. **Error codes** - Use application-specific error codes
8. **Subscriptions** - Use for real-time updates
9. **Batch operations** - Consider batch mutations
10. **Cache results** - At resolver or data layer
