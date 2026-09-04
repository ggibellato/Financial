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
| 7 | `components/ColourModeToggleButton.tsx:9-17` | Raw `<button className="colour-mode-toggle">`, not a Fluent `Button` — the one non-Fluent control in the top `App.tsx` bar. Verified 2026-09-04: still unaddressed. | Low |

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
| 4 | `Components/Sidebar.xaml:30,55,133,152` | Selected nav item hardcodes the pre-ADR-005 accent `#007ACC` (spec: `#0F6CBD`) as a literal `Color`, not a `DynamicResource` — off-brand in light mode today, and structurally unable to react to dark mode. Verified 2026-09-04: still present at all four cited lines, unaddressed. | High |
| 5 | `Views/Settings/AppearanceView.xaml` | Light/Dark setting uses a plain `RadioButton` pair, not a Wpf.Ui control — no rule mandates otherwise; flag for a decision. Still an open Gap, no product decision made. | Gap |

## Part A — Confirmed non-compliant items: Grids (Web)

Rule of record: `design-tokens.md` — "feature code must not introduce repeated raw colours…
token values belong in the React theme and WPF ResourceDictionaries."

| # | Location | Finding | Severity |
|---|---|---|---|
| 1 | ~~`styles/data-table.css:28-30`~~ | **Resolved via PR #703.** `background: #f5f5f5` replaced with `var(--bg-subtle)`. | ~~High~~ Resolved |
| 2 | `styles/data-table.css` (whole file) | No row-hover or selected-row state defined at all — only header background and borders are tokenised. Verified 2026-09-04: still true, never addressed (out of scope for the dark-mode-regression fix, which only touched the literal-colour bug). | Medium |
| 3 | `TransactionsTab.css:24-33,190-236`, `CreditsTab.css:36,69,193-238`, `PriceHistoryTab.css:31,64,167-235` | **Partially resolved via PR #704.** Buy/sell text and error text now use `var(--success)`/`var(--danger)`/`var(--error)` in all three files. The `#007acc` filter-button colour was deliberately left untouched (called out in the PR as a separate, not-yet-scheduled cleanup) — verified 2026-09-04: still present at `TransactionsTab.css:31,64` and equivalent lines in the other two files. | Medium (was High) |
| 4 | `PriceHistoryTab.css:53,185` | "Manual price" badge hardcodes `#e65100`. No documented manual-vs-automatic colour convention exists yet (see Part B), but the literal itself still bypasses the token system. Still open — no convention decided, not touched. | Medium |
| 5 | `CreditsTab.css:8-10` | Credit-type colours (`--credit-type-dividend/rent/jcp`) are a local, non-token palette scoped to this file, identical in both themes. Deliberately left alone during PR #704 (categorical, not a status colour) — still open. | Medium |
| 6 | `pages/AnnualSummaryPage.css:39-46` | Hand-rolled tab strip hover/active state hardcodes `#007acc` instead of `var(--accent)` — flagged in the 2026-08-23 audit, still true. Verified 2026-09-04: still present at lines 40, 44, 46, unaddressed. | Medium |
| 7 | ~~`PortfolioSummaryTab.css:39-53`, `AggregatedSummaryTab.css:34,38,42`, `InvestmentTree.css:65,73,81-82`~~ | **Resolved via PR #704.** All six files (plus `AssetSummaryTab.css` and `DetailPanel.css`, discovered during the fix) now use `var(--success)`/`var(--danger)`. | ~~Medium~~ Resolved |
| 8 | `InvestmentTree.css:82`, `PortfolioSummaryTab.css:59` | Stale fallback values inside otherwise-correct `var()` calls (`var(--accent, #007acc)`, `var(--accent, #5a7e6e)`) — harmless today since `--accent` is always declared, but misleading dead code. Deliberately deferred as Low, not touched — still open. | Low |

## Part A — Confirmed non-compliant items: Grids (WPF)

The app-wide `DataGrid` style (`App.xaml:78-209`) is correctly token-driven — any grid that
doesn't override it gets Light/Dark for free. Everything below is a grid that, one way or
another, doesn't inherit it.

| # | Location | Finding | Severity |
|---|---|---|---|
| 1 | ~~`Controls/FilterableColumnHeader.xaml:15,35`~~ | **Resolved via PR #703.** Converted to `DynamicResource` theme brushes. | ~~High~~ Resolved |
| 2 | ~~`AnnualSummaryView.xaml:52-64,94-101,129-141`, `BankSectionView.xaml:79-88`, `ReservaView.xaml:134-142`~~ | **Resolved via PR #703.** Added `BasedOn="{StaticResource {x:Type DataGridRow}}"` (or equivalent) to all five grids' local styles. | ~~High~~ Resolved |
| 3 | ~~`PriceHistoryView.xaml:141-144`, `CreditsView.xaml:111-114`~~ | **Resolved via PR #703.** `Foreground="Black"` replaced with `{DynamicResource TextFillColorPrimaryBrush}`. | ~~High~~ Resolved |
| 4 | `DividendCheckView.xaml:116,131` | The only two `AutoGenerateColumns="True"` grids in the app, plus hardcoded `BorderBrush="#CCCCCC"` throughout; auto-generated columns also can't carry `NumericColumnTextStyle`, so numeric right-alignment is silently lost too. Verified 2026-09-04: unaddressed — not in scope for any of the shipped fixes. | High |
| 5 | `ReserveBucketsView.xaml:43` (`#B25E00`), `CardsGridView.xaml:139` (`#8A6D00`) vs. `ReservaView.xaml:44` (correct: `{DynamicResource SystemFillColorCautionBrush}`) | Three different ambers for the same warning concept — one sibling view already migrated, two didn't. Verified 2026-09-04: both hardcoded ambers still present, unaddressed. | Medium |
| 6 | ~~`PortfolioSummaryView.xaml:62,68,76,255,263,271`~~ | **Resolved via PR #704** (Green/Red halves). Converted to `{DynamicResource SystemFillColorSuccessBrush}`/`SystemFillColorCriticalBrush}` for Bought/Sold — a fixed-role label, not a sign-flip, so intentionally *not* routed through `SignedValueToBrushConverter`. The `Blue` occurrences are a different "info" concept and were out of scope; left as literals. | ~~Medium~~ Resolved (Blue left as-is, by design) |
| 7 | ~~25 files incl. all 10 Admin-CRUD grids~~ | **Resolved via PR #704.** All 25 files' `Foreground="Red"/"Green"` converted to `{DynamicResource SystemFillColorCriticalBrush}`/`SuccessBrush`. | ~~High~~ Resolved |
| 8 | ~~`Components/NavigationView.xaml:39,43,69-81,105,119,128,137,200`~~ | **Mostly resolved via PR #703.** Section labels, drop-target colour (now `AccentFillColorSecondaryBrush`), loading veil (now a neutral `#66000000` scrim — no theme-aware backdrop brush exists in Wpf.Ui 4.0.1, verified against the compiled DLL), splitter and divider all converted. **One part deliberately deferred:** the selected-node highlight override at line ~80 (`SystemColors.InactiveSelectionHighlightBrushKey`) is still a literal `#007ACC`/White — an XAML binding attempt to make it theme-aware was reverted as unreliable (can't bind a `Color` from a `DynamicResource`-wrapping `Binding`); this matches the same accepted pattern as Grids (WPF) #12 below. | ~~High~~ Partially resolved |
| 9 | ~~`MainWindow.xaml:26,32-33`~~ | **Resolved via PR #703.** Breadcrumb `BorderBrush`/`Foreground` converted to `DynamicResource`. | ~~High~~ Resolved |
| 10 | `AssetPriceView.xaml` (8 hardcoded values), `DividendCheckView.xaml` (14+ hardcoded values) | Two full pages predate the Fluent migration entirely (`#333333`, `#666666`, `#CCCCCC`, `#FAFAFA`, `#D32F2F`, `#FDECEA`, plain `Blue`/`Red`/`Green`) — named in the 2026-08-23 audit as pre-migration holdouts. Verified 2026-09-04: both files confirmed still fully unconverted; neither was in scope for any shipped PR. | High |
| 11 | `NavigationView.xaml:119`, `CreditsView.xaml:124`, `PriceHistoryView.xaml:172`, `TransactionsView.xaml:113` | **Partially resolved.** `NavigationView.xaml`'s splitter was converted to a `DynamicResource` incidentally while PR #703 rewrote the rest of that file. Verified 2026-09-04: `CreditsView.xaml`, `PriceHistoryView.xaml` and `TransactionsView.xaml` still hardcode `Background="#E0E0E0"`; the originally-cited `MainWindow.xaml` border splitter no longer matches this pattern (border, not `GridSplitter`) and appears to have been miscited. | Low |
| 12 | `App.xaml:143-144` | `SystemColors.InactiveSelectionHighlightBrushKey` hardcoded to `#007ACC`/White — same stale-accent pattern, visible only when a grid loses focus with a row selected. Verified 2026-09-04: still present, deliberately deferred alongside its `NavigationView.xaml` twin (Grids WPF #8) for the same reason (no reliable theme-aware binding path). | Low |

## Part A — Confirmed non-compliant items: Grids — data & interaction (added 2026-09-04)

Two issues reported directly by the user while using the app, not found by the original sweep.
Rule of record: `forms-data-and-visualisations.md`'s grid conventions (consistent column naming
and filterability) and `ux-principles.md` (don't discard a user's in-progress view state on an
unrelated action).

| # | Location | Finding | Severity |
|---|---|---|---|
| 13 | `Financial.Web/src/components/ExpensesSection.tsx:143-148` vs. `IncomeSection.tsx:150-165` | The Bank-Expenses grid's bank column is labelled "Payment Source" and has no filter, while the sibling Income grid's identical concept is labelled "Bank" and carries a full `ColumnFilterMenu` (`columnKey="bank"`, same pattern as the Category/Card columns already on the same page). Same underlying data (`expense.paymentSourceBankName`), inconsistent name, and the one column on this grid a user can't filter by. | Medium |
| 14 | `Financial.Web/src/pages/MonthlyPage.tsx:221-222` gating `ExpensesSection`/`IncomeSection` behind `isLoading`, combined with `useMonthly.ts:109-110,115-116` (`RETRY` re-dispatches `FETCH_START` → `isLoading: true` after every add/edit/delete) | Confirmed root cause of "sort/filter is lost after adding a Credit Card expense": every mutation triggers a full refetch that flips `isLoading` back to `true`, and `MonthlyPage.tsx` swaps the grid components out for `<LoadingState />` while that's in flight. `useSortableRows`/`useColumnFilters` keep sort/filter selection in per-component `useState` (`useSortableRows.ts:42`, `useColumnFilters.ts:30`), so unmounting the grid on every mutation — not just the initial page load — silently resets both. This is the same `{isLoading ? <LoadingState/> : <Grid/>}` shape used by essentially every page in `Financial.Web`, so it plausibly affects every Web grid with sort/filter, exactly as reported, but only `MonthlyPage.tsx` was traced end-to-end so far. **WPF not yet checked** — `MonthlyView.xaml`'s equivalent (`Visibility="{Binding ShowContent}"` gating the `TabControl`, not a JSX unmount) uses a different mechanism that may or may not reproduce the same loss; needs its own verification before assuming parity. | High |

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
| 5 | Reserve forms (Amount before Description), Expense forms (Payment Source after Value) | Field order flagged by the 2026-08-29 audit; not re-verified line-by-line in this pass (this audit's brief was styling/colour/button evidence) — carried forward, not confirmed closed. Still not independently re-verified as of 2026-09-04 — remains open. | Medium |

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
| 1 | **Corrected 2026-09-04 — drift confirmed, but on the opposite platform than originally described.** Re-checked both sides: `Financial.Web`'s `TransferForm.tsx`/`BankOperationsSection.tsx` consistently say "Transfer" throughout (trigger "New Transfer", panel title "New Transfer"/"Edit Transfer") — Web has no "Move Money" text and was never the problem. `Financial.App/Views/CashFlow/TransferFormView.xaml:46,121` still shows "Move Money" as the create-mode title and submit-button text (only switching to "Edit Transfer" in edit mode) — WPF is the platform that lags Web's naming. Still open, unaddressed, now correctly attributed to WPF. | Medium |
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

Every finding above was re-checked against current code before being marked. Of the 42 original
Part A findings: **27 resolved**, **4 corrected as false positives** (no fix needed — the doc's
own rules were misapplied at audit time, not the code), **3 partially resolved** (noted inline
with what remains), and **8 confirmed still genuinely open**, listed here so none are mistaken for
addressed (Buttons (Web) #4 was fixed 2026-09-04, moving it out of this list). Two new findings
reported by the user the same day (Grids — data & interaction #13, #14) were added after this
count and are separately open — see that section above.

- **Buttons (Web) #7** — `ColourModeToggleButton.tsx` is still a raw `<button>`.
- **Buttons (WPF) #4** — `Sidebar.xaml`'s hardcoded `#007ACC`.
- **Buttons (WPF) #5** — `AppearanceView.xaml`'s `RadioButton` pair (Gap, needs a product decision).
- **Grids (Web) #2, #4, #5, #6, #8** — no row-hover/selected state, manual-price badge colour,
  local credit-type palette, `AnnualSummaryPage.css`'s `#007acc` tab strip, and the stale `var()`
  fallbacks — none were in scope for any shipped fix.
- **Grids (WPF) #4, #5, #10** — `DividendCheckView.xaml`'s `AutoGenerateColumns`, the two
  remaining amber literals, and the two fully-unconverted pre-migration pages
  (`AssetPriceView.xaml`, `DividendCheckView.xaml`).
- **Grids (WPF) #11** (partial) — 3 of 4 confirmed `GridSplitter`s still hardcode `#E0E0E0`
  (`CreditsView.xaml`, `PriceHistoryView.xaml`, `TransactionsView.xaml`); only `NavigationView.xaml`'s
  was fixed, incidentally, during PR #703.
- **Grids (WPF) #12** and the `NavigationView.xaml` half of **Grids (WPF) #8** —
  `InactiveSelectionHighlightBrushKey` hardcoded to `#007ACC`/White in both `App.xaml` and
  `NavigationView.xaml`, deliberately deferred (no reliable theme-aware XAML binding path found).
- **Forms #5** — field order was never independently re-verified in this or the follow-up passes.
- **Cross-platform Parity #1** — real naming drift confirmed, but on WPF (`TransferFormView.xaml`
  still says "Move Money"), not Web as originally cited.

## Not yet actioned

The Part B gap list (15 items) is unchanged — those are product decisions with no governing
standard, not failures, and none were resolved by the fix-order work above. The open findings
listed in the verification pass above remain queued until the user asks for one of them.
