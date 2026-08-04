# Implementation Plan: F02. Web Collapsed-Mode Flyouts & Tooltips

**Prerequisites:**
- F01 (Web Sidebar Navigation Shell) implemented — `Sidebar.tsx`, `navTree.ts`, `Sidebar.css` already exist
- No new dependencies required (uses React's built-in `createPortal`)

### Stage 1: Flyout Component

**1. SidebarFlyout Component** - Create the portaled flyout panel that renders a category's label as a non-clickable title followed by its children as clickable links, positioned from a passed-in trigger rectangle. Include the 250ms close-delay timer (started on leaving both the trigger and the flyout, cancelled on re-entry) and the Escape key handler that signals the parent to close and restore focus. Cover it with unit tests for rendering, click-to-navigate, and Escape.

**2. Flyout Styling** - Style the flyout panel using the app's existing floating-panel and link-hover conventions, fixed-positioned above all other content.

### Stage 2: Sidebar Integration

**3. Sidebar Trigger Wiring** - Add the open-category state, the per-category trigger element refs, and the hover/focus/blur handlers to the Sidebar's category headers, rendering the flyout only when the sidebar is Collapsed and that category is the open one. Wire blur-outside to close immediately and Escape's close signal to refocus the correct trigger.

**4. Integration Tests** - Add the hover/focus/delay/escape/expanded-suppression test matrix from the spec's testing strategy to the existing Sidebar test suite, confirming the flyout's content stays derived from the same navigation tree F01's sidebar renders from.
