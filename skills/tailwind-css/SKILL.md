---
name: tailwind-css
description: Utility-first CSS framework for rapid UI development. Use when building responsive components, styling layouts, implementing dark mode, customizing themes, designing accessible interfaces, optimizing CSS production builds, integrating Tailwind with React/Vue/Svelte projects, using Tailwind plugins, creating component libraries, or applying Tailwind best practices. Covers utility classes, responsive design (sm/md/lg/xl), pseudo-classes (hover/focus/active), configuration, arbitrary values, and performance optimization.
license: Complete terms in LICENSE.txt
---

# Tailwind CSS Skill

Build responsive, modern UIs efficiently with Tailwind's utility-first approach.

## When to Use This Skill

Load this skill when you need to:

- Build UI components with Tailwind utility classes (buttons, cards, forms, modals)
- Style responsive layouts using breakpoint prefixes (sm:, md:, lg:, etc.)
- Implement dark mode with `dark:` prefix
- Customize themes (colors, spacing, fonts) in `tailwind.config.js`
- Optimize CSS output for production
- Apply accessibility patterns with focus-visible and aria-\* utilities
- Integrate Tailwind with React, Vue, Svelte, or other frameworks

## Prerequisites

**Install Tailwind CSS:**

```bash
npm install -D tailwindcss postcss autoprefixer
npx tailwindcss init -p
```

**Configure content paths** in `tailwind.config.js`:

```js
export default {
	content: ["./src/**/*.{js,jsx,ts,tsx}", "./index.html"],
	theme: { extend: {} },
	plugins: [],
};
```

**Add directives** to your main CSS file:

```css
@tailwind base;
@tailwind components;
@tailwind utilities;
```

**Optional:** Install [Tailwind CSS IntelliSense](https://marketplace.visualstudio.com/items?itemName=bradlc.vscode-tailwindcss) for VS Code autocomplete.

## Key Concepts

### Utility-First

Compose low-level utility classes to build any design:

```html
<div class="flex items-center justify-between p-4 bg-white rounded-lg shadow">
	<h2 class="text-lg font-semibold text-gray-900">Title</h2>
	<button class="px-3 py-1 bg-blue-600 text-white rounded hover:bg-blue-700">
		Action
	</button>
</div>
```

### Responsive Design (Mobile-First)

Use breakpoint prefixes: `sm:`, `md:`, `lg:`, `xl:`, `2xl:`

```html
<!-- 1 col mobile, 2 md+, 3 lg+ -->
<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
	<div>Item 1</div>
	<div>Item 2</div>
	<div>Item 3</div>
</div>
```

### State Variants

Apply styles for `hover:`, `focus:`, `focus-visible:`, `active:`, `disabled:`, `dark:`, etc.

```html
<button
	class="bg-blue-600 hover:bg-blue-700 focus-visible:ring-2 active:scale-95 disabled:opacity-50"
>
	Button
</button>
```

## Quick Workflows

**Setup:** Follow Prerequisites above, then start using utilities in templates.

**Build component:** Compose utilities inline, then extract repeated patterns with `@apply` in CSS or component functions.

**Dark mode:** Set `darkMode: 'class'` in config, toggle `dark` class on root element, use `dark:` prefixes.

**Customize theme:** Extend in `tailwind.config.js` under `theme.extend.colors`, `theme.extend.spacing`, etc.

**Optimize:** Configure `content` paths correctly to purge unused CSS. Production builds drop from ~100KB to ~10-20KB.

## Common Patterns & Reference

See [common-patterns.md](./references/common-patterns.md) for 50+ patterns:

- Layout patterns (containers, grids, flexbox centering)
- Typography, spacing, colors, shadows
- Forms, buttons, cards, navigation
- Dark mode, accessibility, animations
- Sizing, overflow, positioning, display utilities

## Responsive & Dark Mode Advanced Guide

See [responsive-dark-mode.md](./references/responsive-dark-mode.md) for:

- Mobile-first design principles and breakpoint strategy
- Dark mode implementation (manual, system preference)
- Advanced responsive techniques (container queries, aspect ratios)
- Performance optimization and testing checklist

## Templates & Examples

**HTML landing page:** [landing-page.html](./templates/landing-page.html)

**React components:** [react-components.jsx](./templates/react-components.jsx) (UserProfileCard, ContactForm with dark mode)

**Full config:** [tailwind.config.js](./templates/tailwind.config.js) with custom colors, spacing, animations, plugins

## Troubleshooting

| Issue                            | Solution                                                                                                                                |
| -------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------- |
| Styles not applying              | Verify `content` paths in `tailwind.config.js` are correct and relative to config location.                                             |
| CSS file too large in production | Ensure `content` paths are set correctly. Avoid dynamic class names like `'bg-' + color`—use safelisting.                               |
| IntelliSense not working         | Install "Tailwind CSS IntelliSense" extension. Restart editor. Ensure `tailwind.config.{js,ts}` exists in project root.                 |
| Dark mode won't toggle           | Use `darkMode: 'class'` in config. Add/remove `dark` class on `<html>` or `<body>`: `document.documentElement.classList.toggle('dark')` |
| PostCSS errors                   | Run `npx tailwindcss init -p` to generate `postcss.config.js`. Ensure tailwindcss plugin listed before autoprefixer.                    |

## Best Practices

✅ **Do:** Start with semantic HTML, then apply utilities. Use mobile-first responsive design. Extract repeated patterns with `@apply` or component functions. Test dark mode thoroughly.

❌ **Avoid:** Over-applying `@apply`. Inlining >10-15 utilities per element. Mixing Tailwind with other CSS frameworks. Assuming arbitrary values perform well—add to config if reused. Forgetting content paths (biggest issue).

## References

- [Tailwind CSS Docs](https://tailwindcss.com/docs)
- [Responsive Design Guide](https://tailwindcss.com/docs/responsive-design)
- [Dark Mode](https://tailwindcss.com/docs/dark-mode)
- [Next.js Integration](https://nextjs.org/docs/app/building-your-application/styling/tailwind-css)
- [Headless UI Components](https://headlessui.com/)
- [Shadcn/ui Component Library](https://ui.shadcn.com/)
