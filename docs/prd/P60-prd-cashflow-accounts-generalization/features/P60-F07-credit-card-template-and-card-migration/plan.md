# Implementation Plan: Credit Card Template and Card Migration

**Prerequisites:**
- .NET 10 SDK, Node as for `Financial.Web`, PowerShell 7. No new packages.
- A fresh copy of the live `data-cashflow.json` downloaded from Google Drive for each stage's parity run. The parity baseline is `p60-deployed-f06` (the script's default).
- Each stage is implemented on its own branch off an up-to-date `main`, and starts only after the previous stage's PR is merged.
- Strict no-comments policy in all code, XML docs included.

### Stage 1: Card model on the unchanged V2 file

**1. Template and capabilities** - Add the Credit card template with its capabilities and GBP currency (not yet in the default group), the two new capabilities, and the card's due date as a temporary account property allowed only with `HasStatements`. See spec §3 and §4 Domain.

**2. Cards become accounts in the model** - Change the `CreditCard` reference on expenses, statements and investment accounts to a card account, keeping today's messages, then delete the `CreditCard` entity and the document root's card collection.

**3. Card balance, charge filter and actions** - Make an unpaid charge contribute to its card through the shared balance calculation, add the one charge filter, refuse a card account as a payer with the PRD message, and give card accounts no actions.

**4. Application on card accounts** - Serve `/credit-cards`, statements, the calendar sync, the payments-due banner, expenses, investment accounts and snapshot suggestions from accounts with `HasStatements`, with outstanding and accumulated figures from the filter and the shared calculation. Card names become unique across accounts.

**5. In use, Admin → Accounts and the card grid** - Extend the in-use check to charges, statements and investment links for both delete routes; trigger the calendar sync when Admin → Accounts creates, edits or deletes a card account; show a card's unpaid charges read-only on its grid. The OpenAPI snapshot stays unchanged.

**6. V2 persistence and import** - Read and write card accounts in the V2 `CreditCards` shape so the file stays byte-identical, and make the import seed and resolve cards as accounts.

**7. Parity and stage 1 verification** - Emit card balances in the current month and map them, negated, to accumulated outstanding. Run the full suites, the parity run against `p60-deployed-f06`, `/simplify` and the architecture-reviewer, then push and open the PR.

### Stage 2: V3 format

**8. V3 file shape and guard** - Put cards into `Accounts` and drop `CreditCards`, add the due date to statements, and raise the version guard to 3.

**9. Due date moves to the statement** - Add the statement due date and the rule that works out a card's current due date from its latest dated statement. Make the calendar sync, the payments-due banner and `/credit-cards` read it, make setting or clearing the date on `/credit-cards` write the statement of that month, and delete the temporary account property. See spec §3 "Statement due date" and "Editing the due date".

**10. V2→V3 step** - Add the step that moves cards into accounts, checks every card reference, backfills missing statements with the paid rule and moves each card's due date onto the statement of its month (spec §6), and add it to the migration chain.

**11. A charge creates its statement** - Add the statement creation helper and call it when a card charge is added, edited onto a card or moved to another statement month; route the statements GET through the same helper.

**12. Seed, test and example data** - Move the API test seed, the web-e2e data and the example file to V3, and update the migration and fresh-import tests to version 3.

**13. Stage 2 verification** - Run the full suites and the architecture test, then the parity run (the Migrate step now runs V2→V3), `/simplify` and the architecture-reviewer, then push and open the PR.

### Stage 3: Summary card fields and Monthly

**14. Cards join the default group** - Turn on the template's default-group membership, and add the member statement fields, the owed flag and the per-currency card total to the summary, with the bank total excluding card members. Add the owed flag to the account detail. Regenerate the snapshot and the web types.

**15. Monthly reads the summary** - Build Monthly's bank rows from non-card members and show the card total from the summary totals, with no browser sum of statements.

**16. Parity and stage 3 verification** - Emit the summary's card member and card total figures and map them to today's statement and Monthly figures. Run the suites, the parity run, `/simplify`, the architecture-reviewer and the ui-reviewer, then push and open the PR.

### Stage 4: Owed display, template labels and PRD ticks

**17. Owed display and template labels** - Show a card's balance as owed on the Accounts page and the account header, and add display names for templates on those screens and in Admin → Accounts.

**18. Stage 4 verification and ticks** - Run the suites and the web build, the parity run, `/simplify`, the architecture-reviewer and the ui-reviewer. Tick the F07 acceptance criteria in the PRD in their own commit (cross-feature boxes only when every feature they name has shipped), then push and open the PR.
