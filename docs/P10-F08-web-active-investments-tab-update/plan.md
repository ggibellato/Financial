# Implementation Plan: Web — Active Investments Tab Update

**Prerequisites:**
- F01 (Position Type Domain Model) and F05 (Scoped Navigation & Summary API) merged — provide `Asset.PositionType` and the `scope` query parameter this feature consumes
- No new tools, libraries, or environment variables required

### Stage 1: Backend Metadata Fix

**1. Position Type Metadata Serialization** - Fix the navigation tree's asset metadata so `PositionType` serializes as a string, matching the convention every other typed enum property in the API already follows, instead of the raw integer it currently emits. Update the existing unit and integration test assertions that currently expect the raw enum value.

### Stage 2: Scope-Pure Requests

**2. API Client Scoping** - Update the Web API client so every scope-capable request this page's component tree reaches (navigation tree, broker/portfolio summary, broker breakdown, asset details) explicitly requests the Active scope, per the spec's Component Overview.

### Stage 3: Three-Way Position Indicator and Rebrand

**3. Shared Position Type Typing** - Introduce a typed `PositionType` value on the frontend and thread it through the DTOs and the selected-node context that currently only carry the boolean active/inactive flag.

**4. Tree Indicator** - Replace the tree's boolean active/inactive status rendering with the three-way Long/Flat/Short color mapping, per the spec's color and glyph decisions.

**5. Detail Panel Indicator** - Replace the detail panel's separate boolean-driven status indicator with the same three-way mapping, reconciling its color values with the tree's.

**6. Navigation Rebrand** - Relabel the nav entry and rename the route, page component, and associated files from Portfolio Navigator to Active Investments.

### Stage 4: Frontend Test Coverage

**7. Frontend Component Tests** - Update the tree and detail-panel test suites for the three-way mapping, and add coverage that the selected node correctly carries position type.

**8. Frontend Routing/Nav Tests** - Update the app-level tests asserting the nav label, route path, and root redirect.

**9. API Client Scope Tests** - Add coverage confirming each updated client method's request includes the Active scope.
