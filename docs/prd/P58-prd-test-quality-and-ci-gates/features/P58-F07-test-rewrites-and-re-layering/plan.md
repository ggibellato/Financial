# Implementation Plan: Test Rewrites and Re-Layering

**Prerequisites:**
- An up-to-date `main`, `gh` authenticated, `npm install` done in `Financial.Web`
- Stages merge in order; each is one PR that leaves `main` deployable and the suite green
- Rewrite only tests that exist after F06; do not add behaviour tests for their own sake
- A defect found while rewriting is reported in the PR, not fixed in these stages

### Stage 1: Categories, Live Tests and the Gate (PR1)

**1. Gate** - Add `CategoryTraitCoverageTests`, a source scan that fails on any test class without a valid `Category` trait, with self-tests.

**2. Apply traits** - Tag every test class from the per-project default table, correcting mixed projects (`Api.Tests` host-based vs host-less, Investment.Infrastructure real-JSON classes) by hand.

**3. Live tests and CI** - Remove `Skip` from the nine web-parser verifications, tag them `Live`, and exclude `Live` from the `backend` test filter.

### Stage 2: Shared.Abstractions Tests (PR2)

**4. New project** - Create `Financial.Shared.Abstractions.Tests`, add it to the solution, and move `UsdBasedExchangeRateProviderTests` into it.

**5. Direct tests** - Add tests for `CompensatingSaveHelper`, `RetryPolicy` and the constructor guards of the assembly.

### Stage 3: Failed-Span Tests, CashFlow (PR3)

**6. Helper** - Add the shared failed-span assertion to `Financial.TestUtilities`.

**7. Rewrite** - Replace each CashFlow.Application "rethrows" test with one failed-span test per service and verify, per service group, that removing the `catch` fails it.

### Stage 4: Failed-Span Tests, Investment (PR4)

**8. Rewrite** - Do the same for Investment.Application services, and review the two Investment.Domain rethrow tests.

### Stage 5: Weak and Mirror Assertions (PR5)

**9. Backend rewrites** - Make the data-quality formatter and the repository-factory tests assert exact, positive outcomes.

**10. Web rewrites** - Replace the generated-class theme assertion and pin the remaining wall-clock-relative dashboard and upcoming-income tests to literal dates.

### Stage 6: Persistence Round-Trip (PR6)

**11. Investment.Infrastructure CRUD** - Make the Credit and Transaction service tests reload from disk and assert persisted state, deleting any that only repeat an Application-layer rule.

### Stage 7: Web Tabs Over the Real Hook (PR7)

**12. Tab tests** - Rewrite the Transactions, Credits and Price History tab tests to mock only the API client and assert visible outcomes.

### Stage 8: Docs and PRD Ticks (PR8)

**13. Documentation** - Document the layer split, the `Category` convention and the literal-expectation rule in the implementation rules, the testing guide, CLAUDE.md and the pipeline document.

**14. PRD ticks** - Tick the satisfied F07 boxes in their own commit.
