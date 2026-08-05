# Credit Card Tab

## 1. Executive Summary

Credit Card Tab is a focused UI change to the Financial app's CashFlow → Monthly page. It gives credit card invoices their own "Credit Card" tab, in addition to Summary, and stops unpaid credit card charges from cluttering the normal Expense list. The person using the app is the app's sole user, managing their own bank accounts and credit cards.

Today, the Monthly page's Summary tab embeds a "Cards grid" (per-card outstanding totals with mark-paid/unmark-paid actions) alongside category totals, bank balances, and incoming totals. At the same time, the Expense list shows every expense regardless of whether it was paid immediately from a bank or charged to a credit card and not yet settled, so unpaid card charges appear as if they were already-paid spend.

This feature adds a second, identical rendering of the existing Cards grid inside a new dedicated "Credit Card" tab (positioned right after the Expense tab) on both the Web and WPF clients — the Summary tab keeps its Cards grid exactly as it is today, unchanged. It also excludes unpaid card charges from the normal Expense list so each expense is visible in exactly one place: the Expense list while paid from a bank (or once a card charge is settled), or the Card tab while an unpaid card charge. No new business logic, data model, or API is introduced — the mark-paid/unmark-paid workflow, statement grouping, and settlement cascade already shipped in a prior feature and are reused unchanged, and both grids read from the same underlying data so they always agree.

## 2. Problem and Opportunity

**The Problem**

- **Ambiguous expense list**: The Expense list shows unpaid credit card charges next to bank-paid expenses with no distinction in list membership, making it unclear which expenses have actually left a bank account.
- **No dedicated card workspace**: There's no single place to focus on "what do I owe on my cards this month" without also looking at bank balances and income totals — and the user still wants the existing Summary overview left exactly as it is, since it's already relied on for a quick at-a-glance check.

**The Opportunity**

- Adding a "Credit Card" tab with its own copy of the Cards grid (mirroring how Bank operations already got their own tab) gives card invoices a focused screen for anyone who wants to work only with cards — without touching the Summary tab's existing at-a-glance view, directly solving the dedicated-workspace problem without any regression risk to Summary.
- Filtering unpaid card charges out of the Expense list makes the list accurately reflect money that has actually left a bank account — directly solves the ambiguous expense list problem.
- Because the underlying settlement data model, mark-paid/unmark-paid actions, and API already exist, this is a low-risk, presentation-only change that can ship quickly and be validated independently of any future reporting changes.

## 3. Target Audience

### Primary Users

**Personal Finance Owner**
- Uses the Financial app as a single-user personal install to track their own bank accounts and credit cards across the month.
- Reviews the Monthly page regularly to see what's been spent, what's owed on cards, and what bank balances look like.
- Wants a clear, uncluttered view of outstanding card invoices without unrelated summary data in the way.

## 4. Objectives

**Product Objectives**

- **Add** a dedicated Card tab that gives credit card invoice tracking its own focused screen.
- **Preserve** the Summary tab exactly as it is today, including its existing Cards grid.
- **Clarify** the Expense list so it only shows expenses that have actually settled against a bank (directly or via card payment).
- **Preserve** all existing mark-paid/unmark-paid functionality without any behavioral change, in both places it now appears.

**Success Metrics**

- The Summary tab's layout and content are pixel-for-pixel unchanged after the change (still 4 grids: category totals, cards, bank balances, incoming totals) — verified visually and via unmodified existing Summary tests.
- The new Card tab's totals match the Summary tab's Cards grid totals exactly for every month, since both read the same underlying data — verified by comparing values side by side.
- 0 unpaid credit card charges appear in the Expense list for any month with outstanding card statements — verified by comparing Expense list contents against known unpaid `CardStatement` data.
- 100% of existing Cards grid interactions (mark paid, unmark paid, bank picker) continue to work identically from both the Summary and Card tab instances, verified by existing/updated automated tests passing.

## 5. User Stories

### F01. Exclude Unpaid Card Charges from Expense List
- As the system, I want to exclude expenses charged to a credit card and not yet settled from the monthly expense list so that the list only shows expenses that have left a bank account
- As a user, I want an unpaid credit card charge to reappear in the Expense list automatically once its invoice is marked paid so that I don't have to re-enter or move it manually

### F02. Web: Dedicated Credit Card Tab
- As a user, I want a "Credit Card" tab on the Monthly page, right after the Expense tab, so that I can view and manage credit card invoices in a focused screen
- As a user, I want the Credit Card tab to show the same per-card outstanding totals and mark-paid/unmark-paid controls I already have today in Summary, as its own copy
- As a user, I want the Summary tab's Cards grid to remain exactly as it is today, so my existing at-a-glance view isn't disrupted

### F03. WPF: Dedicated Credit Card Tab
- As a user, I want a "Credit Card" tab on the WPF Monthly view, right after the Expense tab, so that I can view and manage credit card invoices in a focused screen
- As a user, I want the Credit Card tab to show the same per-card outstanding totals and mark-paid/unmark-paid controls I already have today in Summary, as its own copy
- As a user, I want the Summary tab's Cards grid to remain exactly as it is today, so my existing at-a-glance view isn't disrupted

## 6. Functionalities

### F01. Exclude Unpaid Card Charges from Expense List

**Capabilities:**
- Applies to the monthly expense query used by both the Web and WPF Expense list views (single shared Application-layer change, no per-client logic).
- An expense is excluded from the list when its computed payment status is `CreditCardCharge` (has a `CardTag` set and `SettledAt` is null).
- An expense remains in the list when its computed payment status is `ImmediatePayment` (paid directly from a bank, no card tag) or `CreditCardSettled` (card-tagged but already settled, i.e. `SettledAt` is set).
- No change to expense ordering (still descending by date) or to any other field returned for expenses that remain in the list.
- No change to the underlying `Expense` entity, `CardStatement` entity, or the mark-paid/unmark-paid workflow — this feature only changes which expenses the monthly list query returns.

**Experience:**
- User opens the Expense tab for a month that has one or more unpaid credit card charges: those charges do not appear in the list; only bank-paid and already-settled card expenses are shown.
- User (or the system, via the existing mark-paid action in the Card tab) marks a card statement as paid: on next load of the Expense tab for that statement's month, the previously-hidden expenses now appear in the list, since their computed status changed to `CreditCardSettled`.
- No new UI states, loading indicators, or error messages are introduced — this is a filtering change on data that was already being fetched.

### F02. Web: Dedicated Credit Card Tab

**Capabilities:**
- `MONTHLY_TABS` gains a 5th entry (`card` / "Credit Card"), positioned right after the existing `expense` tab (new order: Summary, Expense, Credit Card, Incoming, Bank).
- The existing `CardsGrid` component (props, internal logic, and its data source in `useMonthly`) is unchanged. It is rendered a second time inside the new `activeTab === 'card'` block, in addition to its existing rendering inside the `activeTab === 'summary'` block — the Summary tab's JSX and layout are untouched.
- Both renderings are bound to the same `useMonthly` state (`cardStatements`, `banks`, `markPaidSources`, etc.), so a mark-paid/unmark-paid action taken from either tab updates both instantly — there is no separate data fetch or state to keep in sync.
- Each rendering needs a distinct React `key`/DOM context (e.g. wrapping element) only if required to avoid duplicate-id concerns in tests or styling; no change to `CardsGrid`'s internal markup is otherwise needed.

**Experience:**
- User clicks the "Credit Card" tab (between Expense and Incoming): sees a copy of the same per-card outstanding totals, bank-picker dropdown, and Mark Paid / Unmark Paid buttons shown in Summary.
- User clicks the "Summary" tab: still sees the Cards grid exactly as before, alongside category totals, bank balances, and incoming totals — no visual or behavioral change.
- Marking a statement paid from either tab is reflected immediately in the other tab's grid too, since both read the same state.
- Switching tabs, month navigation, and loading/error states behave exactly as they do for the existing Bank tab (same tab-switching pattern already in place).

### F03. WPF: Dedicated Credit Card Tab

**Capabilities:**
- `MonthlyView.xaml` gains a new `TabItem` ("Credit Card") alongside the existing Summary, Expense, Incoming, and Bank tabs, positioned right after Expense (new order: Summary, Expense, Credit Card, Incoming, Bank).
- The existing `CardsGridView` (and its bindings to `MonthlyViewModel`'s `MarkStatementPaidCommand` / `UnmarkStatementPaidCommand`) is unchanged. A second `CardsGridView` instance is added as the new tab's content, bound to the same `MonthlyViewModel` — `MonthlySummaryView.xaml` keeps its existing `CardsGridView` untouched.
- WPF supports multiple views bound to the same view-model instance/properties without conflict, so both `CardsGridView` instances stay in sync automatically when a mark-paid/unmark-paid command executes from either one.

**Experience:**
- User selects the "Credit Card" tab (between Expense and Incoming): sees a copy of the same per-card outstanding totals and Mark Paid / Unmark Paid controls shown in Summary.
- User selects the "Summary" tab: still sees the Cards grid exactly as before, alongside category totals, bank balances, and incoming totals — no visual or behavioral change.
- Marking a statement paid from either tab is reflected immediately in the other tab's grid too, since both bind to the same view-model state.
- Tab switching and month navigation behave exactly as they do for the existing Bank tab (same `TabControl` pattern already in place).

## 7. Out of Scope

**Reporting and category totals**
- Excluding unsettled credit card charges from category totals, or counting settled card expenses in the month/year they were paid rather than charged. Category totals behavior is unchanged in this PRD.

**Invoice detail**
- Showing the list of individual expenses that make up a card statement/invoice (expense-level drill-down). The Card tab continues to show only per-card statement totals, as it does today.

**Paid invoice history**
- A history section listing past paid invoices across months. The Card tab shows only the current month's statements, as it does today.

**Settlement business logic**
- Any change to how mark-paid/unmark-paid works, how statements are created, how the settlement cascade sets `SettledAt`/`PaymentSource` on expenses, or the API contract. All reused as-is.

**Credit card management**
- Adding, editing, or removing credit cards. The fixed set of supported cards is unchanged.

## 8. Dependency Graph

### Part 1: Dependency Table

| # | Feature | Priority | Dependencies |
|---|---------|----------|--------------|
| F01 | Exclude Unpaid Card Charges from Expense List | 1 | None |
| F02 | Web: Dedicated Credit Card Tab | 1 | None |
| F03 | WPF: Dedicated Credit Card Tab | 1 | None |

### Execution Waves
Features within the same wave can be built in parallel. A wave starts only after every feature in earlier waves is complete.

- **Wave 1**: F01, F02, F03

### Priority levels
- **1** = Essential — product does not work without it
- **2** = Important — significant value addition
- **3** = Desirable — incremental improvement

```mermaid
graph TD
  F01[Exclude Unpaid]
  F02[Web Credit Card Tab]
  F03[WPF Credit Card Tab]
```

## 9. Acceptance Criteria

### F01. Exclude Unpaid Card Charges from Expense List
- [x] For a month with an unpaid credit card statement, expenses charged to that card and not yet settled do not appear in the monthly Expense list (Web and WPF).
- [x] For the same month, expenses paid directly from a bank still appear in the Expense list, unchanged.
- [x] After a card statement is marked paid, its expenses appear in the Expense list for that month on next load, without creating any new expense record.
- [x] After an unmark-paid action reverses a settlement, the affected expenses are excluded from the Expense list again.

### F02. Web: Dedicated Credit Card Tab
- [x] The Monthly page shows a "Credit Card" tab positioned immediately after the Expense tab (order: Summary, Expense, Credit Card, Incoming, Bank).
- [x] The Credit Card tab displays the same per-card outstanding totals shown in Summary, for the selected month.
- [x] The Summary tab continues to display its Cards grid unchanged — same content, same position, no regression.
- [x] Mark Paid and Unmark Paid actions work identically from the Credit Card tab and from Summary (same bank-picker requirement, same resulting state).
- [x] Marking a statement paid from one tab is reflected in the other tab's grid without a page reload.

### F03. WPF: Dedicated Credit Card Tab
- [ ] The Monthly view shows a "Credit Card" tab positioned immediately after the Expense tab (order: Summary, Expense, Credit Card, Incoming, Bank).
- [ ] The Credit Card tab displays the same per-card outstanding totals shown in Summary, for the selected month.
- [ ] The Summary tab continues to display its Cards grid unchanged — same content, same position, no regression.
- [ ] Mark Paid and Unmark Paid actions work identically from the Credit Card tab and from Summary (same bank-picker requirement, same resulting state).
- [ ] Marking a statement paid from one tab is reflected in the other tab's grid without a manual refresh.

### Cross-Feature Integration
- [ ] No cross-feature integration criteria apply to this PRD: F01, F02, and F03 have no functional data dependencies between them (no Consumes/Provides declared in Section 6).
