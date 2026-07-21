Project overview

The spreadsheet (Despesas.xlsx) is the source of truth. This document is a guide for reading it, not an independent specification — where anything here and the live file disagree, the file wins, and this document should be re-synced.

I maintain a large personal finance spreadsheet that tracks my bank accounts, credit cards, income, and internal transfers. The spreadsheet is my main tool for cash-flow control: it helps me know the balance of each account, plan transfers, track recurring bills, and compare spending month by month and year by year. It also adds family-specific flows, reserve logic, and investment monitoring.

Scope: the workbook contains monthly tabs from 2014 through 2026 (140+ sheets). All of this history is in scope for automation, not just the current format. Sheet naming and column layout are not consistent across years — e.g. Jul2026 vs. Julho 2014 vs. Janeiro 2017 — and column counts range from 8 to 25 depending on the year. Any importer must handle multiple legacy layouts, not assume the current-year format applies throughout.

Architecture: a separate domain from the existing Financial system

This data is a different domain from the investment-tracking system already built in the Financial solution (Broker/Portfolio/Asset/Transaction/Credit). It should be developed as its own, fully independent domain — Financial.CashFlow — sitting alongside the existing Financial.Investments domain:

- Each domain gets its own Domain / Application / Infrastructure projects (Financial.CashFlow.Domain, Financial.CashFlow.Application, Financial.CashFlow.Infrastructure), following the same Clean Architecture layering already used for Financial.Investments.
- No cross-domain references. Financial.CashFlow must not depend on Financial.Investments or vice versa — they are independent bounded contexts that happen to live in the same person's finances, not a shared model.
- Only the Presentation layer (the WPF app and the React web app) depends on both domains, composing them into one UI. Business logic stays out of Presentation, per the existing architecture rules.
- Both domains live in the same repository and solution (Financial.slnx) as separate projects — not split into separate git repos. This keeps things simple for a personal, non-scaling project while still giving each domain a clean boundary.

Two separate data files, one per domain, instead of a single shared data.json:

- The existing investments data file stays with Financial.Investments.Infrastructure.
- Financial.CashFlow.Infrastructure gets its own JSON file (e.g. data-cashflow.json) storing expenses, categories, reserve ledger, bank/card balances, and the monthly/yearly summaries — persisted via the same repository-abstraction pattern already used (LocalJson / GoogleDrive, selectable via config).

This is a documentation-only decision at this stage — no restructuring of the actual solution has been done yet. Actual project creation/migration should go through a proper plan (spec-writer / implement-feature) when this domain is ready to be built.

What the spreadsheet tracks

The workbook contains every expense, including direct debits, card purchases, shared household spending, and small everyday payments such as coffee. Each expense is assigned to exactly one single-level category group:

- Ariana
- Carro
- Casa
- Estudo
- Extras
- Familia
- Gleison
- Mercado
- Samuel
- Saude
- Viagem
- Dizimo
- Investimento
- Reserva

Reserve logic

Reserva is a special account and should be treated as its own money flow, not just a normal spending category, tracked in its own Reservas sheet.

Corrected split (verified against the live Reservas sheet formulas): after my wife's wages are received and tax is removed, 10% tithe ("Dz") is deducted first, and the remainder ("Limpo") splits into exact fractions:

- 1/3 for investment.
- 1/3 for house expenses and special treats like dinners and trips. (Note: this bucket's column in the sheet is literally named Viagem, which is a legacy/inaccurate label — it functions as the general house/treats bucket, not travel-only.)
- 1/6 for my wife.
- 1/6 for me.

(Previously described as 33%/33%/16.5%/16.5%, which doesn't sum to 100% — the real math is exact thirds and sixths.)

In the monthly tabs, the Reserva category rows represent net money moved in/out of this reserve pool — negative values are returns or money moved out of Reserva into a bank account — while the Reservas sheet itself holds the per-bucket running ledger (Investimento / house-treats / Ariana / Gleison columns) with its own running balances.

Monthly workbook structure

Each month has its own tab, named like Jul/2026 or Ago/2026 in current-format years (older years use full Portuguese month names with varying formats — see Scope above). These monthly tabs contain all expenses for that month, category totals at the top, and bank-account balances in the same sheet, plus Trading 212, where Saldo represents emergency money. The E column identifies which bank/payment source the transaction belongs to:

- `` (empty) = Barclays.
- T = Trading 212.
- C = Chase.

Negative values are used for returns or for money moved out of Reserva into a bank account.

Per-transaction card attribution is not needed and not modeled. The E column's blank/T/C tag is the only payment-source detail captured at transaction level — it never identifies which of the five credit cards was used. This is intentional and sufficient; card balances are tracked separately as monthly aggregates (see Credit card handling).

Credit card handling

Credit cards are recorded inside the monthly tab, but they follow a special rule: a credit-card charge is entered as a normal transaction row, so it immediately counts toward category totals, but it should not reduce the bank balance until the statement is paid.

Corrected mechanism: there is no dedicated credit-card block sitting inside the monthly transaction list. Instead, each month has a small reconciliation block in columns J–K (Mes, Movido reservas, two Barclays Card rows) that feeds an Ajuste (adjustment) figure. That adjustment cell happens to land on row 14 in most months, but only by coincidence of transaction-row count — it is not a fixed dedicated cell, and its exact row should be located per-sheet rather than hardcoded.

When a card statement is paid, its rows move into the normal transaction list and its offset is removed from the adjustment figure.

The real card/credit list is five lines, not four (confirmed from Resumo/YYYY rows 30–34):

- Barclays Platinum Visa 8003
- Barclays Platinum Visa 6007
- Chase Master 4023
- BA Amex
- Paypal credit (previously missing from this list)

Investment tracking

On the first day of each month, I also enter information about my investments so I can track whether they are moving in the right direction over time.

Canonical account list (confirmed from Resumo/YYYY rows 29–39 — use this list as-is, no additions or omissions):

- Blue Rewards Saver
- Platinum Visa 8003 (liability)
- Platinum Visa 6007 (liability)
- Chase Master 4023 (liability)
- BA Amex (liability)
- Paypal credit (liability)
- Chip Cash ISA (Gleison)
- Chase save
- Chip Cash ISA (Ariana)
- Trading 212 Invested
- Reservas pessoais

This information should be modeled as a regular monthly snapshot in the automation.

Summary sheets

The Resumo/2026 sheet is my yearly report and contains several calculated sections. The total block at the bottom runs from lines 29 to 41, including totals. Below that, lines 47 to 57 show the differences between months within the year. These formulas have been fixed in the source file (previously there were #REF! errors from a deleted column reference) — every diff cell now consistently computes as thisMonth − prevMonth, following the same pattern as its neighbors. These calculations are part of my monthly and yearly reporting and need to be included in the automated version.

The Resumo/Year logic is important because it is not just a display sheet: it also contains formulas and comparisons that feed my financial reporting. The automation should generate monthly totals, yearly totals, and month-over-month differences as first-class outputs, not as manual spreadsheet artifacts.

Other tabs

- Reserva/Reservas: dedicated reserve-account flow (see Reserve logic above).
- Mensais — recurring monthly bills, now in scope. Split into two areas within the same sheet: Brasil and UK (a label row marks where each area starts). Per area:
  - Column A: day of the month the bill is due.
  - Column B: expense description.
  - Column C: value.
  - Column D: status flag — '' (empty) = nothing done yet, a (agendado) = payment scheduled, x = payment done. The month itself is indicated once at the top of the sheet, not per row.
  - Column E: free-text note about the expense (e.g. "Direct debit", "until 01/27").
  - some Brazil-side rows also carry a NIT number (related to Brazilian public pention) and mininal wage value in column F, e.g. the INSS row.
- Controle mae — cash flow between me and my mother, now in scope. Very simple, one list:
  - Column A: description of the expense, including the date or Month/Year.
  - Column B: value in Reais (BRL).
  - Column C: value in Pounds (GBP).
  - Conversion happens on the date of the expense — a BRL expense is converted to GBP at that day's rate (and vice versa for a GBP expense), and both values are recorded.
  - Column E: free-text annotation.
  - (Observed: the earliest rows in this tab, from ~2018–2019, don't consistently fill both currency columns — some only have one value plus a rate note embedded in the description text. Since full 2014–2026 history is in scope, the importer will need to tolerate these older, less structured rows.)
- Still out of scope: Pagamento apartamento, Viagem, Casa, Media Anual, Morando com a mae, Viagem Gabriel e mae.