# CashFlow Tithe Accuracy

## 1. Executive Summary

CashFlow Tithe Accuracy fixes two related gaps in how this app calculates monthly tithe: income that never lands in a tracked bank account currently can't be recorded at all, and every expense filed under the "Dizimo" (tithe) category counts against the tithe balance even when it's actually a separate charitable offer.

It exists for the single account owner who self-hosts this tool to track personal cash flow instead of a spreadsheet, and who wants the tithe calculation — 10% of net income — to be both complete and accurate without extra bookkeeping outside the app. Today, income like monthly ISA dividends that never touch a tracked bank has no way in, so it's either skipped (understating tithe) or forces creating a bank entity that doesn't represent anything real. Separately, recording a charitable offer under the same "Dizimo" category as real tithe payments silently reduces the calculated tithe balance, understating what's actually still owed.

At a high level, this covers two independent fixes to the CashFlow bounded context: making an income's Bank association optional (with a new optional description so the user can still note where the money came from) and adding a per-expense flag that lets a Dizimo-categorized expense opt out of counting toward the tithe balance, defaulting to counting as it does today.

## 2. Problem and Opportunity

**The Problem**

- **Unrecordable income sources**: income that never lands in a tracked bank account (e.g. monthly ISA dividends) can't be entered at all today, because `Bank` is a mandatory field on every income record — forcing the user to either skip recording it (understating tithe) or create a bank entity that doesn't represent a real account.
- **No provenance for untracked income**: even once Bank becomes optional, there's no field today to note where such income came from, making entries with no bank context unclear in the Incomes list.
- **Overstated "tithe already paid"**: the "Dizimo" category mixes genuine tithe payments with separate charitable offers, and today's calculation counts every expense filed under that category against the tithe balance — so recording an offer there silently and incorrectly reduces what the app reports as still owed.

**The Opportunity**

- Making `Income.Bank` optional (mirroring how `Expense.PaymentSourceBank` already works) directly solves the unrecordable-income problem — the user records the income once, it counts toward tithe like any other, and it simply doesn't touch any bank balance.
- Adding an optional free-text description to Income solves the provenance gap in the same change, so a bank-less entry is still self-explanatory in the Incomes list.
- A per-expense "counts toward tithe" flag on Expense solves the overstated-tithe problem without touching Categories at all — avoiding a premature redesign, since the user has a separate future plan to make Categories hierarchical.

## 3. Target Audience

### Primary Users

**The Account Owner**
- Manages personal household cash flow — income, expenses, bank balances, and a 10%-of-net-income tithe — using this self-hosted app as the single source of truth instead of a spreadsheet.
- Receives income that doesn't land in a tracked bank account (e.g. dividends from an ISA holding) and currently has no way to record it.
- Uses the same expense category for both tithe payments and separate charitable offers, and needs the calculated tithe balance to reflect only the former.

## 4. Objectives

**Product Objectives**

- **Capture** every income the user actually receives, regardless of whether it lands in a tracked bank account.
- **Preserve** tithe-calculation accuracy on both sides: bank-less incomes still count toward what's owed, and only genuine tithe payments (not offers) count toward what's been paid.
- **Avoid** a premature Categories redesign, keeping the offer/tithe distinction scoped to the expense level per the user's stated future plan for hierarchical categories.

**Success Metrics**

- **Capture**: 100% of bank-less incomes recorded contribute to that month's `CalculatedTithe` — verified by test.
- **Preserve accuracy**: 0% of Dizimo-categorized "offer" expenses (flag unchecked) reduce `TitheBalance` — verified by test; genuine tithe payments (flag checked, the default) continue to reduce it exactly as today.
- **Avoid redesign**: 0 new Bank or Category entities are required to record either scenario — verified by the absence of any placeholder bank or new category in the implementation.

## 5. User Stories

### F01. Optional Bank & Description for Income
- As a user, I want to record an income without selecting a bank, so that dividends or other income that never lands in a tracked account can still be recorded.
- As a user, I want to add an optional description to an income, so I can note where money with no bank association came from.
- As a user, I want a bank-less income to still count toward my monthly tithe calculation, so my tithe isn't understated just because the money didn't go into a tracked bank.
- As a user, I want a bank-less income to never affect any bank's balance, so my bank totals stay accurate.

### F02. Per-Expense Tithe Contribution Flag
- As a user, I want to mark an individual Dizimo-categorized expense as not counting toward tithe, so I can record charitable offers in the same category without them reducing my calculated tithe balance.
- As a user, I want new Dizimo expenses to count toward tithe by default, so I don't have to remember to toggle a setting for the common case of an actual tithe payment.
- As a user, I want to see my updated tithe balance immediately after saving an expense with the flag unchecked, so I know the offer was recorded correctly.

## 6. Functionalities

### F01. Optional Bank & Description for Income

**Capabilities:**
- Bank becomes optional when creating or editing an income; when omitted, the income has no bank association.
- New optional Description field: free text, up to 200 characters, no minimum — mirrors the existing character cap already used for `Expense.Description`, though Income's is optional rather than required.
- A bank-less income is excluded from every bank's balance calculation — it contributes to no bank's running total.
- A bank-less income still contributes to the month's tithe base (`CalculatedTithe`) exactly as a banked income would; the tithe calculation has never filtered by bank and continues not to.
- Existing incomes recorded before this feature (all of which have a Bank, since it was previously required) are unaffected — no data migration needed.

**Experience:**
The income creation/edit form's Bank field changes from required to optional (e.g. a "None" option becomes valid, or the field can simply be left unselected). A new optional "Description" text field appears in the same form (e.g. below Net Value), with placeholder text like "e.g. Chip ISA dividend". In the Incomes list, the Bank column shows blank for bank-less incomes rather than an error. Description, when present, is visible in the Incomes list; it's simply empty when omitted.

**Error Handling:**
- A Description longer than 200 characters is rejected with "Description must be 200 characters or fewer." and the income is not saved.
- Selecting a bank ID that no longer exists (pre-existing edge case, unrelated to this feature) continues to be rejected with "Bank '...' is not recognized."
- Net value validation (must not be negative) is unchanged and continues to apply regardless of Bank presence.

### F02. Per-Expense Tithe Contribution Flag

**Capabilities:**
- New boolean flag on every expense, "Counts As Tithe," defaulting to `true` on creation.
- Only meaningful for expenses filed under a tithe-flagged category (e.g. "Dizimo"): the month's "already paid" tithe total sums only expenses where both the category is tithe-flagged AND this flag is `true`.
- Expenses in a non-tithe category are unaffected by this flag's value — they're never included in the tithe total regardless of the flag.
- The flag can be toggled at creation or later via edit, for any expense in (or moved into) a tithe-flagged category.

**Experience:**
The expense creation/edit form shows a "Counts toward tithe" checkbox, defaulting to checked, visible/enabled only when the selected category is the tithe category (e.g. "Dizimo") — for any other category, the control is hidden. Unchecking it before saving records the expense as an offer: it still appears normally in the category's expense list, but is excluded from the calculated tithe-paid total. The existing monthly Tithe summary reflects the change immediately after saving — an unchecked Dizimo expense no longer reduces the amount still owed.

## 7. Out of Scope

- **Hierarchical/nested categories** (e.g. "Dizimo/Dz", "Dizimo/Offer") — explicitly deferred to a future PRD per the user's stated plan for restructuring Categories.
- **A new "Oferta" category, or any category create/edit/delete capability** — Categories remain seeded and read-only in this version; this fix stays entirely at the expense level.
- **A synthetic "external" bank for bank-less income** — bank-less incomes are excluded from all bank totals entirely, not attributed to a placeholder bank.
- **Bulk-editing the tithe flag** across many existing expenses at once — each expense is toggled individually through its own edit form in this version.
- **Description affecting tithe calculation, sorting, search, or reporting** — it is informational display only in this version.

## 8. Dependency Graph

| # | Feature | Priority | Dependencies |
|---|---------|----------|--------------|
| F01 | Optional Bank & Description for Income | 1 | None |
| F02 | Per-Expense Tithe Contribution Flag | 1 | None |

### Execution Waves
Features within the same wave can be built in parallel. A wave starts only after every feature in earlier waves is complete.

- **Wave 1**: F01, F02

### Priority levels
- **1** = Essential — product does not work without it
- **2** = Important — significant value addition
- **3** = Desirable — incremental improvement

```mermaid
graph TD
  F01[Bank-less Income]
  F02[Tithe Flag]
```

## 9. Acceptance Criteria

### F01. Optional Bank & Description for Income
- [ ] An income can be created with no Bank selected; the income saves successfully.
- [ ] An income created with no Bank shows blank in the Bank column of the Incomes list.
- [ ] A bank-less income's Net Value is included in that month's `CalculatedTithe`.
- [ ] A bank-less income is excluded from every bank's balance calculation.
- [ ] An income can be created/edited with a Description up to 200 characters, and it displays in the Incomes list.
- [ ] A Description over 200 characters is rejected with a validation message and the income is not saved.
- [ ] Existing incomes recorded before this feature (all with a Bank) continue to display and calculate correctly, unaffected by the change.

### F02. Per-Expense Tithe Contribution Flag
- [ ] A new expense in the Dizimo category defaults to `CountsAsTithe = true`.
- [ ] Unchecking "Counts toward tithe" on a Dizimo expense and saving excludes its value from that month's tithe-paid total.
- [ ] A Dizimo expense with the flag checked (the default) continues to reduce the tithe balance, matching current behavior.
- [ ] The "Counts toward tithe" control is not shown (or is disabled) when the selected category is not the tithe category.
- [ ] Changing an expense's category away from Dizimo does not affect the tithe calculation, regardless of the flag's stored value.
- [ ] Toggling the flag on an existing Dizimo expense and re-saving updates that month's tithe summary on next fetch.

### Cross-Feature Integration
- [ ] A month's tithe balance correctly reflects both a bank-less income (F01) contributing to `CalculatedTithe` and a Dizimo expense with `CountsAsTithe` unchecked (F02) being excluded from the paid total, when both are recorded together in the same month.
