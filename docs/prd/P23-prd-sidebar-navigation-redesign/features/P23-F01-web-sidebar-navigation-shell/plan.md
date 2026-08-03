# Implementation Plan: F01. Web Sidebar Navigation Shell

**Prerequisites:**
- `Financial.Web` React + TypeScript + Vite app, `react-router-dom@^7`, Vitest + React Testing Library already configured
- No new dependencies required

### Stage 1: Navigation Data and Persistence

**1. Navigation Tree Data Module** - Create the shared, framework-agnostic navigation tree data module that defines the two categories and their ordered children. This becomes the single source of truth this feature's sidebar reads from, and that later features (collapsed-mode flyouts, breadcrumb header) will also consume.

**2. Sidebar Storage Helper** - Create the persistence helper for the sidebar's collapsed/expanded state, following the existing storage-helper pattern in the codebase (try/catch read and write, silent degrade on failure). Add its unit tests covering the default, round-trip, and failure paths.

### Stage 2: Sidebar Component and Shell Integration

**3. Sidebar Component** - Build the sidebar component: reads the persisted collapsed state synchronously before first render, renders the toggle button and the two category sections with their children from the navigation tree data, applies active-route highlighting, and includes the three hand-rolled inline SVG icons (two category icons, one toggle icon). Cover it with component tests against the spec's testing strategy.

**4. Sidebar Styling** - Style the sidebar for both Expanded and Collapsed states per the spec's decisions: fixed widths, the 150ms width transition, active-item and parent-icon accent highlighting, and icon-only layout when collapsed.

**5. App Shell Integration** - Update the app shell to render the sidebar alongside the routed content in a row layout, removing the old domain-switcher markup and styles. Update the shell's existing tests to reflect the new structure while preserving its unrelated domain-tracking behavior.

### Stage 3: Route Flattening and Cleanup

**6. Route Table Update** - Flatten the ten leaf routes to render directly under the app shell route, removing the now-unnecessary per-domain layout nesting, while keeping every route's URL path unchanged.

**7. Remove Superseded Layout Components** - Delete the per-domain layout components, their styles, and their tests now that the sidebar owns all navigation responsibility, per the spec's decision to avoid keeping empty pass-through wrappers.
