---
name: typescript-patterns
description: Production TypeScript patterns, type design, generics, and workflows for type-safe applications. Use when typing React components, APIs, hooks, generics, discriminated unions, runtime validation (Zod), or migrating JavaScript to TypeScript. 
metadata: TypeScript, generics, discriminated-unions, zod, react-types, api-design
license: Complete terms in LICENSE.txt
---

# TypeScript Patterns

Production patterns for TypeScript covering type design, generics, React/Node typings, and runtime validation.

## When to Use This Skill

- Designing type-safe APIs with discriminated unions and Result types
- Typing React components, hooks, and context
- Implementing advanced generics and utility types
- Adding runtime validation with Zod
- Migrating JavaScript to TypeScript safely
- Fixing type errors and null safety issues

## Prerequisites

```bash
npm install typescript@latest
npm install zod  # For runtime validation (optional)
```

Configure `tsconfig.json`:

```json
{
  "compilerOptions": {
    "strict": true,
    "strictNullChecks": true,
    "target": "ES2020"
  }
}
```

## Workflows

1. **Type-safe API responses** — Design discriminated union Result types
   - Reference: [./references/api-design.md](./references/api-design.md)
   - Sample: [./sample_codes/common-patterns/result-type.ts](./sample_codes/common-patterns/result-type.ts)

2. **React component typing** — Properly type components, hooks, and refs
   - Reference: [./references/react-patterns.md](./references/react-patterns.md)
   - Sample: [./sample_codes/common-patterns/react-typed-component.tsx](./sample_codes/common-patterns/react-typed-component.tsx)

3. **Runtime validation** — Combine Zod schemas with TypeScript types
   - Reference: [./references/runtime-validation.md](./references/runtime-validation.md)
   - Sample: [./sample_codes/common-patterns/zod-validation.ts](./sample_codes/common-patterns/zod-validation.ts)

4. **Advanced generics** — Build reusable, type-safe utilities
   - Reference: [./references/advanced-types.md](./references/advanced-types.md)
   - Sample: [./sample_codes/common-patterns/advanced-generics.ts](./sample_codes/common-patterns/advanced-generics.ts)

5. **JavaScript migration** — Incrementally adopt TypeScript
   - Reference: [./references/migration-guide.md](./references/migration-guide.md)
   - Guide: [./sample_codes/getting-started/setup-guide.md](./sample_codes/getting-started/setup-guide.md)

## Troubleshooting

| Problem | Solution |
|---------|----------|
| `Object is possibly 'null'` | Enable `strictNullChecks` and add explicit guards (`if (x == null)`) |
| `Type 'X' is not assignable to 'Y'` | Add discriminant fields (`kind: 'error' \| 'success'`) and narrow unions |
| Generics lose inference | Add explicit constraints (`T extends object`) or helper overloads |
| Runtime mismatches with API types | Use Zod for schema validation and derive types with `z.infer<typeof schema>` |

## Getting Started

- **New to TypeScript?** Start with [./sample_codes/getting-started/setup-guide.md](./sample_codes/getting-started/setup-guide.md)
- **Sample config:** [./sample_codes/getting-started/tsconfig.json](./sample_codes/getting-started/tsconfig.json)
- **Full examples:** [./sample_codes/common-patterns/](./sample_codes/common-patterns/)

## References

- Local guides: [./references/](./references/)
- Official TypeScript Handbook: https://www.typescriptlang.org/docs/handbook/
- Zod documentation: https://github.com/colinhacks/zod
