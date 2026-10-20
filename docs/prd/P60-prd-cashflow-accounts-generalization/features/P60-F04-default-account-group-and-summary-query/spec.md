# Technical Specification: Default Account Group and Summary Query

Complexity: **medium**. It touches Domain, Application, the API, the parity scripts and Monthly in the web app. There is no data-file change.

## 1. Technical Overview

**What.**
- A Domain rule, `DefaultAccountGroup`, works out the default group ("Main") for a month. Nothing is stored: members are the accounts whose template joins the default group, in file order, and an inactive account is included only in a month where it still matters.
- One endpoint, `GET accounts/groups/default/summary/{year}/{month}`, returns:
  - the members with their balances and round-up totals;
  - totals per currency;
  - the category totals sum;
  - income totals per income source and total incoming.
- Monthly's summary area reads that endpoint. It no longer sums bank balances, round-ups, category totals or income totals, and it matches banks by Id instead of name.
- The parity report gains the summary figures. `figure-map.json` gets its first entries, mapping them to the figures F01's script computes the way Monthly does.

**Why.**
- Monthly adds figures up in the browser and joins bank balances to banks by name (`useMonthly.ts:304–335`). The design wants that maths on the server.
- F05's Accounts page lists the default group with per-currency totals. F07 adds cards to the group and moves the card outstanding total into the summary.

**Delivery.** Two PRs, each leaving `main` deployable and each ending with a parity run showing 0 differences (review decision 6). Stage 1 has 13 non-test code files plus 3 parity script files. The user accepted that as one PR (interview Q5): 4 of the files are flat DTOs and 5 are small edits.

| PR | Content | Non-test code files |
|---|---|---|
| **A — summary endpoint** | Default-group rule; `Account.TemplateName` and `Account.JoinsDefaultGroup`; `Expense.ListingDate`; summary service, DTOs, controller and DI; OpenAPI snapshot and generated web types; parity summary figures and mappings | 13 + 3 parity scripts |
| **B — Monthly on the summary** | `useMonthly` reads the summary in its existing `Promise.all`; the bank, category and income grids show its figures; `TotalsGrid` empty message; API client and type aliases; ui-reviewer; PRD ticks | 9 |

**Scope.**

*Included:*
- PRD F04 Capabilities and Experience, and every Section 9 F04 criterion.
- Consumes from F02: the account model (`Account`, `AccountTemplates`, `Account.IsActive`, `Account.Currency`, `Capability.SupportsRoundUp`) and the shared balance calculation (`AccountBalanceRule` over `IAccountTransaction`).
- Provides to F05 and F07: the default group, through `DefaultAccountGroup`, and the summary endpoint.

*Not included:*
- The card outstanding total. It stays a browser sum (`adjustmentTotal`) until F07 makes cards accounts (review decision 2).
- Tithe, which keeps `tithe/month/{y}/{m}` (review decision 7).
- Stored or user-chosen groups and group screens (PRD Out of Scope).
- The per-category rows of Category totals and the expense and income lists, which keep their own endpoints (PRD Capabilities).

## 2. Architecture Impact

**Where does this belong?** CashFlow.
- **Domain:** group membership. Which accounts make up the default group for a month is a rule about accounts and their records, with no I/O.
- **Application:** composes the summary from the repository, the membership rule and the balance rule, and maps it to DTOs.
- **API:** routes only.
- **Web:** displays the figures.

**Layers and direction.**

```
Financial.Web            useMonthly ──► apiClient.getDefaultGroupSummary
                         BanksGrid / CategoryTotalsGrid / IncomingGrid ──► TotalsGrid (emptyMessage)
      │ HTTP
Financial.Api            AccountGroupsController (new)
      │
Financial.CashFlow.Application
      AccountGroupSummaryService (new) ──► DefaultAccountGroup.MembersFor
                                       ──► AccountBalanceRule.Balance
                                       ──► Expense.ListingDate / ReportingDate, Income
Financial.CashFlow.Domain
      DefaultAccountGroup (new, Rules/)   Account (+TemplateName, +JoinsDefaultGroup)
      AccountTemplates (+default-group set)   Expense (+ListingDate)
```

**What keeps Domain away from Infrastructure?** `ICashFlowRepository` is declared in Application and implemented in Infrastructure. The membership rule takes accounts and records as arguments and reads nothing itself.

**The template rule.** F02's architecture test lets only `Account`, `BankService` and `AccountJsonConverter` read `Account.Template` (`AccountTemplateReadRuleTests.cs:16`). The summary and the membership rule never read it. They use `Account.TemplateName` and `Account.JoinsDefaultGroup`, both computed inside `Account`, so the allow-list doesn't change (interview Q1).

**SOLID.**
- **Open/closed:** F07 makes cards join the group by adding Credit card to the default-group set in `AccountTemplates`, with no change to the rule or the service.
- **Single responsibility:**
  - `DefaultAccountGroup` owns membership only.
  - `AccountBalanceRule` owns balances.
  - The service owns composition and the span/log contract.
- **Interface segregation:** `IAccountGroupSummaryService` is its own interface. No existing service grows.
- **Dependency inversion:** the service depends only on `ICashFlowRepository`, `ITelemetryTracer` and `ILogger<T>`.

## 3. Technical Decisions

| Decision | Choice | Why |
|---|---|---|
| Group storage | None. `DefaultAccountGroup.MembersFor` derives the members on every call | Review decision 1: nothing edits the group before the future group screens |
| Who joins | Accounts with `Account.JoinsDefaultGroup`, which is true when the template is in `AccountTemplates`' default-group set (Bank in F04) | Membership comes from the template without code outside `Account` reading the template enum. F07 adds Credit card to the set. |
| Inactive accounts | Included in a month only when the balance at month end is not 0, or a record dated in that month has a non-zero contribution to the account | Review decision 1 (user): a closed bank with a zero balance drops out, while past months keep their totals, so F06's "figures stay unchanged" holds |
| Member order | File order (`ICashFlowRepository.GetAccounts()`) | Same as Monthly's bank rows today (`BankService.BankAccounts()`) |
| Template name | `Account.TemplateName => Template.ToString()`; the wire value is `"Bank"` | Interview Q1: `Account` is already on the allow-list |
| Route | `GET accounts/groups/default/summary/{year:int}/{month:int}` on a new `AccountGroupsController` (`[Route("accounts/groups")]`) | Review decision 1. A separate controller, so F05 and F06 never edit the same file. A route with an `{id}` comes with stored groups. |
| Invalid month | `new DateOnly` throws `ArgumentOutOfRangeException`, which maps to 400 | Same behaviour as `banks/month/{y}/{m}/balances` |
| Balance | `AccountBalanceRule.Balance(member, endOfMonth, records)`. The records are all incomes, expenses, transfers and balance adjustments, the same list `BankService.ReadAccountTransactions` builds | Must equal today's `banks/month/{y}/{m}/balances`. The four-line list is repeated in the new service, not shared: `BankService` is removed in F13. |
| Round-up per member | Sum of `RoundUpAmount ?? 0` over expenses whose `PaymentSourceAccount` is the member and whose `ListingDate` falls in the month; null when the member lacks `SupportsRoundUp` | Same expenses Monthly sums today (`getExpensesByMonth` plus `paymentSourceBankId`), and PRD "for accounts with `SupportsRoundUp`" |
| Listing date | `Expense.ListingDate => InvoiceDate ?? ChargeDate ?? Date`, moved from `ExpenseService`'s private `ListGroupingDate`, which now uses it | One definition of the month an expense is listed in |
| Totals per currency | One row per currency, in order of first appearance among the members: balance sum and round-up sum (null round-ups count as 0) | Review decisions 3 and 5 |
| Category totals sum | Sum of `Expense.Value` over expenses whose `ReportingDate` is in the month | The same rule as `expenses/month/{y}/{m}/category-totals`, which Monthly sums today |
| Income totals | Incomes dated in the month, grouped by `IncomeSource.Id` in order of each source's first income; net summed; gross summed, or null when no income in the group has a gross value; the source name for display | Review decision 4 |
| Total incoming | Sum of the income rows' net values | Same as Monthly's `totalIncoming` |
| Income currency | Not per currency | Incomes carry no currency today; their totals stay as Monthly shows them |
| Parity before/after | Only the "after" side fetches the summary. `Get-ParityReport` gains `-IncludeSummary`, which `Invoke-Parity.ps1` passes for the after side | The baseline build has no summary endpoint |
| Parity keys | `{month}/summary/members/<name>/balance` and `/roundUpTotal`, `{month}/summary/totals/<currency>/balance` and `/roundUpTotal`, `{month}/summary/categoryTotalsSum`, `{month}/summary/incomes/<name>/net` and `/gross`, `{month}/summary/totalIncoming` | Names match the existing before-keys, so a mapping only renames them |
| Monthly loading | The summary call replaces `getBankBalancesByMonth` in `fetchMonthlyData`'s `Promise.all` | Interview Q2: the page's loading, error-with-Retry and silent refresh cover it, and every save path already calls `refreshSilently` |
| Bank rows in Monthly | `bankTotals` is mapped from the summary members: `bankId` = member account Id, `bank` = name, `balance`, `roundUpTotal` (null shows as 0.00, as today) | `useBalanceAdjustmentForm` uses `bankId` and `balance` from these rows (`useBalanceAdjustmentForm.ts:42–43, 81, 185`) |
| Bank totals footer | One currency: "Bank Balance: X · Round-Up: Y", exactly as today. Several: one "Bank Balance <code>" and one "Round-Up <code>" pair per currency | Interview Q4 |
| Empty states | `TotalsGrid` gains `emptyMessage`, shown in its existing message cell when there are no rows and no filter applies. Banks: "No accounts in the default group for this month". Category totals: "No expenses recorded for this month". Incoming: "No income recorded for this month". Footers still show 0.00. | Interview Q3 |
| Data that stays in the browser | `state.banks`, `state.expenses` and `state.incomes` stay for forms, pickers and lists. `state.bankBalances` and its call go. | Only the summary figures move (PRD Capabilities) |
| Comments | Strict no-comments policy in all code, XML docs included; the new controller carries no XML docs, as in `AccountTransfersController` | CLAUDE.md |

## 4. Component Overview

Paths are relative to the repo root.

**PR A — summary endpoint**

| File | Change |
|---|---|
| `Financial.CashFlow.Domain/Rules/DefaultAccountGroup.cs` | **New.** `public static class DefaultAccountGroup` with `IReadOnlyList<Account> MembersFor(IEnumerable<Account> accounts, int year, int month, IReadOnlyCollection<IAccountTransaction> records)`. It applies the membership rule in §6. |
| `Financial.CashFlow.Domain/Rules/AccountTemplates.cs` | A frozen default-group template set containing `Bank`, and `bool JoinsDefaultGroup(AccountTemplate template)` |
| `Financial.CashFlow.Domain/Entities/Account.cs` | `string TemplateName => Template.ToString()`; `bool JoinsDefaultGroup => AccountTemplates.JoinsDefaultGroup(Template)` |
| `Financial.CashFlow.Domain/Entities/Transactions/Expense.cs` | `DateOnly ListingDate => InvoiceDate ?? ChargeDate ?? Date` |
| `Financial.CashFlow.Application/Services/ExpenseService.cs` | The private `ListGroupingDate` is removed and its uses read `expense.ListingDate` |
| `Financial.CashFlow.Application/DTOs/AccountGroupSummaryDTO.cs` | **New.** `Year`, `Month`, `IReadOnlyList<AccountGroupMemberDTO> Members`, `IReadOnlyList<CurrencyTotalDTO> Totals`, `CategoryTotalsSum`, `IReadOnlyList<IncomeSourceTotalDTO> IncomeTotals`, `TotalIncoming` |
| `Financial.CashFlow.Application/DTOs/AccountGroupMemberDTO.cs` | **New.** `AccountId`, `Name`, `TemplateName`, `Currency`, `Balance`, `decimal? RoundUpTotal` |
| `Financial.CashFlow.Application/DTOs/CurrencyTotalDTO.cs` | **New.** `Currency`, `Balance`, `RoundUpTotal` |
| `Financial.CashFlow.Application/DTOs/IncomeSourceTotalDTO.cs` | **New.** `IncomeSourceId`, `Name`, `NetValue`, `decimal? GrossValue` |
| `Financial.CashFlow.Application/Interfaces/IAccountGroupSummaryService.cs` | **New.** `AccountGroupSummaryDTO GetDefaultGroupSummary(int year, int month)` |
| `Financial.CashFlow.Application/Services/AccountGroupSummaryService.cs` | **New.** Same span/log/try pattern as `AccountTransferService` (`StartServiceSpan("CashFlow", nameof(AccountGroupSummaryService), "GetDefaultGroupSummary", "AccountGroup")`, `MarkSuccess`, `MarkFailed`). Read-only: it never calls `ApplyAndSaveAsync`. |
| `Financial.CashFlow.Application/DependencyInjection/CashFlowApplicationServiceCollectionExtensions.cs` | `AddSingleton<IAccountGroupSummaryService, AccountGroupSummaryService>()` |
| `Financial.Api/Controllers/AccountGroupsController.cs` | **New.** `[ApiController] [Route("accounts/groups")]`, `GET default/summary/{year:int}/{month:int}` returning 200 with `AccountGroupSummaryDTO` and declaring 400 |
| `scripts/parity/Parity.psm1` | `Add-ParitySummaryFigures` (the keys in §3), called from `Add-ParityMonthFigures` when `-IncludeSummary` is set; the switch on `Get-ParityReport` |
| `scripts/parity/Invoke-Parity.ps1` | Passes `-IncludeSummary` for the after side only |
| `scripts/parity/figure-map.json` | The mappings in §6 |
| `scripts/parity/README.md`, `scripts/parity/Parity.test.ps1` | The `summary` family row in the figure table; the family in `$figureFamilies`; a self-test case for the summary keys and the mappings (docs and tests, not counted) |
| `Tests/Financial.Api.Tests/Contract/openapi-v1.snapshot.json`, `Financial.Web/src/api/generated/openapi.ts` | Regenerated (generated files, not counted) |

**PR B — Monthly on the summary**

| File | Change |
|---|---|
| `Financial.Web/src/api/types.ts` | Aliases `AccountGroupSummaryDto`, `AccountGroupMemberDto`, `CurrencyTotalDto`, `IncomeSourceTotalDto` |
| `Financial.Web/src/api/financialApiClient.ts` | `getDefaultGroupSummary(year, month)` → `/accounts/groups/default/summary/${year}/${month}`. `getBankBalancesByMonth` is removed with its client test, because `useMonthly` is its only caller. The backend route stays: parity's before side and F13's retirement list still use it. |
| `Financial.Web/src/hooks/useMonthly.ts` | `summary` state replaces `bankBalances`; the call replaces `getBankBalancesByMonth` in the `Promise.all`; `bankTotals`, the per-currency bank and round-up totals, `categoryTotalsSum`, `incomeTotals` and `totalIncoming` are read from the summary; the browser sums for these are removed; `adjustmentTotal` stays |
| `Financial.Web/src/pages/MonthlyPage.tsx` | Passes the summary-backed values and the per-currency totals to the grids |
| `Financial.Web/src/components/TotalsGrid.tsx` | Optional `emptyMessage` prop, rendered in the existing message cell when `rows` is empty and `isFiltered` is false |
| `Financial.Web/src/components/BanksGrid.tsx` | Takes the per-currency totals; footer per §3; `rowKey` is the bank Id; its empty message |
| `Financial.Web/src/components/CategoryTotalsGrid.tsx` | Its empty message |
| `Financial.Web/src/components/IncomingGrid.tsx` | Rows from the summary's income totals (`rowKey` is the income source Id); its empty message |
| `Financial.Web/src/hooks/useBalanceAdjustmentForm.ts` | Only if the `BankTotal` type changes shape; otherwise untouched |

## 5. API Contracts

Base path `/api/v1/financial`. `ArgumentException` → 400 through the existing middleware.

**`GET accounts/groups/default/summary/{year}/{month}`** — 200 with `AccountGroupSummaryDTO`; 400 when the month is not 1–12. Read-only.

```json
{
  "year": 2026,
  "month": 9,
  "members": [
    { "accountId": "8f3b1c1a-2e3a-4b1a-9a7f-100000000001", "name": "Barclays", "templateName": "Bank", "currency": "GBP", "balance": 1520.40, "roundUpTotal": 0.17 },
    { "accountId": "8f3b1c1a-2e3a-4b1a-9a7f-100000000002", "name": "Chase", "templateName": "Bank", "currency": "GBP", "balance": 830.10, "roundUpTotal": 0.50 },
    { "accountId": "8f3b1c1a-2e3a-4b1a-9a7f-100000000004", "name": "Itaú", "templateName": "Bank", "currency": "BRL", "balance": 640.50, "roundUpTotal": 0.00 }
  ],
  "totals": [
    { "currency": "GBP", "balance": 2350.50, "roundUpTotal": 0.67 },
    { "currency": "BRL", "balance": 640.50, "roundUpTotal": 0.00 }
  ],
  "categoryTotalsSum": 1840.25,
  "incomeTotals": [
    { "incomeSourceId": "2a1d…", "name": "Salary", "netValue": 2000.00, "grossValue": 2500.00 },
    { "incomeSourceId": "7c4e…", "name": "Dividends", "netValue": 112.34, "grossValue": null }
  ],
  "totalIncoming": 2112.34
}
```

`roundUpTotal` on a member is null only for an account without `SupportsRoundUp`; a Bank always has it, so it is a number, 0.00 when nothing rounded up. The `totals` and `incomeTotals` lists can be empty; `members` is empty only when no account joins the group for that month.

No other route changes. `banks/month/{y}/{m}/balances`, `expenses/month/{y}/{m}/category-totals` and `incomes/month/{y}/{m}` keep their shapes.

## 6. Data Model and Business Rules

**Data file.** Unchanged; still version 2. Nothing new is stored.

**Default-group membership,** for a month with `start` = day 1 and `end` = its last day:

1. Take the accounts in file order with `JoinsDefaultGroup`.
2. Keep an account when `IsActive` is true.
3. Otherwise keep it when `AccountBalanceRule.Balance(account, end, records) != 0`, or some record with `start <= Date <= end` has `ContributionTo(account) != 0`.

**Summary composition.** For each member: the balance and the round-up as in §3. Then:
- the totals per currency;
- the category totals sum over all expenses with `ReportingDate` in the month (not only the members' expenses, as today);
- income totals and total incoming over all incomes dated in the month.

**Parity mappings** (`figure-map.json`). Each maps an after-key pattern to the existing before-key, `negate: false`, with reason "F04 group summary":

| after | before |
|---|---|
| `{month}/summary/members/*/balance` | `{month}/banks/*/balance` |
| `{month}/summary/members/*/roundUpTotal` | `{month}/banks/*/roundUpTotal` |
| `{month}/summary/totals/GBP/balance` | `{month}/monthly/bankTotalsSum` |
| `{month}/summary/totals/GBP/roundUpTotal` | `{month}/monthly/roundUpTotalsSum` |
| `{month}/summary/categoryTotalsSum` | `{month}/monthly/categoryTotalsSum` |
| `{month}/summary/incomes/*/net` | `{month}/incomes/*/net` |
| `{month}/summary/incomes/*/gross` | `{month}/incomes/*/gross` |
| `{month}/summary/totalIncoming` | `{month}/incomes/totalIncoming` |

Every live bank is GBP, so the GBP total maps to Monthly's single sum. A non-GBP total would have no before-key and show as a difference, which is correct: today's sum mixes currencies.

**Error handling.** The endpoint is read-only, so no write can fail. An invalid month returns 400. Any other exception marks the span failed and propagates (500 through the middleware). In Monthly, a failed summary call fails the page load like any of the other nine calls: "Unable to load Monthly data" with "Try again".

## 7. Testing Strategy

Following the `testing-guide-Financial` skill:
- **Unit:** Domain rules, the service and the web hooks and components.
- **Integration:** the API host via `ApiEndpointTests`, and Monthly's page with `financialApiClient` mocked.
- **AC tracing:** one tagged test per PRD F04 bullet (`[Trait("AC", "P60-F04-default-account-group-and-summary-query-NN")]`, numbered in PRD order 01–08). Web tests carry the tag in the test name, as the guide requires for Vitest.
- **Other rules:** every class carries `[Trait("Category", …)]`; names follow `Method_Condition_Result`; shared doubles only in `Tests/Financial.TestUtilities`; failure paths assert the failed span and the logged exception type.

| Test file | PR | Type | Target | Coverage goal |
|---|---|---|---|---|
| `Tests/Financial.CashFlow.Domain.Tests/Rules/DefaultAccountGroupTests.cs` | A | Unit | Membership | File order; active kept; inactive with zero balance and no records dropped; inactive kept for non-zero month-end balance; inactive kept for a record in the month; an account not joining the group never included |
| `Tests/Financial.CashFlow.Domain.Tests/Entities/AccountTests.cs` | A | Unit | `Account` | `TemplateName` is `"Bank"`; `JoinsDefaultGroup` true for Bank |
| `Tests/Financial.CashFlow.Domain.Tests/Entities/ExpenseTests.cs` | A | Unit | `Expense` | `ListingDate` prefers invoice, then charge, then date |
| `Tests/Financial.CashFlow.Application.Tests/Services/AccountGroupSummaryServiceTests.cs` | A | Unit | Service | Each figure; currency totals; null round-up for an account without `SupportsRoundUp`; gross null when no gross; empty month; invalid month throws; failed span on exception |
| `Tests/Financial.Api.Tests/Acceptance/DefaultAccountGroupSummaryAcceptanceTests.cs` | A | Integration (AC) | Host | ACs 01–05 against seeded data |
| `Tests/Financial.Api.Tests/ConstructorGuardTests.cs`, `OpenApiContractTests.cs` | A | Unit / Integration | New controller, snapshot | Picked up automatically; the snapshot diff adds only the route and four schemas |
| `Tests/Financial.Architecture.Tests/AccountTemplateReadRuleTests.cs` | A | Integration | Allow-list | Unchanged and still green |
| `scripts/parity/Parity.test.ps1` | A | Self-test | Parity | Summary keys built only with `-IncludeSummary`; the mappings resolve each to its before-key |
| `Financial.Web/src/hooks/__tests__/useMonthly.test.ts` | B | Unit | Hook | Summary call replaces bank balances; bank rows, totals, category sum and income rows come from the summary; no `getBankBalancesByMonth` call; silent refresh refetches the summary |
| `Financial.Web/src/pages/__tests__/MonthlyPage.test.tsx` | B | Integration (frontend) | Page | ACs 06 and 07: loading, error with Try again, empty messages; bank rows keyed by Id; balance adjustment pre-fill still works |
| `Financial.Web/src/components/__tests__/TotalsGrid.test.tsx`, `BanksGrid.test.tsx`, `CategoryTotalsGrid.test.tsx`, `IncomingGrid.test.tsx` | B | Unit | Grids | Empty message shown only with no rows and no filter; single-currency footer unchanged; multi-currency footer labelled |
| `Financial.Web/src/api/__tests__/financialApiClient.test.ts` | B | Unit | Client | URL and result of `getDefaultGroupSummary` |

The acceptance tests seed their own data with `ReplaceCashFlowDataBeforeStart`:
- banks: Barclays (GBP, round-up on), Chase (GBP), Itaú (BRL), an inactive GBP bank with a zero balance, and an inactive GBP bank with money left in it;
- records: expenses with round-ups, card charges, incomes from two sources (one with gross);
- clock: a `TestClock` fixed at 2026-10-15.

**Key functions:**

| Test function | AC | Assertions |
|---|---|---|
| `GetDefaultGroupSummary_SeededBanks_ListsActiveBanksInFileOrderPlusInactiveWithMoney` | 01 | Barclays, Chase, Itaú, then the inactive bank with money; the zero-balance inactive bank absent |
| `GetDefaultGroupSummary_InactiveBankWithRecordInMonth_IncludedThatMonthOnly` | 01 | Present in the month with the record, absent in a later month once its balance is 0 |
| `GetDefaultGroupSummary_BankCreatedThroughBanks_JoinsWithNoOtherChange` | 02 | `POST banks`, then the summary lists it last |
| `MembersFor_AccountNotJoiningTheGroup_IsNeverIncluded` | 02 | A test account whose template is outside the set (Pots and Settlement arrive in F09/F11) is excluded |
| `GetDefaultGroupSummary_AnyMonth_BalancesAndRoundUpsEqualTodaysEndpoints` | 03 | Each member's balance equals `banks/month/{y}/{m}/balances`, and its round-up equals the sum over `expenses/month/{y}/{m}`, for two months |
| `GetDefaultGroupSummary_MixedCurrencies_TotalsPerCurrencyNeverCombined` | 04 | GBP total = Barclays + Chase + inactive GBP; BRL total = Itaú; no total mixes them |
| `GetDefaultGroupSummary_SeededMonth_CategoryIncomeAndIncomingEqualTodaysFigures` | 05 | Category sum equals the sum of `category-totals`; income rows grouped by source Id in first-income order; gross null where none; total incoming equals the net sum |
| `useMonthly_summaryLoaded_readsFiguresWithoutSumming (AC P60-F04-…-06)` | 06 | Figures equal the summary's; one summary request and no `getBankBalancesByMonth` call; bank rows keyed by Id |
| `MonthlyPage_summaryStates_showLoadingErrorAndEmpty (AC P60-F04-…-07)` | 07 | Loading text; error with Try again calls retry; the three empty messages |

**Proved by the Definition of Done rather than a test:**
- **AC 03–05 on real data:** the PR A parity run shows 0 differences, with every summary figure mapped to its before-key, and the copy's SHA-256 unchanged. PR B repeats the run.
- **AC 06, "no client-side sums":** code review of `useMonthly.ts` in PR B. Monthly loads one summary request instead of the bank balances call, checked in the network panel at the parity screen check.
- **AC 08:** parity, `/simplify`, the architecture-reviewer (both PRs) and the ui-reviewer (PR B).

**Cross-feature criteria:** the three that name F04 also name F03, F05, F06 or F07, so none is ticked in F04. For the shared-balance criterion, PR A's AC 03 test proves F04's half.
