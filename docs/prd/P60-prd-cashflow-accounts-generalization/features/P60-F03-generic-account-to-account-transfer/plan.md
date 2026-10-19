# Implementation Plan: Generic Account-to-Account Transfer

**Prerequisites:**
- .NET 10 SDK and Node, as for F02. No new packages.
- A fresh copy of the live `data-cashflow.json` (V2) downloaded from Google Drive, for the parity run at the end of each PR. The parity baseline is the `p60-baseline` tag.
- Each PR is implemented on its own branch off an up-to-date `main`. PR B starts only after PR A is merged.
- Strict no-comments policy in all code, XML docs included.

### Stage 1: PR A — Pair rules and the shared transfer rule set

**1. Pair rules table** - Add `TransferPairRules` in Domain `Rules/` with its default bank → bank pair, the `Allows` check and the destination and source lists. Write its contract tests first, including the extra-pair test. See spec §4 and §6.

**2. Transfer rules and destination amount** - Give `Transfer` the optional destination amount, the `today` parameter and the eleven rules in spec §6 order, including the same-currency rejection, and the edit-only-changed inactive check. Make `ContributionTo` credit the destination with its own amount. Extend the internal account test factory with an active flag, and update the Domain tests, the capability guard tests and the balance rule tests.

**3. Persistence** - Make the CashFlow resolver omit a null transfer destination amount, and add the persistence tests: a same-currency file is byte-identical after load and save, and a cross-currency key round-trips.

**4. `/transfers` on the shared rules** - Inject `TimeProvider` into `TransferService`, pass today, pass the stored destination amount on edit only when both accounts are unchanged, and keep the "Bank … was not found." lookup. Update the service and endpoint tests for the new messages, and add the AC 06 and AC 08 acceptance tests.

**5. PR A verification and hand-off** - Run the full .NET suite and confirm the OpenAPI snapshot is unchanged. Run `Invoke-Parity.ps1 -Step All` on the fresh live copy, expecting 0 differences and an unchanged SHA-256 for the copy. Run `/simplify` and the architecture-reviewer, then push the branch and open the PR, without merging.

### Stage 2: PR B — `accounts/transfers` endpoints

**6. Application service and DTOs** - Add the create, update, transfer and option DTOs, `IAccountTransferService` and `AccountTransferService` with the span/log pattern, and register it. Cover every branch, including the pair-not-allowed acceptance test (AC 02) on a minimal account with nothing saved. See spec §4 and §5.

**7. Controller and contract** - Add `AccountTransfersController` with the five routes in spec §5. Regenerate the OpenAPI snapshot and the web types, and check the diff contains only the new routes and schemas. Add the guard and empty-body tests.

**8. Acceptance tests** - Add the remaining tagged acceptance tests (ACs 01, 03, 04, 05) on seeded data with a GBP, a BRL and an inactive bank and a fixed clock, following spec §7.

**9. PR B verification, PRD ticks and hand-off** - Run the full .NET suite and the web build. Run the parity run on a fresh live copy, expecting 0 differences and an unchanged SHA-256. Run `/simplify` and the architecture-reviewer, tick the F03 acceptance criteria in the PRD in their own commit (no cross-feature boxes), then push the branch and open the PR, without merging.
