# Implementation Plan: Balance-Tracked Holding Option

**Prerequisites:**
- .NET 10 SDK and Node as pinned by the repo; `npm ci` in `Financial.Web` (run it in a Claude-created worktree before any web check).
- A branch per PR off `origin/main`; PR 2 starts after PR 1 merges, PR 3 after PR 2 (each regenerates the same OpenAPI snapshot).
- Read before coding: `docs/rules/implementation.md`, the `testing-guide-Financial` skill, and for PRs 2 and 3 `docs/rules/ui.md`, `docs/ui/forms-data-and-visualisations.md` and `docs/ui/review-checklist.md`.
- No code comments and no XML doc comments in new or changed code (`CLAUDE.md`), even where neighbouring files have them.
- Never run anything against `data/data-investment.json`; any data check uses a temp copy.

### Stage 1: Valuation reads the asset's method (PR 1)

**1. Tracking mode rule** - Add the Domain rule that says which valuation methods mean "tracked by balance", as described in spec §5 PR 1.

**2. Valuation calculators** - Make holding valuation, month-start values and dividend attribution decide between "snapshot is the total" and "snapshot is per unit" from the asset's valuation method passed in by the caller, never from the method copied onto the snapshot. Route the snapshot's zero-price rule through the same Domain rule.

**3. Application callers** - Pass the asset's valuation method from the holding valuation service and the credit mapping, and use the Domain rule in the price lookup's "no automatic fetch" branch.

**4. Data check and PRD ids** - Confirm on a temp copy of the investment data file that no snapshot's stored method differs from its asset's, and record the result in the PR body. Add the `P69-F01-balance-tracked-holding-option-01` … `-08` ids to PRD §9 F01 in a docs commit.

### Stage 2: "Tracked by" choice and lock (PR 2)

**5. Asset mode and lock** - Give the asset its derived tracking mode and "can change tracking mode" state, and refuse a mode change in the valuation-method setter while the asset has any transaction, credit, corporate action or saved price, with the PRD's message (spec §8).

**6. Admin API contract** - Expose the two derived flags on the asset admin response, and regenerate the OpenAPI snapshot and the generated web types.

**7. Asset form and page** - Add the "Tracked by" field to the asset form with its changeable and locked presentations, conflict recovery and dirty tracking, and replace the page's hard-coded valuation method with the mode-to-method mapping (spec §9).

**8. Docs** - Update `context.md` with the tracking mode and its lock, and in a docs commit align the PRD F01 Experience and Error Handling wording with what shipped (read-only locked field, existing save-failure text), listing each deviation in the PR body.

### Stage 3: "Balance" tag in the asset header (PR 3)

**9. Tree contract** - Carry the asset's tracking mode on the navigation tree's asset node and in its metadata, then regenerate the OpenAPI snapshot and the generated web types.

**10. Selection and header** - Read the flag into the asset selection when a tree node is chosen and show the "Balance" badge in the asset header for balance-tracked holdings only. Align the PRD's "next to the asset class" wording with the shipped placement in a docs commit.
