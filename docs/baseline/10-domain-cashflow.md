# Domain: CashFlow Bounded Context

See legend in [README.md](README.md). Covers business capabilities implemented in `Financial.CashFlow.Domain`/`.Application`/`.Infrastructure`. No cross-references to the Investment context exist anywhere in this code (verified). Originates from the user's personal `Despesas.xlsx` spreadsheet; the current priority is reaching an MVP sufficient to retire that spreadsheet entirely.

## Bank Entity & Balance Management

**Purpose:** Track real-world bank accounts; compute a running monthly balance from linked incomes/expenses/transfers/adjustments.

**Entity:** `Bank` (Id, Name, RoundUpEnabled, OpeningBalance, OpeningBalanceDate).

**Rules:**
- Name required, non-blank; opening balance ≥ 0 — **CONFIRMED**.
- Balance = `OpeningBalance + income − (expense + roundUp) + transferIn − transferOut + adjustments`, windowed to `[OpeningBalanceDate, asOfDate]` — **CONFIRMED** for the round-up-subtraction piece (P13 AC); overall formula **OBSERVED**.
- Exactly 3 banks exist by design (Barclays, Trading212, Chase), seeded once by migration, no in-app bank-creation screen — **CONFIRMED** (P13 §6, explicit out-of-scope; no `POST /banks` exists).

**Application service:** `BankService`. **API:** `BanksController`. **WPF:** `MonthlyViewModel.BuildBankTotals`, `BanksGridView.xaml`. **Web:** `useBankOperations.ts`, `BanksGrid.tsx`.

**Note:** "Bank" here (CashFlow) is unrelated to "Broker" in the Investment context, despite both happening to use names like Trading212 and Chase for the same real-world accounts. No code coupling exists between them.

---

## Bank Round-Up

**Purpose (P13):** Model that some banks (Trading212, Chase) automatically round card payments up to the next whole £1, sweeping the difference to savings; Barclays does not.

**Rules — verified consistent across Domain, WPF, and Web:**
- Suggested round-up = `ceil(Value) − Value` — **CONFIRMED** (P13 AC: "£9.40 → suggests £0.60").
- Only settable on a positive-value, bank-paid expense where the bank has `RoundUpEnabled=true` — **CONFIRMED** (P13 §6 F02; rejects card-tagged expenses).
- Range `£0.00`–`£0.99` inclusive — **CONFIRMED**.
- "Sticky" — never auto-recalculated on value edit — **CONFIRMED** (P13 §6 F02, TfL provisional-charge rationale).
- Bank's round-up total shown separately; balance subtracts both `Value` and `RoundUpAmount` — **CONFIRMED** (P13 §9, all ACs complete).

No live bank-API sync — entirely user-entered/suggested (**CONFIRMED**, P13 §7 out-of-scope).

---

## Bank Transfers

**Purpose:** Record money moved between the user's own tracked banks.

**Entity:** `Transfer` (Id, Date, SourceBank, DestinationBank, Amount, Note).

**Rules:** Source ≠ destination, both required, amount > 0 — domain-validated.

**Known gap, accepted for now:** No overdraft/negative-balance check on transfers, unlike Reserve withdrawals (below). **Confirmed acceptable at this stage** — MVP priority is retiring the spreadsheet, not closing this gap.

**Application service:** `TransferService`. **API:** `TransfersController`. **WPF:** `BankOperationRow.cs`. **Web:** `TransferForm.tsx`, `useTransferForm.ts`.

---

## Balance Adjustment / Reconciliation

**Purpose:** Reconcile a bank's computed balance against a real-world value; system computes and stores the `Delta`.

**Entity:** `BalanceAdjustment` (Id, Date, Bank, TargetBalance, Delta, Note).

**Rules:** `TargetBalance ≥ 0` — **CONFIRMED**. `Delta` computed server-side, can be negative — **OBSERVED**.

**Application service:** `BalanceAdjustmentService` (depends on `BankService`). **API:** nested under `BanksController`. **WPF:** `BalanceAdjustmentFormView.xaml`. **Web:** `BalanceAdjustmentForm.tsx`.

---

## Credit Card Entity, Card Statements & Expense Settlement

**Purpose:** Track a small fixed set of credit cards and monthly statements aggregating a card's charges into a payable total, with an explicit paid/unpaid settlement transition — directly reflecting the spreadsheet's rule that a credit-card charge should not reduce the bank balance until the statement is paid.

**Entities:** `CreditCard` (Id, Name, IsActive, NextInvoiceDueDate — deliberately no Bank reference, P13 §7), `CardStatement` (Id, CreditCard ref, Year, Month, IsPaid).

**Rules:**
- An expense has exactly one of `PaymentSourceBank`/`CreditCard`, never both/neither — **CONFIRMED** (P12).
- An unsettled card charge doesn't count toward any bank balance until settled — **CONFIRMED** (P12).
- Marking a statement paid requires choosing which bank paid it; that bank + today's date cascades onto every matched charge — **CONFIRMED** ("auditable transition," P12).
- "Unmark paid" fully reverses the cascade — **CONFIRMED** (P12 closes this as a previously-missing gap).
- Statement↔charge matching is by `CreditCard.Id` + `InvoiceDate.Year/Month`, not a stored FK — **CONFIRMED** ("derive, don't store" pattern).
- `InvoiceDate` fixed at creation, never changes across Settle/Unsettle — **CONFIRMED**.

**Known defect:** Re-invoking mark-paid on an already-paid statement (or unmark-paid on an already-unpaid one) currently silently no-ops with no feedback. **Confirmed mistake — it should surface feedback to the user.** Not fixed in this documentation pass.

`OverdraftConfirmationRequiredException` belongs exclusively to Reserve-bucket withdrawals, not this capability.

**Application service:** `CardStatementService` (the one Application-layer class anywhere in the backend using `ILogger`). **API:** `CardStatementsController`, `CreditCardsController` (`GET`/`PUT` only — no create/delete). **WPF:** `CreditCardsGridView`, `CreditCardExpensesView.xaml`. **Web:** `CardsGrid.tsx`, `useCreditCards.ts`.

## Credit Card Invoice Dates (P25)

**UNKNOWN / not investigated in depth.** Mechanism (`CreditCard.NextInvoiceDueDate`, `Expense.InvoiceDate`/`ChargeDate`) confirmed to exist; specific P25 business rules not read.

---

## Expense Tracking

**Purpose:** Records any household spend, tagged to a Category and either a Bank or CreditCard.

**Entity:** `Expense` (Id, Date, Description, Value [can be negative — reimbursement], Category [required], PaymentSourceBank [nullable], CreditCard [nullable], ChargeDate/InvoiceDate [nullable, card-only], RoundUpAmount [nullable], CountsAsTithe [bool, default true]). Derived, not stored: `PaymentStatus`, `RoundUpSuggestion`, `IsInvestment` (delegates to `Category.IsInvestment`), `ReportingDate`.

**Rules:**
- Payment shape (exactly one of Bank/CreditCard) — **CONFIRMED** (see Credit Card section).
- Settled expenses' bank/card fields immutable except through unsettle — **CONFIRMED** (P12).
- New/updated expenses may only reference an *active* Category — **CONFIRMED** (P30-F02).
- `CountsAsTithe` defaults true, independently toggleable per-expense to exclude an offering from the tithe-paid total — **CONFIRMED** (P33-F02).

**Application service:** `ExpenseService`. **API:** `ExpensesController`. **WPF:** `ExpenseFormView.xaml`. **Web:** `ExpensesSection.tsx`, `ExpenseForm.tsx`, `useExpenseForm.ts`.

---

## Income & Income Sources

**Purpose:** Records income and its origin; feeds bank balances and tithe/annual-summary.

**Entities:** `Income` (Id, Date, IncomeSource ref, GrossValue [nullable], NetValue, Bank [nullable since P33-F01], Description [nullable, max 200 chars]), `IncomeSource` (Id, Name, IsActive, Group).

**Rules:**
- Bank optional; bank-less income excluded from bank balances but still contributes to the tithe base — **CONFIRMED** (P33-F01).
- `NetValue ≥ 0` — **CONFIRMED**.
- Submitted `IncomeSourceId` must resolve to a seeded source; inactive sources are still accepted — **CONFIRMED** (P26-F02).
- `IncomeSource` has no CRUD — seeded once, immutable except by direct JSON edit — **CONFIRMED** (P26).

**Reference storage pattern — CONFIRMED, project-wide standard:** all entity references (Category, Bank, IncomeSource, CreditCard, etc.) store a GUID in JSON and resolve to a full object reference when loaded in memory. (P26's original text describing `IncomeSource` as a bare string was superseded by this pattern; that PRD text is stale.)

**Application services:** `IncomeService` (full CRUD), `IncomeSourceService` (read-only). **API:** `IncomesController`, `IncomeSourcesController` (GET only). **WPF:** `IncomeSectionView`. **Web:** `IncomeSection.tsx`, `IncomeForm.tsx`.

---

## Recurring Bills ("Mensais")

**Purpose:** Tracks recurring monthly bills (UK + Brazil), mirroring the spreadsheet's "Mensais" tab.

**Entity:** `RecurringBill` (Id, DueDay [1–31], Description, Value, Area, Note, NitNumber [nullable, Brazil-only], MinimumWageValue [nullable], Status: Unset/Scheduled/Paid).

**Rules:**
- `NitNumber`/`MinimumWageValue` populated only by the spreadsheet importer, never via the app's own create endpoint — **CONFIRMED**.
- Three-state Status model matches the spreadsheet's flag column — **CONFIRMED**.
- `ResetAllToUnsetAsync` — **CONFIRMED purpose:** run once a month, at the start of the month, to reset all bills to unset before that month's payment tracking begins. (Previously undocumented; not a defect.)

**Application service:** `MensaisService`. **API:** `MensaisController`. **WPF:** `AddBillFormView.xaml`, `EditBillFormView.xaml`, `BillTableView.xaml`. **Web:** `useMensais.ts`.

---

## Categories

**Purpose:** Classifies expenses into 14 fixed household budget categories, with two flags (`IsInvestment`, `IsTithe`) driving downstream logic.

**Entity:** `Category` (Id, Name, Active, IsInvestment, IsTithe).

**Rules:**
- Exactly 14 categories seeded once, matching the spreadsheet verbatim: `Ariana, Carro, Casa, Estudo, Extras, Familia, Gleison, Mercado, Samuel, Saude, Viagem, Dizimo, Investimento, Reserva` — **CONFIRMED** (P30-F01). Only `Investimento` has `IsInvestment=true`; only `Dizimo` has `IsTithe=true`.
- No create/update/delete at any layer — **CONFIRMED** (P30, only `GET /categories` exists).
- An inactive category stays valid for historical references but is rejected for new expenses — **CONFIRMED** (P30-F02).

**"Viagem" naming — CONFIRMED, not a collision.** The `Viagem` category and the Reserve-context "Viagem" label (documented elsewhere as functioning like a general house/treats bucket, not travel-only) are the same thing — the old spreadsheet name carried into the new app unchanged; the spreadsheet itself still uses "Viagem" today.

**Application service:** `CategoryService` (read-only). **API:** `CategoriesController` (GET only).

---

## Tithe Calculation

**Purpose:** Computes 10% tithe on net income and how much has already been paid via Dizimo-categorized expenses. Computed on demand every request, never persisted.

**Ownership — CONFIRMED CashFlow-only.**

**Rules:**
- `CalculatedTithe` = 10% of the sum of **every** Income's NetValue for the month, regardless of source/bank — **CONFIRMED** (P33-F01: "has never filtered by bank and continues not to").
- `TitheBalance = CalculatedTithe − Σ(Expense.Value where Category.IsTithe && Expense.CountsAsTithe)` — **CONFIRMED** (P33-F02).
- Computed strictly per calendar month, no carry-over — **CONFIRMED**.

**Distinct from a separate reserve-splitting rule — CONFIRMED, clarified:** the overall tithe calculation is always 10% of *all* household net income (correct as implemented). Separately, the wife's income specifically is meant to be split into Reserve buckets **net of tithe** — i.e. bucket splitting for her income should use (net income − tithe), not raw net income. **UNKNOWN/needs verification:** whether `ReserveService.PostIncomeSplitAsync` currently applies this tithe deduction specifically for the wife's income source, or splits the full net amount for any income source uniformly. Flagged as an open verification item, not confirmed either way.

**Application service:** `TitheService`. **API:** `TitheController`.

---

## Reserve Buckets

**Purpose:** Split posted income across named percentage-based savings buckets, tracking a running per-bucket balance via signed movements.

**Entities:** `ReserveBucket` (Id, Name, IsActive, SplitPercentage), `ReserveMovement` (Id, Bucket ref, Amount [signed], Date, Description).

**Rules:**
- Each active bucket's split share = `Round(total × SplitPercentage/100, 2, AwayFromZero)` — **CONFIRMED** (P28).
- Buckets are seeded reference data, no CRUD API (`GET /reserve-buckets` only) — **CONFIRMED** (P28).
- A withdrawal exceeding a bucket's balance throws `OverdraftConfirmationRequiredException` (409) unless explicitly confirmed — **OBSERVED** (this is the confirmed home of that exception).
- Deleting one movement from an income split deletes the entire split group (all movements sharing the same Date+Description) — **OBSERVED**, explicit code comment confirms intentional.
- Default seeded buckets are `Investimento`, `HouseTreats`, `Ariana`, `Gleison`, sourced directly from the spreadsheet, which uses the same fixed set — **CONFIRMED, accepted as-is**. (P28 made buckets configurable entities rather than a fixed enum, but the 4 defaults match the spreadsheet's fixed list — this is fine, not a documentation defect requiring correction.)

**Known gap:** A non-blocking warning for when active bucket percentages don't sum to ~100% is a documented P28 objective but was not found implemented in `ReserveService`. **Confirmed missing** — it should be generated server-side and presented to the user in the UI. Not built as of this baseline.

**Known gap:** `data-cashflow.example.json` is missing its `ReserveBuckets` seed data. **Confirmed this is a defect** — the example file should include it, since reserve buckets are part of `data-cashflow.json`. A fresh copy of the example file currently cannot perform an income split (zero active buckets). Not fixed in this documentation pass.

**Application services:** `ReserveService`, `ReserveBucketService` (read-only). **API:** `POST /reserve/income-split`, `POST /reserve/withdrawals`, `GET /reserve/balances`, `GET /reserve/movements` (+ PUT/DELETE), `GET /reserve-buckets`. **WPF:** `EditReserveMovementFormView.xaml`, `ReservaViewModel` (dynamically loads buckets, P28-F07). **Web:** `ReservaPage.tsx`, `useReserva.ts` (dynamic since P28-F06).

---

## Controle Mãe (Household Loan Ledger with Mother)

**Purpose:** Track an informal, dual-currency personal loan/ledger between the user and their mother.

**Entity:** `MaeLedgerEntry` (Id, Date, Description, Note, SourceCurrency, BrlValue?, GbpValue?). **Value object:** `Currency` enum (BRL/GBP).

**Rules:**
- On creation, the non-source currency is auto-converted via a historical FX rate for that date (see [08-integrations.md](08-integrations.md)); a failed lookup leaves the converted value `null` — **OBSERVED**.
- Only BRL/GBP supported — **OBSERVED**.
- `UpdateEntryValuesAsync` allows directly overwriting both currency values post-creation, bypassing FX conversion — **OBSERVED**.

**Sign convention — CONFIRMED:** a **negative** value means a debt from the user (Gleison) to his mother; a **positive** value means the opposite (a debt from his mother to the user). This was previously undefined anywhere in the repository and is now settled.

**Application service:** `ControleMaeService` (depends on `IExchangeRateProvider`). **API:** `ControleMaeController`. **WPF:** `ControleMaeView.xaml`. **Web:** `ControleMaePage.tsx`, `useControleMae.ts`.

---

## CashFlow "Quick-Access" Investment Snapshots

**Purpose — CONFIRMED, clarified.** Manually tracked monthly point-in-time values for named "quick access" investment accounts — **genuinely different from the Investment bounded context**: these are quick-access funds, like emergency funds, deliberately not counted among the Investment context's long-term, not-quickly-accessible holdings. This is a real, intentional domain distinction, not an accidental naming overlap.

**Entities:** `InvestmentAccount` (Id, Name, IsActive, IsLiability, Aliases[]), `InvestmentSnapshot` (Id, Account ref, Year, Month, Value).

**Rules:**
- `GetSnapshotsForMonthAsync` auto-creates a zero-value snapshot for every in-scope account missing one for that month, as a side effect of a read-shaped call — **CONFIRMED intentional**.
- Account scope for **past** years is derived purely from having ≥1 persisted snapshot that year, independent of current active/disabled status; for the current/future year, every active account is in scope — **CONFIRMED** (explicit code comment).
- `IsLiability` accounts contribute negated to net position in Annual Summary — **OBSERVED**.

**Application services:** `InvestmentSnapshotService`, `InvestmentAccountService`. **API:** `GET/PUT /investment-snapshots/{year}/{month}`, `InvestmentAccountsController`. **WPF:** `EditSnapshotValueFormView.xaml`, `InvestmentSnapshotsViewModel`. **Web:** `InvestmentSnapshotsPage.tsx`, `useInvestmentSnapshots.ts`.

---

## Monthly Aggregation

**Purpose:** Single-month household cash-flow view.

**CONFIRMED — there is no `Monthly` Domain entity, no `MonthlyService`, no `/monthly` API endpoint.** "Monthly" is a **UI-layer-only composition**: `useMonthly.ts` (Web) separately calls Expenses/Incomes/Banks/CardStatements/Categories/Tithe endpoints and assembles one page-level state; WPF's `MonthlyViewModel` composes the equivalent sub-views in-process directly against the same Application services. It has no business rules of its own beyond whatever it composes.

---

## Annual Summary

**Purpose:** Server-computed yearly view of category totals, income summary, investment net-position, and historical multi-year averages. Confirmed CashFlow-only — no equivalent route exists on the Investment side.

**Value object:** `MonthlySeries` (12-element decimal array, `Sum()`/`Average()`/`DiffsFrom()`/`Add()`) — the one genuine Value Object identified in this bounded context.

**Rules:**
- Server-side computation, not client-side — **CONFIRMED** (P19).
- `Resultado = salaryAfterTaxes − totalDespesas + investimentoCategoryValue` — the Investimento category is added back because it's already counted once inside total expenses, and money moved into an investment account isn't consumption — **CONFIRMED**, explicit in code comment.
- Two parallel series per category/year (`Display` includes the current partial month; `ForAverage` excludes it) — **OBSERVED**, deliberate design to avoid skewing averages.
- Investment net-position averages/sums are never rounded (kept at full decimal precision), explicitly to stay byte-identical to a pre-refactor output — **OBSERVED, documented rationale**.
- 2017 uses 11 months instead of 12 for averages — **CONFIRMED, known historical data artifact, not a bug**: the user moved to the UK that month, and all GBP-denominated expense tracking starts from February 2017.

**Application service:** `AnnualSummaryService`. **API:** `GET /annual-summary/{year}/category-totals`, `.../investment-annual-result`, `.../historic-summary-averages`. **WPF:** `AnnualSummaryView.xaml`. **Web:** `AnnualSummaryPage.tsx`, `useAnnualSummary.ts`.

## Naming note (see also [09-domain-investment.md](09-domain-investment.md))

"Investment Account"/"Investment Snapshot" here refers exclusively to this CashFlow quick-access-funds concept, not the Investment bounded context's asset/price tracking. Both share vocabulary but are unrelated in code and in real-world meaning.
