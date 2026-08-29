# Inventory: Record-Creation / Data-Entry Forms (Web vs. WPF)

## Context

The user asked for a cross-platform inventory of every record-creation/data-entry
form (modals, dialogs, inline panels) in both `Financial.Web` (React SPA) and
`Financial.App` (WPF), to see where the two front ends match and where they
diverge. `CLAUDE.md`'s UI invariant #1 requires React and WPF to offer
equivalent user tasks, so this inventory is also a parity check. This is a
pure research/reporting task — there is no code change to make, so this
document **is** the deliverable, not a plan to execute.

Both front ends turned out to follow the same convention: nearly every "New X"
action is an **inline panel/UserControl toggled by local state**, not a popup
dialog. The only true modal `Window`/overlay-dialog on either side is the
asset "Move" dialog. WPF additionally keeps three legacy popup `Window`s
(`TransactionDialog`, `CreditDialog`, `PriceDialog`) alive solely for their
delete-confirmation flow — the create/edit UX for those three moved to inline
`*FormView` UserControls, mirroring the Web inline-panel pattern.

## Comparison table

| # | Form / Action | Entity | Web component | WPF view/dialog | In both? |
|---|---|---|---|---|---|
| 1 | New/Edit Expense | Expense | `src/components/ExpenseForm.tsx` (inline, `MonthlyPage.tsx`) | `Views/CashFlow/ExpenseFormView.xaml` (inline UserControl) | ✅ Yes |
| 2 | New/Edit Income | Income | `src/components/IncomeForm.tsx` (inline, `MonthlyPage.tsx`) | `Views/CashFlow/IncomeFormView.xaml` (inline UserControl) | ✅ Yes |
| 3 | New/Edit Transfer ("Move Money") | Bank Transfer | `src/components/TransferForm.tsx` (inline, `MonthlyPage.tsx`) | `Views/CashFlow/TransferFormView.xaml` (inline UserControl) | ✅ Yes |
| 4 | New/Edit Balance Correction | Bank Balance Adjustment | `src/components/BalanceAdjustmentForm.tsx` (inline) | `Views/CashFlow/BalanceAdjustmentFormView.xaml` (inline UserControl) | ✅ Yes |
| 5 | New Withdrawal (Reserve) | Reserve Withdrawal | `src/components/WithdrawalForm.tsx` (inline, `ReservaPage.tsx`) | `Views/CashFlow/WithdrawalFormView.xaml` (inline UserControl) | ✅ Yes |
| 6 | New Income Split (Reserve) | Reserve Income Split | `src/components/IncomeSplitForm.tsx` (inline) | `Views/CashFlow/IncomeSplitFormView.xaml` (inline UserControl) | ✅ Yes |
| 7 | Edit Reserve Movement (edit-only) | Reserve Movement | `src/components/EditMovementForm.tsx` (inline) | `Views/CashFlow/EditReserveMovementFormView.xaml` (inline UserControl) | ✅ Yes |
| 8 | Add/Edit Bill | Recurring Bill | Inline markup in `src/pages/MensaisPage.tsx` (single form, reused for add/edit) | `Views/CashFlow/AddBillFormView.xaml` **and** separate `EditBillFormView.xaml` | ⚠️ See "Correction after deeper investigation" below |
| 9 | New/Edit Entry (Mãe ledger) | Mãe Ledger Entry (BRL/GBP) | Inline markup in `src/pages/ControleMaePage.tsx` (single form) | `Views/CashFlow/CreateEntryFormView.xaml` **and** separate `EditEntryFormView.xaml` | ⚠️ Same as #8 |
| 10 | Edit Investment Snapshot value (edit-only) | Investment Snapshot | `src/pages/InvestmentSnapshotsPage.tsx` — inline "Edit Snapshot" panel in the **same file** as the snapshots grid (missed initially because it isn't a separate component file) | `Views/CashFlow/EditSnapshotValueFormView.xaml` (inline UserControl, `InvestmentSnapshotsView`) | ✅ Yes — corrected, exists on both |
| 11 | New/Edit Investment Transaction | Investment Transaction (Buy/Sell) | `src/components/TransactionsTab.tsx` (inline `InlineForm`) | `Views/Investment/TransactionFormView.xaml` (inline; legacy `TransactionDialog.xaml` Window kept only for delete-confirm) | ✅ Yes |
| 12 | New/Edit Investment Credit | Asset Credit (Dividend/Rent/JCP) | `src/components/CreditsTab.tsx` (inline `InlineForm`) | `Views/Investment/CreditFormView.xaml` (inline; legacy `CreditDialog.xaml` delete-confirm only) | ✅ Yes |
| 13 | New/Edit Price History entry | Manual Asset Price | `src/components/PriceHistoryTab.tsx` (inline `InlineForm`) | `Views/Investment/PriceFormView.xaml` (inline; legacy `PriceDialog.xaml` delete-confirm only) | ✅ Yes |
| 14 | Move Asset (+ optional new-portfolio creation) | Asset scope / Portfolio | `src/components/MoveAssetDialog.tsx` — true modal (`role="dialog"`) | `Views/Investment/MoveAssetDialog.xaml` — true modal `Window` via `DialogService` | ✅ Yes — the one form that's a real modal on **both** platforms |
| 15 | Move Asset / Delete Portfolio (deletion only, no create form) | Portfolio | `src/components/DetailPanel.tsx` + `usePortfolioDeletion.ts` — manual "Delete Portfolio" action, separate step after a move | `Components/NavigationView.xaml` ("Move Asset...", "Delete Portfolio" buttons) + `ViewModels/Investment/MainNavigationViewModelBase.cs` (`DeletePortfolioCommand`, `DeleteSelectedPortfolioAsync`) | ✅ Yes — corrected, exists on both. **Behavioral difference:** WPF additionally auto-prompts ("`\"{name}\" is now empty. Delete it?`") right after a move empties a portfolio (`OfferToDeleteEmptiedPortfolioAsync`); Web requires a separate manual "Delete Portfolio" click with no auto-prompt. |

## Confirmed absent on both platforms

- **New Bank Account / New Card** — neither `BanksGrid.tsx`/`CardsGrid.tsx` (Web)
  nor `BanksGridView.xaml`/`CardsGridView.xaml` (WPF) expose any create action;
  both are read-only/inline-edit grids over existing rows.
- **New Broker / New Portfolio (standalone)** — on both platforms a Portfolio
  can only be created as a side effect of the Move-Asset dialog's "move to a
  new portfolio" option; there's no dedicated Broker or bare-Portfolio
  creation form anywhere.
- **Admin-area CRUD** (Category, IncomeSource management) — no such
  pages/forms exist in either codebase; these are read-only lookup data fed
  into the Expense/Income forms. This matches the existing memory note that
  reserve-bucket CRUD is deferred to a future Admin tab that doesn't exist
  yet.

## Findings after user review (2026-08-29)

The user double-checked all four flagged rows. Corrected status:

1. **#10 Investment Snapshot editing** — not WPF-only. It exists on Web too,
   inline in `src/pages/InvestmentSnapshotsPage.tsx` (same file as the grid,
   which is why the first pass missed it — it isn't a separate component).
   Parity confirmed. No action needed.
2. **#15 Portfolio deletion** — not Web-only. WPF has it via the "Move
   Asset..." / "Delete Portfolio" buttons in `Components/NavigationView.xaml`,
   backed by `DeletePortfolioCommand` in
   `ViewModels/Investment/MainNavigationViewModelBase.cs`. Functionality is
   equivalent, with one behavioral nuance: WPF auto-prompts to delete a
   portfolio immediately after a move leaves it empty
   (`OfferToDeleteEmptiedPortfolioAsync`), while Web requires the user to
   notice and click "Delete Portfolio" separately. Not treated as a bug —
   just a UX difference — unless the user wants Web to adopt the same
   auto-prompt.
3. **#8 Add/Edit Bill** and **#9 Mãe-Entry** — **initially flagged as
   confirmed bugs**: the WPF `EditBillFormView.xaml` / `EditEntryFormView.xaml`
   appeared to have incorrect field/control position alignment relative to
   their Add counterparts. See the correction below — a deeper investigation
   found this is not a coding defect.

## Correction after deeper investigation (2026-08-29, second pass)

A dedicated, line-by-line re-check of `AddBillFormView.xaml`/`EditBillFormView.xaml`
and `CreateEntryFormView.xaml`/`EditEntryFormView.xaml` found **no `Grid.Row`/
`Grid.Column` misindexing** in any of the four files — each file's row
definitions are internally consistent with the fields it shows. What's
happening: Add/Create and Edit intentionally show **different field sets**
(e.g. Bill: Add = Description/DueDay/Value/Area/Note, Edit = Value/Status
only) — this asymmetry matches Web's own `MensaisPage.tsx`/
`ControleMaePage.tsx` exactly, field for field. Because Add has more
preceding fields than Edit, the one field the two variants share ("Value" in
the Bill pair) lands at a different absolute row in each — e.g. row 3 in Add
vs. row 1 in Edit — which is what likely reads as "wrong position alignment"
when flipping between the two forms, but it is a **consequence of the
by-design field-count difference**, not a bug in either file.

One genuine (Low-severity) inconsistency was found instead: `CreateEntryFormView.xaml`'s
primary button is `Width="100"` while `EditEntryFormView.xaml`'s is
`Width="90"` — the Bill pair's buttons are consistently `Width="90"` in both
files, so this 10px mismatch is real but minor.

**This changes items #8 and #9 from "confirmed bugs" back to open
questions** — see the full compliance audit below, which folds this in as a
"no governing standard" item (whether a field shared between an entity's
Add/Create and Edit variants should be pinned to the same absolute grid
position when the two variants' field counts differ by design).

---

# Design Standards Compliance Audit — Web + WPF Forms (2026-08-29)

## Context

Following the inventory above, the user asked for every one of these 15
forms to be reviewed against the repository's UI design standards
(`docs/rules/ui.md`, `docs/ui/*.md`, ADR-001 through ADR-005, the
`fluent-ui` skill) — specifically styles, colors, sizes, field alignment,
and icons — with (a) a list of confirmed non-compliant items and (b) a list
of anything with no governing standard yet, flagged for a decision rather
than guessed at.

A prior full-app audit exists (`docs/ui/standard-compliance-audit-2026-08-23.md`,
2026-08-23) but 12+ PRs have touched more than a dozen of these exact files
since then (column filtering/sorting features, a naming-unification
refactor, a comment-policy cleanup, the Broker/Portfolio Price History tab
removal). This audit re-verifies every file against its **current** content
rather than reusing the prior audit's line numbers or conclusions.

**Headline finding:** the codebase has split into two populations on both
platforms:
- **Migrated (compliant baseline):** Web's `ExpenseForm`/`IncomeForm`/
  `TransferForm`/`BalanceAdjustmentForm`/`WithdrawalForm`/`IncomeSplitForm`/
  `EditMovementForm` (Fluent UI v9 + `formPanelStyles.ts` tokens/grid) and
  WPF's `ExpenseFormView`/`IncomeFormView`/`TransferFormView`/
  `BalanceAdjustmentFormView`/`TransactionFormView`/`CreditFormView`/
  `PriceFormView` (WPF-UI + ADR-005 pinned brand colors + 4-column grid) are
  both genuinely close to standard.
- **Legacy (heavy violations):** Web's `MensaisPage`/`ControleMaePage`/
  `InvestmentSnapshotsPage` inline forms and `TransactionsTab`/`CreditsTab`/
  `PriceHistoryTab` inline forms are still hand-rolled HTML/CSS on the
  legacy `#007acc` blue with `flex-wrap` layout. WPF's Reserva/Mensais/
  ControleMae/Snapshot forms (`WithdrawalFormView`, `IncomeSplitFormView`,
  `EditReserveMovementFormView`, `AddBillFormView`, `EditBillFormView`,
  `CreateEntryFormView`, `EditEntryFormView`, `EditSnapshotValueFormView`)
  are still on the explicitly-forbidden single-column label-left `Grid`,
  with no WPF-UI theme merge and hardcoded colors.

## Part A — Confirmed non-compliant items (Web)

| # | Form | Violations (severity) |
|---|---|---|
| 1 | `ExpenseForm.tsx` | Field order: Payment Source/Card placed after Value instead of before it (Medium) |
| 2 | `IncomeForm.tsx` | Field order: Bank/Description placed after the financial fields (Medium) |
| 3 | `TransferForm.tsx` | Terminology: header/button say "Move Money" instead of the doc-mandated "New Transfer" (Medium) |
| 4 | `BalanceAdjustmentForm.tsx` | Terminology: "Correct Balance" instead of "New Balance Correction" (Medium); Bank shown before Date in create mode (Low, functionally justified) |
| 5 | `WithdrawalForm.tsx` | Field order: Bucket→Amount→Date→Description instead of Date-first (Medium-High) |
| 6 | `IncomeSplitForm.tsx` | Field order: Amount before Description (Medium); post-submit result view uses raw HTML table outside the Fluent/token system (Low) |
| 7 | `EditMovementForm.tsx` | Field order: same Bucket→Amount→Date→Description issue as Withdrawal (Medium-High) |
| 8 | `MensaisPage.tsx` (Bill) | Hardcoded `#007acc`/`#005fa3` (High); non-4px spacing (Medium); `flex-wrap` not CSS Grid (Medium); Area placed after Description/DueDay/Value (Medium); Edit uses raw ✏ emoji next to Fluent Delete icon (Medium); "Add Bill" button right-aligned via `space-between`, not left-and-above-grid (Medium) |
| 9 | `ControleMaePage.tsx` (Entry) | Same cluster as #8: hardcoded color (High), spacing (Medium), flex-wrap (Medium), Currency placed after Description/Note (Medium), icon mismatch (Medium), right-aligned button (Medium) |
| 10 | `InvestmentSnapshotsPage.tsx` | Save/Cancel buttons have **no CSS class at all** — unstyled native buttons (Medium-High); raw ✏ emoji (Medium); non-4px spacing (Medium) |
| 11 | `TransactionsTab.tsx` | Hardcoded color on New/Save buttons (High); "New" button bare label, no icon, not Fluent (High); ✏ vs Fluent-icon mismatch (Medium); flex-wrap layout (Medium); Buy/Sell text hardcodes green/red instead of status tokens (Medium) |
| 12 | `CreditsTab.tsx` | Same cluster as #11 (High/High/Medium/Medium); 3 untokenized blues for Dividend/Rent/JCP (Low) |
| 13 | `PriceHistoryTab.tsx` | Same cluster as #11/#12; manual/automatic color at least self-consistent within the file (Low) |
| 14 | `MoveAssetDialog.tsx` | **No focus trap, no Escape handler, no initial-focus/restore-on-close** (High — real accessibility gap for a genuine modal); hand-rolled backdrop/buttons instead of Fluent `Dialog` (Medium, debt); hardcoded backdrop/shadow colors and non-4px spacing (Medium/Low) |

## Part A — Confirmed non-compliant items (WPF)

| # | Form | Violations (severity) |
|---|---|---|
| 1 | `ExpenseFormView.xaml` | None confirmed |
| 2 | `IncomeFormView.xaml` | "Split to reserve" checkbox placed last instead of right after Net Value, breaking parity with Web's field order (Medium) |
| 3 | `TransferFormView.xaml` | Only one generic error TextBlock — no per-field validation surfacing, unlike Web's per-`Field` errors (Medium) |
| 4 | `BalanceAdjustmentFormView.xaml` | Same missing per-field validation (Medium); confirmation/balance text omits the `£` symbol Web includes (Medium) |
| 5 | `WithdrawalFormView.xaml` | Forbidden single-column label-left `Grid` (High); no WPF-UI theme at all (High); hardcoded `#CCCCCC`/`#FAFAFA`/`Foreground="Red"` (High); no `AutomationProperties.Name` anywhere (Medium); terminology mismatch "New Withdrawal"/"Withdraw" vs Web's "Record a Withdrawal"/"Record Withdrawal" (Medium) |
| 6 | `IncomeSplitFormView.xaml` | Same High cluster as #5; terminology mismatch "New Income Split" vs Web's "Post Monthly Income Split" (Medium); whole total line bolded instead of just the value (Low) |
| 7 | `EditReserveMovementFormView.xaml` | Same High cluster as #5; field order otherwise correctly matches Web |
| 8a/8b | `AddBillFormView.xaml`/`EditBillFormView.xaml` | Same High cluster as #5 in both files; field sets correctly match Web's asymmetry (not a defect) |
| 9a/9b | `CreateEntryFormView.xaml`/`EditEntryFormView.xaml` | Same High cluster; primary button width inconsistent between the pair, 100px vs 90px (Low) |
| 10 | `EditSnapshotValueFormView.xaml` | Same High cluster as #5 |
| 11 | `TransactionFormView.xaml` | Terminology + casing: ViewModel produces "Add Transaction"/"Update Transaction" (Title Case) vs Web's "New transaction"/"Edit transaction" (sentence case) (Medium) |
| 12 | `CreditFormView.xaml` | Same terminology + casing mismatch: "Add Credit"/"Update Credit" vs "New credit"/"Edit credit" (Medium) |
| 13 | `PriceFormView.xaml` | Same terminology + casing mismatch: "Add Price"/"Update Price" vs "New price"/"Edit price" (Medium) |
| 14 | `MoveAssetDialog.xaml` | Hardcoded `Foreground="Red"` (Medium); destination-portfolio combo and new-portfolio-name textbox have no `AutomationProperties.Name` (Medium); no live-region equivalent for the error text (Medium); **no WPF-UI theme** (Low, shared debt — Web's own dialog isn't on Fluent either); missing the "portfolio now empty, delete it?" follow-up state and the "Moving…" in-flight state Web has (functional gap, not style, but worth flagging) |

## Part B — No governing standard exists yet (needs a decision, not a guess)

These can't be scored compliant/non-compliant because no doc addresses them.
Each needs a product/design decision before it can be written up as a rule:

1. **Row-level Edit/Delete icon convention.** Every legacy grid (Mensais,
   ControleMae, TransactionsTab, CreditsTab, PriceHistoryTab, Investment
   Snapshots) mixes a raw ✏ emoji for Edit with the correct Fluent
   `DeleteRegular` for Delete, same row, on Web. This was already flagged in
   the 2026-08-23 audit and remains open. **Need:** confirm the required
   icon (`EditRegular`?) and appearance (subtle/ghost) so both actions match.
2. **Filter/chart-mode/group "chip" toggle buttons** (Transactions/Credits/
   Price History, both platforms) — underlined/link-style buttons with no
   named Fluent primitive. **Need:** decide the intended component (Fluent
   `TabList`? `ToggleButton` group?) and its color/appearance rule.
3. **Manual vs. automatic price-source color convention** (Price History
   chart/grid) — still no canonical color pair named anywhere. **Need:** one
   decision, applied to both the chart marker and any grid text/badge.
4. **Positional continuity between an entity's Add/Create and Edit forms
   when their field sets differ by design** (the Bill/Entry question from
   above). **Need:** decide whether a field shared by both variants (or the
   Save/Cancel row) should be pinned to the same row regardless of how many
   other fields precede it, or whether the current "just follow the field
   list, wherever it lands" approach is acceptable.
5. **How to visually mark a required field.** The forms doc requires a
   "required indicator where relevant," but no form in either platform
   implements one, and no doc names the mechanism (asterisk? caption text?).
   **Need:** pick a non-color-alone convention and document it.
6. **Contextual help mechanism** (tooltip vs. info icon vs. inline caption)
   — required by the same doc line as above, implemented nowhere.
7. **Layout rule for a multi-step decision dialog** (`MoveAssetDialog` on
   both platforms) — ADR-002's 4-column grid is written for parallel
   field-entry forms, not a linear radio/combo decision tree. **Need:** rule
   on whether ADR-002 applies to dialogs, or a separate layout convention.
8. **Whether an inline computed-value sentence** (e.g.
   BalanceAdjustment's "Current calculated balance..." /"Adjustment of ...
   recorded") **should be bold**, the way a grid total row must be. The
   Totals rule is written for labeled grid/list totals, not inline prose.
   Both platforms currently leave these unbolded and matching each other,
   which may be fine — but there's no rule confirming that's intentional.
9. **Whether Save/Cancel/Confirm buttons inside a form need icons.** The
   icon rule in `forms-data-and-visualisations.md` only covers the "New X"
   grid-create button, not a form's own submit/cancel actions. None of the
   16 WPF forms or 14 Web forms give these icons; no rule says whether they
   should.
10. **Post-submit itemized result view** (e.g. `IncomeSplitForm`'s "posted"
    summary table) — not a grid, not a form, not a labeled total — no doc
    addresses its expected component/typography.

## Part A (continued) — Unresolved findings carried forward from the 2026-08-23 audit

These fall just outside the 15-form file list (they're on the button/page
that *triggers* a form, or on a form's legacy delete-confirmation
counterpart) but are directly adjacent to it and were re-verified against
current file contents on 2026-08-29 — all still unresolved:

| Item | Files | Status |
|---|---|---|
| WPF's Bank tab buttons now have the correct icon+primary styling (fixed since 2026-08-23), but still say **"Move Money"/"Correct Balance"**, not "New Transfer"/"New Balance Correction" | `BankSectionView.xaml:34,36` | Still open (Medium) — and now a same-platform-both-sides issue: Web's own outer toggle button (`BankOperationsSection.tsx:95,98`) *does* say "New Transfer"/"New Balance Correction" correctly, but its own inner form (`TransferForm.tsx`, per Part A above) retitles to "Move Money" — so the mismatch is "outer button vs. inner form," consistently on both platforms, not a Web-vs-WPF drift |
| Same row's Edit actions are still raw `✏`/`🗑` emoji (not Fluent/`ui:SymbolIcon`) | `BankSectionView.xaml:98,112,133` | Still open (Medium) — folds into the cross-cutting row-icon gap below |
| Reserva's split-percentage warning still renders in the same `Foreground="Red"` as genuine errors, no distinguishing treatment | `ReservaView.xaml:16,33,55` (all three the identical red) | Still open (High) — non-blocking warning is visually identical to a blocking error |
| Investment Snapshots' Edit trigger button still has only `ToolTip="Edit"`, no `AutomationProperties.Name` | `InvestmentSnapshotsView.xaml:40` | Still open (Medium) |
| The three legacy delete-only dialogs still hardcode `Foreground="Red"` (unmigrated, though correctly exempt from inline-form conversion) | `TransactionDialog.xaml:74`, `CreditDialog.xaml:57`, `PriceDialog.xaml:45` (plus `MoveAssetDialog.xaml:71`, already noted in Part A) | Still open (Low) |
| **Re-opened, not resolved:** whether `MoveAssetDialog` should be a dialog at all, on either platform | Both `MoveAssetDialog.tsx`/`.xaml` | The 2026-08-23 audit's own appendix called this "a product decision, not something an audit can resolve" — ADR-003 reserves drawers for "contextual editing... that should preserve page context," which arguably describes Move Asset better than a blocking dialog. This audit's Part A entries above focused on fixing the *dialog's* accessibility gaps (focus trap, Escape, restore-focus) rather than re-litigating dialog-vs-drawer — that architectural question is still open and belongs to the user/product owner, not something to guess at |

## Part C — Fluent UI React v9 component research for the missing-standard items

### Methodology note (access limitation)

The user asked to cross-check against `https://storybooks.fluentui.dev/react`.
That site is a client-rendered SPA (Storybook 7+) — `WebFetch` cannot execute
its JavaScript, so every page (including direct `?path=/docs/...` URLs)
returns only the empty app shell/title, and `react.fluentui.dev` 301-redirects
to the same site. The one static asset available, `index.json` (the story
index), is large enough that a single fetch truncated before reaching any
component past "Carousel" alphabetically — `Field`, `Dialog`, `MessageBar`,
`Table`, `TabList`, `ToggleButton`, `InfoLabel`, and `Combobox` never came
through. Per the user's direction, the proposals below are grounded in the
model's trained knowledge of the actual published `@fluentui/react-components`
v9 API (the same package `ADR-004` already adopted, and the exact library that
Storybook showcases) rather than a live-scraped citation — flagged here so
this is auditable as "expert judgment," not "verified against the live site."

### Proposed standards, one per gap

1. **Row-level Edit/Delete icon convention.** Fluent's icon package
   (`@fluentui/react-icons`) exports paired regular/filled icons per action —
   `EditRegular`/`EditFilled` alongside the already-correctly-used
   `DeleteRegular`/`DeleteFilled`. Proposed rule: row actions use
   `Button appearance="subtle"` with `icon={<EditRegular />}` /
   `icon={<DeleteRegular />}`, an explicit `aria-label` (Tooltip supplements,
   doesn't replace it), sized to the row (16–20px icon). WPF: `ui:Button
   Appearance="Transparent"` with `ui:SymbolIcon Symbol="Edit24"` /
   `Symbol="Delete24"` and `AutomationProperties.Name`.
2. **Filter/chart-mode/group toggle "chips."** These are single-select among
   mutually-exclusive options (This Month/Last 3/6/12/YTD/All, Bar/Line,
   Stacked/Grouped) — that's Fluent's `TabList`/`Tab` semantic (not
   `ToggleButton`, which implies independent multi-toggle state). Proposed:
   replace the hand-rolled underlined-link buttons with `TabList
   appearance="subtle"` / `Tab`, which also closes the "no ARIA
   tablist/keyboard nav" gap noted elsewhere in the codebase for free. WPF:
   a `RadioButton`-per-option group sharing one `GroupName`, restyled flat/
   underlined to match, since WPF-UI has no direct `TabList` equivalent
   distinct from the page-level `TabControl`.
3. **Manual vs. automatic price-source color.** Not a layout/component gap
   so much as an undecided domain color — Fluent's `Badge` component
   (`color="informative"|"subtle"|...`) is the natural fit for a small
   inline tag distinguishing the two, replacing the current ad hoc colored
   dot with a labeled badge (closes the "color alone" accessibility angle
   too, since the badge carries its own text).
4. ~~Positional continuity between Add/Create and Edit~~ — covered by the
   documented follow-up below (a project-specific layout rule, not a
   component question).
5. **Required field indicator.** Fluent's `Field` component has a first-class
   `required` prop that renders a visible asterisk after the label *and*
   wires `aria-required` on the control — this is exactly the mechanism the
   forms doc already calls for without naming it. Proposed: use `<Field
   label="X" required>` wherever `forms-data-and-visualisations.md`'s
   "Required indicator where relevant" applies. WPF: a `Run` with a themed
   danger-color asterisk appended to the label `TextBlock`, plus
   `AutomationProperties.HelpText="Required"`.
6. **Contextual help mechanism.** Fluent's `InfoLabel` component (label with
   a trailing info icon opening a `Popover`) is the documented pattern for
   exactly this — "Contextual help for complex financial concepts" is
   already named as a priority in `ux-principles.md` without a mechanism.
   Proposed: `InfoLabel` in place of a plain `Field label` wherever a field
   needs one-line domain explanation (e.g. "Round-Up", "JCP"). WPF: a small
   `ui:SymbolIcon Symbol="Info16"` beside the label opening a `ui:Flyout`
   with the same text.
7. **Layout for a multi-step decision dialog (`MoveAssetDialog`).** Fluent's
   `Dialog`/`DialogSurface`/`DialogBody`/`DialogActions` primitives ship
   built-in focus trap, Escape-to-close, and focus-restoration-on-close —
   adopting them on Web would directly close the High-severity accessibility
   gap found in Part A, and their content area is free-form (not grid-based),
   so ADR-002's 4-column rule simply doesn't apply to dialogs — worth stating
   explicitly rather than leaving it implicit. This is independent of the
   still-open dialog-vs-drawer question above.
8. **Inline computed-value sentence bolding.** Fluent's `Text` component
   supports `weight="semibold"` scoped to a span — proposed: wrap only the
   numeric portion of a sentence like "Adjustment of £45.00 recorded" in
   `<Text weight="semibold">`, consistent with the existing Totals rule's
   "bold the value, not the label" principle, just applied to prose instead
   of a grid row.
9. **Save/Cancel/Confirm button icons.** Fluent's own usage convention
   reserves leading icons for actions where recognition value is high (Add,
   Delete, Edit) and leaves primary form-submit actions (Save/Cancel/Submit)
   as plain text buttons. Proposed resolution: icons are **not** required
   (and shouldn't be added) on Save/Cancel — this closes the ambiguity with
   a definite answer rather than leaving it a silent gap.
10. **Post-submit itemized result view** (e.g. `IncomeSplitForm`'s "posted"
    summary). Proposed: a brief `MessageBar intent="success"` line plus a
    real Fluent `Table` for the itemized rows, replacing the current ad hoc
    HTML table borrowed from `ReservaPage.css`'s grid classes.

### Alignment check: existing documented standards vs. the real Fluent v9 API

- **Good alignment, confirmed:** `forms-data-and-visualisations.md`'s
  "Field rules" (visible label, accessible name, required indicator,
  contextual help, validation) map directly onto `Field`'s actual supported
  props (`label`, `required`, `hint`, `validationState`, `validationMessage`)
  — the doc names the right requirements, it just never named the mechanism,
  which is exactly items 5–6 above. Same for the "New X" icon+primary-button
  rule, which matches `Button`'s real `icon`/`appearance="primary"` API
  exactly.
- **Confirmed non-issue:** ADR-002's 4-column custom form grid has no Fluent
  counterpart to misalign with — Fluent v9 ships no opinionated form-layout
  component, so a project-specific grid convention is appropriate, not a gap.
- **Biggest real opportunity, already known as debt, re-confirmed here:**
  every grid in this app is hand-rolled HTML/`DataGrid` (WPF)/plain `<table>`
  (Web), never Fluent's actual `DataGrid` component — which would provide
  sortable headers, keyboard navigation, and a documented row-actions pattern
  for free, closing several accessibility gaps already flagged elsewhere in
  this and the prior audit. Not new, but worth restating as the single
  highest-leverage adoption if a grid-modernization pass is ever scoped.

## Cross-cutting note: phantom CSS custom properties (Web)

Several "tokenized-looking" colors on Web (`var(--bg-subtle, ...)`,
`var(--text-muted, ...)`, `var(--danger, #c0392b)`, `var(--error, ...)`) are
never actually declared in `index.css` — only their hardcoded fallback ever
renders. This silently defeats dark-mode/theme support wherever it appears
(most of the legacy forms above) and should be fixed at the `index.css`
level in one pass rather than file-by-file, the same way the brand-color fix
was.

## Not yet actioned

This entire document is a diagnostic/planning deliverable — **no source
files have been changed**, including the Bill/Entry item below. The user
chose "just keep the report" for the ~30 confirmed violations and 9 of the
10 missing-standard items: no code changes planned for those at all right
now. For missing-standard item #4 (Bill/Entry Add-vs-Edit row alignment),
the user approved the *standard* (pin shared fields/actions to the same row)
and asked for the concrete fix to be written up as a documented, ready-to
-execute plan — not carried out yet. It stays queued below until the user
explicitly asks for it to be implemented.

---

## Documented follow-up (not yet implemented): pin shared fields/actions between Add/Create and Edit

### Decision

New rule, to be added to `docs/ui/forms-data-and-visualisations.md` (new
subsection "Add/Edit variant layout continuity", placed after the existing
"### Layout" subsection under "## Forms"):

> When an entity's Add/Create and Edit forms are separate views with
> different field sets by design, any field that appears in both variants —
> plus the trailing validation-error and Save/Cancel action rows — must
> occupy the same absolute grid row in both views. Reserve empty,
> fixed-height rows in the shorter variant for fields it doesn't show,
> rather than compacting its shared field/action rows upward, so the value
> someone is editing and the buttons they'll press stay in the same place
> whether they just opened Add or Edit for the same entity. This does not
> require both variants to have the same field count or add fields neither
> design calls for — it only fixes the vertical position of what they
> already share.

### Files to change

**`Financial.App/Views/CashFlow/EditBillFormView.xaml`** — currently 5 rows
(Title, Value, Status, Error, Buttons); restructure to 8 rows to match
`AddBillFormView.xaml`'s row count, since Add's Value sits at row 3:
- Row 0: Title (unchanged)
- Row 1, 2: new empty `RowDefinition` with a fixed `Height` (reserving the
  vertical space Add's Description/Due Day rows occupy — a one-line comment
  explaining why is warranted here per the non-obvious-constraint exception
  in the root `CLAUDE.md`'s comment policy)
- Row 3: Value (moved down from row 1 — now pinned to match Add)
- Row 4: Status (moved down from row 2)
- Row 5: new empty reserved row (matches Add's Note row)
- Row 6: Error banner (moved down from row 3 — now pinned to match Add)
- Row 7: Save/Cancel buttons (moved down from row 4 — now pinned to match Add)

`AddBillFormView.xaml` itself needs no changes — it's already the 8-row
reference layout.

**`Financial.App/Views/CashFlow/EditEntryFormView.xaml`** — currently 5 rows
(Title, BRL, GBP, Error, Buttons); restructure to 8 rows to match
`CreateEntryFormView.xaml`'s row count (Create has no field in common with
Edit's BRL/GBP, so only the trailing Error/Buttons rows get pinned):
- Row 0: Title (unchanged)
- Row 1: BRL (unchanged position)
- Row 2: GBP (unchanged position)
- Rows 3, 4, 5: new empty reserved rows (fixed height, matching the vertical
  space Create's Note/Currency/Value rows occupy)
- Row 6: Error banner (moved down from row 3 — now pinned to match Create)
- Row 7: Save/Cancel buttons (moved down from row 4 — now pinned to match Create)

**`Financial.App/Views/CashFlow/CreateEntryFormView.xaml`** — fix the
already-confirmed Low-severity button-width slip: change the primary
button's `Width="100"` to `Width="90"`, matching every other Save/Add button
in this form family (Bill pair and Edit Entry are already `90`).

Use a fixed pixel height for the new reserved rows (not `Auto`, which
collapses to 0 with no content) — pick a value matching this app's typical
single field row height (label + `TextBox`/`ComboBox`, e.g. `30`–`32`) so
the reserved space actually displaces content visually, not just by
`Grid.Row` index.

### Verification

- `dotnet build --configuration Release` for `Financial.App` (and the
  solution, to catch any ripple).
- `dotnet test Tests/Financial.App.Tests` (or the relevant WPF test project)
  if it covers these views/viewmodels — the change is purely visual/XAML,
  view-model bindings and commands are untouched, so no VM test should
  break.
- Launch the built WPF app and open Mensais → "Add Bill" then edit an
  existing bill, and Controle Mãe → "New Entry" then edit an existing entry
  — confirm the Value field (Bill) and the Save/Cancel row (both pairs) now
  render at the same vertical position switching between Add/Create and
  Edit, per the `fluent-ui` skill's "actually run and look at it" completion
  requirement (a clean build doesn't guarantee the visual result is right).

---

## Documented follow-up (not yet implemented): persistent create-form defaults within a session

### Decision (QoL enhancement, requested 2026-08-29)

For every record-creation form in the inventory above: default the date
field to today only the first time a "New X" form is opened in an app run.
After that — and after every subsequent create/save — retain whatever date
and entity-relation selection(s) (bank, credit card, category, or that
form's closest equivalent) were last used, but always clear the
amount/value and free-text description/note fields back to blank. This is
**documentation only, applied to the report — no code changes, no
development done in this turn**, per explicit user instruction. It's queued
here so a future implementation pass has a concrete, per-form starting
point instead of a vague restatement of the request.

### Mechanism (design, not implemented)

**Web:** confirmed via this session's research that no "remember last used
value" pattern exists anywhere in the codebase today (`grep -rn
"remember|Remember|LastUsed|lastUsed"` across `Financial.Web/src` returns
nothing) — every create form rebuilds state from a `BLANK_FORM`/`BLANK_STATE`
constant on open (e.g. `useExpenseForm.ts:58-71,84-97`,
`useTransferForm.ts:21-33,56-63`). The closest existing precedent is
`Financial.Web/src/utils/domainStorage.ts` — a small `sessionStorage`-backed
typed getter/setter, try/catch guarded, single string key. A new module
following that exact pattern (not `sidebarStorage.ts`'s `localStorage`
variant, since the user chose "until tab closes," not "survives restart")
would back this feature: each form's `showCreateForm`/`openCreateForm`
action reads the stored date/relation value(s) instead of unconditionally
spreading `BLANK_FORM`, while amount/description fields always come from
`BLANK_FORM` untouched; on save, the just-used values get written back.

**WPF:** no persistence infrastructure needed. Research confirmed
`MonthlyView`/`ReservaView` (and by extension the other CashFlow/Investment
tab hosts) and their workflow ViewModels are constructed **once** and kept
alive for the app's lifetime (`MainWindow.xaml.cs:15-26,42-54`), despite
being registered `AddTransient` in DI — nothing ever re-resolves them mid-run.
A plain private field per remembered value (e.g. `_lastUsedDate`,
`_lastUsedBankId`) on each workflow ViewModel, read inside its existing
`ShowCreate...Form()` method instead of unconditionally assigning
`DateTime.Today`/`null`, is sufficient — no settings file, no singleton
service, no new class.

### Per-form field mapping

Six CashFlow forms were researched in full this session; the remaining
forms are mapped from the existing inventory/audit above and flagged where
not yet verified in detail:

| Form | Date currently defaults to... | Entity-relation field(s) to persist | Always-clear fields |
|---|---|---|---|
| Expense (`useExpenseForm.ts` / `ExpenseWorkflowViewModel.cs`) | Blank on Web (`BLANK_FORM.date=''`, `useExpenseForm.ts:59`) / `DateTime.Today` on WPF (`ExpenseWorkflowViewModel.cs:284`) | `paymentSource`/`ExpenseFormPaymentSource` (bank), `creditCardId`/`ExpenseFormCreditCardId`, `categoryId`/`ExpenseFormCategoryId` | Amount, description |
| Income (`useIncomeForm.ts` / `IncomeWorkflowViewModel.cs`) | Blank (Web) / Today (WPF) | `bank`/`IncomeFormBank`, `incomeSource`/`IncomeFormSource` | Gross/Net value, description |
| Transfer (`useTransferForm.ts` / `TransferWorkflowViewModel.cs`) | Today already, every time, both platforms (`useTransferForm.ts:60`, `TransferWorkflowViewModel.cs:119`) | `sourceBank`/`TransferFormSourceBank`, `destinationBank`/`TransferFormDestinationBank` | Amount, note |
| Balance Correction (`useBalanceAdjustmentForm.ts` / `AdjustmentWorkflowViewModel.cs`) | Today already, every time, both platforms | `bankName`/`AdjustmentFormBankName` — **note:** Web's own code comment currently says "Create opens with no bank pre-selected" by deliberate design (`useBalanceAdjustmentForm.ts:62-65`); persisting it would be a conscious reversal of that decision, worth confirming when this is actually built | Target balance, note |
| Withdrawal (`useReserva.ts` / `WithdrawalViewModel.cs`) | Blank (Web) / Today (WPF) | `withdrawalBucketId`/`WithdrawalBucketId` | Amount, description |
| Income Split (`useReserva.ts` / `IncomeSplitViewModel.cs`) | Blank (Web) / Today (WPF) | None identified — date/amount/description only | Amount, description |
| Add Bill (`MensaisPage.tsx` / `AddBillFormView.xaml`) | N/A — this form has no date field at all (`DueDay`, an integer, not a date) | `Area`/`NewArea` | Description, Value, Note |
| Create Entry (`ControleMaePage.tsx` / `CreateEntryFormView.xaml`) | Not verified this session — same today-default rule presumably applies | `Currency`/`CreateCurrency` | Description, Note, Value |
| Investment Transaction (`TransactionsTab.tsx` / `TransactionFormView.xaml`) | Not verified this session | `Type` (Buy/Sell) | Quantity, Unit Price, Fees |
| Investment Credit (`CreditsTab.tsx` / `CreditFormView.xaml`) | Not verified this session | `Type` (Dividend/Rent/JCP) | Value |
| Price History (`PriceHistoryTab.tsx` / `PriceFormView.xaml`) | Not verified this session | None — only Date + Price fields exist | Price |
| Edit Investment Snapshot value | **N/A** — edit-only, not a repeated create workflow; this enhancement doesn't naturally apply | — | — |
| Move Asset dialog | **N/A** — a one-off contextual multi-step action, not a repeated create-with-common-fields workflow; doesn't naturally fit this pattern | — | — |

### Explicitly not implemented

No source files are touched by this section — no storage module created, no
ViewModel fields added, no form wired up. This is queued for a future
implementation pass. When that pass happens: the four Web forms that
currently open with a **blank** date (Expense, Income, Withdrawal, Income
Split) would gain a real "defaults to today" behavior for the first time,
which is a small independent behavior change worth flagging to the user
before that work starts, separate from the persistence mechanism itself.

---

# Part D — Trigger-to-form naming consistency (new standard, 2026-08-29)

## The standard

The user identified a real, already-followed pattern on the Expense
workflow — trigger button "New Expense" → form title "New Expense" →
confirm button "Add Expense" — and asked for it to be codified. It has been
added to `docs/ui/forms-data-and-visualisations.md` as a new "### Trigger-
to-form naming consistency" subsection (immediately after "### Grid
create/new actions" under "## Data grids"):

> The "New X" trigger button, the form it opens, and that form's own
> confirm action must all name the same thing... `ExpensesSection.tsx`/
> `ExpenseForm.tsx` is the reference: the trigger reads "New Expense", the
> form's own title reads "New Expense" (and "Edit Expense" once populated
> from an existing row), and the confirm button reads "Add Expense" (and
> "Save" once editing) — never a generic "Submit"/"Confirm"/"OK" that drops
> the entity name. Do not let the form re-title itself into different
> wording once open... the trigger's noun carries through unchanged.

## Violation check across all 15 forms

| Form | Trigger label | Form title | Confirm button | Verdict |
|---|---|---|---|---|
| Expense | "New Expense" (`ExpensesSection.tsx:65-67`) | "New Expense"/"Edit Expense" (`ExpenseForm.tsx:74`) | "Add Expense"/"Save" (`ExpenseForm.tsx:178`) | ✅ Compliant — the reference itself |
| Income | "New Income" (Web `IncomeSection.tsx:98`, WPF `IncomeSectionView.xaml:27`) | "New Income"/"Edit Income" (`IncomeForm.tsx:53`) | "Add Income"/"Save" (Web `IncomeForm.tsx:119`; WPF `IncomeFormView.xaml:116,119`) | ✅ Compliant on both platforms |
| Transfer (Web) | "New Transfer" (`BankOperationsSection.tsx:95`) | "Move Money"/"Edit Transfer" (`TransferForm.tsx:55`) | "Move Money"/"Save" (`TransferForm.tsx:107`) | ❌ **Violation** — trigger disagrees with the form it opens |
| Transfer (WPF) | "Move Money" (`BankSectionView.xaml:34`) | (Move Money, inferred from confirm binding) | "Move Money"/"Save" (`TransferFormView.xaml:105,108`) | Internally consistent (not a new-rule violation), but the whole chain still uses the non-canonical name — a separate, already-documented terminology issue (Part A, continued) |
| Balance Correction (Web) | "New Balance Correction" (`BankOperationsSection.tsx:98`) | "Correct Balance"/"Edit Balance Adjustment" (`BalanceAdjustmentForm.tsx:74`) | "Correct Balance"/"Save" (`BalanceAdjustmentForm.tsx:132`) | ❌ **Violation** — same pattern as Transfer |
| Balance Correction (WPF) | "Correct Balance" (`BankSectionView.xaml:36`) | Not directly confirmed this session; success message reads "Balance Corrected" (`BalanceAdjustmentFormView.xaml:34`) | Not directly confirmed this session | Internally consistent with its own trigger (inferred); non-canonical name, pre-existing issue |
| Withdrawal (Web) | "New Withdrawal" (`ReservaPage.tsx:118`) | "Record a Withdrawal" (`WithdrawalForm.tsx:36`) | "Record Withdrawal"/"Saving..." (`WithdrawalForm.tsx:71`) | ❌ **Violation** — trigger vs. form mismatch, different verb entirely |
| Withdrawal (WPF) | "New Withdrawal" (`ReservaView.xaml:31`) | "New Withdrawal" | "Withdraw"/"Withdrawing..." | ⚠️ Partial — trigger/title match; confirm button drops the explicit noun |
| Income Split (Web) | "New Income Split" (`ReservaPage.tsx:115`) | "Post Monthly Income Split" (`IncomeSplitForm.tsx:70`) | "Post Income Split" (`IncomeSplitForm.tsx:94`) | ❌ **Violation** — trigger vs. form mismatch |
| Income Split (WPF) | "New Income Split" (`ReservaView.xaml:30`) | "New Income Split" | Not fully re-verified this pass (only the later result-panel wording was seen) | Trigger/title match confirmed; confirm-button text needs one more look |
| Edit Reserve Movement | (row edit icon, not a "New X" trigger) | "Edit Movement" (both platforms) | "Save" (both platforms) | ✅ Compliant — Edit-mode plain "Save" matches the reference's own Edit-mode convention |
| Add/Edit Bill (WPF) | Not directly re-verified this pass | "Add Bill"/"Edit Bill" (`AddBillFormView.xaml:23`, `EditBillFormView.xaml:20`) | "Add"/"Adding..." (`AddBillFormView.xaml:49-53`) — **drops "Bill"** — and "Save" for Edit | ❌ **Violation** on the Add confirm button specifically |
| Create/Edit Entry (WPF) | Not directly re-verified this pass | "New Entry"/"Edit Entry" (`CreateEntryFormView.xaml:22`, `EditEntryFormView.xaml:18`) | "Add Entry"/"Saving..." (`CreateEntryFormView.xaml:50`) and "Save" for Edit | ✅ Compliant — matches the reference shape exactly |
| Investment Transaction (Web) | Bare **"New"** (`TransactionsTab.tsx:350-352`) | "New transaction"/"Edit transaction" (`TransactionsTab.tsx:112`) | Plain **"Save"** for both create and edit (`TransactionsTab.tsx:182`) | ❌ **Violation** — trigger AND confirm both drop the entity name entirely |
| Investment Transaction (WPF) | Bare **"New"** visible label, `ToolTip="New transaction"` only (`TransactionsView.xaml:122-123`) | Bound `ConfirmLabel` → "Add Transaction"/"Update Transaction" (`TransactionDialogViewModel.cs`) | Same bound `ConfirmLabel` (`TransactionFormView.xaml:104`) | ❌ Partial — confirm button correctly carries the name; trigger's *visible* label doesn't (tooltip-only, and per the accessibility doc, "tooltips supplement, don't replace" the accessible/visible name) |
| Investment Credit (Web) | Bare "New" (same file structure as Transactions) | "New credit"/"Edit credit" (`CreditsTab.tsx:127`) | Plain "Save" inferred from the shared `InlineForm` structure (not individually re-verified) | ❌ Same violation shape as Transaction |
| Investment Credit (WPF) | Bare "New", `ToolTip="New credit"` (`CreditsView.xaml:47-48`) | Bound `ConfirmLabel` → "Add Credit"/"Update Credit" (`CreditDialogViewModel.cs`) | Same | ❌ Same partial pattern as Transaction |
| Investment Price (Web) | Bare "New" (`PriceHistoryTab.tsx:251`) | "New price"/"Edit price" (`PriceHistoryTab.tsx:99`) | Plain "Save" inferred, not individually re-verified | ❌ Same violation shape |
| Investment Price (WPF) | Bare "New", `ToolTip="New price"` (`PriceHistoryView.xaml:83-84`) | Bound `ConfirmLabel` → "Add Price"/"Update Price" (`PriceDialogViewModel.cs`) | Same | ❌ Same partial pattern |
| Move Asset dialog | Not a row-create trigger — a contextual multi-step action ("Move..."/"Move Asset...") | N/A | N/A | Rule doesn't naturally apply |

## Two failure shapes, worth fixing as separate slices later

1. **Trigger says one thing, the form says another** (Transfer, Balance
   Correction, Withdrawal, Income Split — all on Web). The trigger already
   uses the doc's canonical name (per the existing "Grid create/new
   actions" rule); the form's own internal copy needs to adopt it, not the
   other way around.
2. **The trigger button's visible label is just "New"** (Investment
   Transaction/Credit/Price, both platforms). Already flagged in Part A as
   a High-severity styling violation ("bare label, no icon, not Fluent")
   — this new rule adds an independent naming reason to fix the same
   button: a tooltip alone doesn't satisfy the trigger-to-form chain.

## Not yet actioned

This is a documentation update, not a code change. A few cells above are
marked "not fully re-verified this pass" rather than asserted as settled —
`Add/Edit Bill` and `Investment Credit`/`Investment Price`'s Web confirm-
button text, WPF Balance Correction's form title/confirm text, and WPF
Income Split's confirm-button text — a follow-up read would firm these up
before treating them as final.

---

# Part E — `InvestmentTree.tsx`: adopt Fluent's `Tree` component (2026-08-29)

## Question

The Active/Historic Investments navigation (broker → portfolio → asset) is
a hand-built `<ul>/<li>`+`<button>` structure. The user copied Fluent UI
React v9's `Tree` Storybook doc page (`docs/ui/fluent-ui-react-v9-pages/tree.md`)
and asked whether it's a better option to replace what's there now.

## Recommendation: yes, adopt it — Web-only change

This isn't a new finding — it closes a gap already documented twice: this
project's own `docs/ui/forms-data-and-visualisations.md` "## Tree views"
section requires keyboard navigation and accessible expand/collapsed state,
and the 2026-08-23 audit's Group E already flagged this exact file as High
severity ("Hand-built `<ul>/<li>` with no `role="tree"`/`treeitem`, no
`aria-expanded`, no arrow-key roving... WPF's native `TreeView` provides all
of this for free" — one of the few places WPF is ahead of Web).
**`Financial.App/Components/NavigationView.xaml` already uses WPF's native
`TreeView`**, so no WPF change is needed here — this is purely a Web-side
fix to reach parity with what WPF already has.

## How the current structure (`InvestmentTree.tsx`, 412 lines) maps onto Fluent `Tree`

| Current (hand-built) | Fluent `Tree` equivalent |
|---|---|
| `<ul className="investment-tree__list">` root | `<Tree aria-label="Investments">` |
| `<li>` per Broker/Portfolio (expandable) | `<TreeItem itemType="branch">` |
| `<li>` per Asset (leaf) | `<TreeItem itemType="leaf">` |
| Custom chevron `<button onClick={toggle}>▾/▸</button>` (`InvestmentTree.tsx:196-203,282-289`) | `TreeItemLayout`'s built-in `expandIcon` slot (default chevron, or override) |
| Custom `●` status-dot `<span>` (`InvestmentTree.tsx:138`) | `TreeItemLayout`'s `iconBefore` slot |
| `useState(expanded)` per node (`InvestmentTree.tsx:153,240`) | `Tree`'s `openItems`/`defaultOpenItems`/`onOpenChange` (controlled or uncontrolled) |
| Manual `isSelected` class + `onClick` (`nodeMatchesSelected`, `InvestmentTree.tsx:58-65`) | **Not** the built-in `selectionMode="single"` (that renders radio buttons — wrong for this UI). Instead: manual `onClick` + `aria-selected`, exactly what the Tree doc's own Best Practices section recommends for "selection used for navigation purposes" (`tree.md` line 26) |
| Native HTML5 `draggable`/`onDragStart`/`onDragOver`/`onDrop` (`InvestmentTree.tsx:124-131,181-194,267-280`) | Not a documented Fluent feature — no built-in drag-and-drop in `tree.md`. Fluent v9 components generally forward unrecognized native props to their underlying element, so attaching the same handlers directly to `TreeItem` should keep drag-and-drop working, but this needs hands-on verification during implementation, not just a docs read |
| Asset-class `<select>` filter above the tree (`InvestmentTree.tsx:349-366`) | Unchanged — stays outside the `Tree` itself |

## Assessment

- **Real win:** native `role="tree"`/`treeitem` semantics, `aria-expanded`,
  and arrow-key roving navigation, all for free — directly fixes the High
  severity Group E finding and brings Web to parity with WPF's `TreeView`.
- **Real risk, not a blocker:** the drag-and-drop-to-move-an-asset
  interaction is fully custom today and isn't part of Fluent's documented
  `Tree` feature set — needs verification that native HTML5 DnD props still
  work when placed on `TreeItem` (very likely, given Fluent v9's general
  prop-forwarding pattern, but unverified until actually tried).
- **Correctly avoid:** `selectionMode="single"`, since it renders a radio
  button per item — not the look this app wants. Handle "current selection"
  manually, which the component's own docs explicitly anticipate.
- **Scope:** this is a bigger lift than the other findings in this report (a
  ~400-line component rewrite, not a few rows in a form) — should be scoped
  as its own implementation slice if pursued, not folded into a smaller
  change.

## Not yet actioned

This is analysis only — `InvestmentTree.tsx` has not been touched.
