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
| Card statements | M1, M2 | Each card's outstanding, accumulated outstanding and paid status. In a month with no statement for a card, the after side shows 0.00, "—" for accumulated and Unpaid, with no Mark Paid | | | |
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

Account-screen rows (bank, card, pots, settlement) are added by the feature that builds each screen.
