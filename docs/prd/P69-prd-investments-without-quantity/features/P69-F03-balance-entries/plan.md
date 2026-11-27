# Implementation Plan: Balance Entries

**Note:** `spec.md` §13 (review amendments) is binding and supersedes earlier spec sections where they differ; read it before each stage.

**Prerequisites:**
- F01 built and merged: its valuation policy, the asset's tracking-mode flag and the reshaped valuation calculator (F01 PRs 1 and 2) before Stage 1; the tree selection's tracking-mode flag (F01 PR 3) before Stage 3. Everything the spec marks "from F01".
- .NET 10 SDK and Node as pinned by the repo; `npm ci` in `Financial.Web` (run it in a Claude-created worktree before any web check).
- A branch per PR off `origin/main`, each started after the previous one merges. PRs 2 and 4 regenerate the OpenAPI snapshot (`UPDATE_OPENAPI_SNAPSHOT=1 dotnet test Tests/Financial.Api.Tests --filter "FullyQualifiedName~OpenApiContractTests"`) and the generated web types (`npm run generate-api-types`); if a wave-2 F07 contract PR merges first, rebase and regenerate rather than hand-merge.
- Read before coding: `docs/rules/implementation.md`, the `testing-guide-Financial` skill, and for Stages 3 and 4 `docs/rules/ui.md`, `docs/ui/forms-data-and-visualisations.md` and `docs/ui/review-checklist.md`.
- No code comments and no XML doc comments in new or changed code (`CLAUDE.md`), even where neighbouring files have them.
- Never run anything against `data/data-investment.json`; any data check uses a temp copy.

### Stage 1: Balance on a date and the 35-day status (PR 1)

**1. Balance flow abstraction** - Give every transaction type a balance effect, none for every existing type, and expose each transaction's balance flow and the signed net flows between an entry date and a date, as described in spec §5 PR 1 and §8.

**2. Valuation policy** - Extend F01's valuation policy so the position-total policy adds the net flows since the snapshot to its value and reports Current or Stale by the 35-day rule, while the per-unit policy keeps today's value and weekday rule.

**3. Calculators and services** - Make holding valuation, month-start values and the price lookup take value and status from the asset's policy, passing the net flows since the snapshot, with no balance branch of their own (spec §5 PR 1).

**4. PRD ids** - Add the `P69-F03-balance-entries-01` … `-09` ids to PRD §9 F03 in a docs commit.

### Stage 2: Balance entries through the price-history API (PR 2)

**5. Editable entries and entry rules** - Treat provider-valuation snapshots as investor-entered so they can be edited and deleted, and add the balance-only entry rules (decimals, note length) to the valuation policy, applied when the investor saves a value.

**6. Change since previous entry** - Add the Domain rule that computes each balance entry's change, change percent and net flows since the previous entry.

**7. Asset details contract** - Expose the tracking-mode flag and the per-entry change figures on the asset details response, filled only for balance holdings, then regenerate the OpenAPI snapshot and the generated web types and fix the fixtures the type check flags.

### Stage 3: "Balance history" tab (PR 3)

**8. Mode copy and rules** - Add the web helper holding the price-mode and balance-mode wording, the entry validation per mode and the same-date replace check (spec §9).

**9. Price history hook** - Carry the tracking mode, the note, the dirty state, the replace confirmation, the success toast and the balance-mode failure text through the price history hook.

**10. Tab and panel** - Relabel the tab and its content for balance holdings with the balance grid columns, note field, empty state, replace, discard and delete dialogs, and focus on the first invalid field, keeping price mode as it is apart from the discard prompt. Align the PRD F03 delete-failure wording with what shipped in a docs commit.

### Stage 4: Balance stale badge (PR 4)

**11. Holdings contract** - Expose the tracking-mode flag on the portfolio holdings item and regenerate the OpenAPI snapshot and generated web types.

**12. Stale badge text** - Show "Balance as of DD/MM/YYYY (N days ago)" as the stale badge's meaning on the Summary tab and in the portfolio and broker holdings grids for balance holdings, and update `context.md` with balance entries and the 35-day rule.
