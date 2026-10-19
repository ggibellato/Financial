# Technical Specification: Generic Account-to-Account Transfer

Complexity: **medium**. It touches Domain, Application, Infrastructure (one serializer rule) and the API. There is no frontend change.

## 1. Technical Overview

**What.**
- The transfer pair rules become a Domain table, `TransferPairRules`. A source and destination are allowed when some pair in the table matches their capabilities. F03 has one pair: `CanSendTransfers` → `CanReceiveTransfers`.
- `Transfer` gains an optional `DestinationAmount`, stored only when the two accounts' currencies differ. It also takes on the full rule set: pair rules, different accounts, active accounts, amounts, currency, date not in the future, and note length.
- `/transfers` keeps its routes and DTO shapes, and goes through the same rules and messages, because they live in the entity.
- New endpoints under `accounts`, served by their own `AccountTransfersController`: create, edit and delete a transfer, and list the allowed destinations and sources for an account.
- The data file stays at version 2. Same-currency transfers store nothing new, so the file is byte-for-byte unchanged.

**Why.**
- F05's account screen needs "Transfer to…" and "Transfer from…" with lists that follow the pair rules.
- F07 (cards), F09 (pots) and F11 (settlement) each add templates; the pair table is where they decide which transfers are allowed.
- Cross-currency moves (GBP ↔ BRL) need both amounts recorded.

**Delivery.** Two PRs, each leaving `main` deployable and each ending with a parity run showing 0 differences. The split follows the review decision: the feature has 13 non-test code files (spec §4), over the 10-file single-PR limit agreed for F03.

| PR | Content | Non-test code files |
|---|---|---|
| **A — rules** | Pair rules table; `DestinationAmount` on `Transfer` and its serializer rule; the full rule set and messages in the entity; `/transfers` passes today's date and, when an edit keeps both accounts, the stored destination amount | 5 |
| **B — endpoints** | `accounts/transfers` POST, PUT, DELETE; `accounts/{id}/transfer-destinations` and `transfer-sources`; their service, DTOs and DI; OpenAPI snapshot and generated web types; PRD ticks | 8 |

**Scope.**

*Included:*
- PRD F03 Capabilities, Error Handling and every Section 9 F03 criterion.
- Consumes from F02: the account model and references (`Account`, `Capability`, `Account.Currency`, `Account.IsActive`) and the balance rule (`AccountBalanceRule`, through `IAccountTransaction.ContributionTo`).
- Provides to F05: the transfer operation (`accounts/transfers`) and the pair rules, exposed as `transfer-destinations` and `transfer-sources`.

*Not included:*
- Any web change. Monthly's transfer form keeps calling `/transfers`. The changed and new messages show in its general error banner instead of under a field, because `mapTransferErrorToField.ts` only maps today's wording. That form is removed in F13 (user decision, 2026-10-19).
- The FX rate. It is display-only (destination amount ÷ amount), and F05 computes it. It is never stored or returned.
- Deactivating an account (F06). F03 only honours `IsActive`.
- Bank → card statement payments (F08 adds that pair).

## 2. Architecture Impact

**Where does this belong?** CashFlow. The rules are Domain: which pairs are allowed, which amounts apply and which accounts may take part are invariants of a transfer, not workflow. Application orchestrates: it resolves Ids, supplies today's date, saves, and maps DTOs. The API only routes.

**Layers and direction.**

```
Financial.Api            AccountTransfersController (new), TransfersController (unchanged)
      │
Financial.CashFlow.Application
      AccountTransferService (new) ──┐
      TransferService (today's date) ─┤──► Transfer.Create / UpdateDetails
                                      └──► TransferPairRules.Default.DestinationsFor / SourcesFor
Financial.CashFlow.Domain
      TransferPairRules (new, Rules/)   Transfer (+DestinationAmount, full rule set)   Account
Financial.CashFlow.Infrastructure
      CashFlowTypeInfoResolver: omit Transfer.DestinationAmount when null
```

**What keeps Domain away from Infrastructure?** `ICashFlowRepository` is declared in Application, and Infrastructure implements it. Domain never reads the clock: the service passes `today` from the injected `TimeProvider` (`GetLocalToday()` in `Financial.Shared.Abstractions/Time/TimeProviderExtensions.cs`). The decision to omit a null `DestinationAmount` sits in the Infrastructure resolver, because Domain can't use `System.Text.Json` attributes.

**SOLID.**
- **Open/closed:** a new allowed pair is one table entry. No code in `Transfer`, the services or the controller changes, and the destination and source lists follow.
- **Single responsibility:** `TransferPairRules` owns only "which accounts may pair". `Transfer` owns a transfer's invariants. The services own orchestration and the span/log contract.
- **Interface segregation:** `IAccountTransferService` is its own interface. `ITransferService` isn't widened, so F13 can delete it whole.
- **Dependency inversion:** the services depend on `ICashFlowRepository` and `TimeProvider`, both abstractions.

## 3. Technical Decisions

| Decision | Choice | Why |
|---|---|---|
| Destination amount storage | `decimal? DestinationAmount`, stored only when the currencies differ; absent means equal to the amount | PRD review decision 1: derivable for same-currency transfers, so no migration; the file stays V2 |
| Destination amount sent for a same-currency pair | 400 `Destination amount only applies when the currencies differ.`, on create and edit, through both routes; nothing saved | Interview Q2 (revised by the user): reject rather than silently drop, so the client always knows exactly what was stored. On an edit that moves a transfer onto a same-currency pair, the client must clear the field. |
| Omitting the field on save | `CashFlowTypeInfoResolver` sets `ShouldSerialize` on `Transfer.DestinationAmount` only (written when not null) | Keeps same-currency files byte-identical. Other nullable properties such as `Note` keep writing `null` as today. |
| One rule set for both routes | All checks in `Transfer.Create` / `UpdateDetails` | PRD review decision 2 |
| Today's date | `Create` and `UpdateDetails` take a `today` parameter; services pass `TimeProvider.GetLocalToday()` | Domain can't read the clock; same message and source of today as `ControleMaeService` |
| Inactive accounts on edit | Rejected only when the edit moves the transfer onto a different account that is inactive; create always checks; delete never does | Interview Q3: old transfers on a later-deactivated account stay correctable |
| Pair rules shape | `TransferPairRules` instance holding `(Capability Source, Capability Destination)` pairs, with a static `Default`; `Transfer` uses `Default` | A test can build a table with an extra pair and see the lists change with no other code (PRD criterion) |
| Destination and source lists | On `TransferPairRules`: active accounts, not the account itself, allowed by the table, ordered by name | The list rule lives with the table, so the contract test covers it directly |
| `/transfers` "not found" message | Unchanged: `Bank '<id>' was not found.` | It comes from the service lookup, not the entity, and Monthly's form maps it to a field |
| `accounts/transfers` "not found" message | `Account '<id>' was not found.` (400), from `AccountTransferService` | Same behaviour as `/transfers`, with account wording |
| `/transfers` edit of a cross-currency transfer | `TransferService.UpdateTransferAsync` passes the stored `DestinationAmount` only when the edit keeps both accounts; otherwise it passes null | `/transfers` has no destination-amount field. Keeping both accounts lets Monthly correct the date, amount or note of a transfer created through `accounts/transfers`. Changing an account of a cross-currency transfer from Monthly gets 400 "Enter the amount received in <currency>.", which is correct: the new pair needs a new amount that Monthly can't send. Passing null when an account changes also avoids a false "only applies" error when Monthly moves it onto a same-currency pair. |
| `/transfers` DTOs | Unchanged; no destination amount | PRD: `/transfers` returns the same data; OpenAPI snapshot unchanged in PR A |
| New controller | `AccountTransfersController`, routes `accounts/transfers` and `accounts/{id:guid}/transfer-…` | PRD review decision 4: F04 and F06 add their own `accounts/…` controllers without editing this file |
| Create and update DTOs | Separate `AccountTransferCreateDTO` and `AccountTransferUpdateDTO` | Repository convention: every resource has separate create and update DTOs |
| `AccountTransferDTO.DestinationAmount` | Always filled with the effective value (the amount when absent) | The client never needs the storage rule; F05 shows both amounts and the rate from it |
| Account currency on the wire | Currency code string (`"GBP"`) | Same as other CashFlow DTOs (`MaeLedgerEntryDTO.SourceCurrency`) |
| Inactive test accounts | `Account.CreateWithCapabilities` gains an optional `isActive` parameter (internal, already visible to Domain and Application tests) | No deactivate method exists before F06; the existing internal test factory is the precedent |
| Pair-not-allowed acceptance tests | In `Application.Tests` with `StubCashFlowRepository` and a minimal account | Every account in a JSON file comes from the Bank template, which has both capabilities, so the API host can't load an account the pair rules refuse in F03. The PRD criterion itself says "a test account without the capability". |
| Comments | Strict no-comments policy in all code. New controllers carry no XML docs, as in `DashboardController` and `UpcomingIncomeController`; the OpenAPI snapshot holds no summaries. | CLAUDE.md |

## 4. Component Overview

Paths are relative to the repo root.

**PR A — rules**

| File | Change |
|---|---|
| `Financial.CashFlow.Domain/Rules/TransferPairRules.cs` | **New.** `public sealed class TransferPairRules`: constructor taking `IEnumerable<(Capability Source, Capability Destination)>`; `static Default` with `(CanSendTransfers, CanReceiveTransfers)`; `bool Allows(Account source, Account destination)`; `IReadOnlyList<Account> DestinationsFor(Account source, IEnumerable<Account> accounts)`; `IReadOnlyList<Account> SourcesFor(Account destination, IEnumerable<Account> accounts)` |
| `Financial.CashFlow.Domain/Entities/Transactions/Transfer.cs` | `decimal? DestinationAmount { get; private set; }`. `Create(date, source, destination, amount, destinationAmount, note, today)` and `UpdateDetails(…same…)`. The rule set in §6 replaces `Validate`. `ContributionTo` credits the destination with `DestinationAmount ?? Amount`. |
| `Financial.CashFlow.Domain/Entities/Account.cs` | `CreateWithCapabilities(name, currency, capabilities, bool isActive = true)` |
| `Financial.CashFlow.Application/Services/TransferService.cs` | Injects `TimeProvider`. Create passes `destinationAmount: null` and today. Update passes the stored `DestinationAmount` when both accounts are unchanged (otherwise null) and today. Lookup and messages otherwise unchanged. |
| `Financial.CashFlow.Infrastructure/Persistence/CashFlowTypeInfoResolver.cs` | For `(Transfer, DestinationAmount)`, set `ShouldSerialize = (_, value) => value is not null` |

**PR B — endpoints**

| File | Change |
|---|---|
| `Financial.CashFlow.Application/DTOs/AccountTransferCreateDTO.cs` | **New.** `Date`, `SourceAccountId`, `DestinationAccountId`, `Amount`, `decimal? DestinationAmount`, `string? Note` |
| `Financial.CashFlow.Application/DTOs/AccountTransferUpdateDTO.cs` | **New.** Same fields |
| `Financial.CashFlow.Application/DTOs/AccountTransferDTO.cs` | **New.** `Id`, `Date`, `SourceAccountId`, `SourceAccountName`, `SourceCurrency`, `DestinationAccountId`, `DestinationAccountName`, `DestinationCurrency`, `Amount`, `DestinationAmount`, `Note` |
| `Financial.CashFlow.Application/DTOs/TransferAccountOptionDTO.cs` | **New.** `Id`, `Name`, `Currency` |
| `Financial.CashFlow.Application/Interfaces/IAccountTransferService.cs` | **New.** `AddAsync`, `UpdateAsync`, `DeleteAsync`, `GetDestinations(Guid accountId)`, `GetSources(Guid accountId)` |
| `Financial.CashFlow.Application/Services/AccountTransferService.cs` | **New.** Same span/log/try pattern as `TransferService` (`StartServiceSpan`, `MarkSuccess`, `MarkFailed`); saves only through `ApplyAndSaveAsync`; today from `TimeProvider` |
| `Financial.CashFlow.Application/DependencyInjection/CashFlowApplicationServiceCollectionExtensions.cs` | `AddSingleton<IAccountTransferService, AccountTransferService>()` |
| `Financial.Api/Controllers/AccountTransfersController.cs` | **New.** The five routes in §5; null-body guard returns `BadRequest()` as `TransfersController` does |
| `Tests/Financial.Api.Tests/Contract/openapi-v1.snapshot.json`, `Financial.Web/src/api/generated/openapi.ts` | Regenerated (generated files, not counted) |

## 5. API Contracts

Base path `/api/v1/financial`. `ArgumentException` → 400 and `KeyNotFoundException` → 404, through the existing middleware.

**`POST accounts/transfers`** — 200 with `AccountTransferDTO`; 400 for a missing body or a broken rule (§6); nothing saved on 400.

```json
{
  "date": "2026-10-12",
  "sourceAccountId": "8f3b1c1a-2e3a-4b1a-9a7f-100000000001",
  "destinationAccountId": "8f3b1c1a-2e3a-4b1a-9a7f-100000000004",
  "amount": 100.00,
  "destinationAmount": 640.50,
  "note": "Send to Itaú"
}
```

```json
{
  "id": "5b0c…",
  "date": "2026-10-12",
  "sourceAccountId": "8f3b1c1a-2e3a-4b1a-9a7f-100000000001",
  "sourceAccountName": "Barclays",
  "sourceCurrency": "GBP",
  "destinationAccountId": "8f3b1c1a-2e3a-4b1a-9a7f-100000000004",
  "destinationAccountName": "Itaú",
  "destinationCurrency": "BRL",
  "amount": 100.00,
  "destinationAmount": 640.50,
  "note": "Send to Itaú"
}
```

`destinationAmount` must be absent (or null) for a same-currency pair and present for a cross-currency one; anything else is a 400 (§6 rules 7–9). For a same-currency pair the response's `destinationAmount` equals `amount`.

The two currencies may produce any positive rate, including 1.00: `amount: 100` with `destinationAmount: 100` from Barclays (GBP) to Itaú (BRL) is valid and stores 100 as the destination amount, because the currencies differ. No plausibility check is made on the rate; F05 shows it next to the amounts.

**`PUT accounts/transfers/{id}`** — same body as POST. 200 with `AccountTransferDTO`; 400 as above; 404 when the transfer doesn't exist.

**`DELETE accounts/transfers/{id}`** — 200; 404 when the transfer doesn't exist. No account checks.

**`GET accounts/{id}/transfer-destinations`** — 200 with the accounts the given account may send to; 404 when the account doesn't exist. An account that can't send gets an empty list.

```json
[
  { "id": "8f3b1c1a-2e3a-4b1a-9a7f-100000000002", "name": "Chase", "currency": "GBP" },
  { "id": "8f3b1c1a-2e3a-4b1a-9a7f-100000000004", "name": "Itaú", "currency": "BRL" }
]
```

**`GET accounts/{id}/transfer-sources`** — the same for the receiving side.

**`/transfers`** — routes and DTOs unchanged. Behaviour changes are listed in §6.

## 6. Data Model and Business Rules

**Data file.** The version stays 2. A transfer gets one optional key, written only when the currencies differ:

```json
{ "Id": "…", "Date": "2026-10-12", "SourceAccountId": "…", "DestinationAccountId": "…", "Amount": 100.00, "Note": "Send to Itaú", "DestinationAmount": 640.50 }
```

The new key follows `Note`, in declaration order. A missing key loads as null. Existing transfers have no key, so loading and saving them is byte-for-byte unchanged.

**Pair rules.** `Allows(source, destination)` is true when some pair has `source.Has(pair.Source)` and `destination.Has(pair.Destination)`. `DestinationsFor(source, accounts)` returns accounts that are active, aren't `source`, and pass `Allows(source, a)`, ordered by name (ordinal, ignoring case). `SourcesFor` is the mirror image.

**Transfer rules,** checked in this order (the first one that fails throws `ArgumentException`):

| # | Rule | Message |
|---|---|---|
| 1 | Source and destination present | `Source account is required.` / `Destination account is required.` |
| 2 | Different accounts | `Source and destination must be different accounts.` |
| 3 | Pair allowed by `TransferPairRules.Default` | `Transfers from <source name> to <destination name> are not allowed.` |
| 4 | Source active (create; edit only when the source changes) | `<source name> is inactive.` |
| 5 | Destination active (create; edit only when the destination changes) | `<destination name> is inactive.` |
| 6 | Amount > 0 | `Transfer amount must be greater than zero.` |
| 7 | Currencies differ → destination amount present | `Enter the amount received in <destination currency code>.` |
| 8 | Currencies differ → destination amount > 0 | `Destination amount must be greater than zero.` |
| 9 | Currencies match → no destination amount | `Destination amount only applies when the currencies differ.` |
| 10 | Date ≤ today | `Date must not be in the future.` |
| 11 | Note ≤ 200 characters (`string.Length`; null allowed) | `Note must be 200 characters or fewer.` |

`DestinationAmount` is therefore stored exactly when the currencies differ. An edit that moves a transfer onto a same-currency pair must send no destination amount, and the stored one is cleared.

**Balance contribution.** `ContributionTo(account)` = (`DestinationAmount ?? Amount` if the account is the destination) − (`Amount` if the account is the source). `AccountBalanceRule` is unchanged.

**What changes for `/transfers` callers** (Monthly's form shows each of these in its general error banner):

| Today | From F03 PR A |
|---|---|
| `A transfer must move money between two different banks.` | `Source and destination must be different accounts.` |
| `Account '<name>' cannot send transfers.` / `cannot receive transfers.` | `Transfers from <source> to <destination> are not allowed.` |
| `Source bank is required.` / `Destination bank is required.` | `Source account is required.` / `Destination account is required.` (not reachable through the API) |
| — | `<account> is inactive.` |
| — | `Date must not be in the future.` |
| — | `Note must be 200 characters or fewer.` |
| — | `Enter the amount received in <currency>.` (only for a cross-currency pair, which no existing bank pair is) |

Unchanged: `Bank '<id>' was not found.` and `Transfer amount must be greater than zero.`, both still mapped to fields by Monthly's form.

**Error handling (PRD).**
- Pair not allowed, same account, missing destination amount, inactive account: 400 with the messages above; nothing saved, because the entity throws before `ApplyAndSaveAsync` writes.
- Save failure: `ApplyAndSaveAsync` makes a single save, so nothing is persisted. The span is marked failed and the exception propagates (500 through the middleware).

## 7. Testing Strategy

Following the `testing-guide-Financial` skill:
- **Unit:** Domain and Application.
- **Integration:** persistence and the API host via `ApiEndpointTests`.
- **AC tracing:** one tagged test per PRD F03 bullet (`[Trait("AC", "P60-F03-generic-account-to-account-transfer-NN")]`), numbered in PRD order 01–10.
- **Other rules:** every class carries `[Trait("Category", …)]`; names follow `Method_Condition_Result`; shared doubles only in `Tests/Financial.TestUtilities` (`StubCashFlowRepository`, `RecordingTelemetryTracer`, `RecordingLogger<T>`); the clock is `FakeTimeProvider`, as in the existing acceptance tests; failure paths assert the failed span and the logged exception type, never a message in a log.

| Test file | PR | Type | Target | Coverage goal |
|---|---|---|---|---|
| `Tests/Financial.CashFlow.Domain.Tests/Rules/TransferPairRulesTests.cs` | A | Unit | Pair table | Default table; an extra pair changes `DestinationsFor`/`SourcesFor`; inactive and self excluded; ordering |
| `Tests/Financial.CashFlow.Domain.Tests/Entities/TransferTests.cs` | A | Unit | `Transfer` | Each of the 11 rules, the boundaries (amount 0, note 200/201, date today/tomorrow), the same-currency rejection, the edit-only-changed inactive rule, `ContributionTo` with and without a destination amount |
| `Tests/Financial.CashFlow.Domain.Tests/Entities/CapabilityGuardTests.cs` | A | Unit | Transfer guards | Updated to the pair message |
| `Tests/Financial.CashFlow.Domain.Tests/Rules/AccountBalanceRuleTests.cs` | A | Unit | Balance | A cross-currency transfer moves the destination by its own amount |
| `Tests/Financial.CashFlow.Application.Tests/Services/TransferServiceTests.cs` | A | Unit | `TransferService` | `FakeTimeProvider` today; stored destination amount kept on edit; pair-not-allowed rejected with nothing saved; failed span on each rejection |
| `Tests/Financial.CashFlow.Infrastructure.Tests/Persistence/TransferDestinationAmountPersistenceTests.cs` | A | Integration | Resolver and serializer | Same-currency file byte-identical after load and save; cross-currency key written and read back; missing key loads as null |
| `Tests/Financial.Api.Tests/TransfersEndpointsTests.cs` | A | Integration | `/transfers` | Existing tests updated for the new messages |
| `Tests/Financial.Api.Tests/Acceptance/GenericAccountTransferAcceptanceTests.cs` | A, B | Integration (AC) | Host | ACs 01, 03, 04, 05, 06 and 08 (date, note, inactive). PR A adds 06 and 08; PR B the rest. |
| `Tests/Financial.CashFlow.Application.Tests/Services/AccountTransferServiceTests.cs` | B | Unit | `AccountTransferService` | Each branch; Account-not-found; 404 for unknown transfer or account; pair-not-allowed with nothing saved; failed spans |
| `Tests/Financial.Api.Tests/ConstructorGuardTests.cs`, `EmptyBodyEndpointsTests.cs` | B | Unit / Integration | New controller | Null service guard; empty body on POST and PUT returns 400 |
| `Tests/Financial.Api.Tests/OpenApiContractTests.cs` | B | Integration | Snapshot | Only the five new routes and four new DTO schemas added |

The acceptance tests seed their own data with `ReplaceCashFlowDataBeforeStart`: Barclays (GBP), Chase (GBP), Itaú (BRL), an inactive GBP bank, and one existing Barclays → Chase transfer. The clock is a `FakeTimeProvider` fixed at 2026-10-15.

**Key functions:**

| Test function | AC | Assertions |
|---|---|---|
| `PostAccountTransfer_BankToBank_MovesBothBalancesOnItsDate` | 01 | `/banks/month/2026/10/balances`: Barclays −100 and Chase +100; September unchanged |
| `AddAsync_SourceWithoutCanSendTransfers_RejectedAndNothingSaved` / `…DestinationWithoutCanReceiveTransfers…` | 02 | `ArgumentException` with the pair message; stub repository has no transfer; failed span |
| `PostAccountTransfer_SameAccount_Returns400` | 03 | 400 with the PRD message; transfer list unchanged |
| `PostAccountTransfer_DifferentCurrenciesWithoutDestinationAmount_Returns400` | 04 | 400 `Enter the amount received in BRL.`; nothing saved |
| `PostAccountTransfer_DifferentCurrenciesWithDestinationAmount_EachSideMovesByItsOwnAmount` | 04 | Barclays −100 GBP; Itaú +640.50 BRL |
| `GetTransferDestinations_Bank_ListsOtherActiveBanksOnly` / `GetTransferSources_Bank_ListsOtherActiveBanksOnly` | 05 | Chase and Itaú; not Barclays, not the inactive bank; ordered by name |
| `GetTransfersByMonth_ExistingTransfer_ReturnsSameDataAsBefore` | 06 | Response JSON equals the pre-F03 expected JSON |
| `LoadAndSave_SameCurrencyTransfersOnly_FileIsByteIdentical` | 07 | Bytes equal; no `DestinationAmount` key |
| `PostTransfers_FutureDate_Returns400WithSameMessage`, `…NoteOver200…`, `…InactiveAccount…` | 08 | 400 with the same messages as `accounts/transfers`; nothing saved |
| `AddTransferAsync_PairNotAllowed_RejectedWithSameMessage` | 08 (pair) | `TransferService` throws the pair message on a minimal account |
| `DestinationsFor_TableWithExtraPair_IncludesNewlyAllowedAccount` | 09 | Built with an extra pair; an account with only that pair's capability appears; with `Default` it doesn't |
| `Allows_DefaultTable_BankToBankOnly` | 09 | Exact default pairs |
| `UpdateDetails_UnchangedInactiveAccount_Succeeds` / `UpdateDetails_SwitchToInactiveAccount_Throws` | Q3 | As named |
| `Create_SameCurrencyWithDestinationAmount_Throws` | Q2 | `ArgumentException` with the "only applies" message |
| `PostAccountTransfer_SameCurrencyWithDestinationAmount_Returns400` | Q2 | 400 with the same message; nothing saved |
| `Create_CrossCurrencyEqualAmounts_StoresDestinationAmount` | §5 | Barclays → Itaú, 100 and 100: `DestinationAmount` is 100; Itaú moves by 100 BRL |
| `UpdateTransferAsync_AccountsUnchanged_KeepsStoredDestinationAmount` / `…AccountChanged_PassesNone` | §3 | As named |

**Proved by the Definition of Done rather than a test:**
- **AC 06 and 07 on real data:** the parity run on the latest live copy shows 0 differences after PR A and after PR B, and the copy's SHA-256 is unchanged.
- **AC 10:** parity, `/simplify` and the architecture-reviewer, on each PR.

**Cross-feature criteria:** the two that name F03 also name F04, F05, F06 or F07, so neither is ticked in F03.
