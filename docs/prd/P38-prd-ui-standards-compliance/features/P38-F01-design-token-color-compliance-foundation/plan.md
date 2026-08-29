# Implementation Plan: F01. Design Token & Color Compliance Foundation

**Prerequisites:**
- None — this is the first feature implemented; no other P38 feature exists in the codebase yet.
- No new tools/libraries/environment variables required.

### Stage 1: Web Token Declaration

**1. Declare the full undeclared-token set in `index.css`** - Add all 11 currently-undeclared CSS
custom properties (the 4 named in the PRD plus 7 more found in active use without a declaration) to
both the `:root` block and the existing `@media (prefers-color-scheme: dark)` block, using the
light/dark values from the spec. Light values must match each property's current hardcoded fallback
exactly so no page's appearance changes in light mode; dark values are new.

### Stage 2: Web Hardcoded Color Replacement

**2. Replace `#007acc`/`#005fa3` in `MensaisPage.css` and `ControleMaePage.css`** - Swap every
hardcoded occurrence for the existing `--accent` token (already declared and already the correct
brand blue per ADR-005), preserving hover-state behavior.

**3. Replace `#007acc`/`#005fa3` on the New/Save buttons in `TransactionsTab.css`, `CreditsTab.css`,
and `PriceHistoryTab.css`** - Same accent-token swap, scoped to the specific button selectors named
in the spec (not the whole file).

**4. Tokenize the Dividend/Rent/JCP colors in `CreditsTab.css`** - Declare the 3 named local custom
properties at the top of the file and reference them from the 3 existing type-color selectors,
keeping the rendered colors unchanged.

### Stage 3: WPF Theme Merge

**5. Merge the WPF-UI theme into the 8 legacy WPF forms** - For each of `WithdrawalFormView`,
`IncomeSplitFormView`, `EditReserveMovementFormView`, `AddBillFormView`, `EditBillFormView`,
`CreateEntryFormView`, `EditEntryFormView`, and `EditSnapshotValueFormView`, add the same scoped
theme-merge resource block already proven in `TransferFormView.xaml`, then replace that form's
hardcoded border/background and error-text color literals with the corresponding theme brushes.
This is one mechanical change repeated across all 8 files.

**6. Merge the WPF-UI theme into `MoveAssetDialog.xaml`** - Same resource block, placed in
`Window.Resources` since this file is a `Window` rather than a `UserControl`; replace its one
hardcoded `Foreground="Red"` with the theme brush.

### Stage 4: Reserva Warning/Error Distinction and Verification

**7. Give `ReservaView.xaml`'s split-percentage warning a distinct color** - Replace the warning
line's hardcoded red with a caution-toned theme brush, and normalize the file's two genuine-error
lines to the same critical-brush key used everywhere else in the app, so all three lines in one file
follow one consistent, non-colliding convention.

**8. Verify every touched view renders correctly** - Build `Financial.App` and open each of the 8
forms, `MoveAssetDialog`, and `ReservaView` to confirm the theme merge actually applies and that the
caution-brush key used for the warning line resolves at runtime (a `DynamicResource` failure is
silent, not a build error, so this cannot be confirmed by a successful build alone). Open each
touched Web page in both light and dark mode to confirm token values render as intended.
