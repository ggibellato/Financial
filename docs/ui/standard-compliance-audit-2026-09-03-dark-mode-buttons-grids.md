# Design Standards Compliance Audit — Buttons, Grids & Dark Mode (2026-09-03)

## Context

The user asked for every form (grid and data-entry) in both front ends to be re-verified against
`docs/rules/ui.md` and every document in `docs/ui/`, using the `fluent-ui` skill, after noticing
three known problems (button size, button colour, button position) plus a new one that appeared
after the F01/F02 Colour Mode rollout (`3fcb814d` web, `3814a887` WPF, both merged 2026-09-03):
grids now render with a bad colour in dark mode, suspected to be pre-existing hardcoded grid
colours that never had to react to a theme change before.

Two prior audits already exist — `standard-compliance-audit-2026-08-23.md` and
`standard-compliance-audit-2026-08-29-forms.md`. This audit does not reuse their findings as fact;
every claim below was re-verified against current file content. Several previously-flagged items
turned out to be fixed already (see below) — they are not repeated as open findings.

## Already fixed since the prior audits — not re-flagged

- `MoveAssetDialog.tsx` is now a real Fluent `Dialog`/`DialogSurface`/`DialogBody`/`DialogTitle`
  (was a hand-rolled backdrop).
- `CurrentValuesPage.tsx`/`DividendCheckPage.tsx` "Check" buttons are now Fluent
  `Button appearance="primary"` with icons (were hardcoded `#007acc` divs).
- `TransactionsTab.tsx`/`InvestmentSnapshotsPage.tsx` row actions are now Fluent
  `Button appearance="subtle" size="small"` with `EditRegular`/`DeleteRegular` icons (were raw
  ✏ emoji).
- WPF Investment tabs' "New X" triggers now carry full labels ("New transaction"/"New credit"/
  "New price") — were bare "New" with only a tooltip.
- `index.css`'s CSS custom properties are now actually declared for both themes — the "phantom
  CSS variable" bug from the 2026-08-29 audit (fallback-only, never-declared tokens) is closed.

## Headline findings

1. **Button size/colour/position is inconsistent on both platforms**, confirming the three
   problems the user already suspected. On WPF alone: three incompatible row Edit/Delete icon
   conventions, five different primary-button `MinWidth` values, and two competing Save/Cancel
   row alignments. On Web: `size="small"` — reserved by the spec for icon-only row actions — is
   used on several labelled page/panel triggers instead.
2. **Dark mode exposed pre-existing hardcoded grid colours, exactly as suspected.**
   `Financial.Web/src/styles/data-table.css:29` sets the even-row stripe as a bare `#f5f5f5`
   literal (no `var()`) — every even row in Banks/Income/Category Totals/Cards/Totals grids stays
   light-grey against the dark theme. On WPF, the shared column-filter popup
   (`Controls/FilterableColumnHeader.xaml:35`) hardcodes `Background="White"`, used by every
   filterable grid column app-wide — the single highest-reach dark-mode defect found. Five WPF
   grids additionally lost their entire themed style to a `Style`/`RowStyle` scoping bug (local
   style without `BasedOn` replaces, rather than layers on, the app-wide theme-aware style).

## Part A — Confirmed non-compliant items: Buttons (Web)

Rule of record: `forms-data-and-visualisations.md` §"Action buttons" — every real action is
primary-appearance, standard size, left-positioned; `size="small"` is reserved for icon-only row
actions inside a grid, never a labelled panel action.

| # | Location | Finding | Severity |
|---|---|---|---|
| 1 | ~~`BankOperationsSection.tsx:92-98`, `IncomeSection.tsx:46,55`, `ExpensesSection.tsx:42,51`, `CreditsTab.tsx:81,90`, `PriceHistoryTab.tsx:58,69`, `TransactionsTab.tsx:59,68`, `DetailPanel.tsx:88,98,109`~~ | **Resolved via PR #705.** Re-verification before fixing found six of the seven cited files already correct (icon-only row actions, not the labelled trigger — false-positive citations); the only genuine violation was `DetailPanel.tsx`'s Move/Delete Portfolio pair, fixed by removing `size="small"`. | ~~Medium~~ Resolved |
| 2 | ~~`BanksPage.tsx:94-108` (+ 9 sibling Admin pages) vs. `TransactionsTab.tsx:57-73`~~ | **Corrected 2026-09-04 — not a violation.** Re-checked while fixing Buttons (WPF) #3 (PR #707): every Admin-CRUD page and every Investment tab already uses the identical `appearance="subtle" size="small"` pattern for row Edit/Delete. The two-appearance claim didn't hold against current code; no fix needed or made. | ~~Medium~~ N/A |
| 3 | ~~`components/DetailPanel.tsx:96-116`~~ | **Corrected 2026-09-04 — not a violation.** Originally flagged as "Move…"/"Delete Portfolio" needing a distinct destructive treatment. Re-read of `forms-data-and-visualisations.md` lines 146-150 and 172-174 found the doc names this exact pair by name as the canonical example of peer action buttons that must **both stay primary, same style** — "never treat a Move/Delete pair as if it were a Save/Cancel pair." The `size="small"` part of the original finding was real and is fixed (see Buttons #1); the appearance/distinction part was a misreading of the rule and required no change. WPF's equivalent (`NavigationView.xaml`'s Move Asset/Delete Portfolio buttons) was already compliant with the same rule for the same reason. | ~~High~~ N/A |
| 4 | ~~`pages/MensaisPage.tsx:258-268`~~ | **Resolved 2026-09-04.** "Reset All to Unset" changed to `appearance="secondary"`, leaving "Add Bill" as the toolbar's one primary action — unlike Buttons (Web) #3's Move/Delete pair, these are an unrelated create action and a bulk-destructive action, not two peers on one entity, so the "both stay primary" exception doesn't apply here. | ~~Medium~~ Resolved |
| 5 | ~~`TransactionsTab.css:162-188`, `CreditsTab.css`, `PriceHistoryTab.css`~~ | **Resolved via PR #710.** `TransactionsTab.tsx`, `CreditsTab.tsx` and `PriceHistoryTab.tsx` were rewritten with real Fluent `Field`/`Input`/`Button` inline forms as part of the Web Investment legacy-form migration; the raw-HTML Save/Cancel classes were removed. | ~~Low~~ Resolved |
| 6 | ~~`TransferForm.tsx:116`, `BankOperationsSection.tsx:92-98`~~ | **Corrected 2026-09-04 — not a violation on Web.** Re-checked: `BankOperationsSection.tsx`'s trigger reads "New Transfer", and `TransferForm.tsx:57` titles the panel "New Transfer"/"Edit Transfer" — there is no "Move Money" text anywhere in the current Web naming chain. The drift the original citation described does exist, but on the **opposite** platform — see the corrected Cross-platform Parity #1 below: `Financial.App/Views/CashFlow/TransferFormView.xaml:46,121` still shows "Move Money" for the create-mode title/button while Web says "New Transfer". | ~~Medium~~ N/A (see Parity #1) |
| 7 | ~~`components/ColourModeToggleButton.tsx:9-17`~~ | **Resolved 2026-09-05.** Converted to a Fluent `Button appearance="subtle"` with an `icon` prop, matching the icon-only button convention used elsewhere in the app; `onClick`/`aria-label`/`title` all pass through unchanged (confirmed via the existing test suite, no test changes needed). Removed the now-dead `.colour-mode-toggle` CSS class from `App.css`. | ~~Low~~ Resolved |

## Part A — Confirmed non-compliant items: Buttons (WPF)

The `AccentButtonBackground`/`AccentButtonBackgroundPointerOver`/`AccentButtonBackgroundPressed`
overrides pinned in ~50 files are the documented, deliberate ADR-005 mechanism for matching Web's
exact hex (`ApplicationAccentColorManager` generates the wrong shade from a seed colour) — **not**
a finding.

| # | Location | Finding | Severity |
|---|---|---|---|
| 1 | ~~`BankFormDialog.xaml:47-50` (right, 11 Admin dialogs + `MoveAssetDialog`) vs. `WithdrawalFormView.xaml:95-110` (left/default, 12 inline forms)~~ | **Resolved via PR #705.** Removed `HorizontalAlignment="Right"` from all 15 popup-dialog files, unifying Save/Cancel to left-aligned everywhere, matching Web's actual behaviour (`formPanelStyles.ts`'s `.actions` has no `justify-content`). | ~~Medium~~ Resolved |
| 2 | ~~90 (11 Admin dialogs), 100 (`AddBillFormView`), 110 (7 inline forms), 130 (`IncomeSplitFormView`), 80/70 (`UkExpensePromptDialog`)~~ | **Resolved via PR #705.** Standardised primary-button width to `MinWidth="90"` across all 11 affected files. | ~~Medium~~ Resolved |
| 3 | ~~`ExpenseSectionView.xaml:40-44` (correct `ui:SymbolIcon`) vs. ~16 files with `Button Content="✏"/"🗑"` emoji vs. `TransactionsView.xaml:150-161` (Segoe MDL2 glyph in a `TextBlock`)~~ | **Resolved via PR #707.** Converted all 20 emoji/glyph row-action buttons to `ui:Button Appearance="Transparent" Icon="{ui:SymbolIcon Symbol=Edit16/Delete16}"`, matching the one already-correct convention. | ~~Medium~~ Resolved |
| 4 | ~~`Components/Sidebar.xaml:30,55,133,152`~~ | **Resolved 2026-09-05.** All four `Foreground`/`Stroke`/`TextElement.Foreground` setters converted from the literal `#007ACC` to `{DynamicResource AccentTextFillColorPrimaryBrush}` — confirmed present in the compiled Wpf.Ui 4.0.1 DLL (same string-scan verification method as PR #703's brush choices) and the correct Fluent 2/WinUI semantic key for accent-colored text, matching what a selected nav label needs. | ~~High~~ Resolved |
| 5 | ~~`Views/Settings/AppearanceView.xaml`~~ | **Re-verified 2026-09-04, closed — false positive.** `App.xaml:13-16` merges `ui:ThemesDictionary`/`ui:ControlsDictionary` app-wide, and a string-scan of the compiled `Wpf.Ui.dll` confirms it ships implicit Fluent-styled resources for the standard `RadioButton` control (`RadioButtonCheckGlyphSize`, `RadioButtonOuterEllipseCheckedStroke`, etc. — the same mechanism that Fluent-styles the plain `TextBox`/`DatePicker`/`ComboBox` used unprefixed throughout the rest of the app). The plain `<RadioButton>` here already renders Fluent-themed; there is no Wpf.Ui-specific `RadioButton` control to swap to, and no gap to close. | ~~Gap~~ Resolved |

## Part A — Confirmed non-compliant items: Grids (Web)

Rule of record: `design-tokens.md` — "feature code must not introduce repeated raw colours…
token values belong in the React theme and WPF ResourceDictionaries."

| # | Location | Finding | Severity |
|---|---|---|---|
| 1 | ~~`styles/data-table.css:28-30`~~ | **Resolved via PR #703.** `background: #f5f5f5` replaced with `var(--bg-subtle)`. | ~~High~~ Resolved |
| 2 | ~~`styles/data-table.css` (whole file)~~ | **Resolved 2026-09-04.** Added `.data-table tbody tr:hover { background: var(--bg-hover) }` using the existing theme-aware token (already defined for both themes in `index.css`, previously only used as a fallback on `.data-table__action-btn:hover`). Checked whether "selected-row" also applies first: surveyed all 23 files using `.data-table` and found none implement row selection — every grid uses per-row action buttons instead (`InvestmentTree.tsx` is the only component with click-to-select, and it isn't a `.data-table` grid) — so per the docs' "applicable" qualifier, only hover applies here. Verified live in the browser (CashFlow Monthly grids) in both light and dark mode. | ~~Medium~~ Resolved |
| 3 | ~~`TransactionsTab.css:24-33,190-236`, `CreditsTab.css:36,69,193-238`, `PriceHistoryTab.css:31,64,167-235`~~ | **Fully resolved 2026-09-05.** PR #704 already converted buy/sell and error text to `var(--success)`/`var(--danger)`/`var(--error)`. The remaining `#007acc` filter/mode-button colour (the same stale pre-rebrand accent fixed elsewhere in this pass) is now `var(--accent)` in all three files — 2 occurrences each in `TransactionsTab.css`/`CreditsTab.css`, 1 in `PriceHistoryTab.css` (its second cited line, 64, no longer contains a colour rule — an unrelated layout line today; the original citation was stale). | ~~Medium~~ Resolved |
| 4 | ~~`PriceHistoryTab.css:53,185`~~ | **Resolved 2026-09-04.** `.price-history-tab__source--manual`'s `#e65100` replaced with `var(--warning-text)` — this is exactly what that token is for ("flag this value, it wasn't auto-fetched"), no new convention needed. Also removed the dead `var(--text-muted, #888)` fallback on the `--automatic` sibling rule (`--text-muted` is unconditionally defined, same stale-fallback pattern as the `--accent` cleanup in PR #720). Verified: contrast-checked `--warning-text` at 4.92:1 (light) / 9.11:1 (dark) against `--bg`, both pass WCAG AA; `npm test` (1471 passed), `npm run build` clean. | ~~Medium~~ Resolved |
| 5 | ~~`CreditsTab.css:8-10`~~ | **Resolved 2026-09-04 — this was a real accessibility bug, not just a design preference.** The three credit-type colours are legitimately categorical (kept local, non-token) and pass WCAG AA against the light-mode background (4.5-5.7:1) — but measured against the dark-mode background (`--bg: #16171d`) they fall to 3.1-4.0:1, below the 4.5:1 AA baseline `docs/rules/ui.md`/CLAUDE.md mandate for text. Added a `:root[data-theme='dark'] .credits-tab` override lightening all three hues (dividend `#1565c0`→`#5b9bd8`, rent `#0277bd`→`#4dabf7`, jcp `#00838f`→`#3bc9db`) to 6.1-9.0:1 in dark mode while leaving the already-passing light-mode values untouched. Verified: contrast ratios computed and checked programmatically; confirmed legible live in the browser (TAEE3's Credits grid, dark mode) — Dividend/JCP labels clearly readable against the dark background where they were previously borderline. | ~~Medium~~ Resolved |
| 6 | ~~`pages/AnnualSummaryPage.css:39-46`~~ | **Resolved 2026-09-05.** All three `#007acc` occurrences (hover and active tab state) replaced with `var(--accent)`. | ~~Medium~~ Resolved |
| 7 | ~~`PortfolioSummaryTab.css:39-53`, `AggregatedSummaryTab.css:34,38,42`, `InvestmentTree.css:65,73,81-82`~~ | **Resolved via PR #704.** All six files (plus `AssetSummaryTab.css` and `DetailPanel.css`, discovered during the fix) now use `var(--success)`/`var(--danger)`. | ~~Medium~~ Resolved |
| 8 | ~~`InvestmentTree.css:82`, `PortfolioSummaryTab.css:59`~~ | **Resolved 2026-09-05.** Both stale fallback values removed — `--accent` is always declared, so `var(--accent, #007acc)`/`var(--accent, #5a7e6e)` simplify to plain `var(--accent)`. | ~~Low~~ Resolved |

## Part A — Confirmed non-compliant items: Grids (WPF)

The app-wide `DataGrid` style (`App.xaml:78-209`) is correctly token-driven — any grid that
doesn't override it gets Light/Dark for free. Everything below is a grid that, one way or
another, doesn't inherit it.

| # | Location | Finding | Severity |
|---|---|---|---|
| 1 | ~~`Controls/FilterableColumnHeader.xaml:15,35`~~ | **Resolved via PR #703.** Converted to `DynamicResource` theme brushes. | ~~High~~ Resolved |
| 2 | ~~`AnnualSummaryView.xaml:52-64,94-101,129-141`, `BankSectionView.xaml:79-88`, `ReservaView.xaml:134-142`~~ | **Resolved via PR #703.** Added `BasedOn="{StaticResource {x:Type DataGridRow}}"` (or equivalent) to all five grids' local styles. | ~~High~~ Resolved |
| 3 | ~~`PriceHistoryView.xaml:141-144`, `CreditsView.xaml:111-114`~~ | **Resolved via PR #703.** `Foreground="Black"` replaced with `{DynamicResource TextFillColorPrimaryBrush}`. | ~~High~~ Resolved |
| 4 | ~~`DividendCheckView.xaml:116,131`~~ | **Resolved 2026-09-05.** Re-checked the original claim before fixing: the auto-generated columns' code-behind (`AutoGeneratingColumn` handlers) actually did apply `NumericColumnTextStyle` via `BasedOn`, so alignment wasn't silently lost as originally worded — but it also hardcoded `Foreground = Brushes.Black` on the Value/Total columns, a real dark-mode break the original finding missed. Converted both grids to explicit `AutoGenerateColumns="False"` with hand-authored columns (Type/Date/Value for the History grid, Year/Total for the By Year grid), matching every other grid in the app; removed the now-dead `AutoGeneratingColumn` event handlers and their helper methods from the code-behind entirely. | ~~High~~ Resolved |
| 5 | ~~`ReserveBucketsView.xaml:43` (`#B25E00`), `CardsGridView.xaml:139` (`#8A6D00`) vs. `ReservaView.xaml:44` (correct: `{DynamicResource SystemFillColorCautionBrush}`)~~ | **Resolved 2026-09-05.** Both hardcoded ambers converted to `{DynamicResource SystemFillColorCautionBrush}`, matching `ReservaView.xaml`'s already-correct usage — all three warning texts now use the same theme-aware brush. | ~~Medium~~ Resolved |
| 6 | ~~`PortfolioSummaryView.xaml:62,68,76,255,263,271`~~ | **Resolved via PR #704** (Green/Red halves). Converted to `{DynamicResource SystemFillColorSuccessBrush}`/`SystemFillColorCriticalBrush}` for Bought/Sold — a fixed-role label, not a sign-flip, so intentionally *not* routed through `SignedValueToBrushConverter`. The `Blue` occurrences are a different "info" concept and were out of scope; left as literals. | ~~Medium~~ Resolved (Blue left as-is, by design) |
| 7 | ~~25 files incl. all 10 Admin-CRUD grids~~ | **Resolved via PR #704.** All 25 files' `Foreground="Red"/"Green"` converted to `{DynamicResource SystemFillColorCriticalBrush}`/`SuccessBrush`. | ~~High~~ Resolved |
| 8 | ~~`Components/NavigationView.xaml:39,43,69-81,105,119,128,137,200`~~ | **Mostly resolved via PR #703**, remaining part fixed 2026-09-04 pending user spot-check. Section labels, drop-target colour (now `AccentFillColorSecondaryBrush`), loading veil (now a neutral `#66000000` scrim — no theme-aware backdrop brush exists in Wpf.Ui 4.0.1, verified against the compiled DLL), splitter and divider all converted via PR #703. The selected-node highlight override at line ~80 (`SystemColors.InactiveSelectionHighlightBrushKey`/`InactiveSelectionHighlightTextBrushKey`) — literal `#007ACC`/White — was fixed in PR #726 using a different technique than the previously-reverted attempt: `{DynamicResource SystemAccentColorPrimary}`/`{DynamicResource TextOnAccentFillColorPrimary}` directly on the `Color` property (not a `Binding` on a `StaticResource`-wrapped `Brush`, which doesn't propagate reliably in a loose `ResourceDictionary`). Confirmed via the Wpf.Ui 4.0.0 GitHub source that these are the exact same two `Color` resources already backing `ListBoxItemSelectedBackgroundThemeBrush`/`ListBoxItemSelectedForegroundThemeBrush` — the brushes this same style already uses for the focused-selected state — so the fix makes the unfocused-selected look identical to the focused one, in both themes, by construction. This environment can't visually verify a WPF GUI change, so this is not marked Resolved until confirmed live. | ~~High~~ Partially resolved, fix pending verification |
| 9 | ~~`MainWindow.xaml:26,32-33`~~ | **Resolved via PR #703.** Breadcrumb `BorderBrush`/`Foreground` converted to `DynamicResource`. | ~~High~~ Resolved |
| 10 | ~~`AssetPriceView.xaml` (8 hardcoded values), `DividendCheckView.xaml` (14+ hardcoded values)~~ | **Resolved 2026-09-05.** Both pages fully converted to theme-aware `DynamicResource` brushes: `#333333`/headings → `TextFillColorPrimaryBrush`, `#666666`/secondary text → `TextFillColorSecondaryBrush`, `#CCCCCC` card and grid borders → `ControlElevationBorderBrush` (the same key used throughout the already-migrated CashFlow forms), `#FAFAFA` card backgrounds → `ControlFillColorDefaultBrush`, `Black` → `TextFillColorPrimaryBrush`, `Red`/`Green` (price-good/bad indicator) → `SystemFillColorCriticalBrush`/`SystemFillColorSuccessBrush`, `Blue` (Average Dividend/Dividend Yield informational stats) → `AccentTextFillColorPrimaryBrush`. `DividendCheckView.xaml`'s bordered/tinted error banner (`#D32F2F`/`#FDECEA`) had no equivalent anywhere else in the app and Wpf.Ui has no theme-aware "critical background" brush to build one from, so it was simplified to match the app's one established error-display convention (plain text, `SystemFillColorCriticalBrush`) instead of inventing a new pattern — see `MonthlyView.xaml`'s error `TextBlock` for the precedent. | ~~High~~ Resolved |
| 11 | ~~`NavigationView.xaml:119`, `CreditsView.xaml:124`, `PriceHistoryView.xaml:172`, `TransactionsView.xaml:113`~~ | **Fully resolved 2026-09-05.** The three remaining `GridSplitter`s (`CreditsView.xaml`, `PriceHistoryView.xaml`, `TransactionsView.xaml`) converted from `Background="#E0E0E0"` to `{DynamicResource ControlStrokeColorDefaultBrush}`, matching `NavigationView.xaml`'s splitter fixed incidentally by PR #703. The originally-cited `MainWindow.xaml` border splitter remains out of scope — confirmed again it's a `Border`, not a `GridSplitter`, so this finding never applied to it. | ~~Low~~ Resolved |
| 12 | `App.xaml:143-144` | **Fix applied 2026-09-04 via PR #726, pending user spot-check.** `SystemColors.InactiveSelectionHighlightBrushKey`/`InactiveSelectionHighlightTextBrushKey` (hardcoded `#007ACC`/White) replaced with `{DynamicResource SystemAccentColorPrimary}`/`{DynamicResource TextOnAccentFillColorPrimary}` — see Grids (WPF) #8 above for the full rationale (same fix, same two files). Not marked Resolved until confirmed live in the running app (select a grid row, click away to defocus, check both light and dark mode) — this environment has no WPF GUI to verify with, and a prior fix attempt at this exact spot had to be reverted. | Low, fix pending verification |
| 13 | ~~`Controls/FilterableColumnHeader.xaml:18`, `Components/MonthYearPicker.xaml:25`~~ | **Reported by the user 2026-09-04 while spot-checking PR #726 ("grid column headers hard to read in dark mode, same for the icons"), root-caused and fixed.** Not caused by PR #726 (confirmed present on `main` too). `FilterableColumnHeader.xaml`'s filter-icon `Foreground` hardcoded the pre-rebrand light-mode accent `#0F6CBD` for its "column is filtered" state — every filterable column's icon, app-wide (the same shared control fixed for a different bug in Grids (WPF) #1). Against the dark-mode background this is ~3:1 contrast, below the 4.5:1 AA baseline — the reported "icons hard to read." Found the identical pattern in `MonthYearPicker.xaml`'s unselected month/prev-year/next-year button `Foreground` (used by the Invoice Month picker and the CashFlow Monthly page's month selector). Both replaced with `{DynamicResource AccentTextFillColorPrimaryBrush}`, the same theme-aware accent-text token already used for `Sidebar.xaml`'s selected nav item (PR #715). `MonthYearPicker.xaml`'s *selected*-month `Background="#0F6CBD"`/`Foreground="White"` pairing was left as-is — self-contained contrast (white-on-blue, ~5.7:1 regardless of theme), matching the same deliberately-pinned-brand-blue convention already established for Primary buttons, not a readability bug. The column *label* text itself uses `App.xaml`'s already theme-aware `DataGridColumnHeader` style (`TextFillColorPrimaryBrush`) and was not touched — if header text (not just icons) is still hard to read after this fix, that would point to a separate, deeper issue and should be reported again with specifics. Verified: `dotnet build`/`dotnet test Tests/Financial.Presentation.Tests` (1198 passed) clean; pending the user's visual confirmation like #12 above (no WPF GUI in this environment). | Medium, fix pending verification |

## Part A — Confirmed non-compliant items: Grids — data & interaction (added 2026-09-04)

Two issues reported directly by the user while using the app, not found by the original sweep.
Rule of record: `forms-data-and-visualisations.md`'s grid conventions (consistent column naming
and filterability) and `ux-principles.md` (don't discard a user's in-progress view state on an
unrelated action).

| # | Location | Finding | Severity |
|---|---|---|---|
| 13 | ~~`Financial.Web/src/components/ExpensesSection.tsx:143-148` vs. `IncomeSection.tsx:150-165`~~ | **Resolved 2026-09-04 on Web, then also on WPF once the user pointed out the same gap there.** Web: renamed the column from "Payment Source" to "Bank" and added a `ColumnFilterMenu` keyed on `expense.paymentSourceBankName`, mirroring `IncomeSection.tsx`'s existing "Bank" column exactly. WPF: `ExpenseSectionView.xaml`'s equivalent Expenses grid had the identical gap — column still named "Payment Source", no filter — while `BanksGridView.xaml` (Bank Summary) and `BankSectionView.xaml` (Transfers/Adjustments) already had their own correctly-scoped, independent Bank filters (`MonthlyViewModel.BankFilter` and `BankOperationsWorkflowViewModel.BankFilter` respectively — verified these are two genuinely separate, working filters, not one misapplied instance). Added `ExpenseWorkflowViewModel.ExpensesBankFilter` (same `ColumnFilterViewModel<ExpenseDTO>` pattern as the existing `ExpensesCategoryFilter`/`ExpensesCardFilter`) and renamed the column to "Bank", closing the actual gap. `CreditCardExpensesView.xaml`'s own "Payment Source" column and `ExpenseFormView.xaml`'s form field label of the same name are unrelated (different grid; a form field, not a column) and were left untouched. | ~~Medium~~ Resolved |
| 14 | ~~`Financial.Web/src/pages/MonthlyPage.tsx:221-222` gating `ExpensesSection`/`IncomeSection` behind `isLoading`, combined with `useMonthly.ts` (`RETRY` re-dispatches `FETCH_START` → `isLoading: true` after every add/edit/delete)~~ | **Resolved 2026-09-04, wider than first scoped — 7 hooks affected, not just `useMonthly.ts`.** Traced and fixed the full set of Web hooks with this shape: `useMonthly.ts`, `useBankOperations.ts`, `useCreditCards.ts`, `useMensais.ts`, `useControleMae.ts`, `useInvestmentSnapshots.ts`, `useReserva.ts`. Each hook's fetch logic was extracted into a reusable function and a new `refreshSilently()` (or, for `useReserva.ts`, its existing `fetchReservaData()` with the `FETCH_START` dispatch moved out to only the initial-load effect) added: mutation success handlers now call it directly instead of dispatching `RETRY`, so a post-mutation refresh never flips `isLoading` and the grid component — and the sort/filter `useState` it owns — stays mounted. The genuine error-retry path (`ErrorState`'s "Retry" button) is untouched and still shows a full reload, which is correct there. **`useCredits.ts`/`usePriceHistory.ts`/`useTransactions.ts` (the three Investment tabs) turned out to already be correct** — their mutation handlers dispatch `SAVE_SUCCESS`/`DELETE_SUCCESS` directly with the mutation response's own fresh data, never touching `isLoading`; they were not part of this bug. Verified: `dotnet build`/`tsc -b`/`eslint` clean, full `vitest run` (1471 passed, one confirmed-unrelated flaky timeout), and one `MonthlyPage.test.tsx` case updated to match the new (now-immediate rather than next-render-deferred) refresh timing. **WPF not checked** — `MonthlyView.xaml`'s equivalent uses `Visibility` binding, a different mechanism that may or may not reproduce the same loss; out of scope for this pass. | ~~High~~ Resolved (Web only) |

## Part A — Confirmed non-compliant items: Forms

Rule of record: `forms-data-and-visualisations.md` §"Inline form, dialog, drawer, or page" —
**"'New X' create actions are always inline forms, never a popup Window/modal dialog, on both
platforms."**

| # | Location | Finding | Severity |
|---|---|---|---|
| 1 | Web: 10 `*FormDialog.tsx` (Fluent `Dialog`); WPF: 11 `*FormDialog.xaml` (popup `Window`, e.g. `BankFormDialog.xaml`) | **Resolved 2026-09-04 via `decisions/ADR-006-admin-crud-modal-dialogs.md`** — Admin lookup-entity CRUD (Bank, Broker, Category, CreditCard, IncomeSource, InvestmentAccount, Portfolio, RecurringBill, ReserveBucket, Asset) is now a documented exception to the "New X is inline" rule: no associated chart/running total, rarely edited, genuinely short forms — exactly the case the doc's own guidance already names as correct for a dialog. No source files changed | ~~High~~ Resolved |
| 2 | ~~`MensaisPage.tsx:271-376`, `ControleMaePage.tsx` form, three Investment tabs' inline "New X" forms, `InvestmentSnapshotsPage` edit panel~~ | **Resolved via PR #709 (CashFlow) + #710 (Investment).** All five surfaces rewritten with Fluent `Field`/`Input`/`Select`, required asterisks, and inline `validationMessage`; `ControleMaePage`'s BRL/GBP inputs also gained number validation they previously lacked entirely. | ~~Medium~~ Resolved |
| 3 | ~~`AddBillFormView.xaml`, `EditBillFormView.xaml`, `CreateEntryFormView.xaml`, `EditEntryFormView.xaml`, `EditSnapshotValueFormView.xaml`~~ | **Resolved via PR #708.** All five rewritten to the 4-column responsive grid with `FieldLabelStyle`. | ~~High~~ Resolved |
| 4 | ~~Same five files as #3~~ | **Resolved via PR #708** (required-marker/validation half). Added required-marker runs and per-field `FieldErrorTextStyle` bindings with corresponding ViewModel field-error properties. The "hardcoded `#CCCCCC`/`#FAFAFA` borders" half of the original finding was re-checked before fixing and found already stale — these files were already using `DynamicResource` borders at investigation time, so no border change was needed. | ~~Medium~~ Resolved |
| 5 | ~~Reserve forms (Amount before Description), Expense forms (Payment Source after Value)~~ | **Re-verified line-by-line 2026-09-04, resolved.** `ExpenseForm.tsx`/`ExpenseFormView.xaml` already put Payment Source/Card *before* Value on both platforms — that half of the original 2026-08-29 finding was stale even before this pass. Of the three Reserve forms, `WithdrawalForm.tsx`/`WithdrawalFormView.xaml` and `EditMovementForm.tsx`/`EditReserveMovementFormView.xaml` already order Description before Amount (also stale); only `IncomeSplitForm.tsx`/`IncomeSplitFormView.xaml` genuinely had Amount before Description on both platforms — fixed by swapping the two fields so Description now precedes Amount, matching `docs/ui/forms-data-and-visualisations.md`'s default field order and the other two Reserve forms. Verified: Web `vitest run` (1471 passed), `tsc -b`/`vite build` clean; `dotnet test Tests/Financial.Presentation.Tests --filter FullyQualifiedName~IncomeSplit` (19 passed), WPF Release build clean. | ~~Medium~~ Resolved |
| 6 | ~~Web: 8 files, 9 message bars. WPF: 10 ViewModels, 12 views (see prior revision of this row for the full file list).~~ | **Reported by the user 2026-09-04, resolved in three stages, all now confirmed fixed.** **(a) Duplication:** a field-specific validation error rendered twice — inline under the field, and again in the bottom-of-form message. Fixed by only showing the bottom message when no field claimed it (Web: `MessageBar` guarded by `Object.keys(saveErrorFields).length === 0`; WPF: new `*GeneralSaveError` per-ViewModel property). **(b) "WPF shows all invalid fields at once, Web only the first" — user preferred WPF's behaviour, but tracing WPF's actual mechanism found a quirk:** it joins every failing message into one string and each field's error property re-shows the *entire* joined string if its own fragment is anywhere in it — so today, WPF repeats the same multi-line block under every invalid field, not each field's own line. Given the choice, the user picked the cleaner design (each field shows only its own message) over exactly replicating WPF's quirk. Implemented on **both** platforms: Web's 11 form hooks now validate every field per submit and store a `Partial<Record<Field,string>>` map (`useFieldError` redesigned around it) instead of stopping at the first failure; WPF's per-field `Match*FieldError` helpers now split the joined message on `Environment.NewLine` and return only the one matching line instead of the whole string. Verified: `dotnet test Tests/Financial.Presentation.Tests` (1198 passed), Web `vitest run` (1471 passed), `tsc -b`/`eslint` clean. **(c) Field misalignment — confirmed to be a Web-only bug (not WPF as originally guessed), root-caused and fixed.** Measured live in a browser (`formPanelStyles.ts`'s 4-column grid, no `align-items` set): the outer grid's default `align-items: stretch` gives every `Field` the tallest sibling's height; Fluent's `Field` root is itself bare `display:grid` with implicit `auto` rows and no `align-content` override, so the browser's default stretch behaviour redistributes that extra height unevenly between a field's label/control/message rows depending on how many rows it currently has — measured up to 13px of vertical drift between a field showing a message and its siblings that weren't. Fixed with one line, `alignItems: 'start'` on `formPanelStyles.grid`; re-measured after the fix — all inputs align within 1px regardless of which sibling shows a message. | ~~Medium~~ Resolved |

## Part A — Theming system (root cause)

| # | Location | Finding | Severity |
|---|---|---|---|
| 1 | ~~`Financial.Web/src/index.css`; `Financial.App/App.xaml` resource keys~~ | **Resolved via PR #704.** Added `--success` (light `#2e7d32` / dark `#66bb6a`) to Web's `index.css`; expanded `SystemFillColorSuccessBrush`/`SystemFillColorCriticalBrush` usage across WPF (see Grids WPF #6, #7). `warning` tokens were already present on both platforms and were never the gap. | ~~Medium~~ Resolved |

**Not a finding:** the ~50 WPF files pinning `AccentButtonBackground*` to literal ADR-005 hex are
intentional (see Part A — Buttons, WPF). The risk worth documenting instead is the pattern behind
Grid finding #2 (WPF): scoping a Wpf.Ui style locally without `BasedOn` silently drops the
ancestor style rather than layering on it — worth a note in `docs/ui/wpf.md` so a future scoped
style change doesn't repeat it.

## Part A — Cross-platform parity

Rule of record: React defines the workflow; WPF must reach the same outcome
(`docs/rules/ui.md`, `standards-hierarchy.md`).

| # | Finding | Severity |
|---|---|---|
| 1 | ~~**Corrected 2026-09-04 — drift confirmed, but on the opposite platform than originally described.**~~ **Resolved 2026-09-05.** Renamed WPF's three remaining "Move Money" strings to match Web's naming exactly: `BankSectionView.xaml`'s trigger button ("Move Money" → "New Transfer"), `TransferFormView.xaml`'s panel title ("Move Money" → "New Transfer", already correctly "Edit Transfer" in edit mode), and its submit button ("Move Money" → "Add Transfer", already correctly "Save" in edit mode) — now an exact match for `BankOperationsSection.tsx`/`TransferForm.tsx`'s trigger→title→confirm chain. Also updated a stale doc comment in `TransferWorkflowViewModel.cs` that referenced "Move Money" by name. Internal identifiers (e.g. `ShowMoveMoneyFormCommand`) were left as-is — not user-facing text, and renaming them wasn't needed to close this finding. | ~~Medium~~ Resolved |
| 2 | ~~Both platforms split along the same "migrated vs. legacy" line...~~ | **Resolved.** The coordinated fix workstream this finding recommended is exactly what PR #708 (WPF), #709 (Web CashFlow) and #710 (Web Investment) delivered — both platforms' legacy form clusters (Mensais, ControleMae, Bill/Entry/Snapshot forms) were migrated together as fix-order item 7. | ~~Medium~~ Resolved |
| 3 | ~~Grids/Buttons finding "Move/Delete Portfolio"...~~ **Corrected 2026-09-04 — not a violation**, see Buttons (Web) #3's correction above. Both platforms already give Move/Delete Portfolio the same primary treatment, which is what the doc requires. | ~~Medium~~ N/A |

## Part B — No governing standard exists yet (needs a decision, not a guess)

Carried forward from the standards docs' own list; not counted as violations above.

1. Row-level Edit/Delete icon convention (only the "New X" create icon is specified).
2. Filter/chart-mode toggle "chip" pattern.
3. Manual-vs-automatic price-source colour (Grids — Web #4).
4. Required-field indicator mechanism.
5. Contextual-help mechanism.
6. Multi-step decision dialog layout (e.g. Move Asset).
7. Whether inline computed-value sentences should be bold.
8. Whether Save/Cancel/Confirm need icons.
9. Post-submit itemized result view styling.
10. Year-only selector (distinct from the documented month+year picker).
11. Breadcrumb semantic structure.
12. Hand-rolled tab-strip component convention.
13. Status-dot/badge convention.
14. Drag-to-resize persistent workspace layout.
15. Async batch-fetch progress indicator shape.

## Suggested fix order

1. ~~**Kill the dark-mode regressions first**~~ **Resolved via PR #703** — Grids (Web) #1, Grids
   (WPF) #1, #2, #3, #8 (mostly), #9. One Web commit, one WPF PR, matching the plan.
2. ~~**Introduce the missing status tokens**~~ **Resolved via PR #704** — Theming #1, plus the
   Grids #7 (Web) / #6-#7 (WPF) find-and-replace this unblocked.
3. ~~**Resolve the button-size/position rule directly**~~ **Resolved via PR #705** — Buttons (Web)
   #1, Buttons (WPF) #1, #2. Note: this item's scope was the size/alignment/width rule only —
   Buttons (Web) #4/#6/#7 and Buttons (WPF) #4/#5 are separate findings, not part of this item,
   and remain open (see their rows above).
4. ~~**Fix the two undifferentiated destructive-action spots**~~ **Closed 2026-09-04, no fix
   needed** — Buttons (Web) #3 and Parity #3 were both re-read against the actual rule text and
   found to be false positives: the doc names this exact Move/Delete Portfolio pairing as the
   canonical example of two peer buttons that must both stay primary. See the corrections in
   those sections above.
5. ~~**Decide the Admin-dialog question**~~ **Resolved 2026-09-04** — Forms #1. The user chose the
   ADR-exception path over re-platforming; see `decisions/ADR-006-admin-crud-modal-dialogs.md`.
   No source files changed.
6. ~~**Consolidate the three row-action icon conventions**~~ **Resolved via PR #707** — Buttons
   (WPF) #3. Buttons (Web) #2 turned out to already be compliant on re-check (false positive, no
   Web files needed changing).
7. ~~**Schedule the legacy-form migration as its own workstream**~~ **Resolved via PR #708 (WPF) +
   #709 (Web CashFlow) + #710 (Web Investment)** — Forms #2, #3, #4.

## Verification pass (2026-09-04)

Every finding above was re-checked against current code before being marked. This section
originally listed 8 items still open as of 2026-09-04; all but one have since been fixed or
closed as false positives in follow-up PRs (#714-#725) tracked inline in the tables above. Only
one item from that original list remains genuinely open:

- **Grids (WPF) #12** and the `NavigationView.xaml` half of **Grids (WPF) #8** —
  `InactiveSelectionHighlightBrushKey`/`InactiveSelectionHighlightTextBrushKey` hardcoded to
  `#007ACC`/White in both `App.xaml` and `NavigationView.xaml`. The prior fix attempt used a
  `Binding` on a `StaticResource`-wrapped `Brush`'s `Color` sub-property, which doesn't propagate
  reliably in a loose `ResourceDictionary` (no `DataContext`, frozen at parse time) and had to be
  reverted. PR #726 tries a different, better-grounded technique: `{DynamicResource}` directly on
  the `Color` property, targeting the same two `Color` resources
  (`SystemAccentColorPrimary`/`TextOnAccentFillColorPrimary`) that already back
  `ListBoxItemSelectedBackgroundThemeBrush`/`ListBoxItemSelectedForegroundThemeBrush` — the
  brushes both styles already use for the *focused*-selected state — confirmed via the Wpf.Ui
  4.0.0 GitHub source. This environment has no WPF GUI to verify the result, so it's flagged
  pending the user's spot-check rather than marked Resolved.

## Not yet actioned

The Part B gap list (15 items) is unchanged — those are product decisions with no governing
standard, not failures. Everything else in this document is resolved, a documented false
positive, or an explicit Gap awaiting a product decision, except **Grids (WPF) #12**/#8 (above),
which has a fix up in PR #726 awaiting the user's visual confirmation.
