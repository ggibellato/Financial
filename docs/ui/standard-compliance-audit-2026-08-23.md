# UI Standard Compliance Audit — 2026-08-23

## Executive summary

This audit compares `Financial.Web` (React, the UX source of truth per the user's instruction for this audit) and `Financial.App` (WPF) against the Fluent UI cross-platform standard defined in `docs/ui/*.md` and `docs/ui/decisions/ADR-001..005.md`, as those docs stood on 2026-08-23. It covers all 10 nav pages and ~29 Web components, and all 10 WPF nav items and ~40 views, split into 7 nav-area groups (A–G) audited in parallel.

**Scope note.** Only 7 of 29 Web components actually import `@fluentui/react-components` (all under CashFlow Monthly's Expense/Income/Bank tabs). Compliance in this report is judged against the *documented behavioral/visual rules*, never against "does this import Fluent" — a legacy-CSS/XAML element that already satisfies a rule's letter is not flagged as a violation for being unmigrated; that's tracked separately as lower-severity "implementation debt."

**Counts.**

| Part | Findings |
|---|---|
| Part 1 — Standard violations | 39 |
| Part 2 — No standard defined (deduplicated) | 13 |
| Part 3 — WPF drift from Web | 24 |

| Group | Area | Part 1 | Part 3 |
|---|---|---|---|
| A | Global shell/chrome | 7 | 4 |
| B | CashFlow Monthly (Expense/Income/Bank) | 7 | 5 |
| C | Reserva/Mensais/Controle Mãe | 14 | 7 |
| D | Investment Snapshots & Annual Summary | 7 | 4 (+2 informational) |
| E | Investment workspace shell (tree/detail) | 6 | 3 (+1 informational) |
| F | Investment Transactions/Credits/PriceHistory | 5 | 3 (+2 informational) |
| G | Investment analytics + Dividend/CurrentValues | 7 | 3 (+1 informational) |

**Top findings, by severity:**

1. **[High] Collapsed-sidebar flyout submenus are keyboard-unreachable on both platforms** (Group A) — a portal/`Popup`-rendered submenu that never receives programmatic focus, so keyboard users can never reach a collapsed category's child links. Foundational nav, affects every screen.
2. **[High] `MoveAssetDialog` is a modal on *both* platforms**, violating ADR-003's dialog/drawer taxonomy on Web too, not just WPF (Group E) — plus a genuine functional gap: WPF never offers to delete an emptied source portfolio after a move, unlike Web.
3. **[High] WPF's Dividend Check has no loading/disabled state during an async lookup** — the check button never disables, no "Checking..." indicator, double-submission is possible (Group G).
4. **[High] `DividendCheckView.xaml`/`AssetPriceView.xaml` hardcode `#007ACC`**, which does not match ADR-005's pinned brand hex (`#0F6CBD`) at all — a genuine color-accuracy violation, not just unmigrated styling (Group G).
5. **[High] Reserva/Mensais/Controle Mãe forms are entirely on the old single-column label-left layout**, the literal anti-pattern `docs/ui/wpf.md` names by name (Group C) — the least-migrated area in the app.
6. **[High] Reserva's split-percentage warning and Controle Mãe's/Reserva's totals rows misuse color/weight**: a non-blocking warning renders in the same red as genuine errors; totals rows that should be bold per the documented rule aren't (Group C).
7. **[High] Web's `InvestmentTree`/`SplitPanel`/`DetailPanel` tab strip fail baseline keyboard-operability rules** that their WPF counterparts (native `TreeView`/`GridSplitter`/`TabControl`) satisfy for free — one of the few places WPF is *ahead* of Web (Group E).
8. **[Medium] Systematic row-action icon-button gap**: the primary "New X" button was migrated to `ui:Button`/Fluent across most areas, but row-level Edit/Delete actions stayed on plain `Button`+emoji/glyph everywhere except `ExpenseSectionView` (Groups B, D, F — deduplicated in Part 2).
9. **[Medium] Three independent hand-rolled tab-strip implementations**, no shared component or documented pattern on either platform (Groups A, B, D, E — deduplicated in Part 2).
10. **[Medium] Terminology drift**: WPF's Investment Add/Update forms kept "Add"/"Update" wording after this session's inline-form conversion, while Web says "New"/"Edit" (Group F); Web's own Bank-tab buttons say "New Transfer"/"New Balance Correction" while WPF says "Move Money"/"Correct Balance" (Group B).

## Methodology

**Groups.** A — Global shell/chrome. B — CashFlow Monthly: Expense/Income/Bank tabs (the most-migrated pilot area). C — CashFlow's Reserva/Mensais/Controle Mãe (the least-migrated area). D — CashFlow's Investment Snapshots & Annual Summary. E — Investment workspace shell (tree + detail navigation, incl. Move Asset). F — Investment detail tabs: Transactions/Credits/Price History. G — Investment summary/analytics + Dividend Check + Current Values. Each group was audited by an independent, read-only research agent given the same category definitions and the relevant `docs/ui/*.md` sections.

**Category definitions** (used verbatim by every group):

- **Category 1 — standard violation.** Judged against the *documented rule* (layout, field order, icon usage, inline-vs-dialog, button styling/placement, month/year picker, chart color/label rules, totals styling, alignment, accessibility baseline) — never against "does it import `@fluentui/react-components`" or "does it merge WPF-UI." A legacy-CSS/XAML element that already satisfies a rule's letter is not Category 1. Separately noted (lower severity, "implementation debt") is anything on the *adopted library* per ADR-004 in spirit but not in literal implementation.
- **Category 2 — no standard defined.** An element with no corresponding rule anywhere in `docs/ui/*.md`/ADRs, confirmed absent by grep/search before flagging.
- **Category 3 — WPF drift from Web.** Actual current behavior (terminology, field order, primary action, states, formatting, inline-vs-dialog) compared regardless of either side's standard-compliance. Web's own on-standard status is noted as a caveat, never as a reason to suppress the finding.

Severity: **High** = affects financial-data trust/legibility or blocks a task; **Medium** = clear documented-rule inconsistency or clear parity gap; **Low** = cosmetic/minor.

**Docs consulted:** `docs/ui/README.md`, `standards-hierarchy.md`, `ux-principles.md`, `design-tokens.md`, `forms-data-and-visualisations.md`, `accessibility.md`, `react.md`, `wpf.md`, `review-checklist.md`, `current-state-audit.md`, and `docs/ui/decisions/ADR-001` through `ADR-005`.

---

## Part 1 — Non-compliance with the defined standard

### Group A — Global shell/chrome

| Page/Component (Web) | WPF counterpart | Rule / Doc ref | Finding | Evidence (file:line) | Severity |
|---|---|---|---|---|---|
| `SidebarFlyout.tsx` | `Sidebar.xaml` `CategoryFlyoutPopup` | `accessibility.md` §Keyboard and focus | Collapsed-sidebar submenu items are keyboard-unreachable on both platforms — a portal (`SidebarFlyout`)/`Popup` submenu never receives programmatic focus and closes on blur before a Tab press can reach it. | Web: `Sidebar.tsx:110-145,166-183`, `SidebarFlyout.tsx:16-49`. WPF: `Sidebar.xaml:44-52,145-182`, `Sidebar.xaml.cs:32-53,88-122` | High |
| — | `Sidebar.xaml` collapse/expand button | `accessibility.md` §Semantics and accessible names | Icon-only toggle button has only a `ToolTip`, no `AutomationProperties.Name` — no accessible name for AT. Web correctly sets `aria-label`. | `Sidebar.xaml:11-37` | Medium |
| `SyncStatusBanner.tsx` | `SyncStatusIndicator.xaml` | `design-tokens.md` §Colours and surfaces; ADR-005 | Both platforms hardcode an identical non-Fluent danger palette (`#C0392B`/`#FDECEA`/`#F5C6CB`); Web's CSS-var "fallback" is the only value ever used since `--error*` is never actually defined in `index.css`. | Web: `SyncStatusBanner.css:4-6`, root cause `index.css`. WPF: `SyncStatusIndicator.xaml:9-10,16` | Medium |
| `SyncStatusBanner.tsx` | `SyncStatusIndicator.xaml` | `accessibility.md` §Charts, icons, and status | Web's banner is `role="alert"` (announced); WPF's has no `AutomationProperties.LiveSetting` — a sync failure is silent to AT on WPF. | Web: `SyncStatusBanner.tsx:29`. WPF: `SyncStatusIndicator.xaml:1-21` | Medium |
| `Sidebar.tsx` icons | `Sidebar.xaml` icons | ADR-004 (implementation debt) | Neither platform's sidebar icons come from the adopted icon library — Web hand-draws SVGs despite already importing `@fluentui/react-icons` elsewhere; WPF hand-draws `Path` geometry instead of `ui:SymbolIcon`. | Web: `Sidebar.tsx:10-68`. WPF: `Sidebar.xaml:28-36,69-88`, `NavTree.cs:5` | Low (debt) |
| — | `Sidebar.xaml` (whole file) | `wpf.md` §Component and theme system (implementation debt) | No `ui:ThemesDictionary`/`ui:ControlsDictionary`, hardcoded `White`/`#E0E0E0`/`#333333`/`#007ACC`. Renders on every screen; also blocks dark/high-contrast theming since WPF has no theme-switching mechanism at all today. | `Sidebar.xaml:9,28-36,61-88,129-133,151-174` | Medium (theme-blocking) |
| `LoadingState.tsx` | (no shell-level equivalent) | `react.md` §Accessibility; §Required state model | Bare `<p>{message}</p>` with no `role="status"`/`aria-live` — used across ~20 files, so a systemic gap, not one-off. | `LoadingState.tsx:1-8` | Medium |

### Group B — CashFlow Monthly: Expense/Income/Bank

| Page/Component (Web) | WPF counterpart | Rule / Doc ref | Finding | Evidence (file:line) | Severity |
|---|---|---|---|---|---|
| `BankOperationsSection.tsx` | `BankSectionView.xaml` | `forms-data-and-visualisations.md` §Grid create/new actions | WPF's Bank CTAs read "Move Money"/"Correct Balance", not "New Transfer"/"New Balance Correction" — breaks the "New X" label pattern even though position/icon/style are otherwise correct. | Web: `BankOperationsSection.tsx:88-93`. WPF: `BankSectionView.xaml:34-37` | Medium |
| `CardsGrid.tsx` | `CardsGridView.xaml` | §Totals | Web bolds the "Combined adjustment figure" value; WPF renders the whole string with no `FontWeight="Bold"` at all — the exact single-bound-string anti-pattern the doc warns against. | Web: `CardsGrid.tsx:178-180`. WPF: `CardsGridView.xaml:136-137` | Medium |
| `IncomingGrid.tsx` | `IncomeTotalsGridView.xaml` | §Totals (the doc's own cited example) | Web bolds all three totals; WPF's `MultiBinding` produces zero bold anywhere, on two tabs (Summary + Income). | Web: `IncomingGrid.tsx:36-45`. WPF: `IncomeTotalsGridView.xaml:25-33` | Medium-High |
| `CardsGrid.tsx` | `CardsGridView.xaml` | `wpf.md` §Data, trees, and charts | Every column is fixed-width, no `Width="*"` — leaves dead blank space, the exact regression already fixed once in `BanksGridView.xaml`. | `CardsGridView.xaml:23,25,43,64,114,123` | Medium |
| `ExpensesSection.tsx` (Fluent icon row actions) | `CreditCardExpensesView.xaml` (plain glyph row actions) | Component-reuse consistency | Same expense entity/commands styled two different ways depending on tab — `ExpenseSectionView` migrated row actions, `CreditCardExpensesView` (same `EditExpenseCommand`/`DeleteExpenseCommand`) did not. | `ExpenseSectionView.xaml:44-45,55-56` vs `CreditCardExpensesView.xaml:43,52` | Medium |
| `IncomeSection.tsx`/`BankOperationsSection.tsx` row actions | `IncomeSectionView.xaml`/`BankSectionView.xaml` row actions | Component-reuse consistency (see Part 2 dedup) | Row Edit/Delete on Income/Bank still legacy plain button+emoji on **both** platforms, while `ExpensesSection`/`ExpenseSectionView` (same page family) already migrated theirs. | Web: `IncomeSection.tsx:26-48`, `BankOperationsSection.tsx:20-43`. WPF: `IncomeSectionView.xaml:43,52`, `BankSectionView.xaml:84-121` | Medium-High |
| `CategoryTotalsGrid.tsx` | `MonthlySummaryView.xaml` | §Totals ("so only the values are bold") | WPF bolds the entire "Total: 45.00" string including the label, not just the value. | Web: `CategoryTotalsGrid.tsx:30-32`. WPF: `MonthlySummaryView.xaml:30-31` | Low |

### Group C — Reserva / Mensais / Controle Mãe

| Page/Component (Web) | WPF counterpart | Rule / Doc ref | Finding | Evidence (file:line) | Severity |
|---|---|---|---|---|---|
| — | All 7 forms (IncomeSplit, Withdrawal, EditReserveMovement, AddBill, EditBill, CreateEntry, EditEntry) | `wpf.md` §Layout (the named anti-pattern) | Every form uses the explicitly-forbidden 2-column label-left `Grid`, one field per row — the exact pattern the doc names by name as forbidden. | e.g. `IncomeSplitFormView.xaml:22-34`, `AddBillFormView.xaml:6-20`, `CreateEntryFormView.xaml:6-20` (7 files total) | High |
| Reserva/Mensais/ControleMae inline forms | — | ADR-002 (4/2/1-column responsive grid) | Web's forms use ad hoc `flex-wrap`, not the mandated grid, on all three pages. | `ReservaPage.css:143-148`, `MensaisPage.css:58-63`, `ControleMaePage.css:107-112` | Medium |
| Reserva/Mensais/ControleMae "New X" buttons | Reserva/Mensais/ControleMae "New X" buttons | §Grid create/new actions (left, primary+icon) | Web right-aligns all three buttons (`flex-end`/`space-between`); WPF is left-aligned but neither has an icon or primary styling. | Web: `ReservaPage.tsx:110-118`, `MensaisPage.tsx:157-160`, `ControleMaePage.tsx:109-121`. WPF: `ReservaView.xaml:27-30`, `MensaisView.xaml:29-33`, `ControleMaeView.xaml:27-30` | Medium |
| Reserva Split form field order | `IncomeSplitFormView.xaml` | §Default field order | Amount (financial value) placed before Description. | `ReservaPage.tsx:128-155`; `IncomeSplitFormView.xaml:38-48` | Medium |
| Reserva Withdrawal / Edit Movement field order | `WithdrawalFormView.xaml`, `EditReserveMovementFormView.xaml` | §Default field order | Date should be first field; it's 3rd, after Bucket and Amount, on both. | `ReservaPage.tsx:199-241,259-300`; both WPF forms | Medium |
| Mensais Add Bill field order | `AddBillFormView.xaml` | §Default field order | Area (classification) placed after Description/Due Day/Value. | `MensaisPage.tsx:184-231`; `AddBillFormView.xaml:24-41` | Medium |
| Controle Mãe Create Entry field order | `CreateEntryFormView.xaml` | §Default field order | Note (metadata) placed before Currency/Value (classification/financial). | `ControleMaePage.tsx:155-201`; `CreateEntryFormView.xaml:24-41` | Medium |
| Controle Mãe totals row | `ControleMaeView.xaml` | §Totals (single-bound-string anti-pattern, named example) | WPF's BRL/GBP totals `MultiBinding` has no `FontWeight="Bold"` at all — the literal case the doc names. | `ControleMaeView.xaml:82-90` | High |
| Reserva group-total row | `ReservaView.xaml` `RowDetailsTemplate` | §Totals | Same anti-pattern: `MultiBinding` into one `TextBlock`, styled *italic*, not bold. | `ReservaView.xaml:90-101` | High |
| Reserva Balances table | `ReservaView.xaml` | §Data grids (label column `Width="*"`) | Both `BucketName`/`Balance` fixed-width — dead blank space in wider panels. | `ReservaView.xaml:38-45` | Medium |
| Reserva split-percentage warning | `ReservaView.xaml` `SplitPercentageWarning` | ADR-005 status palette; `accessibility.md` (no colour-alone status) | WPF renders the warning as plain `Foreground="Red"` — the same red used for genuine errors on the same page, with no distinguishing container. | `ReservaView.xaml:31` vs `ReservaPage.tsx:121`, `.css:200-207` | High |
| — | All 3 views + 7 forms | `wpf.md` §Component and theme system | Hardcoded `#CCCCCC`/`#FAFAFA` borders/backgrounds, plain `Button` throughout — breaks entirely under dark/high-contrast theming. | All 10 files in this group | High |

### Group D — Investment Snapshots & Annual Summary

| Page/Component (Web) | WPF counterpart | Rule / Doc ref | Finding | Evidence (file:line) | Severity |
|---|---|---|---|---|---|
| — | `InvestmentSnapshotsView.xaml`, `EditSnapshotValueFormView.xaml`, `AnnualSummaryView.xaml` | ADR-004; `wpf.md` §Component and theme system | None of the three merge `ui:ThemesDictionary`/use `ui:Button` — only the embedded `MonthYearPicker` is migrated. | `InvestmentSnapshotsView.xaml:16,40`; `EditSnapshotValueFormView.xaml:29,41`; `AnnualSummaryView.xaml:15,26,29` | Medium |
| `InvestmentSnapshotsPage.tsx` edit button | `InvestmentSnapshotsView.xaml` edit button | ADR-004 (icon library) | Raw `✏` emoji/glyph on both platforms instead of an icon from the adopted library. | `InvestmentSnapshotsPage.tsx:35`; `InvestmentSnapshotsView.xaml:40` | Medium |
| — | `InvestmentSnapshotsView.xaml` edit button | `accessibility.md` §Charts, icons, and status | Icon-only button has only `ToolTip`, no `AutomationProperties.Name`; Web correctly sets `aria-label`. | `InvestmentSnapshotsView.xaml:40` vs `InvestmentSnapshotsPage.tsx:32` | High |
| — | `AnnualSummaryView.xaml` Year `TextBox` | `accessibility.md` §Semantics and accessible names | Only an adjacent, unassociated `TextBlock`, no `AutomationProperties.Name`/`LabeledBy`; Web uses a proper `<label htmlFor>`. | `AnnualSummaryView.xaml:25-26` vs `AnnualSummaryPage.tsx:101-107` | Medium |
| Both pages | — | ADR-004 (implementation debt) | No `@fluentui/react-components` import on either page. | Both `.tsx` files | Medium |
| `AnnualSummaryPage.css` (tabs) | — | ADR-005 | Active/hover tab color still hardcodes legacy `#007acc`/`#005fa3`, not the Fluent brand token. | `AnnualSummaryPage.css:40,44,46` | Low |
| Both pages | Both views | `react.md` §Required state model | No distinct Empty state on either platform for an empty result set (repo-wide gap, not unique to this slice). | `InvestmentSnapshotsPage.tsx:110-129`; `AnnualSummaryPage.tsx:128-304` | Low |

### Group E — Investment workspace shell (tree/detail)

| Page/Component (Web) | WPF counterpart | Rule / Doc ref | Finding | Evidence (file:line) | Severity |
|---|---|---|---|---|---|
| — | `MoveAssetDialog.xaml` (`Window`, `ShowDialog()`) | ADR-003 §Decision; `wpf.md` §Dialogs | Move Asset is contextual editing of one selected node (ADR-003's drawer bucket), not a single-step confirmation (radio groups + async combo + conditional text entry + follow-up step) — should not be a blocking `Window`. See prose note in the group's own report for the "New X" rule vs. ADR-003 reasoning. | `MoveAssetDialog.xaml:1-81`; `MainNavigationViewModelBase.cs:108-118,261-264,366-367` | High |
| `MoveAssetDialog.tsx` | — | ADR-003; ADR-004 | Web independently has the identical problem: a hand-rolled fixed backdrop + centered box, not Fluent `Dialog`, not a drawer, not inline. Confirms this is a real design gap on both platforms, not a WPF-only miss. | `MoveAssetDialog.tsx:205-213`; `.css:1-19` | High |
| `MoveAssetDialog.tsx` | — | `accessibility.md` §Keyboard and focus | No Escape handler, no focus trap, no initial-focus management, no focus restoration on close. | `MoveAssetDialog.tsx:206-213` | Medium |
| `InvestmentTree.tsx` | `NavigationView.xaml` `TreeView` | §Tree views (keyboard nav, accessible expanded/collapsed state) | Hand-built `<ul>/<li>` with no `role="tree"`/`treeitem`, no `aria-expanded`, no arrow-key roving. WPF's native `TreeView` provides all of this for free — inverted from the usual "WPF behind Web" pattern. | `InvestmentTree.tsx:176-230,317-412` | High |
| `InvestmentTree.tsx`/`DetailPanel.tsx` `●` dot | `NavigationView.xaml` `●` dot | `accessibility.md` §Visual accessibility (no colour-alone status) | Same shape for Long/Flat/Short, only color differs, no text/title/aria-label in the tree row on either platform. | `InvestmentTree.tsx:138`; `NavigationView.xaml:82-87` | Medium |
| `SplitPanel.tsx` | `NavigationView.xaml` `GridSplitter` | `accessibility.md` §Keyboard and focus | Resize handle is a bare `<div onMouseDown>`, no `tabIndex`/`role="separator"`/keyboard handler — not keyboard-operable at all. WPF's native `GridSplitter` is. | `SplitPanel.tsx:56-60`; `.css:17-23` | High |
| `DetailPanel.tsx` tab strip | `NavigationView.xaml` `TabControl` | `accessibility.md` §Keyboard and focus (composite-control pattern) | Four plain `<button>`s, no `role="tablist"/tab/tabpanel"`, no `aria-selected`, no arrow-key nav. WPF's native `TabControl` provides this for free. | `DetailPanel.tsx:158-169`; `.css:94-119` | Medium |

### Group F — Investment Transactions/Credits/PriceHistory

| Page/Component (Web) | WPF counterpart | Rule / Doc ref | Finding | Evidence (file:line) | Severity |
|---|---|---|---|---|---|
| Tabs' "New" button | `ui:Button` counterparts (compliant) | §Grid create/new actions (named example) | Web's New button is a plain hand-styled `<button>` (hardcoded `#007acc`, no icon), unlike the reference `ExpensesSection`/`IncomeSection`. WPF's `ui:Button` is compliant. | `TransactionsTab.tsx:343-345`, `CreditsTab.tsx:366-368`, `PriceHistoryTab.tsx:250-252` vs `ExpensesSection.tsx:65-67` | High |
| Same three "New" buttons | Same three `ui:Button` | §Grid create/new actions ("concise label") | Button text is bare "New" on both platforms, not "New Transaction"/"New Credit"/"New Price" — a shared gap, not platform-vs-platform drift. | `TransactionsTab.tsx:344`, `CreditsTab.tsx:367`, `PriceHistoryTab.tsx:251`; matching WPF views | Medium |
| — | All three grids' row actions | `wpf.md` §Component and theme system | Row-level Update/Delete are plain `Button`+Segoe MDL2 glyphs, not `ui:Button`/`ui:SymbolIcon`, despite every other control on the same view being migrated this session. | `TransactionsView.xaml:154-166`, `CreditsView.xaml:188-200`, `PriceHistoryView.xaml:115-129` | Medium |
| `PriceHistoryTab.tsx` manual/automatic dots | `PriceHistoryChartBuilder.cs` (compliant) | §Charts and graphs (legend, non-colour distinction) | Web distinguishes manual/automatic only by fill color, no legend, no shape difference. WPF is the compliant side here — separate series with titles and different marker shapes. | `PriceHistoryTab.tsx:164-181` vs `PriceHistoryChartBuilder.cs:29-49` | Medium |
| — | `TransactionDialog.xaml`, `CreditDialog.xaml`, `PriceDialog.xaml` | `wpf.md` §Component and theme system | Legitimately exempt from inline-form conversion (verified: only used for Delete), but entirely unmigrated — plain `Button`, no WPF-UI merge, hardcoded `Foreground="Red"`. | `TransactionDialog.xaml:85-86`, `CreditDialog.xaml:63-64`, `PriceDialog.xaml:52-53` | Low |

### Group G — Investment analytics + Dividend/CurrentValues

| Page/Component (Web) | WPF counterpart | Rule / Doc ref | Finding | Evidence (file:line) | Severity |
|---|---|---|---|---|---|
| — | `DividendCheckView.xaml`, `AssetPriceView.xaml` | ADR-005 (exact hex); `wpf.md` §Component and theme system | Both hand-roll an identical fake-primary-button `ControlTemplate` hardcoding `#007ACC`/`#005A9E`/`#004578` — none match ADR-005's pinned `#0F6CBD`/`#115EA3`/`#0C3B5E` at all (~15-20% off in hue/lightness at every state). The only two Investment views still on this pre-migration pattern; 15 other views already use the correct `AccentButtonBackground*` keys. | `DividendCheckView.xaml:43-71`; `AssetPriceView.xaml:19-51` | High |
| `CurrentValuesPage.tsx` "Check Prices" button | (same button, above row) | ADR-005; `design-tokens.md` | Web's own button is also still hardcoded `#007acc`, not migrated to Fluent `<Button appearance="primary">`. | `CurrentValuesPage.css:21-30` | Medium |
| Summary tabs (conceptually) | `PortfolioSummaryView.xaml` (4 DataTemplates) | ADR-005 §Consequences (named example) | Every stat hardcodes `Foreground="Green"/"Red"/"Blue"` directly, bypassing the app's own `SignedValueToBrushConverter` used two lines below in the same template — the exact issue ADR-005's own audit flags, still unresolved. | `PortfolioSummaryView.xaml:60,66,74` (and 3 more template locations) | Medium |
| `PortfolioSummaryTab.tsx` footer | `PortfolioSummaryView.xaml` footer | §Totals ("bold the value... not just one side") | WPF bolds the **labels**, leaves values normal weight — the reverse of the documented rule. Web itself only reaches `font-weight:500` (not true bold either — see prose note). | `PortfolioSummaryView.xaml:591-616,875-901` | Medium |
| `DividendCheckPage.tsx` ticker field | `DividendCheckView.xaml` | §Field rules (visible label required) | Web's `TickerCombobox` field has only `aria-label`, no visible label text; WPF has the correct visible `"Select Ticker"` label — WPF is the compliant side here. | `DividendCheckPage.tsx:92-96`, `TickerCombobox.tsx:99-108` vs `DividendCheckView.xaml:19-20` | Medium |
| `CurrentValuesPage.tsx` progress text | `AssetPriceView.xaml` progress text | `accessibility.md` §Semantics and accessible names | Neither platform wires a live-region for per-item progress announcements. | `CurrentValuesPage.tsx:154-156`; `AssetPriceView.xaml:57-58` | Low |
| `BrokerBreakdownCharts.tsx` pies | `BrokerSummaryTemplate` OxyPlot pies | `accessibility.md` §Charts, icons, and status | No textual/tabular accessible-summary fallback for values only exposed via hover tooltip. | `BrokerBreakdownCharts.tsx:69-93` | Medium |

---

## Part 2 — Elements with no standard defined (deduplicated)

| Pattern | Occurrences | Why uncovered | Suggested next step |
|---|---|---|---|
| **Hand-rolled tab-strip implementations** — no shared component, no Fluent `TabList`/WPF-UI tab styling | Web: `DetailPanel.tsx` (E), `MonthlyPage.tsx` (B), `AnnualSummaryPage.tsx` (D). WPF: `NavigationView.xaml` `TabControl` (E), `MonthlyView.xaml` `TabControl` (B), `AnnualSummaryView.xaml` `TabControl` (D) | No tab-strip/tablist component or convention documented anywhere in `docs/ui/*.md` | Add a "Tabs" section to `forms-data-and-visualisations.md`: reference visual treatment (Web: Fluent `TabList`/`Tab`; WPF: WPF-UI-themed `TabControl`) and require the ARIA/Automation composite-control keyboard pattern explicitly (arrow-key nav between tabs, `role="tablist"/tab/tabpanel"`/`aria-selected` on Web) |
| **Row-level Edit/Delete action icon convention**, distinct from the documented "New X" create-button rule | Web: `TransactionsTab`/`CreditsTab`/`PriceHistoryTab` (F, raw `✏` glyph + inline SVG), `IncomeSection`/`BankOperationsSection` (B, emoji/SVG), `InvestmentSnapshotsPage` (D, `✏`). WPF: `TransactionsView`/`CreditsView`/`PriceHistoryView` (F, Segoe MDL2 glyphs), `IncomeSectionView`/`BankSectionView`/`CreditCardExpensesView` (B, emoji), `InvestmentSnapshotsView` (D, `✏`) | `forms-data-and-visualisations.md` §Grid create/new actions only covers the "New X" *create* button, not existing-row action icons | Add a "Grid row actions" sub-rule to §Data grids: icon source (`@fluentui/react-icons` `EditRegular`/`DeleteRegular` on Web, `ui:SymbolIcon` on WPF), appearance (subtle/ghost), and accessible-name requirement — then reconcile every listed view to `ExpenseSectionView`'s already-correct pattern |
| **`●` status-dot glyph** for position type (Long/Flat/Short), color-only with no text alternative | Web: `InvestmentTree.tsx`, `DetailPanel.tsx` (E). WPF: `NavigationView.xaml` (E) | No icon/badge/status-indicator convention exists in any doc except the unrelated "+" grid-create-icon rule | Document a status-indicator convention (shape/color/accessible-name mapping) in `forms-data-and-visualisations.md` or a new status/icons section; require it never be color-only (closes the related Category 1 accessibility gap) |
| **Filter/chart-mode/group "chip" toggle pattern** — transparent button + underlined text, hardcoded `#007ACC` | WPF: `TransactionsView`/`CreditsView`/`PriceHistoryView` (F). Web has an equivalent but distinct hand-rolled pattern in the same files. | No segmented-control/toggle convention documented on either platform | Document as the approved lightweight filter/toggle pattern, or replace with Fluent `Tab`/WPF-UI `ToggleButton` primitives |
| **Manual vs. automatic price-source badge/marker color** | Web: `PriceHistoryTab.tsx` (`#e65100`/`#4682b4`). WPF: `PriceHistoryChartBuilder.cs` (`OrangeRed`/`SteelBlue` — doesn't even match Web's hex) | No "manual"/"automatic" color convention in any doc | Name one canonical color pair in `forms-data-and-visualisations.md`; align both platforms' text-badge and chart-marker colors to it (WPF's grid Source column currently has *no* color distinction at all, only the chart does) |
| **Drag-to-resize split-panel** as a persistent workspace-shell layout (distinct from the grid-and-chart-page `GridSplitter` use already documented) | Web: `SplitPanel.tsx` (E). WPF: `NavigationView.xaml` `GridSplitter` (E) | §Grid-and-chart pages only covers grid+chart tab layouts, not a tree+detail workspace shell | Extend §Grid-and-chart pages or add a "resizable workspace shell" section: default/min widths, width persistence, minimum content widths, narrow-viewport fallback — and close the keyboard-operability gap found in Part 1 |
| **Custom combobox** (`TickerCombobox`) vs. Fluent's native `Combobox` | Web: `TickerCombobox.tsx`, used only in `DividendCheckPage` (G) | No doc says when a hand-rolled combobox is acceptable vs. when Fluent's native component should be used | Decide during Web's ADR-004 migration pass whether `TickerCombobox`'s grouped/watchlist behavior fits Fluent's `Combobox`+`OptionGroup`, or document why a custom control stays |
| **Async batch-fetch progress indication** (determinate bar + "Fetching N of M: item" text) | Web: `CurrentValuesPage.tsx` (native `<progress>`). WPF: `AssetPriceView.xaml` (`ProgressBar`) — the two already agree closely (near-verbatim message format) | Only generic "Show progress on the initiating action" exists; no shape spec for a *multi-item batch* progress indicator | Once confirmed as the intended shared pattern, write it up as the reference "batch async progress" convention (determinate bar, `N of M: item` text, disable the triggering action) |
| **Read-only summary/stat panel** (label-above-value grid, not a form, not a grid-total row) | Web: `PortfolioSummaryTab`/`AssetSummaryTab`/`AggregatedSummaryTab` (G). WPF: `PortfolioSummaryView.xaml`'s 4 DataTemplates (G) | §Forms assumes an editable field; §Totals assumes a grid total row — neither fits a page-level read-only stat block | Add a "Read-only summary panel" subsection defining label/value typography (mapping to `text.caption`/`text.numeric` tokens), spacing, and emphasis rule |
| **Year-only selector** (distinct from the documented month+year `MonthYearPicker` rule) | Web: `AnnualSummaryPage.tsx` (native `<input type="number">`). WPF: `AnnualSummaryView.xaml` (plain `TextBox`) (D) | §Month/year selection only covers combined month+year | Extend §Month/year selection with a "Year-only selection" sub-rule (e.g. bounded `NumberBox`/spinner or lightweight year-picker popup) and name a reference implementation, the way `MonthYearPicker` is for month+year |
| **Spacer/emphasized conditional row styling** driven by a boolean flag on the row model | Web: `AnnualSummaryPage.tsx` (`HISTORIC_SUMMARY_AVERAGE_SPACER_AFTER`/`_EMPHASIZED`). WPF: `AnnualSummaryView.xaml` (`DataTrigger` on `IsSpacer`/`IsEmphasized`) (D) | §Totals only covers bolding a genuine total row, not blank divider/spacer rows or a separate emphasis flag | Document the spacer-row and emphasized-row pattern (which categories get one, exact visual treatment) since it's already deliberately used in two tabs and likely to recur |
| **Portal/flyout submenu focus management**, architecturally distinct from the dialog focus rule already documented | Web: `SidebarFlyout.tsx` (A) | `accessibility.md`'s "Dialogs keep focus while open" rule is scoped to dialogs, not portal-rendered contextual submenus | Extend `accessibility.md` §Keyboard and focus with an explicit rule for portal/flyout submenus (closes the related Category 1 High-severity finding) |
| **Breadcrumb semantic structure** (landmark role, clickable vs. static segments) | Web: `Breadcrumb.tsx` (A). WPF: `MainWindow.xaml` breadcrumb `TextBlock` (A) | No rule anywhere specifies breadcrumb markup/landmark role/interactivity | Add a breadcrumb convention to `react.md`/`wpf.md` (static vs. navigable, landmark/`AutomationProperties` requirement) since this is shared chrome on every page |

*Lower-priority items noted by individual groups but not carried into the deduplicated table above (single-occurrence, low-urgency): Sidebar's hover+250ms-close-delay flyout interaction pattern (A — both platforms already agree, worth naming once the keyboard-focus fix above lands); `CardsGrid`'s "Mark Paid" inline action + per-row `<select>` (B); "Filter by Bank" control styling (B); dual-currency (BRL/GBP) column display (C); `DataGrid.RowDetailsTemplate` as the group-subtotal mechanism (C); recurring-bill status as plain text with no badge (C, both platforms already agree).*

---

## Part 3 — WPF drift from Web

### Group A

| Web component | WPF counterpart | Web actual behavior | WPF actual behavior | Web on-standard? | Finding | Severity |
|---|---|---|---|---|---|---|
| `Sidebar.tsx` collapse/expand | `Sidebar.xaml` | Width animates 240px↔56px, collapsed state persisted to `localStorage` | Width driven by converter, no animation observed; persisted via `Properties.Settings` | Partial | Functional parity good; WPF's transition isn't animated. Category order/terminology/breadcrumb format all match exactly — confirmed genuine parity elsewhere. | Low |
| `SyncStatusBanner.tsx` | `SyncStatusIndicator.xaml` | Above breadcrumb; specific wording with last-error/last-success date | Same stacking order; near-identical wording (WPF correctly copied Web's authoritative text) | Y | Full parity in order/wording/trigger; only gap is the AT-announcement issue already listed in Part 1. | Low |
| `Sidebar.tsx` icons | `Sidebar.xaml` icons | Hand-drawn SVGs (2 category icons + toggle) | Hand-drawn `Path` data, visually different geometry from Web's | N (Web itself unmigrated too) | Neither side is on the adopted icon library, so WPF isn't meaningfully "behind" here — parity of un-migration, not drift. | Low |
| `ErrorState.tsx`/`LoadingState.tsx` | (no shared equivalent) | Reusable components used across ~20 files | No comparable shared component; `NavigationView.xaml` builds its own inline loading overlay | Partial | Structural component-reuse gap: WPF has no shell-level reusable loading/error component. | Medium |

### Group B

| Web component | WPF counterpart | Web actual behavior | WPF actual behavior | Web on-standard? | Finding | Severity |
|---|---|---|---|---|---|---|
| `BalanceAdjustmentForm.tsx` success state | `BalanceAdjustmentFormView.xaml` success state | "Adjustment of {sign}£{value} recorded" | "Adjustment of {value} recorded" — no `£` | Partial (Web is itself the outlier vs. its own app-wide symbol-free convention) | Genuinely different confirmation text between platforms for the same action. | Low-Medium |
| `ExpenseForm.tsx` settled message | `ExpenseFormView.xaml` settled message | "Paid by **{bank}** via card {card}..." | "Paid via {card}..." — omits the bank name | Y | Real information gap: WPF users can't see which bank paid a settled card statement without leaving the form. | Medium |
| `BankOperationsSection.tsx` buttons | `BankSectionView.xaml` buttons | "New Transfer"/"New Balance Correction" | "Move Money"/"Correct Balance" | Y | Same action, different terminology (also Part 1). | Medium |
| `ExpensesSection`/`IncomeSection` row actions | `ExpenseSectionView`/`IncomeSectionView` row actions | Expense = Fluent icon; Income = plain emoji (Web itself inconsistent) | Expense = `ui:Button`+`SymbolIcon`; Income = plain button+emoji | Partial | WPF's per-section split mirrors Web's own inconsistency exactly — parity with an existing Web inconsistency, not new WPF-introduced drift. | Low (as drift) |
| `CardsGrid.tsx` | `CardsGridView.xaml` | Table fills width naturally | Fixed-width columns, dead blank space possible | Y | WPF-only layout defect (also Part 1). | Medium |

### Group C

| Web component | WPF counterpart | Web actual behavior | WPF actual behavior | Web on-standard? | Finding | Severity |
|---|---|---|---|---|---|---|
| Reserva/Mensais/ControleMae toolbars | Same views | Right-aligned "New X" buttons | Left-aligned | N (Web itself violates the left-align rule) | Both platforms wrong, in *opposite* directions. | Medium |
| Reserva split-% warning | `ReservaView.xaml` | Distinct amber warning box, `role="alert"` | Plain red text, same as genuine errors | Y | WPF conflates non-blocking warning with blocking error (also Part 1). | High |
| ControleMae totals row | `ControleMaeView.xaml` | Both BRL/GBP values bold | No bold on values at all, only the label | Y | Totals read as ordinary text on WPF — easy to miss (also Part 1). | High |
| Reserva group-total row | `ReservaView.xaml` `RowDetailsTemplate` | Bold, in normal reading order | Italic, not bold, in an auxiliary details panel | Y | Same "totals not visually distinct" problem in the movements grid. | High |
| Reserva Balances table | `ReservaView.xaml` | Bucket column fills available width | Both columns fixed-width, dead space | Y | WPF-only regression vs. the documented/Web pattern. | Medium |
| Reserva Withdrawal submit label | `WithdrawalFormView.xaml` | "Record Withdrawal"/"Saving..." | "Withdraw"/"Withdrawing..." | — | Terminology drift on primary action + in-flight label. | Low |
| Reserva split-result dismiss | `IncomeSplitFormView.xaml` | "Dismiss" | "Close" | — | Same action, different label. | Low |

*Positive finding: all other submit/cancel labels, field values, and delete/reset confirmation wording verified identical between platforms across all three pages — genuine parity, not a finding.*

### Group D

| Web component | WPF counterpart | Web actual behavior | WPF actual behavior | Web on-standard? | Finding | Severity |
|---|---|---|---|---|---|---|
| `AnnualSummaryPage.tsx` year field | `AnnualSummaryView.xaml` | Native number input, spinner + browser validation UI | Plain `TextBox`, invalid input fails binding silently — zero feedback | N/A (no rule — Part 2) | Both eagerly refetch per keystroke; WPF additionally gives no feedback on invalid input where Web has native affordances. | Medium |
| `AnnualSummaryPage.tsx` Category Totals rows | `AnnualSummaryView.xaml` grid | Average/AnnualTotal bold on *every* row, not just true totals | Bold only on the 2 genuine total rows (`IsEmphasized`) | Partial | WPF's Category Totals tab reads visually "flatter" than Web's for the same data. | Medium |
| `AnnualSummaryPage.tsx` tabs | `AnnualSummaryView.xaml` `TabControl` | 3 tabs, specific order/labels/row grouping | Same 3 tabs, same order/labels/grouping (verified against the ViewModel build methods) | Partial (tab mechanism itself undocumented) | No drift — genuine parity despite two independently-built tab mechanisms. | Low (informational) |
| `InvestmentSnapshotsPage.tsx` edit-value field | `EditSnapshotValueFormView.xaml` | Native number input with spinner | Free-text `TextBox` with custom decimal-keystroke filtering | Y | Same validated outcome via different mechanics — acceptable platform-native adaptation, not a real gap. | Low |

### Group E

| Web component | WPF counterpart | Web actual behavior | WPF actual behavior | Web on-standard? | Finding | Severity |
|---|---|---|---|---|---|---|
| `InvestmentTree.tsx` | `NavigationView.xaml` `TreeView` | Custom `<ul>/<li>` tree, manual expand state, HTML5 drag reorder | Native `TreeView`, two-way bound `IsExpanded`/`IsSelected`, custom drag-drop behavior | Partial (missing keyboard nav/ARIA — Part 1) | Same hierarchy/grouping/drag concept, but WPF's control is genuinely more accessible — inverted from the usual pattern. | High |
| `DetailPanel.tsx` tabs | `NavigationView.xaml` `TabControl` | Order/labels: Summary, Transactions, Credits, Price History | Identical order/labels | Partial (missing ARIA — Part 1) | Content parity is exact; structural/semantic parity favors WPF (native control more complete). | Medium |
| `MoveAssetDialog.tsx` | `MoveAssetDialog.xaml` | Same field shape, plus a follow-up "delete emptied portfolio?" step | Same field shape, **no** follow-up step — `MoveAssetDialogViewModel` has no state for it | N (both Cat 1 — should not be a blocking modal) | Real behavioral gap, not just presentational: WPF silently leaves an empty portfolio behind after a move. | High |
| `MoveAssetDialog` trigger surface | `NavigationView.xaml` toolbar + drag-drop | Reachable via "Move..." button + drag-and-drop onto tree node | Reachable via "Move Asset..." button + drag-and-drop | Y | Genuine parity on both entry points — not a finding, noted for completeness. | — |

### Group F

| Web component | WPF counterpart | Web actual behavior | WPF actual behavior | Web on-standard? | Finding | Severity |
|---|---|---|---|---|---|---|
| Form titles + row-action labels (all 3 tabs) | `TransactionDialogViewModel`/`CreditDialogViewModel`/`PriceDialogViewModel` | Verb pair "New"/"Edit" | Verb pair "Add"/"Update" (never adopted Web's wording when converted to inline forms this session) | Y | Systematic terminology mismatch across all three areas. | Medium |
| Delete flow (all 3 tabs) | `TransactionDialog`/`CreditDialog`/`PriceDialog` | Native `window.confirm('Delete this X?')` — generic one-liner | Full styled `Window` reopening the form read-only, showing every field's value | Y (consistent with a documented CashFlow precedent) | Confirmation richness genuinely differs — not a rule violation (both are legitimate confirmations per ADR-003) but a noticeable UX texture difference. | Low |
| `PriceHistoryTab.tsx` manual-price color | `PriceHistoryChartBuilder.cs` | `#e65100` dot + matching grid text color | `OrangeRed` (`#FF4500`) dot only — grid Source column has **no** color distinction at all | Y | Two stacked drifts: chart hex doesn't match Web's, and WPF's grid loses the manual/automatic distinction entirely. | Medium |
| Filter/chart-mode/group options (all 3 tabs) | `PeriodFilterHelper.cs`/`AssetDetailsViewModel.cs` | This month/Last 3/6/12/YTD/All time, Bar/Line, Stacked/Grouped | Identical enum values, labels, and default selection | Y | No drift — confirmed exact match. | — (informational) |
| Grid-and-chart layout (all 3 tabs) | Matching WPF views | Transactions stacked; Credits/PriceHistory side-by-side | Same layout choices, same rationale cited in XAML comments | Y | No drift — rule correctly implemented identically on both platforms. | — (informational) |

### Group G

| Web component | WPF counterpart | Web actual behavior | WPF actual behavior | Web on-standard? | Finding | Severity |
|---|---|---|---|---|---|---|
| `PortfolioSummaryTab`/`AssetSummaryTab` | `PortfolioSummaryView.xaml` `AssetSummaryTemplate` | Field set/order: Quantity/Avg Price → ISIN/Country → Local Type/Class → Bought/Sold/Credits → Realized-or-Current section | Same field set and grouping order, same section-gating logic | Partial (hardcoded-color findings in Part 1) | One of the better-aligned surfaces in Group G — only styling-layer drift. | Low |
| `AggregatedSummaryTab`/`BrokerBreakdownCharts` | `PortfolioSummaryView.xaml` `BrokerSummaryTemplate` | Stat row + overall pie + per-portfolio pie grid, 3 async states | Same stat row + two-tier pie layout + same 3 async states | Partial (accessible-summary gap, hardcoded colors) | Well-aligned structurally; exact chart color-hue parity not verifiable from this slice's file list (would need `BrokerBreakdownChartBuilder.cs`). | Low |
| `DividendCheckPage.tsx` | `DividendCheckView.xaml`/`DividendCheckViewModel.cs` | `async` check, button reads "Checking..."/disabled while in flight | Fully **synchronous** `Check()`, no busy flag, button never disables, no in-flight indicator | Y | Real, user-visible parity gap: WPF entirely lacks a documented mandatory state (saving/duplicate-prevention) that Web has. | High |
| `CurrentValuesPage.tsx` | `AssetPriceView.xaml`/`AssetPriceFetchViewModel.cs` | Disables button while fetching, determinate progress + "Fetching N of M: TICKER..." | Same disable-while-fetching behavior, near-identical progress message format | Partial (hardcoded-color finding in Part 1) | Strongest behavioral parity in Group G — drift limited to visual styling, not information/task-flow. | Low |
| `BrokerBreakdownCharts.tsx` Series color applicability | `BrokerBreakdownChartBuilder.cs` | 8-color categorical palette for pie slices (correctly not the single-series blue rule, which is scoped to bar/line) | Not reviewed beyond confirming the OxyPlot model exists | Y (rule correctly N/A) | Confirms the Series-color/month-axis chart rules don't apply here — flagged so this isn't later miscited as a violation. | N/A (informational) |

---

## Appendix

### File inventory

Full Web (10 pages, 29 components) and WPF (10 nav items, ~40 views) inventories were gathered by two dedicated research passes prior to this audit and used as the starting map for all 7 groups; see `docs/ui/current-state-audit.md` for the narrative history of what was migrated in which session round.

### Open questions needing human confirmation

- **`Financial.App/Components/Totals.xaml`**: confirmed dead code — no view in the project instantiates it (grepped for `<...:Totals`/`Components.Totals` across every `.xaml` file; only the file's own `x:Class` declaration matches). Left in the codebase; candidate for deletion in a future cleanup, out of scope for this audit.
- **`NavigationView.xaml` chrome/tree split**: Group A audited the outer toolbar ("Move Asset…"/"Delete Portfolio" buttons, asset-class filter, three-column `Grid`/`GridSplitter` shell); Group E audited the `TreeView` and 4-tab `TabControl` content. No overlap or gap found between the two groups' coverage.
- **Group C form ownership**: confirmed by direct read rather than assumption — Reserva hosts `IncomeSplitFormView`/`WithdrawalFormView`/`EditReserveMovementFormView`; Mensais hosts `AddBillFormView`/`EditBillFormView`/`BillTableView` (×2, Brazil/UK); Controle Mãe hosts `CreateEntryFormView`/`EditEntryFormView`. All match the plan's unverified seed list exactly.
- **`MoveAssetDialog`'s correct target surface** (drawer vs. a dialog exception carved into ADR-003) is a product decision, not something this audit can resolve on its own — see Group E's Part 1 entry and its longer reasoning note for the "New X" rule vs. ADR-003 distinction.
- **Whether `PortfolioSummaryTab`'s footer `font-weight:500` counts as "bold"** per the Totals rule's intent (Group G) — Web itself may not be fully on-standard here either; worth a product call before fixing WPF to match Web exactly, versus fixing both to true bold.
