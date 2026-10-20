# Implementation Plan: Default Account Group and Summary Query

**Prerequisites:**
- .NET 10 SDK and Node, as for F03. No new packages.
- A fresh copy of the live `data-cashflow.json`, downloaded from Google Drive, for the parity run at the end of each PR. The parity baseline is the `p60-baseline` tag.
- Each PR is implemented on its own branch off an up-to-date `main`. PR B starts only after PR A is merged.
- Strict no-comments policy in all code, XML docs included.

### Stage 1: PR A — Summary endpoint

**1. Default-group membership** - Add the default-group template set to `AccountTemplates` and expose `TemplateName` and `JoinsDefaultGroup` on `Account`. Add `DefaultAccountGroup` in Domain `Rules/` with the membership rule in spec §6, covering active, inactive-with-money, inactive-with-a-record and non-joining accounts. The template allow-list must stay unchanged.

**2. Listing date** - Move the expense listing-month date onto `Expense` as `ListingDate` and make `ExpenseService` use it, with no change in behaviour.

**3. Summary service and DTOs** - Add the four DTOs, `IAccountGroupSummaryService` and `AccountGroupSummaryService` with the span/log pattern, and register it. The service composes members, balances, round-ups, currency totals, the category sum and the income totals exactly as spec §3 describes. See spec §4 and §5.

**4. Controller and contract** - Add `AccountGroupsController` with the summary route. Regenerate the OpenAPI snapshot and the web types, and check that the diff adds only the route and the four schemas.

**5. Acceptance tests** - Add the tagged acceptance tests for ACs 01–05 on seeded data: two GBP banks, a BRL bank, two inactive GBP banks (one with money left), round-ups, card charges and two income sources, with a fixed clock. Follow spec §7.

**6. Parity** - Add the summary figures to the parity report for the after side only, add the mappings in spec §6 to `figure-map.json`, and extend the README figure table and the parity self-test.

**7. PR A verification and hand-off** - Run the full .NET suite and the parity self-test. Run the parity run on the fresh live copy, expecting 0 differences, every summary key resolved through a mapping, and an unchanged SHA-256 for the copy. Run `/simplify` and the architecture-reviewer, then push the branch and open the PR, without merging.

### Stage 2: PR B — Monthly on the summary

**8. API client** - Add the type aliases and `getDefaultGroupSummary`, and remove the client's `getBankBalancesByMonth` and its test, since Monthly was its only caller.

**9. Monthly hook** - In `useMonthly`, fetch the summary in the existing `Promise.all` in place of bank balances. Read the bank rows, bank and round-up totals per currency, the category sum, the income rows and total incoming from it, and remove the browser sums for those figures; the card outstanding total stays. Keep the bank rows' Id and balance for the balance adjustment form.

**10. Grids and empty states** - Give `TotalsGrid` the empty message, give each summary grid its message, key bank and income rows by Id, and make the Banks footer per currency as spec §3 describes. Update the page, hook, grid and client tests, including the tagged ACs 06 and 07.

**11. PR B verification, PRD ticks and hand-off** - Run the full .NET suite and the web lint, tests and build. Run the parity run on a fresh live copy with `-KeepRunning` and check Monthly's rows in `screen-checklist.md` side by side, confirming one summary request in the network panel. Run `/simplify`, the architecture-reviewer and the ui-reviewer, tick the F04 acceptance criteria in the PRD in their own commit (no cross-feature boxes), then push the branch and open the PR, without merging.
