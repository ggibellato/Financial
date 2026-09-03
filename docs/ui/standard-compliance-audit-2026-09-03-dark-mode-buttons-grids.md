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
| 1 | `BankOperationsSection.tsx:92-98`, `IncomeSection.tsx:46,55`, `ExpensesSection.tsx:42,51`, `CreditsTab.tsx:81,90`, `PriceHistoryTab.tsx:58,69`, `TransactionsTab.tsx:59,68`, `DetailPanel.tsx:88,98,109` | Labelled "New X" triggers set `size="small"`, the size reserved for icon-only row actions; the 10 Admin-CRUD pages leave the equivalent trigger unsized (medium, correct) | Medium |
| 2 | `BanksPage.tsx:94-108` (+ 9 sibling Admin pages) vs. `TransactionsTab.tsx:57-73` | Row Edit/Delete uses two different `appearance` values for the identical action: Admin-CRUD = `size="small"`, no `appearance` (default outline); Investment tabs = `appearance="subtle" size="small"` | Medium |
| 3 | ~~`components/DetailPanel.tsx:96-116`~~ | **Corrected 2026-09-04 — not a violation.** Originally flagged as "Move…"/"Delete Portfolio" needing a distinct destructive treatment. Re-read of `forms-data-and-visualisations.md` lines 146-150 and 172-174 found the doc names this exact pair by name as the canonical example of peer action buttons that must **both stay primary, same style** — "never treat a Move/Delete pair as if it were a Save/Cancel pair." The `size="small"` part of the original finding was real and is fixed (see Buttons #1); the appearance/distinction part was a misreading of the rule and required no change. WPF's equivalent (`NavigationView.xaml`'s Move Asset/Delete Portfolio buttons) was already compliant with the same rule for the same reason. | ~~High~~ N/A |
| 4 | `pages/MensaisPage.tsx:253-263` | Two adjacent `appearance="primary"` buttons ("Add Bill", "Reset All to Unset") — a bulk/destructive action carries the same visual priority as the page's create action | Medium |
| 5 | `TransactionsTab.css:162-188`, `CreditsTab.css`, `PriceHistoryTab.css` | Save/Cancel inside the inline "New X" form are raw HTML (`.transactions-tab__save-btn`, `background: var(--accent); color:#fff; border-radius:3px`), not Fluent `Button`, despite the trigger and row actions in the same file being genuine Fluent components | Low |
| 6 | `TransferForm.tsx:116`, `BankOperationsSection.tsx:92-98` | Trigger reads "New Transfer"; the form it opens is titled and submitted as "Move Money" — breaks the trigger→title→confirm naming chain | Medium |
| 7 | `components/ColourModeToggleButton.tsx:9-17` | Raw `<button className="colour-mode-toggle">`, not a Fluent `Button` — the one non-Fluent control in the top `App.tsx` bar | Low |

## Part A — Confirmed non-compliant items: Buttons (WPF)

The `AccentButtonBackground`/`AccentButtonBackgroundPointerOver`/`AccentButtonBackgroundPressed`
overrides pinned in ~50 files are the documented, deliberate ADR-005 mechanism for matching Web's
exact hex (`ApplicationAccentColorManager` generates the wrong shade from a seed colour) — **not**
a finding.

| # | Location | Finding | Severity |
|---|---|---|---|
| 1 | `BankFormDialog.xaml:47-50` (right, 11 Admin dialogs + `MoveAssetDialog`) vs. `WithdrawalFormView.xaml:95-110` (left/default, 12 inline forms) | Save/Cancel row alignment flips depending on whether the form is a popup `Window` or an inline panel — undocumented, and the concrete shape of the "button position" complaint | Medium |
| 2 | 90 (11 Admin dialogs), 100 (`AddBillFormView`), 110 (7 inline forms), 130 (`IncomeSplitFormView`), 80/70 (`UkExpensePromptDialog`) | Five different primary-button `MinWidth` values for the identical "commit this form" action | Medium |
| 3 | `ExpenseSectionView.xaml:40-44` (correct `ui:SymbolIcon`) vs. ~16 files with `Button Content="✏"/"🗑"` emoji vs. `TransactionsView.xaml:150-161` (Segoe MDL2 glyph in a `TextBlock`) | Row Edit/Delete rendered three incompatible ways; only the first renders actual Fluent chrome | Medium |
| 4 | `Components/Sidebar.xaml:30,55,133,152` | Selected nav item hardcodes the pre-ADR-005 accent `#007ACC` (spec: `#0F6CBD`) as a literal `Color`, not a `DynamicResource` — off-brand in light mode today, and structurally unable to react to dark mode | High |
| 5 | `Views/Settings/AppearanceView.xaml` | Light/Dark setting uses a plain `RadioButton` pair, not a Wpf.Ui control — no rule mandates otherwise; flag for a decision | Gap |

## Part A — Confirmed non-compliant items: Grids (Web)

Rule of record: `design-tokens.md` — "feature code must not introduce repeated raw colours…
token values belong in the React theme and WPF ResourceDictionaries."

| # | Location | Finding | Severity |
|---|---|---|---|
| 1 | `styles/data-table.css:28-30` | `background: #f5f5f5;` on the even-row stripe — a bare literal, no `var()`, unlike every other rule in the file. **The headline dark-mode bug.** Affects Banks, Income, Category Totals, Cards and Totals grids | High |
| 2 | `styles/data-table.css` (whole file) | No row-hover or selected-row state defined at all — only header background and borders are tokenised | Medium |
| 3 | `TransactionsTab.css:24-33,190-236`, `CreditsTab.css:36,69,193-238`, `PriceHistoryTab.css:31,64,167-235` | Investment grid chrome hardcodes the same three colours in three files: filter buttons `#007acc` (stale — current accent is `#0f6cbd`/`#479ef5`), buy/sell text `#2e7d32`/`#c62828`, error text `#c62828` instead of `var(--error)`. None react to `data-theme="dark"` | High |
| 4 | `PriceHistoryTab.css:53,185` | "Manual price" badge hardcodes `#e65100`. No documented manual-vs-automatic colour convention exists yet (see Part B), but the literal itself still bypasses the token system | Medium |
| 5 | `CreditsTab.css:8-10` | Credit-type colours (`--credit-type-dividend/rent/jcp`) are a local, non-token palette scoped to this file, identical in both themes | Medium |
| 6 | `pages/AnnualSummaryPage.css:39-46` | Hand-rolled tab strip hover/active state hardcodes `#007acc` instead of `var(--accent)` — flagged in the 2026-08-23 audit, still true | Medium |
| 7 | `PortfolioSummaryTab.css:39-53`, `AggregatedSummaryTab.css:34,38,42`, `InvestmentTree.css:65,73,81-82` | Profit/loss `#2e7d32`/`#c62828` copy-pasted identically across six files instead of one `color.status.success`/`danger` token | Medium |
| 8 | `InvestmentTree.css:82`, `PortfolioSummaryTab.css:59` | Stale fallback values inside otherwise-correct `var()` calls (`var(--accent, #007acc)`, `var(--accent, #5a7e6e)`) — harmless today since `--accent` is always declared, but misleading dead code | Low |

## Part A — Confirmed non-compliant items: Grids (WPF)

The app-wide `DataGrid` style (`App.xaml:78-209`) is correctly token-driven — any grid that
doesn't override it gets Light/Dark for free. Everything below is a grid that, one way or
another, doesn't inherit it.

| # | Location | Finding | Severity |
|---|---|---|---|
| 1 | `Controls/FilterableColumnHeader.xaml:15,35` | Shared column-filter popup hardcodes `Background="White" BorderBrush="#CCCCCC"` — registered globally for every sortable/filterable grid column app-wide. Highest-reach dark-mode defect in this audit | High |
| 2 | `AnnualSummaryView.xaml:52-64,94-101,129-141`, `BankSectionView.xaml:79-88`, `ReservaView.xaml:134-142` | Five grids' local `Style`/`RowStyle` omit `BasedOn`, so WPF's all-or-nothing style resolution drops the entire app-wide theme-aware style (no alternating rows, no themed selection) in either theme | High |
| 3 | `PriceHistoryView.xaml:141-144`, `CreditsView.xaml:111-114` | Price/Value columns' `ElementStyle` hardcodes `Foreground="Black"` — unreadable against a dark row background | High |
| 4 | `DividendCheckView.xaml:115-118,130-133` | The only two `AutoGenerateColumns="True"` grids in the app, plus a hardcoded `BorderBrush="#CCCCCC"`; auto-generated columns also can't carry `NumericColumnTextStyle`, so numeric right-alignment is silently lost too | High |
| 5 | `ReserveBucketsView.xaml:43` (`#B25E00`), `CardsGridView.xaml:139` (`#8A6D00`) vs. `ReservaView.xaml:44` (correct: `{DynamicResource SystemFillColorCautionBrush}`) | Three different ambers for the same warning concept — one sibling view already migrated, two didn't | Medium |
| 6 | `PortfolioSummaryView.xaml:62,68,76,255,263,271` | Stat `TextBlock`s hardcode `Foreground="Green"/"Red"/"Blue"` while `SignedValueToBrushConverter` — built for exactly this — sits unused two lines below in the same file | Medium |
| 7 | 25 files incl. all 10 Admin-CRUD grids (Banks, Brokers, Categories, CreditCards, IncomeSources, InvestmentAccounts, Portfolios, RecurringBills, ReserveBuckets, Assets) | 45 hardcoded `Foreground="Red"/"Green"` occurrences vs. 60 correctly-tokenised `{DynamicResource SystemFillColor*Brush}` occurrences across 30 files — the ten Admin grids are the largest untouched cluster; their sibling `*FormDialog.xaml` forms already use the correct token | High |
| 8 | `Components/NavigationView.xaml:39,43,69-81,105,119,128,137,200` | The entire Investment tree/detail workspace shell is unconverted: section labels, selected-node highlight (`#007ACC`/White, same stale accent as the Sidebar), drop-target colour, loading veil, splitter, divider, empty-state text all literal hex. Largest single unconverted surface found | High |
| 9 | `MainWindow.xaml:26,32-33` | Breadcrumb bar (`BorderBrush="#E0E0E0"`, text `Foreground="#666666"`) sits above every page in the app, directly beside the Light/Dark toggle button itself, and doesn't react to the theme it lets you switch | High |
| 10 | `AssetPriceView.xaml` (12 hardcoded values), `DividendCheckView.xaml` (22 hardcoded values) | Two full pages predate the Fluent migration entirely (`#333333`, `#666666`, `#CCCCCC`, `#FAFAFA`, `#D32F2F`, `#FDECEA`, plain `Blue`/`Red`/`Green`) — named in the 2026-08-23 audit as pre-migration holdouts, still true, now also a dark-mode defect | High |
| 11 | `NavigationView.xaml:119`, `CreditsView.xaml:124`, `PriceHistoryView.xaml:172`, `TransactionsView.xaml:113`, `MainWindow.xaml` border | Five `GridSplitter`s hardcode `Background="#E0E0E0"` — low contrast either way, cosmetic | Low |
| 12 | `App.xaml:143-144` | `SystemColors.InactiveSelectionHighlightBrushKey` hardcoded to `#007ACC`/White — same stale-accent pattern, visible only when a grid loses focus with a row selected | Low |

## Part A — Confirmed non-compliant items: Forms

Rule of record: `forms-data-and-visualisations.md` §"Inline form, dialog, drawer, or page" —
**"'New X' create actions are always inline forms, never a popup Window/modal dialog, on both
platforms."**

| # | Location | Finding | Severity |
|---|---|---|---|
| 1 | Web: 10 `*FormDialog.tsx` (Fluent `Dialog`); WPF: 11 `*FormDialog.xaml` (popup `Window`, e.g. `BankFormDialog.xaml`) | **Resolved 2026-09-04 via `decisions/ADR-006-admin-crud-modal-dialogs.md`** — Admin lookup-entity CRUD (Bank, Broker, Category, CreditCard, IncomeSource, InvestmentAccount, Portfolio, RecurringBill, ReserveBucket, Asset) is now a documented exception to the "New X is inline" rule: no associated chart/running total, rarely edited, genuinely short forms — exactly the case the doc's own guidance already names as correct for a dialog. No source files changed | ~~High~~ Resolved |
| 2 | `MensaisPage.tsx:271-376`, `ControleMaePage.tsx` form, three Investment tabs' inline "New X" forms, `InvestmentSnapshotsPage` edit panel | No required-marker or per-field validation, unlike the Fluent-`Field` population (Expense/Income/Transfer/… + every Admin dialog), which already implements the required asterisk and inline `validationMessage` correctly — this closes an item the 2026-08-29 audit listed as unbuilt | Medium |
| 3 | `AddBillFormView.xaml`, `EditBillFormView.xaml`, `CreateEntryFormView.xaml`, `EditEntryFormView.xaml`, `EditSnapshotValueFormView.xaml` | Single-column, one-field-per-row, label-to-the-left layout — named by name in `wpf.md` as the anti-pattern the 4-column responsive grid replaced everywhere else | High |
| 4 | Same five files as #3 | No required marker, bottom-only validation, plus hardcoded `#CCCCCC`/`#FAFAFA` borders (also a dark-mode issue) | Medium |
| 5 | Reserve forms (Amount before Description), Expense forms (Payment Source after Value) | Field order flagged by the 2026-08-29 audit; not re-verified line-by-line in this pass (this audit's brief was styling/colour/button evidence) — carried forward, not confirmed closed | Medium |

## Part A — Theming system (root cause)

| # | Location | Finding | Severity |
|---|---|---|---|
| 1 | `Financial.Web/src/index.css`; `Financial.App/App.xaml` resource keys | `design-tokens.md` names `color.status.success`/`warning`/`danger` as required semantic tokens. Web has `--error` and `--warning-*` but no `--success`; WPF has `SystemFillColorSuccessBrush` available but referenced in only a fraction of the files that need a green. Explains Grid findings #7 (Web) and #6/#7 (WPF) at once | Medium |

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
| 1 | "Move Money" naming drift is confirmed on Web (Buttons #6 above); whether Financial.App's Transfer form carries the same drift wasn't independently confirmed this pass — check both before renaming just one | Medium |
| 2 | Both platforms split along the same "migrated vs. legacy" line: Admin CRUD and Investment tabs got the Fluent/token treatment first on both Web and WPF; Mensais/ControleMae/Reserve-adjacent forms lag on both. A single coordinated fix workstream across both platforms' legacy cluster will close more ground than treating each platform as a separate backlog | Medium |
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

1. **Kill the dark-mode regressions first** — Grids (Web) #1, Grids (WPF) #1, #2, #3, #8, #9.
   These are the bugs a user hits the moment they flip the toggle just shipped. One Web PR, one
   WPF PR.
2. **Introduce the missing status tokens** — Theming #1. Add `--success` (Web) and confirm
   `SystemFillColorSuccessBrush` usage (WPF) once; the Grids #7 (Web) / #6-#7 (WPF) find-and-replace
   becomes mechanical afterward.
3. **Resolve the button-size/position rule directly** — Buttons (Web) #1, Buttons (WPF) #1, #2.
   Pick one trigger size, one Save/Cancel alignment, one primary-button width; apply everywhere in
   the same change.
4. ~~**Fix the two undifferentiated destructive-action spots**~~ **Closed 2026-09-04, no fix
   needed** — Buttons (Web) #3 and Parity #3 were both re-read against the actual rule text and
   found to be false positives: the doc names this exact Move/Delete Portfolio pairing as the
   canonical example of two peer buttons that must both stay primary. See the corrections in
   those sections above.
5. ~~**Decide the Admin-dialog question**~~ **Resolved 2026-09-04** — Forms #1. The user chose the
   ADR-exception path over re-platforming; see `decisions/ADR-006-admin-crud-modal-dialogs.md`.
   No source files changed.
6. **Consolidate the three row-action icon conventions** — Buttons (Web) #2, Buttons (WPF) #3.
   Standardise on the one Fluent-correct pattern each platform already has; replace the rest.
7. **Schedule the legacy-form migration as its own workstream** — Forms #2, #3, #4. Larger effort
   (layout, not just colour), touches Mensais/ControleMae/Bill/Entry/Snapshot forms on both
   platforms; do this after the mechanical fixes above, not before.

## Not yet actioned

This entire document is a diagnostic/reporting deliverable — **no source files have been
changed**. All 41 findings above and the 15 open gaps in Part B remain queued until the user
explicitly asks for one of them to be implemented.
