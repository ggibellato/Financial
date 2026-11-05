# Implementation Plan: Card Refunds and Credit

**Prerequisites:**
- .NET 10 SDK, Node as for `Financial.Web`, PowerShell 7. No new packages.
- The live file for parity is the 2026-11-05 download at `E:\dev\projetos\financial\data\data-cashflow.json` (SHA-256 starts `d83852d4`). Every PR runs `scripts/parity/Invoke-Parity.ps1 -Step All -LiveCopy "E:\dev\projetos\financial\data\data-cashflow.json"` against `p60-deployed-f10` and expects only the differences spec §7 lists (0 in stage 1, the same 4 from stage 2) with the SHA-256 unchanged. Parity reads that file and never writes it; local runs use `data/dev/data-cashflow.json`.
- Implement each PR on its own branch off an up-to-date `main`, starting only after the previous PR is merged. Count the files before pushing: at most 8 non-test code files and about 15 in total (generated `openapi.ts` excluded), except the overages the spec names (stages 1, 2, 3 and 4b). Propose a split instead of pushing anything larger.
- Strict no-comments policy in all code, XML docs included.
- Deploy only after stage 6.

### Stage 1 — Credit rule, Statements tab and card header

**1. Statement ledger** - Add the Domain rule that walks a card's statement months and gives each its total, credit in, amount to pay, status and credit carried, with the month lookup and the next month to pay. See spec §3 "Ledger" and "Status values".

**2. Pay and payable statements** - Make paying a statement use the ledger's amount to pay and refuse a Nothing-due statement, and list the bank side's payable statements from the ledger.

**3. Statements list and header data** - Return every ledger month from the card's statements endpoint with the new fields, and add the viewed month's statement to a card's account detail, leaving its balance and total owed unchanged. See spec §3 "Statements list" and "Card header".

**4. Statements tab** - Show the new columns and statuses, offer Pay only on Unpaid rows with a stored statement, and keep every F08 state. See spec §3 "Statements tab".

**5. Card header** - Replace a card's month-end owed figure with the viewed month's statement and its status, and show "Total owed" or the card's credit, sharing one owed-or-credit formatter. Bank headers stay as they are.

**6. Stage 1 verification** - Add checklist row C5. Regenerate the snapshot and web types, run the suites and the parity run, `/simplify`, the architecture-reviewer and the ui-reviewer, then push and open the PR.

### Stage 2 — Nothing due elsewhere

**7. Monthly and summary** - Take Monthly's card statement outstanding, the summary's card member statement and F14's next statement from the ledger, and delete the charge-filter helpers and the record that no longer have a caller. See spec §3 "Monthly's Card tab" and "Summary and next statement".

**8. Due banner and calendar** - Leave a Nothing-due month out of the payment-due banner, remove its calendar event, take the event title from the amount due, and resync when an earlier month's charges change. See spec §3 "Due banner" and "Calendar".

**9. Monthly's Card tab and Accounts page** - Show "Nothing due" without Mark paid on Monthly's Card tab, and show a card in credit as "Credit x" in the Accounts page and Monthly's accumulated cell.

**10. Stage 2 verification** - Update the Card statements checklist row. Regenerate the snapshot and web types, run the suites and the parity run, `/simplify`, the architecture-reviewer and the ui-reviewer, then push and open the PR.

### Stage 3 — Add refund

**11. Refund action and server checks** - Add the Add refund action for active cards, refuse a future-dated negative card charge, and give negative card charges their own row kind. See spec §3 "Add refund", "Refund validation" and "Refund rows".

**12. Refund mode** - Open the expense form in refund mode from Add refund and from Edit on a refund row, entering a positive amount saved as a negative charge, with the refund title, validation, default date and toasts, label refund rows "Refund", and make the Move form say "refund" with the amount positive.

**13. Stage 3 verification** - Run the suites and the parity run, `/simplify`, the architecture-reviewer and the ui-reviewer, then push and open the PR.

### Stage 4 — Refund credit to bank

**14. Credit refund rule and transfer (PR 4a)** - Add the credit refund pair rule, the transfer factory and its identification, and count credit refunds in the ledger with the guard that a refund raises no amount to pay. See spec §3 "Credit refund pair and factory".

**15. Credit refund route and guards (PR 4a)** - Add the route that records a credit refund within the card's credit, return 409 for credit refunds on both transfer routes, and leave them out of the transfer lists. See spec §3 "Credit refund" and "Credit refunds on transfer routes".

**16. Stage 4a verification** - Regenerate the snapshot and web types, run the suites and the parity run, `/simplify` and the architecture-reviewer, then push and open the PR.

**17. Rows and card action (PR 4b)** - Show credit refunds as read-only "Card credit refund" rows on the card and the bank, and offer Refund credit to bank on an active card in credit. See spec §3 "Credit refund rows" and "Refund credit to bank".

**18. Card-side form (PR 4b)** - Add the credit refund form in card mode with its bank list, defaults, limits and every state from spec §7.

**19. Stage 4b verification** - Run the suites and the parity run, `/simplify`, the architecture-reviewer and the ui-reviewer, then push and open the PR.

### Stage 5 — Delete a credit refund

**20. Delete route** - Add the route that deletes a credit refund and resyncs the card's calendar event. See spec §3 "Delete a credit refund".

**21. Delete from the rows** - Offer Delete on credit refund rows of an active screen account on both the card and the bank, with its confirmation and toast.

**22. Stage 5 verification** - Regenerate the snapshot and web types, run the suites and the parity run, `/simplify`, the architecture-reviewer and the ui-reviewer, then push and open the PR.

### Stage 6 — Receive card credit and PRD ticks

**23. Bank action and card list (PR 6a)** - Add the Receive card credit action for active banks and the read that lists every card in credit, inactive ones included. See spec §3 "Receive card credit".

**24. Stage 6a verification** - Regenerate the snapshot and web types, run the suites and the parity run, `/simplify` and the architecture-reviewer, then push and open the PR.

**25. Bank-side form (PR 6b)** - Add the credit refund form's bank mode, choosing a card in credit in the bank's currency and saving through the same route as the card side.

**26. Stage 6b verification and ticks** - Run the suites and the parity run, `/simplify`, the architecture-reviewer and the ui-reviewer. Tick the F15 acceptance criteria in the PRD in their own commit, each naming its test (cross-feature boxes only when every feature they name has shipped), then push and open the PR.
