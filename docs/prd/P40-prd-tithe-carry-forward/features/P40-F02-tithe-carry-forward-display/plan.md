# Implementation Plan: F02 Tithe Carry-Forward Display

**Prerequisites:**
- F01 (Tithe Carry-Forward Calculation) merged into `main` — its API contract, DTOs, and service method are already available.

### Stage 1: Web Footer Control

**1. API Contract Aliases and Client Method** - Add the two new DTO type aliases and the carry-forward toggle client method, following this project's existing generated-type-alias and PUT-request conventions.

**2. Month Hook Mutation** - Extend the monthly data hook with a busy flag and a mutation function for toggling carry-forward inclusion, reusing the hook's existing success/error dispatch pattern so the rest of the month's state (including Tithe Balance) refreshes consistently after a successful toggle.

**3. Footer Checkbox and Page Wiring** - Render the carry-forward checkbox in the existing Tithe footer only when a carry-forward is available, wire it to the new hook mutation and busy state, and surface failures through the existing shared action-error display.

### Stage 2: WPF Footer Control

**4. ViewModel Toggle Method and Derived State** - Add the toggle mutation method and its supporting busy/error/visibility properties to the Monthly view model, following this codebase's established checkbox-toggle-and-persist pattern.

**5. Footer XAML and Code-Behind** - Add the carry-forward checkbox to the existing Tithe footer, bound to the new view model state, with a code-behind event handler delegating the toggle to the view model.
