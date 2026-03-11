# Responsive Design & Dark Mode Guide

Advanced patterns for responsive design and dark mode implementation with Tailwind CSS.

## Mobile-First Design Strategy

### Core Principle

Tailwind follows **mobile-first** design:

1. Write base (mobile) styles with no prefix
2. Add `sm:`, `md:`, `lg:`, etc. for larger screens
3. Never override mobile styles with larger breakpoints

### Breakpoints Reference

| Prefix | Min-width | Use Case                  |
| ------ | --------- | ------------------------- |
| (none) | 0px       | Mobile & default          |
| `sm:`  | 640px     | Small mobile landscape    |
| `md:`  | 768px     | Tablets and small laptops |
| `lg:`  | 1024px    | Desktop screens           |
| `xl:`  | 1280px    | Large desktops            |
| `2xl:` | 1536px    | Ultra-wide displays       |

### Modern Mobile Portrait View Sizes

Common smartphone screen sizes in portrait orientation (modern market):

| Device Type             | Width | Use Case             | Examples                                  |
| ----------------------- | ----- | -------------------- | ----------------------------------------- |
| **Mobile S (Portrait)** | 320px | Older/smaller phones | iPhone SE (1st gen), Small Android phones |
| **Mobile M (Portrait)** | 375px | Standard phones      | iPhone 6/7/8/SE (2nd gen), Galaxy S20     |
| **Mobile L (Portrait)** | 435px | Larger phones        | iPhone 12/13/14, Galaxy S21/S22           |
| **Tablet (Portrait)**   | 768px | iPad, tablets        | iPad Mini, Samsung Tab S                  |

**Recommended approach with Tailwind:**

- Start with base styles at **320px** (Mobile S)
- Use `sm:` (640px) for **landscape mobile** orientation
- Use `md:` (768px) for **tablets and up**

### Configuring Custom Mobile Breakpoints (Optional)

If you need custom breakpoints for specific mobile sizes, configure in `tailwind.config.js`:

```js
export default {
	theme: {
		screens: {
			xs: "320px", // Mobile S (portrait)
			sm: "375px", // Mobile M (portrait)
			md: "435px", // Mobile L (portrait)
			lg: "640px", // Landscape mobile
			xl: "768px", // Tablets
			"2xl": "1024px", // Desktop
		},
	},
};
```

Then use them in markup:

```html
<!-- 320px: 1 col, 375px: 1 col, 435px: 2 col, 640px+: 3 col -->
<div class="grid grid-cols-1 sm:grid-cols-1 md:grid-cols-2 lg:grid-cols-3">
	<div>Item 1</div>
	<div>Item 2</div>
	<div>Item 3</div>
</div>
```

### Mobile-First Example

```html
<!-- BAD: Desktop-first (avoid) -->
<div class="grid grid-cols-3 sm:grid-cols-2 xs:grid-cols-1">
	<!-- Breakpoints go backwards! -->
</div>

<!-- GOOD: Mobile-first (recommended) -->
<div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3">
	<!-- Flows naturally from small to large -->
</div>
```

## Mobile-First Design for Modern Devices

When targeting modern smartphones, design for portrait view first:

### Design Flow for Mobile Portrait Sizes

```html
<!-- Base: optimized for Mobile S (320px) -->
<div class="px-3 py-4 text-sm">Fits snugly on small phones</div>

<!-- sm:375px - Mobile M (slight adjustments) -->
<div class="px-3 sm:px-4 py-4 sm:py-5 text-sm sm:text-base">
	More breathing room
</div>

<!-- md:435px - Mobile L (responsive changes) -->
<div
	class="px-3 sm:px-4 md:px-5 py-4 sm:py-5 md:py-6 text-sm sm:text-base md:text-lg"
>
	Larger screens get more content
</div>

<!-- lg+:640px+ - Landscape & tablets (full layouts) -->
<div
	class="px-3 sm:px-4 md:px-5 lg:px-8 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3"
>
	Side-by-side on bigger screens
</div>
```

### Mobile S (320px) Constraints & Best Practices

**Limited space - be careful with:**

- Avoid wide components; max container width: 288px
- Use compact spacing: `px-3` or `px-4` (12-16px)
- Text: single column layouts only
- Buttons: full-width or side-by-side small buttons
- Images: full container width with proper aspect ratio

```html
<!-- Good for 320px: Compact, single column -->
<div class="max-w-sm px-3">
	<input type="text" class="w-full px-2 py-1 border rounded text-sm" />
	<button class="w-full mt-2 px-3 py-2 bg-blue-600 text-white rounded text-sm">
		Submit
	</button>
</div>
```

### Mobile M (375px) - Sweet Spot for Most Users

**Most common size - optimize here:**

- Comfortable spacing: `px-4` (16px)
- Typography: standard sizes work well
- Two-column layouts possible but cramped
- Avoid icons smaller than 24px (touch targets)

```html
<!-- Good for 375px: Reasonable spacing -->
<div class="px-4 py-6">
	<h2 class="text-lg font-bold mb-3">Title</h2>
	<p class="text-sm text-gray-600 mb-4">Description</p>
	<button class="w-full px-4 py-2 bg-blue-600 text-white rounded">
		Action
	</button>
</div>
```

### Mobile L (435px) - Enable 2-Column Layouts

**Larger phones - start multi-column:**

- Two-column grid becomes practical
- Touch targets can be slightly smaller
- More breathing room for content

```html
<!-- Good for 435px+: Start side-by-side layouts -->
<div class="grid grid-cols-2 gap-3 px-3 sm:px-4 md:px-5">
	<div class="bg-white rounded p-3 text-center">
		<span class="text-2xl">📱</span>
		<p class="text-xs font-semibold mt-2">Feature 1</p>
	</div>
	<div class="bg-white rounded p-3 text-center">
		<span class="text-2xl">🎨</span>
		<p class="text-xs font-semibold mt-2">Feature 2</p>
	</div>
</div>
```

## Common Responsive Patterns

### 2-Column to 1-Column Layout

```html
<!-- Stacked on mobile, side-by-side on md+ -->
<div class="grid grid-cols-1 md:grid-cols-2 gap-8">
	<aside class="md:order-1">Sidebar</aside>
	<main class="md:order-2">Content</main>
</div>
```

### 3-Column to 2-Column to 1-Column

```html
<!-- 1 col mobile, 2 md, 3 lg -->
<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
	<div>Card 1</div>
	<div>Card 2</div>
	<div>Card 3</div>
</div>
```

### Responsive Font Sizes

```html
<!-- Scales across breakpoints -->
<h1
	class="text-2xl sm:text-3xl md:text-4xl lg:text-5xl font-bold leading-tight"
>
	Responsive Headline
</h1>

<p class="text-base sm:text-lg md:text-xl">Responsive body text</p>
```

### Responsive Spacing

```html
<!-- Different padding per screen size -->
<section class="px-4 sm:px-6 md:px-8 lg:px-12 py-8 sm:py-12 md:py-16">
	<!-- 1rem on mobile, 1.5rem sm, 2rem md, 3rem lg -->
	Content with responsive padding
</section>
```

### Navigation: Hide/Show Elements

```html
<nav class="flex items-center justify-between">
	<!-- Logo always visible -->
	<div class="font-bold">Logo</div>

	<!-- Desktop menu hidden on mobile -->
	<ul class="hidden md:flex gap-6">
		<li><a href="#">Home</a></li>
		<li><a href="#">About</a></li>
		<li><a href="#">Contact</a></li>
	</ul>

	<!-- Mobile hamburger hidden on md+ -->
	<button class="md:hidden">
		<svg class="w-6 h-6">Menu icon</svg>
	</button>
</nav>
```

### Responsive Container

```html
<!-- Constrained width on desktop, full on mobile -->
<div class="w-full sm:max-w-2xl md:max-w-4xl lg:max-w-6xl mx-auto px-4">
	Container content
</div>
```

## Dark Mode Implementation

### Setup in tailwind.config.js

```js
export default {
	// Use 'class' for manual toggle
	darkMode: "class",

	// OR use 'media' to follow system preference
	// darkMode: 'media',

	// Rest of config...
};
```

### Manual Dark Mode Toggle (Recommended)

```jsx
// React example
import { useEffect, useState } from "react";

export function useTheme() {
	const [isDark, setIsDark] = useState(() => {
		// Check localStorage or system preference
		const stored = localStorage.getItem("theme");
		if (stored) return stored === "dark";
		return window.matchMedia("(prefers-color-scheme: dark)").matches;
	});

	useEffect(() => {
		const root = document.documentElement;
		if (isDark) {
			root.classList.add("dark");
			localStorage.setItem("theme", "dark");
		} else {
			root.classList.remove("dark");
			localStorage.setItem("theme", "light");
		}
	}, [isDark]);

	return [isDark, setIsDark];
}

export function ThemeToggle() {
	const [isDark, setIsDark] = useTheme();

	return (
		<button
			onClick={() => setIsDark(!isDark)}
			className="p-2 rounded-lg bg-gray-100 dark:bg-gray-800"
			aria-label="Toggle dark mode"
		>
			{isDark ? "☀️" : "🌙"}
		</button>
	);
}
```

### System Preference Dark Mode

```jsx
// Listen to system theme changes
export function useSystemTheme() {
	const [isDark, setIsDark] = useState(() => {
		return window.matchMedia("(prefers-color-scheme: dark)").matches;
	});

	useEffect(() => {
		const media = window.matchMedia("(prefers-color-scheme: dark)");
		const handler = (e) => setIsDark(e.matches);

		// Modern API
		media.addEventListener("change", handler);
		return () => media.removeEventListener("change", handler);
	}, []);

	return isDark;
}
```

### Dark Mode Styling Patterns

#### Simple Color Swap

```html
<!-- Light mode (default) / Dark mode (with dark: prefix) -->
<div class="bg-white dark:bg-gray-900 text-gray-900 dark:text-white">
	Content adapts to theme
</div>
```

#### Different Styles Per Mode

```html
<button
	class="
  px-4 py-2 rounded-lg font-semibold
  bg-blue-600 text-white
  hover:bg-blue-700
  dark:bg-blue-500
  dark:hover:bg-blue-600
"
>
	Themed Button
</button>
```

#### Conditional Elements

```html
<!-- Only show in light mode -->
<div class="block dark:hidden">Light mode content</div>

<!-- Only show in dark mode -->
<div class="hidden dark:block">Dark mode content</div>
```

#### Shadow Adjustments

```html
<!-- Shadows more prominent in dark mode -->
<div class="shadow-md dark:shadow-lg">Box with responsive shadow</div>
```

#### Opacity Adjustments

```html
<!-- More contrast in dark mode -->
<div class="bg-blue-600 bg-opacity-50 dark:bg-opacity-70">Adaptive opacity</div>
```

### Dark Mode Border Colors

```html
<!-- Borders adapt to theme -->
<input
	class="
    border border-gray-300
    dark:border-gray-600
    focus-visible:ring-2
    focus-visible:ring-blue-500
    dark:focus-visible:ring-blue-400
  "
/>
```

## Advanced Responsive Techniques

### Container Queries (Newer Approach)

```css
/* In your CSS file with @layer */
@layer components {
	@container (min-width: 400px) {
		.card-title {
			@apply text-xl;
		}
	}

	@container (min-width: 600px) {
		.card-title {
			@apply text-2xl;
		}
	}
}
```

### Aspect Ratio Pattern

```html
<!-- 16:9 video frame -->
<div class="aspect-video">
	<video src="video.mp4"></video>
</div>

<!-- 1:1 square image -->
<div class="aspect-square">
	<img src="image.jpg" class="w-full h-full object-cover" />
</div>
```

### Responsive Images

```html
<!-- Images scale with container -->
<picture>
	<source media="(max-width: 640px)" srcset="image-small.jpg" />
	<source media="(max-width: 1024px)" srcset="image-medium.jpg" />
	<img src="image-large.jpg" class="w-full h-auto" />
</picture>
```

### Flexbox Responsive Wrapping

```html
<!-- Items stack when space is tight -->
<div class="flex flex-wrap gap-4">
	<div class="flex-1 min-w-[250px]">Item 1</div>
	<div class="flex-1 min-w-[250px]">Item 2</div>
</div>
```

## Dark Mode with Custom Colors

### Extended Config

```js
export default {
	theme: {
		extend: {
			colors: {
				brand: {
					light: "#e8f4ff",
					DEFAULT: "#0066cc",
					dark: "#004399",
				},
			},
		},
	},
};
```

### Usage

```html
<!-- Uses brand color, darker version in dark mode -->
<div class="bg-brand dark:bg-brand-dark">Automatically themed</div>
```

## Performance Optimization

### Content Configuration

```js
// tailwind.config.js
export default {
	content: [
		"./src/**/*.{js,jsx,ts,tsx}",
		"!./src/**/*.test.*", // Exclude tests
		"!./src/**/*.stories.*", // Exclude storybook
	],
};
```

### Production Build

```bash
# Development (full CSS)
npm run dev

# Production (purged CSS, ~90% smaller)
npm run build
```

## Testing Dark Mode

### Visual Testing Checklist

- [ ] Text has sufficient contrast in both themes
- [ ] Borders are visible in both themes
- [ ] Images look good with background color
- [ ] Shadows are appropriate for each theme
- [ ] Focus indicators are visible in both themes
- [ ] All interactive states (hover, focus, active) work in both themes
- [ ] Form inputs look correct in both themes
- [ ] Custom images/SVGs work well in both themes

### CSS-in-JS Dark Mode (for styled-components, etc.)

```jsx
// If using CSS-in-JS, ensure .dark class is applied to root
const darkModeSupport = `
  [data-theme="dark"] {
    color-scheme: dark;
  }
`;

// Make sure to add/remove 'dark' class from <html> or <body>
document.documentElement.classList.toggle("dark", isDarkMode);
```

## Testing Mobile Portrait View

### Mobile Device Testing Checklist

Test your app on these real device widths (or browser dev tools viewport sizes):

#### Mobile S (320px)

- [ ] Layout doesn't overflow horizontally
- [ ] Text is readable without horizontal scroll
- [ ] Buttons and touch targets are ≥44px (ideally ≥48px height)
- [ ] Images/videos scale properly within container
- [ ] Form inputs have enough padding and are easy to tap
- [ ] No elements hidden due to width constraints
- [ ] Navigation is accessible (hamburger menu works)

#### Mobile M (375px)

- [ ] Content has breathing room with appropriate padding
- [ ] Typography hierarchy is clear and readable
- [ ] Two-column layouts don't feel cramped
- [ ] Icons and status indicators are visible
- [ ] Cards/components don't feel squeezed

#### Mobile L (435px)

- [ ] Two-column grids look balanced
- [ ] Three-column layouts can start appearing
- [ ] Spacing feels proportional
- [ ] Images maintain good aspect ratios
- [ ] No unnecessary scrolling for key content

#### General Testing Tips

- [ ] Test with actual mobile devices (not just browser emulation)
- [ ] Test in both portrait and landscape orientations
- [ ] Check touch targets: minimum 44x44px, ideally 48x48px
- [ ] Verify readability at arm's length distance
- [ ] Test with slow 3G/4G networks for performance
- [ ] Test on real keyboards (iOS and Android software keyboards)
- [ ] Verify all forms submit correctly on small screens
- [ ] Test with screen readers (VoiceOver, TalkBack)

### Browser DevTools Viewport Testing

Use your browser's responsive design mode to test at these sizes:

```javascript
// Device sizes to test (width x height, portrait)
const testDevices = [
	{ name: "Mobile S", width: 320, height: 568 }, // iPhone SE
	{ name: "Mobile M", width: 375, height: 667 }, // iPhone 8/SE 2nd gen
	{ name: "Mobile L", width: 435, height: 812 }, // iPhone 12/13/14
	{ name: "Landscape", width: 812, height: 375 }, // Mobile L landscape
	{ name: "Tablet", width: 768, height: 1024 }, // iPad Mini
];
```

### Best Devices to Test On

| Device              | Width       | OS      | Notes                        |
| ------------------- | ----------- | ------- | ---------------------------- |
| iPhone SE (3rd gen) | 375px       | iOS     | Standard size representative |
| iPhone 14/14 Pro    | 390px/393px | iOS     | Current flagship             |
| Samsung Galaxy S20  | 360px       | Android | Popular Android phone        |
| Samsung Galaxy S23  | 360px       | Android | Latest flagship              |
| OnePlus 11          | 432px       | Android | Large Android phone          |
| iPad Mini           | 768px       | iOS     | Tablet reference             |

## Best Practices

### ✅ Do's

- Start with mobile-first base styles
- Use semantic breakpoint naming (`md:`, `lg:`)
- Test at actual breakpoint widths on real devices
- Provide dark mode toggle for better UX
- Use system preference as fallback
- Store user preference in localStorage
- Test all interactive elements in both themes
- Design for 320px minimum width
- Use touch-friendly sizing (48px minimum for buttons)

### ❌ Avoid

- Don't use arbitrary breakpoints—stick to Tailwind defaults
- Don't make assumptions about screen size—test on real devices
- Don't forget focus states in dark mode
- Don't use plain black/white—use shades of gray
- Don't skip testing dark mode thoroughly
- Don't force dark mode if users prefer light
- Don't forget to test on actual small phones (not just browser zoom)
- Don't use small touch targets (<44px)
