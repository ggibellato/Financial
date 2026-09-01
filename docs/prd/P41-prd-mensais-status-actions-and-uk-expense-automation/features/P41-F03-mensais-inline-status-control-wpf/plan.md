# Implementation Plan: F03. Mensais Inline Status Control (WPF)

**Prerequisites:**
- F01 merged to `main` (`IMensaisService.UpdateBillStatusAsync` available)
- F02 merged to `main` (colors and menu-content precedent to mirror)
- WPF-UI 4.0.1 already referenced in `Financial.App.csproj` — no new package required

### Stage 1: Status Color Mapping and Reusable Control

**1. Status Color Converter** - Add a converter mapping each of the three statuses to the same background/foreground colors already shipping in the React status tag, sampled from the running app for pixel parity, and register it as an application resource.

**2. Reusable Status Split Button** - Build the new status tag control on top of the existing `Wpf.Ui.Controls.SplitButton`: a colored tag whose chevron opens a flyout listing every status, the current one shown checked and disabled, following the existing Flyout/DataContext pattern already used elsewhere in this app. Selecting a different status raises a command carrying the row and the new value.

### Stage 2: ViewModel and Grid Wiring

**3. Mensais View Model Status Command** - Add a command to the Mensais view model that calls the status-only service update directly (no HTTP involved) and replaces the affected bill in whichever of the Brasil/UK collections holds it, following the same error-handling shape already used by the existing delete command. Cover it with view model tests for the success and failure paths across both areas.

**4. Bill Table and Mensais View Wiring** - Replace the read-only status column in the shared bill table view with the new control, and wire the new command and its error message through to both the Brasil and UK sections of the Mensais view, leaving the existing edit-form status field untouched.

### Stage 3: Documentation

**5. WPF Pattern Documentation** - Document the Flyout/DataContext-inheritance pattern this control relies on in the WPF UI rules, so the next contextual popup control in this app follows the same approach instead of rediscovering it.
