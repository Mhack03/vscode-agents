# TypeScript Project Setup Guide

## Quick Start

### 1. Initialize Project

```bash
mkdir my-app && cd my-app
npm init -y
npm install --save-dev typescript @types/node
```

### 2. Configure TypeScript

Copy `tsconfig.json` to your project root. This configuration:
- Targets ES2020 for modern JavaScript features
- Enables strict mode for maximum type safety
- Enables JSX for React projects
- Maps paths for cleaner imports (`@/*`)
- Generates source maps for debugging

### 3. Create Source Directory

```bash
mkdir src
```

### 4. Add Build Script

Update `package.json`:

```json
{
  "scripts": {
    "build": "tsc",
    "dev": "tsc --watch"
  }
}
```

### 5. Create First File

`src/index.ts`:

```typescript
function greet(name: string): void {
  console.log(`Hello, ${name}!`);
}

greet("World");
```

### 6. Compile

```bash
npm run build
# Output: dist/index.js
```

## Installation by Project Type

### React Project

```bash
npm install react react-dom
npm install --save-dev @types/react @types/react-dom
```

Update `tsconfig.json`:
```json
{
  "compilerOptions": {
    "jsx": "react-jsx"
  }
}
```

### Node.js Backend

```bash
npm install express
npm install --save-dev @types/express @types/node
```

### Full Stack (React + Express)

```bash
npm install react react-dom express
npm install --save-dev @types/react @types/react-dom @types/express @types/node
```

## Development Workflow

### Watch Mode

```bash
npm run dev
```

This automatically recompiles on file changes.

### Using Path Aliases

In `tsconfig.json`:
```json
{
  "baseUrl": "./src",
  "paths": {
    "@/*": ["./*"],
    "@services/*": ["./services/*"],
    "@types/*": ["./types/*"]
  }
}
```

Then import cleanly:
```typescript
import { UserService } from '@services/UserService';
import type { User } from '@types/User';
```

## IDE Setup

### VS Code Extensions

- **TypeScript Vue Plugin** (if using Vue)
- **Prettier** (code formatter)
- **ESLint** (linter)

### VS Code Settings

Create `.vscode/settings.json`:

```json
{
  "editor.defaultFormatter": "esbenp.prettier-vscode",
  "[typescript]": {
    "editor.defaultFormatter": "esbenp.prettier-vscode",
    "editor.formatOnSave": true
  },
  "typescript.enablePromptUseWorkspaceTsdk": true
}
```

## Common Issues

| Issue | Solution |
|-------|----------|
| `Cannot find module '@types/...'` | Run `npm install --save-dev @types/module-name` |
| `Property 'X' does not exist on type 'Y'` | Check spelling; may need @types package |
| `Type 'X' is not assignable to 'Y'` | Verify types match; check union types |
| `Object is possibly 'null'` | Enable `strictNullChecks` and add null checks |

## Next Steps

1. Read [./../../references/](../../references/) for detailed patterns
2. Copy sample code from [./../../sample_codes/common-patterns/](../../sample_codes/common-patterns/)
3. Start with API design or React components based on your needs
