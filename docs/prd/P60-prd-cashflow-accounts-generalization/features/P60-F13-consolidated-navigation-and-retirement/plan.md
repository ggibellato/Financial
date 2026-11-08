# Implementation Plan: Consolidated Navigation and Retirement

**Prerequisites:**
- .NET 10 SDK, Node as for `Financial.Web`, PowerShell 7. No new packages.
- The live file for parity is `E:\dev\projetos\financial\data\data-cashflow.json` (V5). Every PR runs `scripts/parity/Invoke-Parity.ps1 -Step All -LiveCopy "E:\dev\projetos\financial\data\data-cashflow.json"` against `p60-deployed-f12` and expects `Parity OK: 0 differences` with the SHA-256 unchanged. Parity reads that file and never writes it; local runs use `data/dev/data-cashflow.json`. PR 4's run uses a fresh Google Drive download the user saves there first.
- Implement each PR on its own branch off an up-to-date `main`, starting only after the PRs it depends on are merged (spec §1 "Compatibility between PRs"): 1e before 3a and 3b; 1a and 1d before 3d; 1b and 1c before 3e; 1d before 3f; 2 before 3d–3g; 3b before 3c and 3d; 3e before 3f.
- 14 PRs: 1a, 1b, 1c, 1d, 1e, 2, 3a, 3b, 3c, 3d, 3e, 3f, 3g, 4. Count the files before pushing: at most 8 non-test code files (spec §4 lists each PR's); totals above about 15 come only from the repeated changes and the earlier tests the spec names, otherwise propose the split in spec §3 "PR size" instead of pushing.
- Strict no-comments policy in all code, XML docs included.
- Each PR body lists the earlier features' tests it moved onto replacement routes and the ones it deleted (spec §3 "Earlier features' tests").
- Deploy only after stage 4.

### Stage 1 — Moves

**PR 1a — Adjustments account route**

**1. Adjustment service** - Let the adjustment service work out an account's balance itself, find the account among all accounts, and stop depending on the bank service; remove the bank service's balance-as-of method. See spec §3 "Adjustment balance".

**2. Adjustment routes** - Add the account-scoped add, edit and delete adjustment routes, remove the three bank-scoped writes, and point the web client's adjustment methods at the new routes. See spec §3 "Adjustments route" and §5.

**3. PR 1a verification** - Regenerate the snapshot and web types, run the suites and the parity run, `/simplify` and the architecture-reviewer, then push and open the PR.

**PR 1b — Per-statement due date**

**4. Due-date rule** - Add the Domain rule that sets one statement's due date within its own month and clears the card's later statements' dates. See spec §3 "Due-date rule".

**5. Due-date service and route** - Add the service and the account route that set or clear the due date of a month the Statements tab lists, creating the statement when needed, and trigger the calendar sync after every save. See spec §3 "Due-date route", "Due-date service" and §5.

**6. README** - Update the calendar section to say the sync runs when a statement's due date is set or cleared on the card's Statements tab.

**7. PR 1b verification** - Regenerate the snapshot and web types, run the suites and the parity run, `/simplify` and the architecture-reviewer, then push and open the PR.

**PR 1c — Due date on the Statements tab**

**8. Due-date form** - Add the inline due-date form with its validation, saving, error, success and unsaved-changes states. See spec §3 "Due-date action" and "Due-date form states".

**9. Statements tab** - Add the Due date action to every statement row that isn't paid, open the form from it, reload the statements after a save and join the screen's discard prompt.

**10. PR 1c verification** - Add screen checklist rows N1 and N6. Run the suites and the parity run, `/simplify`, the architecture-reviewer and the ui-reviewer, set and clear a date in a browser on the dev data, then push and open the PR.

**PR 1d — Readers off `/banks` and `/credit-cards`**

**11. Bank option type** - Add the front-end bank option type and move every form and hook that takes banks onto it. See spec §3 "`BankOption`".

**12. Card due date on accounts** - Add each card's current due date to the accounts list. See spec §3 "Card due date for Integrations".

**13. Readers** - Make the account screen's form options, the statement payer list, Mensais, Integrations and Investment accounts read the accounts list, filtered by capability. See spec §3 "Bank lists" and "Investment accounts".

**14. PR 1d verification** - Regenerate the snapshot and web types, run the suites and the parity run, `/simplify`, the architecture-reviewer and the ui-reviewer, then push and open the PR.

**PR 1e — Old-link target**

**15. Replaced page on accounts** - Mark the account each old page showed in the accounts list, using the existing Reserva and Mãe rules. See spec §3 "Old-link target".

**16. Redirect** - Add the moved-page redirect with its loading, error and no-target states, serve the Admin reserve-buckets address with it, and show the moved notice on the Accounts page. See spec §3 "Redirect component" and "Moved notice".

**17. PR 1e verification** - Add the reserve-buckets part of checklist row N4. Regenerate the snapshot and web types, run the suites and the parity run, `/simplify`, the architecture-reviewer and the ui-reviewer, then push and open the PR.

### Stage 2 — Overview

**18. Monthly overview** - Reduce Monthly to the Summary and All transactions tabs, slim its data hook to the three reads the overview needs, make the cards grid read-only from the group summary, and link each bank and card row to its account screen at the viewed month; delete the components and hooks only the old tabs used. See spec §3 "Monthly overview", "Cards grid" and "Row links".

**19. Landing and menu** - Land CashFlow on Accounts and order the CashFlow menu as the PRD. See spec §3 "Landing" and "Menus".

**20. Add-expense journey** - Move the add-expense E2E journey to the account screen. See spec §3 "E2E".

**21. Stage 2 verification** - Rewrite the Monthly checklist rows and add N2, N3 and N5. Run the suites, the parity run and the E2E smoke specs, `/simplify`, the architecture-reviewer and the ui-reviewer, look at Monthly in a browser on the dev data, then push and open the PR.

### Stage 3 — Retire

**PR 3a — Controle Mãe**

**22. Parity side switch** - Let parity read retired routes on the before side only, remove the Mãe fallback, and read the Mãe totals from the Mãe account's current balances on the after side. See spec §3 "Parity side switch", "Parity: Mãe fallback" and the mapping table.

**23. Retire Controle Mãe** - Remove the page, its hook, menu entry, client methods, type aliases, controller, service and DTOs, and redirect its address to the Mãe screen; move the earlier tests that used its routes.

**24. PR 3a verification** - Rewrite the Controle Mãe checklist rows and extend N4. Regenerate the snapshot and web types, run the suites, the parity self-test and the parity run, `/simplify`, the architecture-reviewer and the ui-reviewer, then push and open the PR.

**PR 3b — Reserva page**

**25. Retire the Reserva page** - Remove the page, its hook, the split and withdrawal forms and the menu entry, and redirect its address to Reserva's Pots tab.

**26. PR 3b verification** - Rewrite the Reserva checklist rows and extend N4. Run the suites and the parity run, `/simplify`, the architecture-reviewer and the ui-reviewer, then push and open the PR.

**PR 3c — Reserva API**

**27. Parity pots** - Read pot balances from the Reserva Pots tab on the after side, emit only non-zero pot balances on both sides, and read the current pot balances from the Pots tab at the run's last month. See spec §3 "Parity: pots" and the mapping table.

**28. Retire the Reserva API** - Remove the reserve routes except movement edit and delete, the reserve-buckets routes and service, the fixed Reserva pot lookup, their DTOs, client methods and type aliases; resolve an edited movement's pot among its own account's pots; move the earlier tests that used these routes.

**29. PR 3c verification** - Regenerate the snapshot and web types, run the suites, the parity self-test and the parity run, `/simplify` and the architecture-reviewer, then push and open the PR.

**PR 3d — Admin Banks and `/banks`**

**30. Parity banks** - Take the bank list from the accounts list and each bank's round-up total from its account rows on the after side, and rely on the existing mappings for balances and sums. See spec's mapping table.

**31. Retire Admin Banks and `/banks`** - Remove the page, its hook and dialog, the menu entry, the bank routes, service, DTOs, client methods and type aliases and the bank adjustments list; redirect the address to Admin → Accounts; drop the bank service from the architecture allow-list; move the earlier tests and the typed bank fixtures.

**32. PR 3d verification** - Extend N4. Regenerate the snapshot and web types, run the suites, the parity self-test and the parity run, `/simplify`, the architecture-reviewer and the ui-reviewer, then push and open the PR.

**PR 3e — Admin Credit cards page**

**33. Retire Admin Credit cards** - Remove the page, its hook and dialog and the menu entry, and redirect the address to Admin → Accounts.

**34. Expense form** - Remove the expense form's credit-card list, now unused. See spec §3 "Expense form card list".

**35. PR 3e verification** - Complete N3 and N4. Run the suites and the parity run, `/simplify`, the architecture-reviewer and the ui-reviewer, then push and open the PR.

**PR 3f — `/credit-cards`, `/transfers` and monthly statements**

**36. Parity cards** - Read each card's statement figures from its account detail on the after side, fill missing statements under the new family, and drop the card warning figure. See spec §3 "Parity: card warning" and the mapping table.

**37. Retire the routes** - Remove the credit-card and transfer routes, services and DTOs, the monthly card-statements read, F07's clear-all due-date rule, their client methods and type aliases; move the earlier tests and the typed card fixtures.

**38. PR 3f verification** - Regenerate the snapshot and web types, run the suites, the parity self-test and the parity run, `/simplify` and the architecture-reviewer, then push and open the PR.

**PR 3g — Monthly expense reads**

**39. Parity unpaid charges** - Read the unpaid card charges total from the month's transactions on the after side. See spec's mapping table.

**40. Retire the routes** - Remove the monthly expenses and unpaid card charges reads, their service methods and client methods, and move the earlier tests that used them.

**41. PR 3g verification** - Regenerate the snapshot and web types, run the suites, the parity self-test and the parity run, `/simplify` and the architecture-reviewer, then push and open the PR.

### Stage 4 — Close

**42. Seed and journeys** - Add a card with an unpaid statement and a Settlement account with entries to the test seed, and add the pay-statement, manual pots deposit and settle smoke journeys, each undoing its own change. See spec §3 "Seed" and "E2E".

**43. Fixed-name test** - Add the test that keeps the fixed Reserva lookups out of production code, and record the bank-name join grep in the PR body. See spec §3 "Fixed-name lookups".

**44. Docs and example data** - Update `context.md`, README, `docs/ui/` references, the example data and the design document's status. See spec §3 "Docs" and "Example data".

**45. Final parity run and checklist** - After the user saves a fresh Google Drive download, run parity on it and tick the full screen checklist.

**46. Stage 4 verification and ticks** - Run the suites and the E2E smoke specs, `/simplify`, the architecture-reviewer and the ui-reviewer. Tick the F13 acceptance criteria and the F13 cross-feature box in the PRD in their own commit, each naming its test, then push and open the PR.
