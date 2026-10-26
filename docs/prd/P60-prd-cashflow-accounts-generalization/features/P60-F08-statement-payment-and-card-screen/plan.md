# Implementation Plan: Statement Payment and Card Screen

**Prerequisites:**
- .NET 10 SDK, Node as for `Financial.Web`, PowerShell 7. No new packages.
- A fresh copy of the live `data-cashflow.json` from Google Drive for each stage's parity run. The parity baseline is `p60-deployed-f07` (the script's default).
- Implement each stage on its own branch off an up-to-date `main`, starting only after the previous stage's PR is merged.
- Strict no-comments policy in all code, XML docs included.

### Stage 1: Paid rule, charge locks and read-only statements

**1. Paid rule** - Give the charge filter the statements so that a charge counts as paid when it is settled the old way or its statement is paid, and switch the statement outstanding, the calendar total and the summary card member to it. See spec §3 "Paid rule".

**2. Charge locks** - Refuse adding, deleting or changing anything but description, category and counts-as-tithe on a charge whose statement is paid, while Monthly keeps moving unpaid charges either way.

**3. Statement responses and reads** - Add the 409 conflict exception and its mapping. Return 409 for already paid and for unmarking an unpaid statement, 400 for nothing to pay, and remove the warning. Stop the statements GET from creating statements.

**4. Monthly** - Show the card actions' errors instead of warnings, render a card with no statement this month as 0.00 / — / Unpaid with nothing to pay, and lock date and value on a settled charge's form.

**5. Parity and stage 1 verification** - Add the after-side fill for missing statements, with self-test cases, and update the Card statements checklist row. Regenerate the snapshot and web types, run the suites, the parity run, `/simplify`, the architecture-reviewer and the ui-reviewer, then push and open the PR.

### Stage 2: Payment as a transfer

**6. Payment pair, transfer and link** - Add the statement payment pair rule, the payment transfer factory and its identification, and the statement's optional link to its transfer, omitted from the file when empty.

**7. Pay and unmark** - Make mark-paid write the transfer for the statement total on an optional date and link it in one save, and make unmark delete the linked transfer or run today's reversal for legacy statements, with the PRD's conflict when the transfer is gone.

**8. Transfer routes** - Leave payment transfers out of the `/transfers` lists and refuse editing or deleting them on both transfer routes with 409.

**9. Monthly shows charges paid by transfer** - Report the new payment status for charges on a statement paid by transfer, move them from Monthly's unpaid list to its expense list, and show them in the form and the Bank cell with the card's name.

**10. Stage 2 verification** - Regenerate the snapshot and web types, run the suites, the parity run, `/simplify`, the architecture-reviewer and the ui-reviewer, then push and open the PR.

### Stage 3: Card screen

**11. Statements endpoint and card transactions** - Add the card's statements list with totals, due dates and paid details. Make a card's Transactions pick charges by statement month, paid or not, with card payments as rows.

**12. Tab bar and Statements tab** - Add the Transactions and Statements tabs for card accounts, kept in the URL and guarded by the discard prompt, and the Statements tab with the viewed month highlighted, Pay (inline form) and Unmark (confirmation), with every F05 state. Label the card grid's date column "Purchased".

**13. Stage 3 verification** - Add the card screen checklist rows. Regenerate the snapshot and web types, run the suites, the parity run with the new rows, `/simplify`, the architecture-reviewer and the ui-reviewer, then push and open the PR.

### Stage 4: Card charges

**14. Add charge and charge row actions** - Add the Add charge action with the card locked in the expense form, and Edit and Delete on charges of unpaid statements.

**15. Move to a later statement** - Add the move endpoint with the PRD rule and the row action's inline form listing the next 12 unpaid statement months.

**16. Stage 4 verification** - Regenerate the snapshot and web types, run the suites, the parity run, `/simplify`, the architecture-reviewer and the ui-reviewer, then push and open the PR.

### Stage 5: Bank side and PRD ticks

**17. Pay card statement from a bank** - Add the bank action, the payable statements endpoint and the form that chooses the card and statement and pays through the same route as the card.

**18. Card payment row** - Show statement payments on banks as read-only "Card payment" rows that open the card's Statements tab on that month.

**19. Stage 5 verification and ticks** - Regenerate the snapshot and web types, run the suites, the parity run, `/simplify`, the architecture-reviewer and the ui-reviewer. Tick the F08 acceptance criteria in the PRD in their own commit (cross-feature boxes only when every feature they name has shipped), then push and open the PR.
