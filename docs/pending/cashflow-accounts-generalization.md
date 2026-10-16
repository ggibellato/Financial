# CashFlow accounts generalization — discussion summary

Status: **design agreed, PRD not started.** Next step: write the WPF-removal ADR, then run `prd-writer` for the features below.
Scope: **CashFlow becomes account-centred, and the WPF front end is removed** from the whole application (F00). Everything else raised during the discussion is listed under [Future features](#future-features-not-in-this-project). Discussion date: 2026-10-08 to 2026-10-15.

Terminology: a **transaction** is anything that touched an account (expense, income, transfer, card charge, card payment, pot deposit or withdrawal, settlement entry). When one action writes transactions on several accounts, they are **linked transactions**.

## Goal

Replace the separate Bank, Credit Card, Reserva and Mãe concepts with one generic **Account** model, where each account has a set of **capabilities** (what it can do), pre-filled from a **built-in template** (Bank, Credit card, Pots, Settlement). Allow **groups of accounts** with their own summary (like the CashFlow main page). Adding a new account from an existing template must need no code, and moving money between accounts must be one generic mechanism.

**Final objective: an account-centred UI** (Web only; see [WPF removal](#wpf-removal)). Instead of today's options spread across separate pages and levels (Monthly, Reserva, Controle Mãe, card statements…), the user selects one account and does everything for it from there: pay a card statement from the card, transfer or pay a recurring bill from a bank, deposit to or withdraw from Reserva, add an entry or settle with Mãe. The account model is what makes this possible; the UI is part of this work, not a later add-on.

## Current state (inventory, 2026-10-08)

No shared account abstraction exists. Each concept is a separate class with its own balance logic. All balances are derived from transaction records, except investment snapshots.

| Concept | Class | Balance |
|---|---|---|
| Bank | `Entities/Bank.cs` | Derived in `BankService.ComputeBalance`: opening balance + incomes − expenses (incl. round-up) ± transfers + reconciliation adjustments |
| Credit card | `Entities/CreditCard.cs` + `CardStatement.cs` | Derived: unpaid charges per statement (`CardStatementService`) |
| Reserva pots | `Entities/ReserveBucket.cs` + `ReserveMovement.cs` | Derived: sum of the pot's transactions |
| Mãe | `Entities/MaeLedgerEntry.cs` | Two totals (BRL, GBP); no account entity, just a flat list |
| Investment account | `Entities/InvestmentAccount.cs` + `InvestmentSnapshot.cs` | Stored monthly value |
| Tithe | `TitheService` + `TitheCarryForward.cs` | Derived amount owed (10% of net income − tithe expenses, carried forward) |

Not accounts: income sources (where money comes from), categories (where expense money goes; only `Expense` has one), recurring bills (Mensais: scheduled obligations).

### Current money flows

| Operation | From → To | Where |
|---|---|---|
| Bank expense (+ optional round-up) | Bank → category | `ExpenseService.AddExpenseAsync` |
| Card charge; move to a later statement | Card → category | `Expense` with `CreditCard`; `SetInvoiceDate` |
| Statement payment | Bank → card | `CardStatementService.MarkStatementPaidAsync`: re-points every charge to the bank and re-dates it to today; no payment record |
| Bank transfer | Bank → bank | `TransferService` (bank-to-bank only) |
| Reconciliation | adjustment → bank | `BalanceAdjustmentService` |
| Income | Income source → bank (bank optional) | `IncomeService` |
| Income split | Income → pots (90% of net, by percentage) | `IncomeService.BuildSplitMovements`, `ReserveService.PostIncomeSplitAsync` |
| Pot withdrawal via bank | Pot → bank | `ReserveService.PostWithdrawalAsync`: two cancelling bank expenses (−"Reserva", +real category) |
| Mãe entry | BRL ↔ GBP | `ControleMaeService.CreateEntryAsync` (historical FX rate) |
| UK bill paid → expense | Bill → bank expense | `MensaisService.MarkBillPaidAsync` (`POST {id}/paid`): expense + Paid status in one save (moved from the UIs in PR #1014) |
| Snapshot suggestion | Card / pots → investment account | `InvestmentSnapshotService.GetSuggestionsForMonthAsync` |

### Known weak spots

- Bank balances and category totals are matched by **name string** in the web client (`useMonthly.ts`).
- The "Reserva" category is looked up by its exact name (`Category.ReservaName`); renaming it breaks withdrawals via bank.
- "Charges for card/period" filter is duplicated in `CardStatementService` and `InvestmentSnapshotService`.
- The main page fires ~10 API calls and sums totals in the browser; there is no server-side summary query.
- Mãe and Reserva are single fixed instances; a second one cannot be created.
- Statements and snapshots are created lazily inside GET requests.

## User answers that shaped the design

1. **Reserva** is an earmark on money held in the Trading 212 **investment** account, which is outside CashFlow (its value is larger than the Reserva total). Money goes in by moving it from a bank (e.g. Barclays, or the Trading 212 cash account, which is a CashFlow bank) to the Trading 212 investment, recording an expense with category "Reserva" on the bank that sent it.
2. **Mãe** is a running balance between the user and their mother: each buys things for the other (UK / Brazil), values are recorded, and at some point the balance is zeroed. There is no fixed period: it has been roughly yearly, but a zero-out can come sooner or later. Real money moves through any account (Brazil or UK), but in the Mãe account the settlement is just another entry to zero the balance. No bank link needed.
3. **Investment accounts** stay their own monthly-snapshot feature; not part of the account model for now.
4. **Round-up** destinations (Chase saver, Trading 212 auto-invest) are outside CashFlow and need no tracking; round-up stays money leaving the bank.
5. A warning when Reserva exceeds the Trading 212 snapshot is **not wanted**.
6. A second Settlement account (the user's son, GBP only) is expected soon.
7. **What each account is:**
   - **Banks:** Barclays, Chase and Trading 212 cash. Ordinary accounts that hold money, pay daily interest (recorded as income) and have a debit card for expenses.
   - **Credit cards:** Barclays (×2), Chase, BA Amex (no linked bank; paid from any bank) and **PayPal** (Pay Later / Pay in 4). For now a Pay in 4 purchase is recorded as 4 charges, each moved to its own statement month; proper instalments are a future feature.
   - **Reserva:** money set aside for specific purposes, in pots: Investimento, Casa/Viagem/Saídas, and personal pots for Gleison, Ariana and Samuel.
   - **Mãe:** a record of what the user paid for their mother and what she paid for them, in BRL and GBP. A similar GBP-only account with the user's son may follow.
   - **Investments** (monthly values of easy-to-withdraw savings) and **Tithe** (tithe owed on all income, including income not tracked in CashFlow such as dividends) are **controls, not accounts**.

## Decisions

### Account templates (built-in)

A template is the *kind* of account; the user's accounts are named instances of it (e.g. "Barclays" is made from **Bank**, "Reserva" from **Pots**). Capability names are indicative; the PRD/spec fixes the final list.

| Template | Behaviour | Capabilities | Transactions |
|---|---|---|---|
| **Bank** | Opening balance, income, categories, transfers in/out, reconciliation, round-up, pays recurring bills | `HasOpeningBalance`, `ReceivesIncome`, `HasCategories`, `CanSendTransfers`, `CanReceiveTransfers`, `CanReconcile`, `CanPayBills`, `SupportsRoundUp` | Opening balance, income, expenses (+ round-up), transfers in/out, card payments out, reconciliation adjustments, Reserva deposit/withdrawal expenses, recurring-bill payments |
| **Credit card** | Statements, due date, future statement (move a charge to a later statement), categories, payment from a bank | `HasStatements`, `HasCategories`, `CanPayBills`, `ReceivesPayments` | Charges (negative), statement payments from a bank (positive) |
| **Pots** | Contains pots with split percentages; receives deposits (from an income split or manual); withdrawals (optionally via a bank); no categories. Several Pots accounts allowed | `HasPots`, `ReceivesIncomeSplit`, `CanWithdraw` | Per pot: deposits (split), withdrawals |
| **Settlement** | Participants; each entry records who paid, value and currency; the zero-out (whenever it happens; no fixed period) is a settle entry; no categories, no bank link. Each account lists its currencies: Mãe = BRL + GBP; son = GBP only | `HasParticipants`, `MultiCurrency`, `CanSettle` | Entries (paid by a participant), settle entries |

### Model

- `Account` aggregate: `Id`, `Name`, `Template`, `Currency` (Settlement: its currencies), `IsActive`, optional parent (pots under a Pots account).
- **Capabilities are the only thing code checks.** Services, validation and the allowed-actions API ask "does this account have `CanPayBills`?", never "is this a Bank?".
- **Templates are defined in code**: a named, fixed capability set. Templates decide which operations may post transactions to an account, never how its balance is computed (see [One balance model](#one-balance-model)).
- An account records its template, and its capabilities come from the template **live**, so a template change reaches existing accounts. Per-account settings stay limited to the ones already agreed: Settlement participants and currencies, bank round-up, active.
- A capability cannot be removed from an account that has history using it.
- Capability-specific data lives in small value objects (card terms, opening balance, pot split), **not** an inheritance tree or one class with type switches.
- **Account groups**: named, ordered, chosen member accounts, backed by **one server-side summary query** (replacing the ~10 client calls and browser-side totals). A group total only sums accounts in the same currency; other currencies (e.g. Mãe BRL) show separately, never silently converted.
- **Generic account-to-account transfer** (introduced in F01): any account on either side, amount in each account's currency, optional FX rate. A transfer is two linked transactions with no category. Replaces today's bank-only transfer.
- **Reserva deposits stay as a "Reserva"-category expense** on the sending bank (money leaving CashFlow; keeps Reserva in category totals and historic averages), now **linked** to the pot credits (see [Pots](#pots)). Converting it to a transfer was rejected: it would remove Reserva from category totals.
- **"Reserva" becomes a flag on the category** (like `IsTithe` / `IsInvestment`) instead of a name lookup.
- Tithe stays a derived amount owed, not an account. Investment accounts, income sources, categories and recurring bills stay outside the account model.

### One balance model

- **Every account's balance = opening balance + sum of its transactions**, per currency, up to a date. The opening balance is the account's first transaction (removes today's `OpeningBalanceDate` window special case).
- One shared calculation replaces today's four: `BankService.ComputeBalance`, card outstanding in `CardStatementService`, the pot sums in `ReserveService` / `ReserveBucketAsOfDateResolver`, and `ControleMaeService.GetTotals`.
- Amounts are signed. A card's balance is negative while money is owed and returns to zero when paid (its "transient" nature needs no special rule); the UI shows it as "owed".
- **Category belongs to the transaction, not the account.** Bank and card expenses carry one; transfers, card payments, pot transactions and settlement entries do not.
- **Effects on other accounts** = one operation writing linked transactions on several accounts in one save; editing or deleting the operation changes all of them together.
- Money entering or leaving CashFlow (income, expenses, the Reserva deposit into the Trading 212 investment) is a single transaction with no counter-account. This is single-entry with links, not full double-entry bookkeeping.

### Credit cards and statements

- **Statements stay month/year, as today.** Each charge keeps its stored statement month/year (today's `InvoiceDate`), set on creation and movable to a later statement. `CardStatement` (card, year, month, paid) stays. A statement's charges are the charges with that stored month, not a date-range filter.
- **Paying a statement** = one bank → card transfer on the payment date for the statement total, **with no category**, linked to the statement. A statement is paid in full in one payment (no partial payments, as today; see [Future features](#future-features-not-in-this-project)).
- **Category reporting is unchanged** (today's `Expense.ReportingDate` rule): an unpaid charge counts in its statement month; once paid it counts on the payment date, taken from the linked payment instead of rewriting the charge's date. Category totals and tithe land in the same months as today.
- The bank shows **one "card payment" line**; from it the user opens the statement and sees every transaction with its own category.

### Settlement

- A Settlement account has **participants** (Mãe: the user and their mother; son: the user and their son) and one or more **currencies**.
- Each entry records **who paid** (a participant, explicit; not the sign of the amount), the amount, and its **currency** (asked only when the account has more than one currency). No category, no bank link.
- **Multi-currency accounts (Mãe) keep today's rule:** each entry stores its original amount plus the converted value in the account's other currency at the entry date's rate. The balance is the sum of the BRL values and the sum of the GBP values: **one debt shown in both currencies**, not two separate debts.
- **Single-currency accounts (son)**: no conversion; the balance is a plain sum.
- **Settle action** (zero the balance at any time): the user picks the currency the money actually moved in; the entry is pre-filled so **both** totals reach zero in one entry. The difference from the day's exchange rate is the small adjustment, kept inside that entry and visible as its implied rate. Both values stay editable.

**Worked example (Mãe):**

| # | Entry | Paid by | Original | BRL value | GBP value |
|---|---|---|---|---|---|
| 1 | Shopping for Mãe in the UK (Jan) | User | £70 | R$500 (Jan rate) | £70 |
| 2 | Mãe buys something for the user in Brazil (Mar) | Mãe | R$360 | R$360 | £50 (Mar rate) |
| | **Mãe owes the user** | | | **R$140** | **£20** |

R$140 and £20 are the same debt in two currencies (approximately equal because each entry used its own date's rate).

- **Mãe pays R$140 using Settle (BRL):** the entry is R$140 / £20, and both totals become 0. If converted at today's rate it might be £19; Settle uses £20 and the £1 difference is the adjustment.
- **Mãe pays £20 using Settle (GBP):** the entry is £20 / R$140, and both totals become 0.
- **An ordinary entry instead of Settle** converts at the day's rate, so one currency can be left with a small leftover (e.g. £1).
- **A partial payment** (ordinary entry, R$100): the balance becomes R$40 / ~£6: still one debt, now smaller.

### Pots

- A **Pots** account (the user's is "Reserva") contains pots as sub-accounts. Pot balance = sum of its transactions (same numbers as today); the Pots account balance = sum of its pots. No category on pot transactions; a pot goes negative only with confirmation; split percentages total 100 within each Pots account.
- **Withdrawal through a bank keeps today's records**, now written and linked as one operation: pot −X; bank expense −X in "Reserva"; bank expense +X in the real category. The bank nets to zero and the Reserva category total stays deposits − withdrawals. (Corrects an earlier idea of a pot → bank transfer, which would have stopped withdrawals reducing the Reserva category total.)
- **Deposit is linked (new):** one operation writes the bank "Reserva" expense(s) and the pot credits, split once across pots by percentage, in one save.
  - From an income marked split-to-reserve: amount is always 90% of net and the date is always the income's date; neither is editable.
  - Funded from one or more banks: by default one line from the income's bank for the full amount; the user can split it (e.g. Barclays £85 + Trading 212 cash £5). The lines must add up to the deposit amount. Splitting the pots once (not per funding line) avoids per-pot rounding differences.
  - The income itself always lands in one bank. A bank is required for an income that splits to the reserve; an income without a split keeps the bank optional, as today.
  - Editing the income recalculates the deposit, its pot split and its funding lines; if hand-split lines no longer add up, the user is asked to adjust them. Deleting the income removes everything linked to it.
  - A manual deposit (no income behind it) works the same way, with the user choosing amount and date. It may also have **no funding lines at all**, for money that comes from outside CashFlow (e.g. moved from an investment straight into the pots): the pots are credited and no bank or "Reserva" expense is touched, so it does not count in the Reserva category total.
- The user no longer records the "Reserva" expense by hand; the deposit creates it.

### Testing capabilities

- Each capability is its own unit (rule + handler) with its own tests, run against a minimal test account that has only that capability. A new capability ships with its own tests; existing capabilities' tests are untouched.
- The balance is **one shared calculation**, tested once; capabilities only add transactions, so they cannot change how a balance is computed.
- Capabilities never call each other. They interact only through the **linked-transaction operations** and the **transfer pair rules** (who can send to whom), which have their own contract tests, so a new capability that plugs into them is checked against the same contract. This is the only place a new capability can affect the others.
- One combination test per built-in template (4 today) checks the template's capabilities together, including the transactions they post. Because templates are fixed in code, this stays one test per template, not a combination matrix.
- An architecture test forbids code from checking the template name, so behaviour can only hang off capabilities.

### Account-centred UI

One account screen, the same shape for every template:

- **Header:** name, template, balance (shown in both currencies for a multi-currency Settlement account; "owed" for a card), statement status for a card.
- **Transactions:** everything that touched this account in one list (expenses, incomes, transfers, card payments, pot deposits and withdrawals, settlement entries).
- **Actions:** only the ones the account's capabilities allow.

| Selected account | Actions |
|---|---|
| Bank | Add expense, add income, transfer to another account, **pay recurring bill**, reconcile balance |
| Credit card | Add charge, view statements, move a charge to a later statement, pay statement (choose the bank) |
| Pots | Deposit into pots (from one or more banks, or none), withdraw from a pot (optionally via a bank), manage pots |
| Settlement | Add entry (who paid, currency), settle (zero the balance, at any time) |

**Pay recurring bill** (bank): lists unpaid UK recurring bills with this bank pre-selected and records the expense + Paid status through the existing `POST {id}/paid` (`MensaisService.MarkBillPaidAsync`). The Mensais overview screen keeps paying by due date too.

Rules:

- **The API returns each account's allowed actions** (derived from the account's capabilities), and the front end only renders them. The rule "what can a card do" stays in the back end, not in React; keeping rules server-side is what PR #1014 did for Mensais, and it keeps the door open for any future client.
- **Transfer is one shared action**: offered on any account with `CanSendTransfers`, and the destination list shows only accounts with `CanReceiveTransfers` that the transfer pair rules allow. A newly allowed move (e.g. Settlement → Bank) appears in the UI with no front-end change.
- **Overview screens stay**, because they are about time or the whole household, not one account: monthly summary by category and account-group summaries, tithe, annual summary, historic averages, Mensais (bills are obligations; paying one creates an expense on the chosen bank), investment snapshots.
- Built in React only, designed with the `fluent-ui` skill and checked against `docs/ui/review-checklist.md`. Old pages are retired only once the account screen covers their tasks.

### WPF removal

**Decision (2026-10-13): the application stops supporting two front ends. `Financial.App` (WPF) is removed from the whole application** (CashFlow and Investment), and `Financial.Web` (React) becomes the only front end. Reason: the account-centred UI is a large UX redesign; building and keeping it at parity in two front ends would roughly double the UI work for a single-user tool.

- **An ADR is required** before work starts, in `docs/decisions/` (next number: ADR-002). It records the decision, the reason, the consequences below, and supersedes the "React-led UX with equivalent WPF outcomes" invariant.
- **Removed first, as F00**, so F01–F05 never build WPF screens that would be deleted. Recommended order; the PRD confirms it.
- **What F00 touches** (inventory to confirm in the PRD):
  - projects: `Financial.App`, its test projects (`Financial.Presentation.Tests`, `Financial.App.E2ETests`) and the `Financial.Architecture.Tests` rules that reference it; entries in `Financial.slnx`;
  - CI: the `wpf` and `wpf-e2e` jobs, `detect-changes.sh` classification, the `wpf` entry in `coverage-baseline.json`, the `wpf` row of the coverage comment, the WPF exclusions in `coverlet.runsettings` and the backend job's `-assemblyfilters`;
  - nightly: the `e2e-wpf` job in `nightly.yml`, its entry in the failure-issue job's `needs`, and its case in `.github/scripts/nightly-issue.test.ps1`. Mutation testing has no WPF target (`scripts/stryker.ps1` covers only the two Domain projects; `Financial.Web/stryker.config.json` covers the SPA), so nothing changes there;
  - coverage tooling that knows the `wpf` job: `.github/actions/coverage-gate/action.yml`, `.github/scripts/coverage-baseline.ps1`, `baseline-reminder.ps1` and their tests (`coverage-gate.test.ps1`, `baseline-reminder.test.ps1`, `detect-changes.test.sh`);
  - local deploy: `scripts/deploy.ps1` stops publishing `Financial.App` (publish step, process stop, `appsettings.Production.json` stamping, summary lines), `scripts/deploy-templates/start-app.ps1` is deleted and `start-all.ps1` starts only the web server, `GoogleCalendarAppRedirectUri` leaves `deploy.local.example.json` (the WPF OAuth loopback on port 8082 goes away; its redirect URI can then be removed from the Google Cloud OAuth client). The script also deletes a stale `deploy/Financial.App` folder and `deploy/start-app.ps1` left by earlier runs, since it only cleans the folders it knows;
  - dev tooling: the `Financial.App` build entry in `.vscode/tasks.json`;
  - `Tools/ImportGoogleSpreadSheets` is a separate legacy WPF utility (one-time import, run from Visual Studio). Whether it goes too is decided in the PRD;
  - rules and docs: CLAUDE.md (solution layout, WPF commands, UI invariant 1, `Financial.App` mentions), `Financial.App/CLAUDE.md`, `docs/rules/ui.md`, `docs/ui/wpf.md` and WPF items in `docs/ui/review-checklist.md`, the `fluent-ui` and `testing-guide-Financial` skills, the `ui-reviewer` agent, `README.md`, `context.md`;
  - memory notes about WPF patterns become obsolete.
- **Until F00 merges, the current rules still apply**: any change made before then keeps WPF parity. CLAUDE.md and the rule files are changed by F00 itself, not in advance.
- **Investment is affected too**: its WPF screens go with `Financial.App`. Every Investment workflow must already exist in React before WPF is deleted (confirm in the PRD).

### Features (slices — each ships with its own data migration tested on a temp copy of the data file)

F00 removes WPF. From F01 the account screen is built in React and grows with each template, so every slice ends with the user doing that template's tasks from the account screen.

| Feature | Content |
|---|---|
| **F00** | ADR + remove `Financial.App` (WPF), its tests, CI and nightly jobs, coverage entries and WPF-specific rules/docs; React becomes the only front end |
| **F01** | `Account` + capabilities + built-in templates (Bank first) + account groups + server-side summary query + generic account-to-account transfer; Bank migrated. **UI:** account list, account screen (header, transactions, actions from the API) for banks, including pay recurring bill; group summary |
| **F02** | Credit card template added and cards migrated (incl. PayPal); statement payment becomes a real bank → card transfer, uncategorised and linked to its statement (instead of re-pointing/re-dating each charge), with charges' categories still counting on the payment date; de-duplicate the card/period charge filter. **UI:** card actions on the account screen (statements, pay statement, move charge to a later statement) |
| **F03** | Pots template added and Reserva migrated; linked withdrawal via bank (today's records, written as one operation); linked deposit funded from one or more banks or none (automatic from a split-to-reserve income, or manual); "Reserva" category flag. **UI:** Pots on the account screen (pots, deposit, withdraw) |
| **F04** | Settlement template added with participants, explicit "paid by" and per-account currencies; Mãe migrated (BRL + GBP); the son's account becomes a create-only operation (GBP). **UI:** Settlement on the account screen (entries, balance in each currency, settle at any time) |
| **F05** | Consolidated navigation: the account list + overview screens become the CashFlow entry point; retire the per-type pages (Reserva, Controle Mãe, card/bank parts of Monthly); update E2E journeys and docs |

The user chose the full refactor over a lighter two-feature route (Settlement supporting multiple accounts + groups over existing entities), with the account-centred UI as the end goal, and decided to drop WPF so that UI is built once.

## Expected outcome

- A new account from an existing template (bank, card, Pots, Settlement) needs **no code**; it appears in the account list and in any group it is added to.
- New today: multiple Settlement and Pots accounts; custom groups with summaries.
- Already possible today and unchanged: new banks and cards.
- A new **template made only of existing capabilities** (e.g. a savings account = bank without categories) is one entry in the code's template table, with no new behaviour code; it could become user-defined later (see Future features).
- A new **capability** (e.g. loan interest) needs code: the capability, its tests, and its screens. It only adds transactions, so the shared balance calculation, groups and summaries pick it up without touching other capabilities.
- All existing moves keep working, covered by tests in each slice.
- A **new move between existing templates** (e.g. Settlement → Bank) becomes a transfer pair rule + a test, plus a one-off decision on what the move means:
  - **currency**: cross-currency moves need an FX rate and both amounts recorded;
  - **meaning**: e.g. whether "Mãe → Barclays" is only a transfer or also income / a category;
  - **card balances**: transfers into a card count as payments (handled once in F02).
- **One place per account**: the user selects an account and finds all its actions there, instead of options spread across pages and levels. A new account, or a newly allowed move, shows up on the account screen without UI changes.

### Code size (honest expectation)

Excluding F00, roughly the same amount of code overall, possibly slightly more during F01–F04 while old and new screens coexist. Removed: four separate balance calculations (replaced by one), name-string joins, fixed-name Reserva lookup, duplicated card filter, client-side summary maths, single-instance Mãe/Reserva code, and (in F05) the per-type pages. Added: `Account`, groups, summary query, capabilities and built-in templates, allowed-actions API, four migrations, OpenAPI changes, the account screen in React. F00 removes the whole WPF app, its tests and CI jobs: a large net reduction, and every later slice is about half the UI work. Per-capability behaviour (statements, splits, FX) stays: it is real domain complexity, now organised in one place per capability.

## Future features (not in this project)

- **Credit card partial payments, interest, fees and instalments.** Deferred: partial payments make "paid" no longer yes/no, need rules for which charges a payment covers and which date their categories count on, and bring interest, fees and carry-over between statements. Includes proper **instalment purchases** (e.g. PayPal Pay in 4 creating its 4 charges automatically).
- **Group settlements** (e.g. a family trip: 4 people, BRL + GBP + EUR): each expense shared among some participants, each person's balance kept **per currency without conversion**, and a final step suggesting the simplest set of payments to square everyone up. F04's explicit participants, "paid by" and currency are the foundation for it.
- **Recurring-bill defaults**: when a bill is paid, store the bank and category used and pre-fill them next time; if the user changes either, the bill remembers the new choice.
- **User-defined templates**: each account stores its own capability list, and the user creates and edits templates in the app. Not now, because it needs rules for which capability combinations are valid, a decision on template drift (do existing accounts follow template edits?), rules for changing capabilities on accounts with history, a combination test matrix, and roughly 1–2 extra features (template entity + editor). Upgrade path: move the template table from code into data and add a template editor; services do not change, because they already depend only on capabilities.

## Open points for the PRD

- How groups are managed (create/edit/reorder screens) and whether a default group reproduces today's main page.
- Account screen UX: layout of header/transactions/actions, how capability-specific panels (card statements, pots, balances in two currencies) sit inside the common shape.
- F00: confirm every Investment and CashFlow workflow exists in React before `Financial.App` is deleted, and the full list of files, CI jobs and docs to change.
- Where actions that span accounts start (e.g. adding income with a split to pots: from the bank, from the Pots account, or both).
- Whether old pages stay reachable during F01–F04 (coexistence) and the exact retirement list for F05.
- **Decided:** an income without a reserve split keeps the bank optional, as today. A manual deposit into the pots may have no bank (money from outside CashFlow, e.g. an investment).
- **Decided: migrations are forward-only.** Each migration makes the data correct for the new model from now on; past records keep working as they are today and are not rebuilt (e.g. paid card charges stay as bank expenses on their payment date; old Reserva deposits, withdrawal pairs and transfers stay unlinked; existing Mãe entries get "paid by" derived from their sign, and the migration must leave the Mãe BRL and GBP totals exactly as they are today, currently zero).
- **Resolved (PR #1014): Mensais "UK bill paid → expense" moved to the back end.** `MensaisService.MarkBillPaidAsync(id, RecurringBillMarkPaidDTO)` (`POST {id}/paid`) builds the bank expense and sets the bill to Paid inside one `ApplyAndSaveAsync`, so a half-done result is no longer possible and both UIs' retry paths are gone. The service rejects an expense for a non-UK bill or a bill already Paid. Both front ends (`MensaisViewModel.MarkUkBillPaidViaExpensePromptAsync`, `useMensais.markBillPaid`) now only collect the prompt values and make one call; skip sends no expense.
  - Still in both front ends, by design: the UI decision to *open* the prompt (`Area == "UK"` and moving to Paid), a string compare on the area. It is presentation flow, and the server enforces the rule. After F00 only the React copy remains.
  - Still open: `POST {id}/status` with `Paid` sets a UK bill Paid without going through `/paid`. Harmless today (equivalent to skip), but the PRD can decide whether `/status` should refuse Paid.
  - For F01: the expense's `PaymentSourceBankId` in `RecurringBillPaymentExpenseDTO` becomes the account reference when banks move to `Account`; the bank screen's "pay recurring bill" action reuses this endpoint.
- `context.md` and `data/data-cashflow.example.json` are stale (fixed 5 buckets; missing collections) and should be updated by the slices that touch them.
