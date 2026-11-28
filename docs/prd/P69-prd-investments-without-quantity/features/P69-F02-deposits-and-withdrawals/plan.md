# Implementation Plan: Deposits and Withdrawals

**Note:** `spec.md` §13 (review amendments) is binding and supersedes earlier spec sections where they differ; read it before each stage.

**Prerequisites:**
- F01 built and merged: the asset's tracking-mode flag and valuation policy (F01 PR 2) before Stage 1, and the tree selection's tracking-mode flag (F01 PR 3) before Stage 3.
- F03 built and merged: balance flows, the flow calculator and balance on a date (F03 PR 1) before Stage 1, the asset-details tracking flag and the history's net-flows field (F03 PR 2) before Stage 2, and the "Balance history" tab label (F03 PR 3) before Stage 3. F03 `spec.md` §13 is binding. If the built F01/F03 code differs from their specs, follow the code and record the difference in the PR's docs commit.
- .NET 10 SDK and Node as pinned by the repo; `npm ci` in `Financial.Web` (run it in a Claude-created worktree before any web check).
- A branch per PR off `origin/main`, each started after the previous one merges. PRs 2 and 4 regenerate the OpenAPI snapshot (`UPDATE_OPENAPI_SNAPSHOT=1 dotnet test Tests/Financial.Api.Tests --filter "FullyQualifiedName~OpenApiContractTests"`) and the generated web types (`npm run generate-api-types`). F08 is built in the same wave, so if one of its contract PRs merges first, rebase and regenerate rather than hand-merge.
- Read before coding: `docs/rules/implementation.md`, the `testing-guide-Financial` skill, and for Stages 3 and 5 `docs/rules/ui.md`, `docs/ui/forms-data-and-visualisations.md` and `docs/ui/review-checklist.md`.
- No code comments and no XML doc comments in new or changed code (`CLAUDE.md`), except the `Financial.Api` controller action's OpenAPI docs in Stage 4.
- Never run anything against `data/data-investment.json`; data checks and browser passes use a temp copy.

### Stage 1: Deposit and Withdrawal types with proportional cost (PR 1)

**1. Transaction types and amounts** - Add the Deposit and Withdrawal types as cash-only types that carry an amount, store the amount and the balance before on the transaction with their limits and the not-in-the-future rule, and make the transaction's gross amount read the stored amount, as described in spec §5 PR 1 and §8.

**2. Proportional cost rule** - Add the Domain rule that replays a balance holding's flows into its invested amount, works out each withdrawal's cost removed and builds its realised-gain record, and put withdrawals after same-date deposits in the replay order.

**3. Realised gains and tax** - Choose the realised-gain calculator by transaction type in both the recording path and the regeneration, and classify a balance holding's realised gains with no tax rule.

**4. Holding invariants** - Make a balance holding refuse every other transaction type, credits and corporate actions, make a quantity holding refuse deposits and withdrawals, refuse any change that leaves a withdrawal without a deposit before it, and derive the remaining invested amount and the open or closed position for balance holdings.

**5. PRD ids** - Add the `P69-F02-deposits-and-withdrawals-01` … `-18`, `P69-CF-cross-feature-01` and `-03` ids to the PRD in a docs commit.

### Stage 2: Transaction API carries amounts and realised gains (PR 2)

**6. Request and response contract** - Carry the amount and balance before on transaction create and update, return them with each withdrawal's gross and net realised gain on the transaction response, and let the transaction service pass local today to the balance-flow factory. Regenerate the OpenAPI snapshot and the generated web types.

**7. Archive gate** - Refuse to archive a balance holding that still has money invested, using its position rather than its quantity.

### Stage 3: Balance Transactions tab (PR 3)

**8. Copy and validation** - Add the web helper holding the deposit and withdrawal wording, the field validation and the amount limit (spec §9).

**9. Transactions hook and tab** - Add the hook that loads the holding's flows, keeps the form state, validates on blur and submit, saves with a success toast, protects unsaved changes and deletes. Add the tab and its form with the grid, empty state, dialogs and focus behaviour. Register both components for mutation testing.

**10. Panel and realised gains** - Show the balance Transactions tab for balance holdings, stop offering the Credits and Corporate actions tabs for them, and show "—" for quantity and "Proportional cost" for their realised gains. Align the PRD F02 Experience wording with what shipped in a docs commit.

### Stage 4: Withdrawal preview endpoint (PR 4)

**11. Domain preview** - Add the rule for a withdrawal's invested amount at its position and the holding's withdrawal preview: balance before from the latest balance entry plus the other flows since, and the cost removed and gains when the inputs are valid.

**12. Preview use case and endpoint** - Expose the preview through the transaction query service and a new read-only POST endpoint on the transactions controller. Regenerate the OpenAPI snapshot and the generated web types.

### Stage 5: Pre-fill and live preview (PR 5)

**13. Client and preview hook** - Add the preview call to the web API client and a debounced preview hook that ignores stale responses.

**14. Form pre-fill and preview line** - Pre-fill the balance before from the preview until the user types in it, show the hint for each pre-fill state, and show the live cost-removed and gain line. Update `context.md`, then tick the F02 PRD boxes in the PR's last commit.
