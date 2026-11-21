# Implementation Plan: Investment Withdrawal on Statement Payment

**Prerequisites:**
- P62 F01 merged; branch off an up-to-date `main` (`git merge --ff-only origin/main` first); start 2b only after 2a is merged.
- .NET 10 SDK, Node as for `Financial.Web`, PowerShell 7. No new packages.
- Count files before pushing; propose a split instead of pushing past the size target.
- Strict no-comments policy in all code.
- No live data file is touched; dev data under `data/dev/` only.

### Stage 2a: Write and remove at statement payment

**1. Reporting month** - Give an expense an optional reporting month that its reporting date prefers, and keep it out of the data file when empty. See spec §3 "Reporting month of the −X" and §6.

**2. Operation's investment withdrawal** - Let a card-paid withdrawal operation build its −X for a bank and date, and recognise it by shape for the status. See spec §4 "Rules in detail".

**3. Statement reader** - Add the domain reader that finds the card-paid withdrawals on a statement. See spec §3 "Shared statement reader".

**4. Mark and unmark paid** - Write the −X records in the payment's own save and remove them in the reversal's, failing first when a Pots account has no category. See spec §4 "Mark paid" and "Unmark paid".

**5. Operation response** - Report the investment withdrawal's bank and date on the operation, then regenerate the OpenAPI snapshot and web types. See spec §5.

**6. Stage 2a verification** - Run the suites, `/simplify` and the architecture reviewer, then push and open the PR.

### Stage 2b: Statement reads and web lines

**7. Statement reads** - List each statement's pot withdrawals on the card statements and payable statements reads, then regenerate the contract and web types. See spec §5.

**8. Pot withdrawal note** - Add the shared note and show it in both Pay forms for the chosen statement and bank. See spec §3 "Web wording".

**9. Unmark message** - Tell the user how many investment withdrawals unmarking removes. See spec §4 "CardStatementsTab".

**10. Docs** - Update the card-paid withdrawal paragraph in `context.md`.

**11. Stage 2b verification and ticks** - Run the suites, `/simplify`, the architecture and UI reviewers and the review checklist on both forms, tick the F02 criteria in the PRD in their own commit, then push and open the PR.
