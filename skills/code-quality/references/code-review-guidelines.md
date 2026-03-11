# Code Review Guidelines & Checklists

Systematic approach to reviewing code for quality, correctness, and maintainability.

## Code Review Checklist

Use this checklist when reviewing pull requests or before committing code.

### Correctness & Logic

- [ ] Does the code implement the requested feature/fix?
- [ ] Does it work as intended in all scenarios?
- [ ] Are edge cases handled properly?
- [ ] Are there any logical errors or off-by-one mistakes?
- [ ] Are boundary conditions checked (null, empty, max/min values)?
- [ ] Is error handling appropriate and complete?

**Example questions:**

- What happens if input is null or empty?
- What happens at the 1st and last iteration of loops?
- Are all code paths tested?

### Testing

- [ ] Are there adequate unit tests?
- [ ] Do tests cover happy path and error scenarios?
- [ ] Are edge cases tested?
- [ ] Is test code clean and maintainable?
- [ ] Are mocks/stubs used appropriately?
- [ ] Do tests actually verify behavior (not just pass)?

**Example issues:**

```typescript
// ❌ Bad test - doesn't verify anything
test("user updates", () => {
	updateUser(user);
	// No assertion!
});

// ✅ Good test - clear behavior verification
test("should update user name and save to database", () => {
	const result = updateUser(user, { name: "Jane" });
	expect(result.name).toBe("Jane");
	expect(mockDb.save).toHaveBeenCalledWith(result);
});
```

### Design & Architecture

- [ ] Does it follow SOLID principles?
- [ ] Is the code DRY (no unnecessary duplication)?
- [ ] Are appropriate abstractions used?
- [ ] Is the code over-engineered?
- [ ] Could this be simplified?
- [ ] Are design patterns used correctly?
- [ ] Do responsibilities seem well-separated?

**Red flags:**

- God classes doing too much
- Deep inheritance hierarchies
- Circular dependencies
- Tight coupling to frameworks

### Code Quality & Readability

- [ ] Are variable/function/class names descriptive?
- [ ] Is the code self-documenting?
- [ ] Are comments helpful (not just repeating code)?
- [ ] Is there unexplained "magic"?
- [ ] Are functions appropriately sized?
- [ ] Is nesting depth reasonable?
- [ ] Is code duplication eliminated?

**Example issues:**

```typescript
// ❌ Poor naming
function f(x: any[]) {
	return x.filter((v) => v > t).map((v) => v * m);
}

// ✅ Clear naming
function calculateSizedItems(items: Item[]) {
	const MIN_THRESHOLD = 100;
	const MULTIPLIER = 2;
	return items
		.filter((item) => item.value > MIN_THRESHOLD)
		.map((item) => item.value * MULTIPLIER);
}
```

### Security

- [ ] Is all user input validated and sanitized?
- [ ] Are secrets stored securely (not hardcoded)?
- [ ] Is SQL injection prevented (parameterized queries)?
- [ ] Are XSS vulnerabilities addressed?
- [ ] Is authentication/authorization implemented correctly?
- [ ] Are sensitive operations logged?
- [ ] Are error messages not leaking sensitive info?

**Security checklist:**

```typescript
// ❌ Security issues
const query = `SELECT * FROM users WHERE email = '${email}'`; // SQL injection!
localStorage.setItem("token", apiKey); // Exposed!
res.json({ error: error.message, stack: error.stack }); // Info leak!

// ✅ Secure
const user = await db.query("SELECT * FROM users WHERE email = ?", [email]);
sessionStorage.setItem("token", apiKey); // HttpOnly cookie better
if (isDevelopment) res.json({ error: error.message });
else res.status(500).json({ error: "Internal server error" });
```

### Performance

- [ ] Are there unnecessary loops or queries?
- [ ] Are appropriate data structures used?
- [ ] Could algorithms be optimized?
- [ ] Are there N+1 query problems?
- [ ] Is caching used appropriately?
- [ ] Are there potential memory leaks?
- [ ] Is rendering performance considered (React)?

**Example issues:**

```typescript
// ❌ Performance issues
for (const item of items) {
	const user = await db.users.findById(item.userId); // N+1 queries!
}

// ✅ Optimized
const userIds = items.map((i) => i.userId);
const users = await db.users.findByIds(userIds);
```

### Error Handling

- [ ] Are all errors caught and handled?
- [ ] Are error messages descriptive and actionable?
- [ ] Are there silent failures?
- [ ] Is error context preserved (stack traces)?
- [ ] Are error types specific enough?
- [ ] Is error recovery possible?

### Documentation

- [ ] Are complex algorithms commented?
- [ ] Are non-obvious decisions explained?
- [ ] Is public API documented (JSDoc/TSDoc)?
- [ ] Is the README updated if needed?
- [ ] Are breaking changes documented?
- [ ] Are examples provided for complex features?

---

## Types of Code Review Issues

### Blocker Issues (Must Fix)

- Security vulnerabilities
- Critical logic errors
- Breaking API changes without migration
- Infinite loops or performance disasters
- Unhandled exceptions

### Major Issues (Fix Before Merge)

- Missing error handling
- Insufficient test coverage
- SOLID principle violations
- Significant code duplication
- Missing or incorrect documentation

### Minor Issues (Consider Fixing)

- Code style inconsistencies
- Sub-optimal variable names
- Over-commenting
- Dead code

### Suggestions (Nice to Have)

- Performance optimizations
- Refactoring suggestions
- Alternative approaches
- Educational tips

---

## Review Process

### 1. Understand the Context

- Read the PR description
- Understand the feature/fix
- Check for related issues
- Review acceptance criteria

### 2. Check Design First

- Are architectural decisions sound?
- Does it fit with existing codebase?
- Are there better approaches?

### 3. Read the Code

- Follow the logic flow
- Trace through examples
- Consider edge cases
- Look for code smells

### 4. Verify Tests

- Are tests adequate?
- Do they test behavior?
- Are new scenarios covered?
- Would you trust passing tests?

### 5. Provide Feedback

**Constructive feedback template:**

```
❌ Problem: [What's the issue?]
✅ Suggestion: [How to fix it]
📖 Why: [Context/explanation]
```

**Example:**

```
❌ Problem: UserService depends on MySQLDatabase directly

✅ Suggestion: Inject a Database interface instead:
   constructor(database: Database) {}

📖 Why: Makes code testable with mocks and allows database switching without service changes
```

---

## Pre-Commit Checklist

Before pushing code:

**Design & Architecture:**

- [ ] Each class/module has single responsibility
- [ ] Code is open for extension, closed for modification
- [ ] No circular dependencies
- [ ] Appropriate abstractions used

**Code Quality:**

- [ ] Functions < 20 lines (ideally)
- [ ] Nesting depth < 3 levels
- [ ] No code duplication (DRY)
- [ ] No magic numbers
- [ ] No dead/commented code

**Naming:**

- [ ] All names are descriptive
- [ ] Boolean variables ask questions (isActive, hasPermission)
- [ ] Functions are verbs (fetchUser, calculateTotal)
- [ ] Classes are nouns (UserService, PaymentProcessor)

**Error Handling:**

- [ ] All errors caught and handled appropriately
- [ ] Error messages are clear
- [ ] No silent failures
- [ ] Custom error types when needed

**Testing:**

- [ ] Unit tests cover critical logic
- [ ] Edge cases tested
- [ ] Tests are readable
- [ ] Mocks used appropriately

**Security:**

- [ ] User input validated/sanitized
- [ ] No hardcoded secrets
- [ ] SQL injection prevented
- [ ] XSS protections present

**Performance:**

- [ ] No N+1 queries
- [ ] Efficient algorithms
- [ ] Appropriate data structures
- [ ] No memory leaks

**Documentation:**

- [ ] Complex logic commented
- [ ] Public APIs documented
- [ ] README updated
- [ ] Commit message clear

**Tools:**

- [ ] Code passes linting
- [ ] TypeScript type checking passes
- [ ] Tests pass locally
- [ ] No console.log/debugging code
