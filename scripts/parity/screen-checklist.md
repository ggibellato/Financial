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
| Monthly → All transactions (after only) | M1 | **T3:** at 320 px wide the rows stack with no horizontal page scroll; Tab reaches every sort button, filter, link and Clear filters with visible focus | — | | |

Account-screen rows (bank, card, pots, settlement) are added by the feature that builds each screen.
