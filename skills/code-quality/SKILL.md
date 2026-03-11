---
name: code-quality
description: Comprehensive guidance on writing clean, maintainable, and scalable code. Covers SOLID principles, design patterns, code smells, refactoring techniques, and clean architecture practices. Use when improving code structure, conducting code reviews, architecting systems, or mentoring developers on best practices.
license: Complete terms in LICENSE.txt
---

# Code Quality & Clean Code

Master the principles and practices for writing professional-grade, maintainable code that scales with your team and project.

## When to Use This Skill

- Writing new features or components
- Refactoring existing code
- Conducting or participating in code reviews
- Architecting new systems or modules
- Improving code maintainability and testability
- Reducing technical debt
- Debugging complex issues caused by poor code structure
- Training junior developers on best practices
- Making design decisions about patterns and architecture

## Core Topics

| Topic                  | Focus                                       | Best For                                          |
| ---------------------- | ------------------------------------------- | ------------------------------------------------- |
| **SOLID Principles**   | Five foundational design principles         | Creating maintainable, flexible architectures     |
| **Design Patterns**    | Proven solutions to common problems         | Structuring complex systems effectively           |
| **Code Smells**        | Anti-patterns and how to fix them           | Identifying and improving problematic code        |
| **Clean Code**         | Naming, functions, comments, error handling | Writing readable, professional code               |
| **Clean Architecture** | Layered design and separation of concerns   | Building scalable, testable systems               |
| **Code Review**        | Systematic evaluation of code quality       | Ensuring team standards and catching issues early |

## Key Principles

### SOLID Principles

The five pillars of object-oriented design:

1. **S (Single Responsibility)** - Each class has one reason to change
2. **O (Open/Closed)** - Open for extension, closed for modification
3. **L (Liskov Substitution)** - Subclasses can replace parent classes safely
4. **I (Interface Segregation)** - Clients depend only on what they use
5. **D (Dependency Inversion)** - Depend on abstractions, not concretions

**See:** [solid-principles.md](./references/solid-principles.md) for detailed examples of each principle.

### Design Patterns

Proven, reusable solutions for common design problems:

**Creational Patterns** - How to create objects efficiently

- Factory Pattern
- Builder Pattern
- Singleton Pattern (use sparingly!)

**Structural Patterns** - How to organize relationships between objects

- Adapter Pattern
- Decorator Pattern
- Facade Pattern

**Behavioral Patterns** - How objects interact and distribute responsibility

- Strategy Pattern
- Observer Pattern
- Command Pattern

**See:** [design-patterns.md](./references/design-patterns.md) for implementations and use cases.

### Code Smells

Red flags indicating code needs refactoring:

| Smell          | Sign                          | Solution                            |
| -------------- | ----------------------------- | ----------------------------------- |
| Long Functions | > 20 lines of complex logic   | Extract into smaller functions      |
| Deep Nesting   | 3+ levels of indentation      | Use guard clauses and early returns |
| Duplicate Code | Same logic in multiple places | Extract into shared functions       |
| God Objects    | Classes doing too much        | Split into focused classes (SRP)    |
| Magic Numbers  | Unexplained hardcoded values  | Use named constants with context    |

**See:** [code-smells-refactoring.md](./references/code-smells-refactoring.md) for detailed refactoring strategies.

### Clean Code Practices

Writing code that is clear, professional, and maintainable:

**Naming Conventions:**

- Functions: Verbs (calculateTotal, fetchUser, isValid)
- Classes: Nouns (UserService, PaymentProcessor)
- Booleans: Questions (isActive, hasPermission, canEdit)
- Constants: UPPER_SNAKE_CASE with context

**Function Design:**

- Keep under 20 lines when possible
- Do one thing, do it well
- Minimal parameters (≤ 3, use objects for more)
- Consistent return types

**Comments:**

- Explain WHY, not WHAT
- Code shows WHAT, comments explain WHY
- Use JSDoc for public APIs
- Avoid redundant comments

**Error Handling:**

- Use specific error types
- Provide context in error messages
- Never silently fail
- Consider Result type pattern for safe operations

**DRY, KISS, YAGNI:**

- Don't Repeat Yourself - extract shared logic
- Keep It Simple, Stupid - favor clarity over cleverness
- You Aren't Gonna Need It - don't build features you don't need

**See:** [clean-code-practices.md](./references/clean-code-practices.md) for detailed practices and examples.

### Clean Architecture

Building systems with proper separation of concerns:

**The Three Layers:**

```
Presentation Layer (UI) ↑
Application Layer (Services) ↓
Data Layer (Repositories) ↑
Domain Layer (Core Logic) ↓
```

**Key Rule:** Dependencies point inward toward the domain layer.

**Benefits:**

- Easy to test - inject mocks at any layer
- Flexible - swap implementations (MySQL ↔ PostgreSQL, REST ↔ GraphQL)
- Maintainable - clear separation of responsibilities
- Independent - domain logic doesn't depend on frameworks

**Example:**

```typescript
// Domain Layer - Pure business logic
function isAdult(user: User): boolean {
	return user.age >= 18;
}

// Data Layer - Repository interface
interface UserRepository {
	findById(id: string): Promise<User>;
}

// Application Layer - Service
class UserService {
	constructor(private repo: UserRepository) {}
	async getAdultUsers(): Promise<User[]> {
		/* ... */
	}
}

// Presentation Layer - API
app.get("/adults", async (req, res) => {
	const service = new UserService(userRepository);
	const adults = await service.getAdultUsers();
	res.json(adults);
});
```

**See:** [clean-architecture.md](./references/clean-architecture.md) for detailed architecture patterns and React examples.

### Code Review Process

Systematic evaluation of code quality before merging:

**What to Check:**

1. **Correctness** - Does it work? Edge cases handled?
2. **Testing** - Adequate coverage? Tests verify behavior?
3. **Design** - SOLID principles? Over-engineered?
4. **Readability** - Clear names? Self-documenting?
5. **Security** - Input validated? Secrets protected?
6. **Performance** - N+1 queries? Inefficient algorithms?
7. **Error Handling** - All paths covered? Messages helpful?
8. **Documentation** - Complex logic explained? APIs documented?

**Review Categories:**

- **Blocker Issues** - Must fix (security, logic errors, breaking changes)
- **Major Issues** - Fix before merge (error handling, tests, duplication)
- **Minor Issues** - Consider fixing (style, naming, optimization)
- **Suggestions** - Nice to have (educational, alternative approaches)

**See:** [code-review-guidelines.md](./references/code-review-guidelines.md) for detailed checklist and review process.

## Quick Reference

### Naming Checklist

```typescript
✅ function calculateTotal()          // Verb + clear action
✅ function fetchUser()               // Verb + clear action
✅ const isLoading = false            // Boolean as question
✅ const hasPermission = true         // Boolean as question
❌ function process()                 // Too vague
❌ const flag = true                  // No context
```

### Function Size Guidelines

```typescript
✅ 5-15 lines      // Optimal - single responsibility
⚠️  15-20 lines    // Watch - consider extracting
❌ 20+ lines       // Too large - extract methods
```

### SOLID Quick Test

```typescript
S: Does this class have more than one reason to change?     → No ✓
O: Would you need to modify this to add new features?       → No ✓
L: Can subclasses safely replace the parent?                → Yes ✓
I: Do all clients use all methods of this interface?        → Yes ✓
D: Do you depend on concrete classes or abstractions?       → Abstractions ✓
```

## Best Practices Summary

1. **Follow SOLID** - Each principle makes code more flexible
2. **Use patterns** - Proven solutions to common problems
3. **Refactor smells** - Address anti-patterns early
4. **Name clearly** - Names reveal intent
5. **Keep functions small** - Do one thing well
6. **Comment WHY** - Explain reasoning, not mechanics
7. **Handle errors** - No silent failures
8. **Separate concerns** - Domain → Application → Data → Presentation
9. **Inject dependencies** - Loose coupling, easy testing
10. **Review systematically** - Use checklists, focus on quality

## References

- [SOLID Principles](./references/solid-principles.md) — Five design principles explained with examples
- [Design Patterns](./references/design-patterns.md) — Creational, Structural, and Behavioral patterns
- [Code Smells & Refactoring](./references/code-smells-refactoring.md) — Anti-patterns and how to fix them
- [Clean Code Practices](./references/clean-code-practices.md) — Naming, functions, comments, error handling
- [Clean Architecture](./references/clean-architecture.md) — Layered design and dependency flow
- [Code Review Guidelines](./references/code-review-guidelines.md) — Systematic review process and checklists
