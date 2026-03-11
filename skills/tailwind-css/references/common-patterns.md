# Tailwind CSS Common Patterns & Reference

A quick reference guide for frequently-used Tailwind CSS patterns and utility combinations.

## Layout Patterns

### Container with Max Width & Centering

```html
<!-- Standard centered container with max-width -->
<div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">Content</div>
```

### Responsive Grid

```html
<!-- 1 col mobile, 2 md, 3 lg, 4 xl -->
<div
	class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4"
>
	<div>Item 1</div>
	<div>Item 2</div>
</div>
```

### Flexbox Center (Horizontal & Vertical)

```html
<!-- Centers content both ways -->
<div class="flex items-center justify-center h-screen">
	<div>Centered</div>
</div>
```

### Sticky Header

```html
<header class="sticky top-0 z-50 bg-white shadow-sm">
	<nav class="max-w-7xl mx-auto px-4 h-16 flex items-center justify-between">
		Logo
	</nav>
</header>
```

## Responsive Typography

### Responsive Text Sizes

```html
<!-- Scales: text-xl on mobile, 2xl on sm, 3xl on md, 4xl on lg -->
<h1 class="text-xl sm:text-2xl md:text-3xl lg:text-4xl font-bold">
	Responsive Heading
</h1>
```

### Text Utilities

```html
<!-- Font sizes -->
<p class="text-xs">Extra small</p>
<p class="text-sm">Small</p>
<p class="text-base">Base (default)</p>
<p class="text-lg">Large</p>
<p class="text-xl">Extra large</p>

<!-- Font weights -->
<p class="font-thin">Thin (100)</p>
<p class="font-light">Light (300)</p>
<p class="font-normal">Normal (400)</p>
<p class="font-semibold">Semibold (600)</p>
<p class="font-bold">Bold (700)</p>
<p class="font-black">Black (900)</p>

<!-- Text formatting -->
<p class="uppercase">Uppercase</p>
<p class="lowercase">Lowercase</p>
<p class="capitalize">Capitalize</p>
<p class="italic">Italic</p>
<p class="underline">Underline</p>
<p class="line-through">Strikethrough</p>
```

## Spacing Patterns

### Padding Consistency

```html
<!-- All directions -->
<div class="p-4">All sides</div>

<!-- Horizontal/Vertical -->
<div class="px-4 py-2">Horizontal 1rem, Vertical 0.5rem</div>

<!-- Individual sides -->
<div class="pt-4 pr-6 pb-2 pl-8">Top/Right/Bottom/Left</div>
```

### Gap Between Elements

```html
<!-- Flex with gap -->
<div class="flex gap-4">
	<div>Item 1</div>
	<div>Item 2</div>
</div>

<!-- Grid with gap -->
<div class="grid gap-4">
	<div>Row 1</div>
	<div>Row 2</div>
</div>

<!-- Stack with space-y -->
<div class="space-y-4">
	<div>Item 1</div>
	<div>Item 2</div>
	<div>Item 3</div>
</div>
```

## Color Utilities

### Text Colors

```html
<!-- Solid colors -->
<p class="text-white">White text</p>
<p class="text-gray-900">Dark gray</p>
<p class="text-blue-600">Blue</p>

<!-- With hover state -->
<a href="#" class="text-blue-600 hover:text-blue-800">Link</a>
```

### Background Colors

```html
<div class="bg-white">White background</div>
<div class="bg-blue-50">Light blue</div>
<div class="bg-blue-600">Blue</div>

<!-- Dark mode -->
<div class="bg-white dark:bg-gray-900">Light or dark depending on mode</div>
```

### Border Colors

```html
<div class="border border-gray-300">Gray border</div>
<div class="border-2 border-blue-600">Thicker blue border</div>
<div class="border-l-4 border-green-500">Left border accent</div>
```

## Shadow & Depth

### Box Shadows

```html
<div class="shadow-sm">Small shadow</div>
<div class="shadow">Default shadow</div>
<div class="shadow-md">Medium shadow</div>
<div class="shadow-lg">Large shadow</div>
<div class="shadow-xl">Extra large shadow</div>
<div class="shadow-2xl">2x large shadow</div>

<!-- Hover effect shadow -->
<div class="shadow-md hover:shadow-xl transition-shadow">Elevates on hover</div>
```

## Rounded Corners

### Border Radius

```html
<div class="rounded">Default radius</div>
<div class="rounded-sm">Small radius</div>
<div class="rounded-md">Medium radius</div>
<div class="rounded-lg">Large radius</div>
<div class="rounded-xl">Extra large radius</div>
<div class="rounded-full">Circle/pill shape</div>

<!-- Individual corners -->
<div class="rounded-t-lg">Top corners</div>
<div class="rounded-b-lg">Bottom corners</div>
<div class="rounded-l-lg">Left corners</div>
<div class="rounded-r-lg">Right corners</div>
```

## Form Input Styling

### Standard Input

```html
<input
	type="text"
	class="w-full px-3 py-2 border border-gray-300 rounded-md focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 focus-visible:border-transparent"
	placeholder="Enter text..."
/>
```

### Textarea

```html
<textarea
	class="w-full px-3 py-2 border border-gray-300 rounded-md focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 resize-none"
	placeholder="Enter message..."
	rows="4"
></textarea>
```

### Checkbox & Radio

```html
<!-- Styled checkbox -->
<input
	type="checkbox"
	class="w-4 h-4 text-blue-600 rounded focus-visible:ring-2 focus-visible:ring-blue-500"
/>

<!-- Radio button -->
<input
	type="radio"
	class="w-4 h-4 text-blue-600 focus-visible:ring-2 focus-visible:ring-blue-500"
/>
```

### Select Dropdown

```html
<select
	class="w-full px-3 py-2 border border-gray-300 rounded-md focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-500"
>
	<option>Select an option</option>
	<option>Option 1</option>
	<option>Option 2</option>
</select>
```

## Buttons

### Primary Button

```html
<button
	class="px-4 py-2 bg-blue-600 text-white font-semibold rounded-lg hover:bg-blue-700 focus-visible:ring-2 focus-visible:ring-blue-300 active:scale-95 transition"
>
	Submit
</button>
```

### Secondary Button

```html
<button
	class="px-4 py-2 bg-gray-100 text-gray-900 font-semibold rounded-lg hover:bg-gray-200 focus-visible:ring-2 focus-visible:ring-gray-300 transition"
>
	Cancel
</button>
```

### Outline Button

```html
<button
	class="px-4 py-2 border-2 border-blue-600 text-blue-600 font-semibold rounded-lg hover:bg-blue-50 focus-visible:ring-2 focus-visible:ring-blue-300 transition"
>
	Outline
</button>
```

### Icon Button

```html
<button
	class="p-2 text-gray-600 hover:text-gray-900 hover:bg-gray-100 rounded-lg transition"
>
	<svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
		<!-- SVG icon -->
	</svg>
</button>
```

## Cards & Containers

### Standard Card

```html
<div class="bg-white rounded-lg shadow-md p-6 hover:shadow-lg transition">
	<!-- Card content -->
</div>
```

### Card with Header

```html
<div class="bg-white rounded-lg shadow-md overflow-hidden">
	<div class="bg-blue-600 text-white px-6 py-4">
		<h3 class="text-lg font-semibold">Card Title</h3>
	</div>
	<div class="px-6 py-4">
		<!-- Card body -->
	</div>
</div>
```

### Card with Hover Effect

```html
<div
	class="bg-white rounded-lg shadow-md p-6 hover:shadow-xl hover:scale-105 cursor-pointer transition transform duration-200"
>
	<!-- Card content -->
</div>
```

## Navigation

### Top Navigation Bar

```html
<nav class="bg-white shadow-sm sticky top-0 z-50">
	<div class="max-w-7xl mx-auto px-4 h-16 flex items-center justify-between">
		<div class="font-bold text-lg">Logo</div>

		<!-- Desktop menu -->
		<ul class="hidden md:flex gap-8">
			<li><a href="#" class="text-gray-600 hover:text-gray-900">Link 1</a></li>
			<li><a href="#" class="text-gray-600 hover:text-gray-900">Link 2</a></li>
		</ul>

		<!-- Mobile menu button -->
		<button class="md:hidden">
			<!-- Hamburger icon -->
		</button>
	</div>
</nav>
```

## Dark Mode

### Conditional Dark Styling

```html
<!-- Toggle visibility based on theme -->
<div class="bg-white dark:bg-gray-900 text-gray-900 dark:text-white">
	Content adapts to dark mode
</div>

<!-- Different styles per mode -->
<button
	class="bg-blue-600 dark:bg-blue-500 hover:bg-blue-700 dark:hover:bg-blue-600"
>
	Button
</button>
```

## Accessibility

### Visible Focus Indicator

```html
<button
	class="px-4 py-2 bg-blue-600 text-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:ring-blue-500"
>
	Keyboard Accessible
</button>
```

### Screen Reader Text

```html
<button>
	<span class="sr-only">Close menu</span>
	<svg class="w-6 h-6" aria-hidden="true"><!-- Icon --></svg>
</button>
```

### ARIA Attributes

```html
<input type="text" aria-label="Search products" />
<button aria-expanded="false">Toggle Menu</button>
<div role="alert" class="text-red-600">Error message</div>
```

## Transitions & Animations

### Hover Transitions

```html
<!-- Color transition -->
<div class="bg-gray-100 hover:bg-blue-100 transition-colors duration-200">
	Background color fades
</div>

<!-- Transform transition -->
<div class="transform hover:scale-105 hover:shadow-lg transition-transform">
	Scales up on hover
</div>

<!-- Opacity transition -->
<div class="opacity-50 hover:opacity-100 transition-opacity">
	Fades in on hover
</div>
```

### Animation Classes

```html
<!-- Pulse animation -->
<div class="animate-pulse">Pulsing element</div>

<!-- Spin animation -->
<svg class="animate-spin w-6 h-6"><!-- SVG --></svg>

<!-- Bounce animation -->
<div class="animate-bounce">Bouncing element</div>

<!-- Ping animation -->
<span class="animate-ping"></span>
```

## Sizing

### Width Utilities

```html
<div class="w-full">100% of parent</div>
<div class="w-1/2">50% of parent</div>
<div class="w-1/3">33.33% of parent</div>
<div class="w-1/4">25% of parent</div>
<div class="w-96">24rem (384px)</div>
<div class="w-screen">100vw</div>
```

### Height Utilities

```html
<div class="h-full">100% of parent</div>
<div class="h-screen">100vh</div>
<div class="h-96">24rem (384px)</div>
<div class="min-h-96">Minimum height</div>
<div class="max-h-96">Maximum height</div>
```

## Overflow & Truncation

### Text Truncation

```html
<!-- Single line truncation -->
<p class="truncate">This text will overflow with ellipsis...</p>

<!-- Multiple line truncation -->
<p class="line-clamp-2">Two lines max with overflow...</p>
<p class="line-clamp-3">Three lines max with overflow...</p>
```

### Scroll Behavior

```html
<div class="overflow-auto">Scrollable content</div>
<div class="overflow-hidden">Hidden overflow</div>
<div class="overflow-x-auto">Horizontal scroll only</div>
```

## Positioning

### Absolute Positioning

```html
<div class="relative">
	<div class="absolute top-0 right-0">Top right</div>
	<div class="absolute bottom-0 left-0">Bottom left</div>
	<div class="absolute inset-0">Covers parent</div>
</div>
```

### Fixed Positioning

```html
<button class="fixed bottom-4 right-4 z-50">Floating action button</button>
```

## Display & Visibility

### Display Types

```html
<div class="block">Block element</div>
<div class="inline-block">Inline block</div>
<div class="flex">Flexbox</div>
<div class="grid">CSS Grid</div>
<div class="hidden">Hidden from display</div>
<div class="invisible">Hidden but takes space</div>
```

### Responsive Display

```html
<!-- Hide on mobile, show on md+ -->
<div class="hidden md:block">Desktop only</div>

<!-- Show on mobile, hide on md+ -->
<div class="block md:hidden">Mobile only</div>
```
