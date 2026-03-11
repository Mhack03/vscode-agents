# Component Architecture

Guidelines for organizing components: single responsibility, composition, presentational/container split, and API design for props/children.

- Prefer small, focused components composed together.
- Expose minimal props and use `children` for flexible content.
- Move heavy logic to hooks or service modules to keep components render-focused.

See `../sample_codes/common-patterns/component-structure.jsx` for runnable examples.
