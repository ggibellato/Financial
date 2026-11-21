# Implementation Plan: Withdraw Forms on Pots Account and Card

**Prerequisites:**
- P62 F01 and F02 merged; branch off an up-to-date `main` (`git merge --ff-only origin/main` first).
- .NET 10 SDK, Node as for `Financial.Web`, PowerShell 7. No new packages.
- One PR (about 7 code files); count files before pushing and propose a split if it grows past the limit.
- Strict no-comments policy in all code.
- Browser check on a dev data copy only; never the live file or port 8080.

### Stage 1: Card entry and statement choices

**1. Card accounts offer Withdraw from pots** - Widen the allowed-actions rule so card accounts get Withdraw from pots. See spec §3 "Card-screen action".

**2. Statement month options** - Add the hook that offers the expense month and the next three unpaid statement months for a card. See spec §4 "Statement options".

### Stage 2: The form

**3. Form model** - Teach the withdrawal form model the payment choice, the card fields, opening from a card, editing a card operation and the locked state. See spec §4 "Rules in detail".

**4. Form view** - Show Paid with, the card and statement pickers, the status line and the locked values, with every state. See spec §3 "Payment choice", "Paid edit" and "Status line".

**5. Screen wiring** - Open Withdraw from pots with the card locked on a card screen and disable Delete while locked. See spec §4.

**6. Docs** - Say in `context.md` that the Withdraw forms offer the card.

**7. Verification and ticks** - Run the suites, `/simplify`, the architecture and UI reviewers and the review checklist, look at the forms in a browser on dev data, tick the F03 criteria and cross-feature 02–03 in the PRD in their own commit, then push and open the PR.
