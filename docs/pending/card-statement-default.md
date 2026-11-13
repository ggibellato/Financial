# Default statement month for new card charges

Status: pending. Noted 2026-11-12.

## Problem

- A card charge stores the statement it belongs to as `Expense.InvoiceDate`, always the first of a month.
- When a charge is created, the statement month defaults to the purchase month:
  - Domain: `Expense.Create` sets `InvoiceDate = FirstOfMonth(invoiceDate ?? date)` (`Financial.CashFlow.Domain/Entities/Transactions/Expense.cs`, around line 86).
  - Web: `computeDefaultInvoiceMonth` in `Financial.Web/src/utils/expenseDefaults.ts` returns the purchase date's month, and the expense form uses it.
- For most charges that default is wrong. On the dev data (`data/dev/data-cashflow.json`), comparing each of the 142 card charges' purchase date (`ChargeDate`) with its statement month:

  | Statement month vs purchase month | Charges |
  |---|---|
  | Same month | 30 |
  | One month later | 96 |
  | Two months later | 15 |
  | Five months later | 1 |

  So the user corrects the statement month by hand on most new charges.
- The P25 PRD (`docs/prd/P25-prd-credit-card-invoice-dates/`) kept billing cycles out of scope on purpose. Its section 7 says `InvoiceDate` stays manually set with a simple default, not derived from a per-card cut-off rule.

## Already shipped

- #1256: editing a charge's date keeps a statement month that differs from the purchase month (the form used to reset it to the new date's month). When the date change does move the statement month, the form shows a hint.

## Options

### (a) Small: default to the statement being viewed

On a card's account screen, New charge defaults the statement month to the statement the screen is showing (the screen's month), not the purchase month.

- **For:** a small web-only change with no data change. It matches how the user enters charges, while looking at a statement.
- **Against:** wrong when entering a charge for a different statement than the one on screen. The app still knows no rule; the default just follows the screen.

### (b) PRD-sized: a closing day per card

Store a closing day (statement cut-off) on each card account. A purchase on or before the closing day goes on that month's statement; a later one goes on the next month's. Use that rule as the default on create, and possibly to check edits.

Points to settle:

- **Data:** a new card field means a data-file version change and a migration.
- **Short months:** a closing day past the end of a month (for example 31 in February).
- **History:** the closing day can change over time; decide whether the rule needs dates of validity.
- **Existing charges:** left as they are; the rule only sets defaults for new ones.
- **Paid statements:** the P60 rules that lock paid statements still apply.
- **Calendar sync:** statement due dates sync to the calendar (P60 F07/F08); decide whether the closing day touches that.
- **Parity:** category totals and the tithe use the statement month, so any change to existing data would show in the parity run.

It needs a short PRD and a migration.

## Decision needed

The user chooses (a), (b), or (a) now and (b) later.
