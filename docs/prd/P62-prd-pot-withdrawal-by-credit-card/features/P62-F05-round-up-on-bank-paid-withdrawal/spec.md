# Spec: Round-Up on a Bank-Paid Pot Withdrawal

**Complexity:** small (one optional request/response field; the round-up set on an existing record; one form field). Two PRs: a refactor that moves the round-up suggestion into a shared web util, then F05 itself.

## 1. Technical Overview

**What:** A pot withdrawal paid from a bank writes, on that bank, a −X expense in the Pots account's category (money in from the pot) and a +X expense in the chosen category (the spend). Today the +X spend never carries a round-up, unlike a direct bank expense. F05 lets the withdrawal carry a round-up on that +X spend, calculated and editable exactly as on a direct bank expense.

**Why:** A bank spend paid out of a pot is still a bank spend; when the bank rounds up, the round-up leaves the bank like any other. Without it the bank balance drifts from the real statement.

**Scope:**

*Included*
- `PotWithdrawalRequestDTO.RoundUpAmount` (optional) and `PotOperationDTO.RoundUpAmount`.
- `PotWithdrawal.Build` sets the round-up on the +X spend through `Expense.SetRoundUpAmount` (its existing rules: bank-paid only, bank has round-up enabled, £0.00–£0.99).
- Create and edit of a withdrawal; the Withdraw form's "Round-up" field, suggested from the amount and editable, `0` allowed.

*Excluded*
- The card-paid withdrawal (its spend is a card charge; round-up never applies to card charges) and the −X records (negative values never round up).
- Any change to the pot movement or the −X: both stay at the main amount X.

## 2. Before you design (docs/rules/design.md)

1. **Where it belongs:** CashFlow. The round-up rule already lives on `Expense` (Domain); `PotWithdrawal.Build` (Domain) is where the bank spend is created, so it passes the round-up to it. `PotsService` (Application) only carries the field through.
2. **Layers touched:** Domain (`PotWithdrawal.Build`) → Application (DTOs, `PotsService`) → API contract (regenerated snapshot) → Web (form). Infrastructure is untouched: `Expense.RoundUpAmount` is already persisted.
3. **Domain stays free of Infrastructure:** nothing new crosses; `PotsService` keeps using `ICashFlowRepository` (Application) as today.
4. **SOLID:** Single responsibility: the round-up rule stays only in `Expense.SetRoundUpAmount`; `PotWithdrawal` reuses it rather than re-checking. Open/closed: the request field is optional, so existing callers and stored data are unchanged.

## 3. Technical Decisions

| Decision | Chosen | Alternative | Trade-off |
|---|---|---|---|
| Where the round-up is recorded | `RoundUpAmount` on the +X spend expense | A separate round-up record | Matches a direct bank expense exactly: the bank balance, transactions grid and month totals already understand `RoundUpAmount` |
| API default | `null` = no round-up | Server computes a suggestion when omitted | Keeps existing API callers and old withdrawals unchanged; the form sends the suggestion it showed, so the user always sees what is saved |
| Round-up with a card or no payment | 400 "Round-up only applies to a withdrawal paid from a bank." | Ignore silently | An ignored value would hide a client bug |
| Form behaviour | Same as the expense form: suggested from the amount while the user hasn't edited it; blank = none; `0` allowed; shown only for a bank with round-up enabled | Always show | Consistent with the existing round-up field |
| Where the web suggestion is calculated | One shared util, `Financial.Web/src/utils/roundUp.ts` (`computeRoundUpSuggestion`), used by the expense form and the Withdraw form | Keep it in `useExpenseForm.ts` and import it from there; or ask the server for the suggestion | The suggestion has to update live as the user types, so it stays client-side, mirroring `Expense.ComputeRoundUpSuggestion`; one copy instead of one form reaching into another form's hook. The server still validates whatever is sent |
| Edit of an old withdrawal (no round-up) | Opens blank; a suggestion appears only when the amount or bank changes | Suggest on open | Opening and saving an old withdrawal never adds a round-up the user didn't see change |

## 4. Component Overview

### PR 1: shared round-up util (refactor, no behaviour change)

| File Path | New/Modified | Responsibilities |
|---|---|---|
| `Financial.Web/src/utils/roundUp.ts` | New | `computeRoundUpSuggestion(value)` moved here unchanged |
| `Financial.Web/src/hooks/useExpenseForm.ts` | Modified | Imports it from the util; no longer exports it |
| `Financial.Web/src/utils/__tests__/roundUp.test.ts` | New (moved) | The existing `computeRoundUpSuggestion` cases, moved from `useExpenseForm.test.ts` |

### PR 2: F05

| File Path | New/Modified | Responsibilities |
|---|---|---|
| `Financial.CashFlow.Domain/Rules/PotWithdrawal.cs` | Modified | `Build(..., decimal? roundUpAmount = null)`: bank path calls `SetRoundUpAmount` on the +X spend; card / none path rejects a non-null round-up |
| `Financial.CashFlow.Application/DTOs/PotWithdrawalRequestDTO.cs` | Modified | `RoundUpAmount` (decimal?) |
| `Financial.CashFlow.Application/DTOs/PotOperationDTO.cs` | Modified | `RoundUpAmount` (decimal?, from the spend expense) |
| `Financial.CashFlow.Domain/Rules/PotOperation.cs` | Modified | `RoundUpAmount => ChosenExpense?.RoundUpAmount` (the chosen expense is private today) |
| `Financial.CashFlow.Application/Services/PotsService.cs` | Modified | Pass `request.RoundUpAmount` to `Build` on create and rebuild; map it to the DTO |
| `Financial.Web/src/hooks/usePotWithdrawalForm.ts` | Modified | `roundUpAmount` value + auto flag, suggestion via `utils/roundUp`'s `computeRoundUpSuggestion`, validation, request field, cleared when Paid with leaves Bank |
| `Financial.Web/src/components/PotWithdrawalForm.tsx` | Modified | "Round-up" field after Amount when Paid with = Bank and the bank has round-up enabled |

Plus the regenerated OpenAPI snapshot and web types, and `context.md`. PR 2 has 7 non-test code files; folding PR 1 in would make 9, over the limit of 8.

## 5. Rules in detail

- **Create / edit** (`POST /accounts/{id}/withdrawals`, `PUT /accounts/{id}/withdrawals/{operationId}`): `roundUpAmount` is optional.
  - With a bank: `null` → no round-up; a value → set on the +X spend. Bank without round-up → 400 "Bank '<name>' does not support round-up."; outside £0.00–£0.99 → 400 "Round-up amount must be between £0.00 and £0.99." (existing `Expense` messages).
  - With a card or no payment: a non-null value → 400 "Round-up only applies to a withdrawal paid from a bank.".
- **Amounts:** the pot movement and the −X stay at X; the +X spend is X with round-up R, so the bank goes down by R overall (−X in, X + R out), exactly as a direct expense of X with round-up R.
- **Read:** `PotOperationDTO.roundUpAmount` reports the stored value (null when none).

## 6. API Contract

Request adds `roundUpAmount` (number, optional, null). Response `PotOperationDTO` adds `roundUpAmount` (number?, null unless set). No data file change.

## 7. Web

- Field order: … Amount, then **Round-up**, shown only when Paid with = Bank and the chosen bank has round-up enabled (same `Input type="number" step="0.01"` as the expense form).
- New withdrawal: as the amount or bank changes, Round-up fills with the suggestion (`utils/roundUp`'s `computeRoundUpSuggestion`, the same one the expense form uses) until the user types in it; the user can change it or set `0`; blank saves no round-up.
- Edit: opens with the stored round-up (or blank); the suggestion follows amount/bank changes only while the field is untouched and was blank on open.
- Validation: outside £0.00–£0.99 → "Round-up amount must be between £0.00 and £0.99." on the field; the form sends `roundUpAmount` only for Paid with = Bank on a round-up bank.
- States: unchanged form states (saving, server error, unsaved changes); the field takes part in the dirty check.

## 8. Testing Strategy

| Test File | Type | Target |
|---|---|---|
| `Tests/Financial.CashFlow.Domain.Tests/Rules/PotWithdrawalTests.cs` | Unit | Round-up on the spend only; pot and −X stay X; rejected with a card / no payment; bank without round-up rejected |
| `Tests/Financial.CashFlow.Application.Tests/Services/PotsServiceTests.cs` | Unit | Create and edit carry the round-up; DTO reports it |
| `Tests/Financial.Api.Tests/Acceptance/PotWithdrawalRoundUpAcceptanceTests.cs` | Integration (new) | PRD criteria |
| `Financial.Web/src/utils/__tests__/roundUp.test.ts` | Unit (PR 1) | The moved suggestion cases; `useExpenseForm.test.ts` still covers the form's use of it |
| `Financial.Web/src/hooks/__tests__/usePotWithdrawalForm.test.ts`, `src/components/__tests__/PotWithdrawalForm.test.tsx` | Unit | Suggestion, edit to 0, blank, hidden for a non-round-up bank or a card, validation, request field, edit opening |

**Acceptance**

| Test | AC |
|---|---|
| `PotWithdrawalForm.test.tsx` "[AC …-01] suggests the round-up for a round-up bank and lets the user change it, including to 0" | P62-F05-round-up-on-bank-paid-withdrawal-01 |
| `Withdraw_FromRoundUpBank_RecordsRoundUpOnTheSpendOnly` | -02 |
| `Withdraw_RoundUpOnBankWithoutRoundUp_Returns400`, `Withdraw_RoundUpWithCard_Returns400` | -03 |
| `EditWithdrawal_ChangesTheRoundUp` | -04 |
