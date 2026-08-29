# Implementation Plan: F07. Investment Forms & Dialogs Compliance

**Prerequisites:**
- F01-F06 merged.
- Two of F07's five PRD acceptance criteria are already satisfied (completed incidentally by F03's
  naming sweep, PR #648) — this plan covers only the three still open.
- No new tools/libraries — `Dialog`/`DialogSurface`/`DialogBody`/`DialogTitle`/`DialogContent`/
  `DialogActions` are already part of the installed `@fluentui/react-components` package.

### Stage 1: Web Move Asset Dialog Migration

**1. Migrate `MoveAssetDialog.tsx` to Fluent `Dialog`** - Replace the hand-rolled backdrop/div markup
with `Dialog`/`DialogSurface`/`DialogBody`/`DialogTitle`/`DialogContent`/`DialogActions`, preserving the
three existing conditional content branches (main form, "Moving…" auto-submit, emptied-source follow-up)
and every existing callback, per spec.md Decisions D1/D4.

**2. Trim `MoveAssetDialog.css`** - Remove the backdrop/wrapper/title/actions rules now superseded by
Fluent's own `DialogSurface`/`DialogTitle`/`DialogActions`/`Button` styling; keep the inner-content rules.

**3. Add an Escape-to-close test** - Extend `MoveAssetDialog.test.tsx` with a test confirming the dialog
renders with `role="dialog"` and that pressing Escape calls `onCancel`; confirm all 18 existing tests
still pass unmodified.

### Stage 2: WPF Automation Properties

**4. Add automation names to `MoveAssetDialog.xaml`** - Add `AutomationProperties.Name` to the
destination-portfolio `ComboBox` and new-portfolio-name `TextBox`; add
`AutomationProperties.LiveSetting="Polite"` to the validation-error `TextBlock`, per spec.md Decision D2.

**5. Add an automation name to `InvestmentSnapshotsView.xaml`'s edit button** - Add
`AutomationProperties.Name="Edit snapshot"` alongside its existing `ToolTip`, per spec.md Decision D3.

### Stage 3: Verification

**6. Confirm the full test suite and both builds are green** - `Financial.Web`: `tsc -b --noEmit`, lint,
`vitest run`. Backend: `dotnet build --configuration Release`, `dotnet test`.

**7. Manually verify both platforms** - Web: open Move Asset, confirm focus starts inside the dialog,
Tab cycles only within it, Escape closes it and returns focus to the triggering element. WPF: confirm a
screen reader announces the destination combo/new-portfolio textbox names and the validation-error text
when it appears; confirm the Snapshot edit button announces "Edit snapshot." Per
`docs/ui/review-checklist.md`.
