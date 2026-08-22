# Current-State UI Audit

Read-only inventory of what is actually implemented in `Financial.Web` and
`Financial.App` today, as of 2026-08-22. This is not a specification — it
exists so the standards in this directory are grounded in reality (see
`ADR-004` and `ADR-005`) and so the eventual page-by-page refactor has a
concrete starting punch list instead of having to re-derive it. Update this
file if the audited state materially changes before the refactor begins;
otherwise treat it as a snapshot, not a living document.

## Financial.Web (React)

### Component library

None. `package.json` dependencies are `react`, `react-dom`,
`react-router-dom`, `recharts` only — no `@fluentui/*`, MUI, Ant Design, or
other component library. No `FluentProvider` or theme provider exists. Every
interactive element (buttons, tables, selects, inputs, dialog overlays) is
hand-built HTML + CSS + React, one `.css` file per component.

### Declared tokens vs. what's actually painted

`src/index.css:1-51` is the only design-token file (CSS custom properties on
`:root`, with a `@media (prefers-color-scheme: dark)` override). Its real
tokens: `--text`, `--text-h`, `--bg`, `--border`, `--accent`/`--accent-bg`,
`--code-bg`, `--shadow`.

Most components bypass this token set: they reference variables like
`--text-muted`, `--bg-hover`, `--bg-subtle`, `--surface`, `--danger`,
`--error`, `--warning-text/bg/border`, `--drop-target` via
`var(--x, fallback)` — but `--x` is **never declared anywhere**, so these
always resolve to their literal fallback hex. Effectively there is one real
token file that most components silently bypass in favor of ad hoc hex
literals or fallback-only `var()` calls.

### Colors actually in use (grepped across `src/**/*.css`)

- **Brand/accent as declared:** `#aa3bff` light / `#c084fc` dark
  (`index.css`) — barely used elsewhere.
- **Brand/accent as actually painted:** `#007acc` (hover `#005fa3`), hardcoded
  per file with no shared variable. Identical value repeated in:
  `components/BankOperationsSection.css:27`, `ExpensesSection.css:26`,
  `IncomeSection.css:26`, `CreditsTab.css:31,64,113,176`,
  `PriceHistoryTab.css:31,80,142`, `DetailPanel.css:45,60-61,112,116,118`,
  `SplitPanel.css:26`, `InvestmentTree.css:81,107`,
  `pages/AnnualSummaryPage.css:40,44,46`, `pages/ControleMaePage.css:29,130`,
  `pages/CurrentValuesPage.css:22`, `pages/MensaisPage.css:34,81`,
  `pages/MonthlyPage.css:42,46,146`, `pages/ReservaPage.css:27,166`. Hover
  shade `#005fa3` in `ExpensesSection.css:33`, `IncomeSection.css:33`,
  `CreditsTab.css:120`, `MonthlyPage.css:153`, `ReservaPage.css:173`.
- **Success/positive:** `#2e7d32` (`AggregatedSummaryTab.css:34`,
  `AssetSummaryTab.css:38`, `PortfolioSummaryTab.css:40`,
  `DetailPanel.css:77`, `InvestmentTree.css:90`); a second green
  `#1b8b4d` only in `pages/DividendCheckPage.css:47`.
- **Danger/negative:** `#c62828` (`AggregatedSummaryTab.css:38`,
  `AssetSummaryTab.css:42`, `PortfolioSummaryTab.css:44`,
  `DetailPanel.css:85`, `InvestmentTree.css:98`, `CreditsTab.css:203,243`,
  `PriceHistoryTab.css:169,209`); a second red via
  `var(--danger/--error, #c0392b)` in `DetailPanel.css:66`,
  `MoveAssetDialog.css:54`, `ControleMaePage.css:160`,
  `InvestmentSnapshotsPage.css:91`, `MensaisPage.css:145`,
  `MonthlyPage.css:176,184`, `ReservaPage.css:196`; a third `#b71c1c`
  (`AssetSummaryTab.css:50`) and a fourth `#b42318`
  (`DividendCheckPage.css:51`).
- **Warning:** `#e65100` (`AssetSummaryTab.css:54`,
  `PortfolioSummaryTab.css:53`, `PriceHistoryTab.css:199`); a second triad
  `var(--warning-text, #8a6d00)` / `var(--warning-bg, #fff8e1)` /
  `var(--warning-border, #f0d878)` (`MonthlyPage.css:190-195`,
  `ReservaPage.css:200-205`).
- **Information:** `#1565c0` (`AggregatedSummaryTab.css:42`,
  `AssetSummaryTab.css:46`, `CreditsTab.css:229`, `DividendCheckPage.css:39`)
  and `#0277bd` (`CreditsTab.css:233`).
- **Neutrals/surfaces:** table alt-row `#f5f5f5`
  (`styles/data-table.css:29`); hover `var(--bg-hover, #f0f0f0)`; subtle
  panel `var(--bg-subtle, #f9f9f9)`; border `#e5e4e7` vs `#e0e0e0` used
  interchangeably; dialog backdrop `rgba(0,0,0,0.35)`
  (`MoveAssetDialog.css:4`); `BrokerBreakdownCharts.css` uses a third,
  unrelated neutral pair (`#e1e0d9` border, `#52514e` text).

**Takeaway:** the declared purple accent is not what users actually see;
status colors have 2-4 competing hex values each; resolved by `ADR-005`.

### Spacing

No spacing scale exists as CSS variables. Raw pixel literals recur across
files consistent with (but never encoded as) a 4px rhythm: `2/4/6/8/10/12/
16/20/24/32px`. Table cell padding `8px 10px`
(`styles/data-table.css:12,22`) is the single most reused spacing pair,
shared by every grid that imports this stylesheet. This is already
consistent with the spacing scale in `docs/ui/design-tokens.md` — no change
needed there, only enforcement once real spacing tokens exist.

### Typography

Body stack (`index.css:14,18`): `system-ui, 'Segoe UI', Roboto, sans-serif`,
base `18px/145%`, `letter-spacing: 0.18px`, responsive to `16px` under
`max-width: 1024px`. Mono stack: `ui-monospace, Consolas, monospace`. No web
font is loaded (`index.html` has no font `<link>`). Headings: `h1` `56px`
(`36px` ≤1024px), `h2` `24px` (`20px` ≤1024px) (`index.css:74-90`). Component
font sizes are hardcoded per file and inconsistent (`11/12/13/14/16/20px`);
`13px` is the de facto body/table size, `12px` the de facto secondary/meta
size.

### Existing shared/reusable components

`src/components/` (flat, no `ui/`/`common/` subfolder):

- Generic: `ErrorState.tsx`, `LoadingState.tsx`, `Breadcrumb.tsx`,
  `SplitPanel.tsx`, `Sidebar.tsx` + `SidebarFlyout.tsx`,
  `SyncStatusBanner.tsx`.
- Grids: every grid is a plain `<table className="data-table ...">` sharing
  `styles/data-table.css` — `BanksGrid.tsx`, `CardsGrid.tsx`,
  `CategoryTotalsGrid.tsx`, `IncomingGrid.tsx`, plus tables embedded directly
  in `CreditsTab.tsx`, `TransactionsTab.tsx`, `PriceHistoryTab.tsx`,
  `PortfolioSummaryTab.tsx`, `BankOperationsSection.tsx`,
  `ExpensesSection.tsx`, `IncomeSection.tsx`. No shared `<DataGrid>`
  component.
- Tree: `InvestmentTree.tsx` — hand-built recursive `ul`/`li` tree with
  drag/drop, not a library.
- Dialog: `MoveAssetDialog.tsx` — custom `role="dialog" aria-modal="true"`
  overlay, no native `<dialog>`, no dialog library.
- Forms: `ExpenseForm.tsx`, `IncomeForm.tsx`, `TransferForm.tsx`,
  `BalanceAdjustmentForm.tsx` — plain controlled inputs, each with its own
  prop-drilled field contract; no shared `<TextField>`/`<FormRow>`.
- Combobox: `TickerCombobox.tsx` — custom, not native `<select>`.
- Buttons: plain `<button>` everywhere; no shared `<Button>` component or
  variant system — primary/secondary/danger styling is ad hoc, inconsistently
  named per file (e.g. `.expenses-section__save-btn` vs
  `.credits-tab__save`).

### Theme handling

Pure CSS: `color-scheme: light dark` plus the `@media
(prefers-color-scheme: dark)` override in `index.css:33-51`. No manual
toggle, `ThemeContext`, `data-theme` attribute, or persisted preference
exists. Dark-mode coverage is partial: only the ~9 declared variables respond
to dark mode; the majority of colors actually painted (§ above) are hardcoded
hex and do not change in dark mode.

## Financial.App (WPF)

### Component library

None. `Financial.App.csproj` package references:
`DotNetProjects.WpfToolkit.DataVisualization`,
`Microsoft.Extensions.DependencyInjection`, `Microsoft.Extensions.Hosting`,
`OxyPlot.Wpf`, `Serilog.Extensions.Hosting`,
`Serilog.Settings.Configuration`, `Serilog.Sinks.File`. No WPF-UI,
ModernWpfUI, MahApps.Metro, HandyControl, or FluentWPF. All controls are
native `System.Windows.Controls`. The one existing Fluent-adjacent touchpoint
is the Segoe MDL2 Assets glyph icon font, already used in
`Components/NavigationView.xaml:137` and the Investment dialogs/views
(`CreditsView.xaml`, `PriceHistoryView.xaml`, `TransactionsView.xaml`).

### Shared styles

No `Themes/`/`Styles/` folder — everything lives inline in
`App.xaml:7-148` (`Application.Resources`): 11 value converters; an
`AppIcon` vector with hardcoded blues (`#1D6FB7`, `#0F4E87`, `#2A82C6`) and
accent yellow (`#F2C94C`/`#C9971E`) not reused elsewhere; a global implicit
`DataGrid` style (`FontSize=13`, `AlternatingRowBackground=#F5F5F5`,
horizontal grid lines only); `DataGridColumnHeader` style
(`Background=#F0F0F0`, `Foreground=#333333`, bold); selection styling using
`InactiveSelectionHighlightBrushKey` overridden to `#007ACC` — the same blue
as Web; `NumericColumnTextStyle`/`PlainColumnTextStyle` for cell alignment;
`GroupHeaderBarStyle`/`GroupHeaderTitleTextStyle` for DataGrid group headers.
No spacing constants exist — every view hardcodes its own `Margin`/`Padding`.

### Spacing

Sampled from `Views/CashFlow/ExpenseFormView.xaml`,
`Components/Sidebar.xaml`, `Views/Investment/MoveAssetDialog.xaml`: values of
4, 8, 12, 16, 20, 28px recur — the same informal 4px-based rhythm as Web,
never named as constants.

### Grids, trees, dialogs

- **DataGrid:** native `System.Windows.Controls.DataGrid`, styled globally
  per above. Used in `BanksGridView.xaml`, `CardsGridView.xaml`,
  `IncomeTotalsGridView.xaml`, `BillTableView.xaml`, `TransactionsView.xaml`,
  `CreditsView.xaml`, `PriceHistoryView.xaml`. No third-party grid.
- **TreeView:** native `System.Windows.Controls.TreeView` with
  `HierarchicalDataTemplate` in `Components/NavigationView.xaml:50-80`,
  virtualized, with a custom drag/drop attached behavior
  (`Behaviors/TreeViewDragDropBehavior`). Selected background `#007ACC`,
  drop-target `#CCE8FF` (`NavigationView.xaml:62,68`). This is the WPF
  counterpart to Web's hand-rolled `InvestmentTree.tsx` — WPF already gets a
  native control for the equivalent job.
- **Dialogs:** modal child `Window`s, not `MessageBox`, not a dialog-service
  library — `MoveAssetDialog.xaml`, `CreditDialog.xaml`, `PriceDialog.xaml`,
  `TransactionDialog.xaml`. Confirm/cancel buttons `Width="90"` with
  `IsDefault`/`IsCancel` wired for Enter/Esc.

### Theme handling

None. No `Theme`/`DarkMode`/`ThemeMode` switch exists; every color in
`App.xaml` and every view is hardcoded hex or a named brush (`White`,
`Green`, `Red`, `Black`, `LightYellow`). The app always renders in one fixed
light palette regardless of the Windows theme setting. Status colors use
**named brushes**, inconsistent with Web's hex values:
`PositionTypeToColorConverter.cs:15-17` (`Long`→`Brushes.Green`,
`Short`→`Brushes.Red`), `TransactionTypeToColorConverter.cs:13-15`
(`Buy`→`Brushes.Green`, else→`Brushes.Red`),
`SignedValueToBrushConverter.cs:13,18` (`>=0`→`Brushes.Green`,
`<0`→`Brushes.Red`). `PortfolioSummaryView.xaml:336,339,342` also hardcodes
`Foreground="Green"/"Red"/"Blue"` directly, bypassing the converters
entirely.

### Fonts

No explicit `FontFamily` at the app/window level — inherits the OS default
(Segoe UI). `FontFamily="Consolas"` is used extensively for numeric/financial
figures (25+ occurrences in `PortfolioSummaryView.xaml` alone), matching
Web's `--mono: ui-monospace, Consolas, monospace` convention.
`FontFamily="Segoe MDL2 Assets"` is used for icon glyphs. Font sizes are set
ad hoc per control.

## Cross-cutting

### Terminology already in parity

Both front ends already share the same nav taxonomy and domain nouns
verbatim: `src/navigation/navTree.ts:14-36`'s `Investments`/`CashFlow`
categories and child labels (including the untranslated Portuguese screen
names "Reserva", "Mensais", "Controle Mae") match
`Components/Sidebar.xaml`/`NavigationView.xaml` in WPF. Both use identical
broker/portfolio/asset field names and near-identical move-asset copy
(`MoveAssetDialog.tsx:214-262` vs `MoveAssetDialog.xaml:36-62`). Status/sign
semantics agree conceptually (positive→green, negative→red) even though
literal values differ (resolved by `ADR-005`). No "Account" label exists on
either platform — the domain uses "Bank"/"Broker"/"Portfolio"/"Card"
instead; "Category" is used identically on both.

### Accessibility gaps

- **Web:** some real ARIA usage exists (67 `aria-*`/`role=` occurrences
  across `components/`+`pages/`), e.g. `MoveAssetDialog.tsx:209-211`,
  `ErrorState.tsx:8`, `App.css:74` tab `aria-selected`. Coverage is
  concentrated in a handful of components (dialogs, error banners, tabs)
  rather than applied systematically to all forms/grids/trees.
- **WPF:** zero `AutomationProperties.*` usages anywhere in the project
  (confirmed by full-project search). No explicit accessible names exist on
  icon-only buttons (e.g. the sidebar collapse button,
  `Components/Sidebar.xaml:11-37`, has a `ToolTip` but no
  `AutomationProperties.Name`), custom `Path` icons, or the drag/drop tree.

## Implications already resolved by ADR-004 / ADR-005

- Adopting Fluent on either platform is a genuinely new dependency, not a
  formalization of something already there — except for WPF's pre-existing
  Segoe MDL2 Assets usage and the cross-platform `#007acc` accent, both
  called out above.
- The real, currently-painted accent color is `#007acc`, not the declared
  `--accent: #aa3bff` — `ADR-005` resolves this in favor of a Fluent brand
  ramp close to the existing blue.
- Status colors have no single canonical value on either platform —
  `ADR-005` resolves this in favor of Fluent's default status palette.
- Spacing is already informally consistent with the 4px scale in
  `docs/ui/design-tokens.md` — no invention needed, only enforcement.
- Fonts: Consolas for numeric/tabular data is already a genuine
  cross-platform convention — carry it forward rather than replacing it.
- Both platforms use 100% native/custom controls for grids, trees, and
  dialogs — no existing grid/tree/dialog library constrains the `ADR-004`
  library choice for those control types.
- Dark mode exists only on Web today (partial coverage) and not at all on
  WPF — full-coverage dark theme on both platforms is new scope, not a
  migration of existing behavior.
- Accessibility attribution is uneven on Web and entirely absent on WPF
  (`AutomationProperties` count: 0) — WPF accessibility work is close to a
  clean slate.

## CashFlow Monthly Expense pilot (2026-08-22)

First real migration under this standard: `ExpenseForm`/`ExpensesSection`
(Web) and `ExpenseFormView`/`ExpenseSectionView` (WPF), per `ADR-004`
(component libraries) and `ADR-005` (brand color). Recorded here because it's
where the standards above were actually tested against a real page instead
of just written down; the general lessons are folded into
`docs/ui/wpf.md`, `docs/ui/react.md`, and
`docs/ui/forms-data-and-visualisations.md` as rules — this section is the
concrete history behind them.

What shipped clean the first time: Fluent UI React v9 components on Web
(`Field`/`Input`/`Select`/`Checkbox`/`Button`/`MessageBar`), WPF-UI scoped to
just the two pilot views on WPF, the Date→Category→Description→Value field
order from `ADR-002`, and `AutomationProperties.Name` added throughout the
WPF form (previously zero).

What passed build and tests but was visibly wrong when the app was actually
run (all fixed same day):

- **WPF `DataGrid` auto-generated a raw column per DTO property** alongside
  the intended ones. Cause: scoping WPF-UI's `ControlsDictionary` locally
  gives the grid a *different* implicit `DataGrid` style than `App.xaml`'s,
  not a merge of the two — `AutoGenerateColumns="False"` from the app-wide
  style silently stopped applying. Fix: set grid behavior properties
  explicitly on the element.
- **WPF action buttons truncated their text** (`Width="90"` was sized for
  the old native `Button`; `ui:Button`'s Fluent padding needs more). Fixed
  with `MinWidth` instead of a fixed `Width`.
- **WPF's brand blue didn't match Web's.** Both platforms defaulted to their
  library's own accent independently. `ApplicationAccentColorManager.Apply`
  looked like the obvious fix but generates its own ramp from the seed color
  (confirmed: feeding it Web's exact `#0F6CBD` produced a visibly different
  `#559CE4` on the actual button) — the fix that worked was overriding the
  three specific brush keys `Wpf.Ui.Controls.Button`'s `Primary` appearance
  binds, with Web's literal `colorBrandBackground`/`Hover`/`Pressed` values.
  Verified by sampling the rendered pixel on both platforms, not by eye.
- **Web's `BanksGrid` had a large, unintentional gap** below its rows when
  used standalone on the Expense tab. Cause: the component's own class
  carried a `max-height`/flex-stretch rule that only makes sense for its
  *other* usage (side-by-side in the Summary tab's grids-row); reused
  standalone, it stretched to fill a height meant for a multi-grid row it
  wasn't part of. Fixed by scoping that rule to the ancestor selector.
- **WPF's totals weren't bold** where Web's were (`<strong>` vs a plain
  `TextBlock` built from a `MultiBinding`). Fixed by splitting the bound
  string into separate `TextBlock.Text` bindings (not `Run.Text` — see
  `docs/ui/wpf.md`).
- **WPF's Bank grid didn't fill its width** (all three columns fixed-width,
  leaving a dead blank header/body area) while Web's table naturally
  stretched. Fixed by giving the leftmost/label column `Width="*"`.
- **WPF's form was a single-column, one-field-per-row, label-left layout**
  where Web's was a 4-column, label-above-control CSS grid (`ADR-002`).
  Rebuilt the WPF form as a matching 4-column `Grid` with label-above cells.

One difference intentionally left as-is: WPF's `DataGrid` got
`CanUserSortColumns="True"` (its native default once the grid was styled
explicitly) while Web's plain `<th>` headers have no sort behavior at all.
Column-header sorting isn't a designed feature on either platform yet — see
`docs/ui/forms-data-and-visualisations.md`.
