# Implementation Plan: F03. Web Breadcrumb Header

**Prerequisites:**
- F01 (Web Sidebar Navigation Shell) implemented — `navTree.ts` already exists
- No new dependencies required

### Stage 1: Breadcrumb Component and Shell Integration

**1. Breadcrumb Component** - Create the breadcrumb component that resolves the current route against the shared navigation tree and renders the two-segment "Category › Child" text, falling back to an em dash for an unmatched route. Style it as a fixed-height bar. Cover it with unit tests against the spec's testing strategy, including the cross-feature check that its labels come from the same navigation tree the sidebar renders from.

**2. App Shell Integration** - Render the breadcrumb above the routed page content in the app shell, and update the shell's existing test to confirm it renders alongside the sidebar.
