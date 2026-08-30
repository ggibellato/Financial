# Implementation Plan: F01 Admin Navigation Foundation

**Prerequisites:**
- No new libraries/dependencies — reuses existing React Router, Fluent, WPF-UI, and DI wiring already present in both front ends.
- No environment or configuration changes.

### Stage 1: Nav Data Model

**1. Web nav data model** - Extend `navTree.ts` with a `NavGroup` type and an optional `groups` field on `NavCategory`, then add the `admin` category with its `investment` and `cashflow` groups and their 10 entity leaves, per the ids/labels/order in the spec.

**2. WPF nav data model** - Mirror the same shape and data in `NavTree.cs` (a `NavGroup` record and an optional `Groups` list on `NavCategory`), keeping ids, labels, and order identical to the Web data.

### Stage 2: Routing and Placeholder Page/View

**3. Shared placeholder page (Web)** - Create `AdminEntityPlaceholderPage`, add its lazy import, and register 10 new `PAGE_ROUTES` entries (`admin/investment/*`, `admin/cashflow/*`) that each render it with the correct entity label.

**4. Shared placeholder view (WPF)** - Create `AdminEntityPlaceholderViewModel` and `AdminEntityPlaceholderView`, then construct and register 10 labeled instances in `MainWindow.xaml.cs`'s `viewsByKey` dictionary, one per entity `ViewKey`.

### Stage 3: Sidebar and Shell Rendering

**5. Sidebar expanded/collapsed rendering (Web)** - Update `Sidebar.tsx` and `SidebarFlyout.tsx` to render a category's `groups` (each independently expandable, one branch open at a time) instead of `children` when `groups` is present, and add the visual divider above the `admin` category; extend active-state highlighting to the group level.

**6. Shell tree rendering and breadcrumb (WPF)** - Extend the shell's category→children template to render the group level when present, and update `MainShellViewModel.BreadcrumbText` to include the group segment for grouped selections.

### Stage 4: Cross-Platform Verification

**7. Nav/route sync test extension** - Extend `routes.test.ts` to flatten `groups[].children` alongside `children`, confirming all 10 Admin leaves have a reachable route and no orphaned routes exist.

**8. Existing-category regression check** - Confirm Investments/CashFlow sidebar behavior, WPF breadcrumb text, and active-state highlighting are pixel/text-identical to before this change, on both platforms.
