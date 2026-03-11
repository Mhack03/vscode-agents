/** @type {import('tailwindcss').Config} */
export default {
	// Enable dark mode using class strategy
	darkMode: "class",

	// Define which files contain Tailwind classes
	content: [
		"./index.html",
		"./src/**/*.{js,ts,jsx,tsx}",
		"./components/**/*.{js,ts,jsx,tsx}",
	],

	theme: {
		// Extend or override the default theme
		extend: {
			// Custom colors
			colors: {
				brand: {
					50: "#f0f9ff",
					100: "#e0f2fe",
					200: "#bae6fd",
					300: "#7dd3fc",
					400: "#38bdf8",
					500: "#0ea5e9",
					600: "#0284c7",
					700: "#0369a1",
					800: "#075985",
					900: "#0c3d66",
				},
				accent: {
					light: "#fbbf24",
					DEFAULT: "#f59e0b",
					dark: "#d97706",
				},
			},

			// Custom spacing (gap, padding, margin, etc.)
			spacing: {
				13: "3.25rem",
				15: "3.75rem",
				128: "32rem",
				144: "36rem",
			},

			// Custom font families
			fontFamily: {
				sans: ["Inter", "system-ui", "sans-serif"],
				serif: ["Merriweather", "Georgia", "serif"],
				mono: ["Fira Code", "Courier New", "monospace"],
				display: ["Space Grotesk", "sans-serif"],
			},

			// Custom font sizes
			fontSize: {
				xs: ["0.75rem", { lineHeight: "1rem" }],
				sm: ["0.875rem", { lineHeight: "1.25rem" }],
				base: ["1rem", { lineHeight: "1.5rem" }],
				lg: ["1.125rem", { lineHeight: "1.75rem" }],
				xl: ["1.25rem", { lineHeight: "1.75rem" }],
				"2xl": ["1.5rem", { lineHeight: "2rem" }],
				"3xl": ["1.875rem", { lineHeight: "2.25rem" }],
				"4xl": ["2.25rem", { lineHeight: "2.5rem" }],
				"5xl": ["3rem", { lineHeight: "1" }],
				"6xl": ["3.75rem", { lineHeight: "1" }],
				"7xl": ["4.5rem", { lineHeight: "1" }],
			},

			// Custom border radius
			borderRadius: {
				none: "0",
				xs: "0.25rem",
				sm: "0.375rem",
				md: "0.5rem",
				lg: "0.75rem",
				xl: "1rem",
				"2xl": "1.5rem",
				"3xl": "2rem",
			},

			// Custom box shadows
			boxShadow: {
				xs: "0 1px 2px 0 rgb(0 0 0 / 0.05)",
				sm: "0 1px 2px 0 rgb(0 0 0 / 0.05)",
				base: "0 1px 3px 0 rgb(0 0 0 / 0.1), 0 1px 2px -1px rgb(0 0 0 / 0.1)",
				md: "0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1)",
				lg: "0 10px 15px -3px rgb(0 0 0 / 0.1), 0 4px 6px -4px rgb(0 0 0 / 0.1)",
				xl: "0 20px 25px -5px rgb(0 0 0 / 0.1), 0 8px 10px -6px rgb(0 0 0 / 0.1)",
				glow: "0 0 15px rgba(59, 130, 246, 0.5)",
			},

			// Custom animations
			animation: {
				pulse: "pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite",
				bounce: "bounce 1s infinite",
				spin: "spin 1s linear infinite",
				ping: "ping 1s cubic-bezier(0, 0, 0.2, 1) infinite",
				"fade-in": "fadeIn 0.3s ease-in",
				"slide-down": "slideDown 0.3s ease-out",
			},

			// Custom keyframes for animations
			keyframes: {
				fadeIn: {
					"0%": { opacity: "0" },
					"100%": { opacity: "1" },
				},
				slideDown: {
					"0%": { transform: "translateY(-10px)", opacity: "0" },
					"100%": { transform: "translateY(0)", opacity: "1" },
				},
			},

			// Custom transition timing
			transitionDuration: {
				0: "0ms",
				75: "75ms",
				100: "100ms",
				150: "150ms",
				200: "200ms",
				300: "300ms",
				500: "500ms",
				700: "700ms",
				1000: "1000ms",
			},

			// Custom zIndex values
			zIndex: {
				auto: "auto",
				0: "0",
				10: "10",
				20: "20",
				30: "30",
				40: "40",
				50: "50",
				dropdown: "1000",
				sticky: "1020",
				fixed: "1030",
				backdrop: "1040",
				modal: "1050",
				popover: "1060",
				tooltip: "1070",
			},

			// Custom screen sizes
			screens: {
				xs: "320px",
				sm: "640px",
				md: "768px",
				lg: "1024px",
				xl: "1280px",
				"2xl": "1536px",
			},

			// Custom width utilities
			width: {
				screen: "100vw",
				min: "min-content",
				max: "max-content",
				fit: "fit-content",
			},

			// Custom height utilities
			height: {
				screen: "100vh",
				min: "min-content",
				max: "max-content",
				fit: "fit-content",
			},

			// Custom opacity levels
			opacity: {
				0: "0",
				5: "0.05",
				10: "0.1",
				20: "0.2",
				25: "0.25",
				30: "0.3",
				40: "0.4",
				50: "0.5",
				60: "0.6",
				70: "0.7",
				75: "0.75",
				80: "0.8",
				90: "0.9",
				95: "0.95",
				100: "1",
			},
		},
	},

	// Plugins can extend Tailwind with additional utilities
	plugins: [
		// Example: @tailwindcss/forms for better form styling
		// require('@tailwindcss/forms'),
		// Example: @tailwindcss/typography for prose styling
		// require('@tailwindcss/typography'),
		// Example: Custom plugin for aspect ratio (already in core in newer versions)
		// require('@tailwindcss/aspect-ratio'),
		// Custom plugin example:
		// require('tailwindcss/plugin')(function({ addBase, addComponents, addUtilities }) {
		//   addUtilities({
		//     '.shadow-outline': {
		//       boxShadow: '0 0 0 3px rgba(66, 153, 225, 0.5)',
		//     },
		//   })
		// })
	],
};
