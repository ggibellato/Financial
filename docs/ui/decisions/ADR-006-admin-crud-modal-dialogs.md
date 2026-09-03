# ADR-006: Admin Lookup-Entity CRUD Uses Modal Dialogs

## Status

Accepted

## Context

`forms-data-and-visualisations.md`'s "New X create actions are inline forms, not
popup dialogs" rule requires every "New X" trigger to expand an inline form in
place, never a modal `Window`/`Dialog`, so the user keeps the grid, chart, and
totals it feeds in view while filling it in. The rule's own reference examples
are all transactional financial entry: New Expense, New Income, New Transfer,
New Transaction, New Credit, New Price.

Ten Admin lookup-entity CRUD screens — Bank, Broker, Category, CreditCard,
IncomeSource, InvestmentAccount, Portfolio, RecurringBill, ReserveBucket, and
Asset — also expose a "New X"-shaped trigger, but implement it as a real modal
(Fluent `Dialog` on Web, a popup `Window` on WPF) rather than inline. The
2026-09-03 compliance audit (`standard-compliance-audit-2026-09-03-dark-mode-
buttons-grids.md`, Forms #1) flagged this as a rule violation across all 20
files (10 Web + 10 WPF) — notably, this population is also the app's most
internally consistent button/form implementation on both platforms.

## Decision

Admin lookup-entity CRUD — flat reference-data lists with no associated chart:
Bank, Broker, Category, CreditCard, IncomeSource, InvestmentAccount, Portfolio,
RecurringBill, ReserveBucket, Asset — is **exempt** from the "New X create
actions are inline forms" rule and may use a modal `Dialog` (Web) / popup
`Window` (WPF) for create and edit instead of an inline panel.

## Rationale

The inline-form rule exists to preserve grid, chart, and running-total context
while a user repeatedly enters rows that directly feed a chart they're actively
watching (Expense, Income, Transfer, Transaction, Credit, Price). Admin lookup
entities have neither of those properties:

- They have no paired chart or running total to keep in view.
- They're edited rarely, as setup/maintenance, not as repeated day-to-day
  transactional entry.
- Each form is genuinely short (2-6 fields) and self-contained.

That combination is exactly what the same document's own top-level guidance
already names as the correct case for a dialog: "Use a dialog for focused,
self-contained confirmation or short blocking work." Converting these ten
entities to inline panels would add UI weight (an expand/collapse row on a
flat list) to screens where a modal's blocking nature is the better fit, not
a compromise forced by convenience.

## Scope

Applies only to the ten named Admin CRUD entities above. It does not extend to
any transactional/financial entry action — Expense, Income, Transfer,
Withdrawal, Balance Correction, Income Split, Investment Transaction, Credit,
Price, Recurring Bill, Mãe Ledger Entry — which remain governed by the
existing inline-form rule unchanged.

A future Admin entity defaults to this same modal pattern only if it shares
the same shape (flat reference data, no chart, short form). A lookup entity
that later grows a chart or becomes part of a repeated-entry workflow should
move to inline instead of being grandfathered into this exception.

## Consequences

- `forms-data-and-visualisations.md`'s "New X create actions are inline forms"
  section cross-references this ADR so the written rule and the real,
  intended behavior agree.
- The 2026-09-03 compliance audit's Forms #1 finding is resolved by this
  decision, not by re-platforming code — no source files change as a result.
- The three legacy delete-only Investment dialogs (`TransactionDialog.xaml`,
  `CreditDialog.xaml`, `PriceDialog.xaml`) are **not** covered by this
  exception — they're confirmation dialogs for a transactional-entry
  workflow, already correctly using the existing "genuine confirmation" carve
  -out in `forms-data-and-visualisations.md`, not this ADR.
