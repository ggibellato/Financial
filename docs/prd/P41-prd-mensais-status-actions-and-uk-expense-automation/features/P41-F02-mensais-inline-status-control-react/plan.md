# Implementation Plan: F02. Mensais Inline Status Control (React)

**Prerequisites:**
- F01 merged to `main` (status-only endpoint available)
- Node/npm environment matching `Financial.Web/package.json`; no new packages required (`MenuButton`, `Menu`, `Badge` already ship in the installed `@fluentui/react-components`)

### Stage 1: API Client Wiring

**1. Status Update API Client Method** - Add the type alias and API client method for the status-only endpoint, following the existing `updateMensaisBill` method's shape, so the hook layer has a typed call available.

### Stage 2: Status Control and Grid Wiring

**2. Reusable Status Menu Button** - Build the new status tag-with-menu component: a colored tag that opens a menu of every status, with the current one shown checked and disabled, calling back when a different status is chosen. Cover it with component tests for rendering, opening, selection, and the disabled/checked current-status treatment.

**3. Mensais Hook Status Action** - Extend the Mensais data hook with a status-only update action that calls the new API method and updates just the affected bill in place on success, without refetching or reloading the table, with its own per-row updating/error tracking. Cover it with hook tests for the success and failure paths.

**4. Mensais Page Integration** - Wire the new status control into both the Brasil and UK bill tables' status column, replacing the plain-text cell, leaving the existing edit-form drawer's status field untouched. Cover it with page-level tests exercising status changes end-to-end through the mocked API client, including the row-level error path and confirming the existing edit-form flow still works unchanged.

### Stage 3: UI Standards Documentation

**5. Status Tag Pattern Documentation** - Add the new standards page documenting the status-tag-with-menu pattern (when to use it, its composition, accessibility notes), and cross-reference it from the existing menu documentation and the fluent-ui skill's control-selection and cross-platform-mapping references.
