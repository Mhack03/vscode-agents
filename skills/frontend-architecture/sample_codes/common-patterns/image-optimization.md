# Image Optimization

Recommendations:

- Use native `loading="lazy"` for non-critical images.
- Prefer responsive `srcset` and sizes attributes for varying device sizes.
- In Next.js, use the `next/image` component for automatic optimization.

Example:

```html
<img
	src="/images/photo-800.jpg"
	srcset="/images/photo-400.jpg 400w, /images/photo-800.jpg 800w"
	sizes="(max-width:600px) 400px, 800px"
	loading="lazy"
	alt="..."
/>
```
