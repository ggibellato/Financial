# Parity screen checklist

Run `.\scripts\parity\Invoke-Parity.ps1 -Step All -LiveCopy <copy> -KeepRunning`. Open
http://localhost:5291 (before) and http://localhost:5292 (after) side by side, and compare each
row for 3 months:

- **M1:** the current month
- **M2:** the last complete month
- **M3:** one month from 2024

Mark a row *Same* only when every value matches to the penny. When done, stop both APIs with the
`Stop-Process` line the script printed.

| Page | Month | Value | Before (5291) | After (5292) | Same? |
|------|-------|-------|---------------|--------------|-------|
| Monthly | M1, M2, M3 | Each bank's balance and round-up total | | | |
| Monthly | M1, M2, M3 | Bank totals sum and round-up totals sum | | | |
| Monthly | M1, M2, M3 | Each card's statement outstanding and paid status; card outstanding total | | | |
| Monthly | M1, M2, M3 | Unpaid card charges (count and total) | | | |
| Monthly | M1, M2, M3 | Category totals and category totals sum | | | |
| Monthly | M1, M2, M3 | Income totals per source (net and gross) and total incoming | | | |
| Monthly | M1, M2, M3 | Tithe: calculated, balance, carry-forward | | | |
| Card statements | M1, M2 | Each card's outstanding, accumulated outstanding and paid status. In a month with no statement for a card, the after side shows 0.00, "—" for accumulated and Nothing due, with no Mark Paid | | | |
| Reserva | — | Each pot's balance and the pots total | | | |
| Reserva | M1, M2 | Movements listed for the month (count and total) | | | |
| Controle Mãe | — | BRL and GBP totals | | | |
| Controle Mãe | — | Entries from 2022-01-01: every BRL and GBP value the same (the 5 pound entries look the same; the page doesn't show their currency) | | | |
| Annual summary | Current year, 2024 | Category totals (annual, average), income summary, result | | | |
| Annual summary | Current year | Investment annual result: net position monthly values and full-year change | | | |
| Investment snapshots | M1, M2 | Suggested and current value per account; accounts not updated and why | | | |
| Bank screen (after only) | M1, M2, M3 | **B1:** the Barclays header balance equals Monthly's Barclays balance for the month | — | | |
| Bank screen (after only) | M1, M2, M3 | **B2:** the Barclays grid has every expense, income, transfer and adjustment Monthly shows for Barclays that month, with the same amounts, and round-ups in their own column | — | | |
| Bank screen (after only) | A month with no records | **B3:** the grid shows "No transactions in <Month Year>" | — | | |
| Bank screen (after only) | M1 | **B4:** at 320 px wide there's no horizontal page scroll | — | | |
| Card screen (after only) | M1, M2 | **C1:** the Statements tab's total and status per month equal Monthly's Card tab for the same card (outstanding when unpaid) | — | | |
| Card screen (after only) | M1, M2 | **C2:** the Transactions tab for a month lists the same charges as Monthly's card charges for that statement month | — | | |
| Card screen (after only) | A month with no charges | **C3:** the grid shows "No transactions in <Month Year>" | — | | |
| Card screen (after only) | M1 | **C4:** at 320 px wide there's no horizontal page scroll on either tab | — | | |
| Card screen (after only) | M1, M2 | **C5:** the card header's "<Month yyyy> statement" figure equals the Statements tab's To pay (or paid amount) for the viewed month, and "Total owed" equals Monthly's accumulated outstanding | — | | |
| Pots screen (after only) | M1 | **P1:** the Pots tab's balance per pot and total equal the Reserva page's | — | | |
| Pots screen (after only) | M1, M2 | **P2:** the Transactions tab lists the same movements as the Reserva page for that month (count and total) | — | | |
| Pots screen (after only) | A month with no movements | **P3:** the Transactions tab shows "No transactions in <Month Year>" | — | | |
| Pots screen (after only) | M1 | **P4:** at 320 px wide there's no horizontal page scroll on either tab | — | | |
| Monthly → All transactions (after only) | M1, M2, M3 | **T1:** filtered to one category, Spend equals that category's Category totals figure | — | | |
| Monthly → All transactions (after only) | M1, M3 | **T2:** filtered to one account, the rows equal that account screen's Transactions tab for the month, except a bank's old settled card charges, which appear only on their card | — | | |
| Monthly → All transactions (after only) | M1 | **T3:** at 320 px wide the rows stack and the grid adds no horizontal scroll (sort and filters are wide-screen only, as on every grid); on a wide screen Tab reaches every sort button, filter, link and Clear filters with visible focus | — | | |
| Accounts page (after only) | M1 | **S1:** the Settlement grid reads "Mãe · Mãe owes you R$167.88 / £7.94" when Controle Mãe's totals are R$167.88 / £7.94 | — | | |
| Accounts page (after only) | M1 | **S2:** Admin → Accounts → New account → Settlement → GBP, "Samuel" → the Accounts page lists "Samuel · Settled"; delete it afterwards (after side's copy only) | — | | |
| Accounts page (after only) | M1 | **S3:** at 320 px wide the Settlement grid stacks with no horizontal page scroll; on a wide screen Tab reaches the sort buttons with visible focus; the Mãe account is in the account screen's picker under Settlement | — | | |
| Settlement screen (after only) | M1, M3 | **S4:** the Mãe screen header reads "Mãe owes you R$167.88 / £7.94", equal to Controle Mãe's totals, in M1 and M3 alike | — | | |
| Settlement screen (after only) | M3, a 2022 month with pound entries | **S5:** the Mãe screen's Transactions for a month list the same entries as Controle Mãe for that month, each amount in its own currency, Converted "—" where Controle Mãe shows no value | — | | |
| Monthly → All transactions (after only) | M1, M3 | **S6:** filtered to Mãe: one row per entry, amount in the entry's currency, footer totals per currency; Spend for every category unchanged | — | | |
| Settlement screen (after only) | M1 | **S7:** at 320 px the Settlement header, actions and grid stack with no horizontal page scroll; on a wide screen Tab reaches the picker, month, Add entry, Settle, the row actions and every form field with visible focus | — | | |
| Settlement screen (after only, on the after side's copy) | M1 | **S8:** Mãe → Settle → BRL pre-fills paid by Mãe, R$167.88, £7.94 and "1 GBP = R$21.14"; Save settlement → "Both totals become zero." → header "Settled", Settle disabled with "Nothing to settle." | — | | |
| Card screen → Statements (after only, on the after side's copy) | M1 | **N1:** Amex → Statements → Due date on October 2026 → 25 Oct → the column and Settings → Integrations show it; Clear due date on November 2026 leaves October's | — | | |
| Card screen → Statements (after only) | M1 | **N6:** at 320 px the Statements tab with the due-date form open has no horizontal page scroll; Tab reaches Due date, the field, Save due date, Cancel and Clear due date | — | | |

Account-screen rows (bank, card, pots, settlement) are added by the feature that builds each screen.
