---
name: design-system
description: White minimalist design system for UI/UX design tasks. Use when styling components, choosing color palettes, designing buttons, cards, forms, navigation, or ensuring accessibility with WCAG AA contrast standards. Covers monochrome palette, typography, interactive elements, minimal borders, and shadow-based depth.
license: Complete terms in LICENSE.txt
---

# Design System — White Minimalist

A clean, white, minimalist design system for all UI/UX design tasks. Follow these guidelines for every design deliverable.

## Color Scheme

You MUST use a clean, white, minimalist color palette for all designs:

### Primary Colors

- **Vantablack (Text)**: `#1a1a1a` (almost black for contrast without harshness)
  - Use for: Primary text, headings, icons
- **White**: `#FFFFFF`
  - Use for: Main backgrounds, cards, strict negative space

### Accents (Monochrome)

- **Charcoal**: `#333333`
  - Use for: Primary buttons, active states
  - Hover state: `#000000`
- **Soft Gray**: `#F0F0F0`
  - Use for: Subtle backgrounds, hover zones, inputs

### Secondary/Support Colors

- **Border Gray**: `#E5E5E5` (very subtle borders)
- **Medium Gray**: `#888888` (secondary text, meta info)
- **Error/Alert**: `#D32F2F` (use sparingly for errors)

## Design Principles

### 1. Minimal Borders

- **Avoid borders whenever possible**
- Use subtle shadows, spacing, or background color changes to create visual separation
- If borders are absolutely necessary:
  - Use `border: 1px solid rgba(0, 0, 0, 0.08)` for light themes
  - Use `border: 1px solid rgba(255, 255, 255, 0.08)` for dark themes
  - Prefer bottom borders over full borders
  - Never use thick borders (max 2px)

**Instead of borders, use:**

- Box shadows: `0 2px 8px rgba(0, 0, 0, 0.08)`
- Background color differences
- Generous spacing/padding
- Subtle divider lines (1px, low opacity)

### 2. Bold and Clean

- Use bold typography for impact
- Embrace white space generously
- Keep layouts clean and uncluttered
- Strong contrast for readability

### 3. Modern and Minimal

- Flat design with subtle depth via shadows
- Simple, clean iconography
- Rounded corners (4px-8px) for softer feel
- Avoid gradients (unless subtle)

### 4. Typography

- Use bold weights for headings
- Clear hierarchy (size, weight, color)
- Ample line spacing for readability
- Black for primary text, dark gray for secondary

### 5. Interactive Elements

**Buttons:**

```css
Primary Button:
- Background: #333333 (charcoal)
- Text: #FFFFFF (white)
- Border: none
- Border-radius: 4px
- Padding: 12px 24px
- Font-weight: 600
- Hover: background #000000
- Active: background #1a1a1a
- Shadow: 0 2px 4px rgba(0, 0, 0, 0.1)

Secondary Button:
- Background: transparent
- Text: #333333 (charcoal)
- Border: 1px solid #333333
- Border-radius: 4px
- Padding: 12px 24px
- Font-weight: 600
- Hover: background rgba(0, 0, 0, 0.05)

Tertiary/Ghost Button:
- Background: transparent
- Text: #333333 (charcoal)
- Border: none
- Padding: 12px 24px
- Font-weight: 600
- Hover: background rgba(0, 0, 0, 0.05)
```

**Links:**

- Color: #333333 (charcoal)
- Hover: underline, color #000000
- No border or background

**Form Inputs:**

```css
Input Fields:
- Background: #FFFFFF
- Border: 1px solid rgba(0, 0, 0, 0.12) (minimal)
- Border-radius: 4px
- Padding: 12px 16px
- Focus: border-color #333333, box-shadow 0 0 0 3px rgba(51, 51, 51, 0.1)
- No heavy borders
```

### 6. Cards and Containers

```css
Cards:
- Background: #FFFFFF
- Border: none (avoid borders!)
- Border-radius: 8px
- Box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08)
- Padding: 24px
- Use shadow for elevation, not borders
```

### 7. Visual Hierarchy

- Use bold weight strategically for emphasis
- Black for primary content
- White space for breathing room
- Shadows for depth and separation
- Size and weight for hierarchy, not borders

## Examples of Minimalist Components

**Hero Section:**

- Clean white background
- Large Vantablack text
- Charcoal CTA button
- Minimal borders, clean sections

**Navigation:**

- White background
- Minimalist text links
- No borders on nav items
- Use subtle background color change for hover

**Cards/Panels:**

- White background
- Soft shadow (no borders)
- Subtle gray accent elements
- Clean typography

**Forms:**

- Minimal input borders
- Charcoal focus states
- Clear labels in black
- Charcoal submit buttons

## Accessibility Requirements

- Ensure sufficient contrast (black on white, white on charcoal)
- Maintain WCAG AA standards minimum
- Provide clear focus indicators
- Use weight and size thoughtfully to not overwhelm

## Quick Reference

✅ **DO:**

- Use charcoal/black for CTAs
- Embrace white space
- Use shadows for depth
- Keep it clean and minimal
- Bold typography for impact

❌ **DON'T:**

- Use heavy borders
- Clutter with unnecessary elements
- Use colors outside the monochrome palette
- Create complex border patterns
- Overuse bold weights (use strategically)
