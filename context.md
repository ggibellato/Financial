# Financial Project

## Purpose

This document provides the context and requirements for my personal Financial Project.

The application is now made up of **two independent domains**, each with its own data store and no cross-domain references (see `docs/prd/P11-prd-cashflow-tracking/cashflow-context.md`):

* **Investment** — consolidates investment transactions across multiple brokers and portfolios.
* **CashFlow** — tracks household income, expenses, bank balances, and a personal savings ledger. See the dedicated [CashFlow Domain](#cashflow-domain) section below.

The Investment domain must allow users to:

* Record asset purchase transactions.
* Record asset sale transactions.
* Register dividends and other forms of investment income.
* Manage brokers, portfolios, and assets (currently done by editing `data-investment.json` directly; no API endpoints exist for adding or removing brokers, portfolios, or assets).

The CashFlow domain must allow users to:

* Keep every money place as an account built from a template: Bank, Credit card, Pots and Settlement.
* Record monthly expenses.
* Record incomes.
* Split savings into the pots of a Pots account (Reserva), each with its own running balance.
* Control borrowed money between myself and my mother (an informal personal loan between us) on a Settlement account.
* Record investments of quick access; these are not the same investments controlled by the Investment domain — they are more like reserve funds.
* Record and control credit card expenses.
* Track recurring bills.
* Calculate the monthly tithe (10% of net income), computed on demand rather than stored.

---

# Project Overview 

## Investment Domain

The current portfolio spans two countries:

* United Kingdom
* Brazil

As a result, the system must support:

* Multiple currencies (each broker has a currency; no cross-currency conversion is currently implemented).
* Different tax regulations.
* Annual tax reporting for both jurisdictions (planned — not yet implemented).
* Investment performance tracking.
* Portfolio analytics.

Supported asset classes (`GlobalAssetClass` enum) include:

* Equity (BR: Acoes; US/UK: Stock)
* RealEstate (BR: FII; US/UK: REIT)
* ETF
* Fund (BR: Fund; US/UK: Fund)
* Bond (BR: Bond, TesouroDireto; US: T-Bill; UK: ConventionalGilt, Bond)
* Cash
* Pension
* PrivateCredit (BR: CreditoImobiliario)
* Cryptocurrency
* Other
* Unknown (fallback when no mapping is found)

Assets are classified using a two-level system: `CountryCode` (BR, US, UK) combined with a `LocalTypeCode` string (e.g., `Acoes`, `FII`, `TesouroDireto`, `ConventionalGilt`, `CreditoImobiliario`) maps to a `GlobalAssetClass`. Cryptocurrencies (e.g. Bitcoin) are held in a dedicated broker (Coinbase); there is no `LocalTypeCode` mapping row for them yet, so the `Cryptocurrency` class is set directly on the asset instead of being derived from the mapping table.

Note: ISA is a UK tax wrapper (account type), not an asset class. ISA accounts at FreeTrade or Trading 212 hold assets of the classes above.

The solution must be extensible so that new asset classes can be added in the future without significant architectural changes.

---

## CashFlow Domain

A second, fully independent domain that tracks household finances — income, expenses, bank/card balances, savings pots, recurring bills, and a household ledger with my mother — separate from investment tracking. It was migrated from a personal Excel workbook (`Despesas.xlsx`) and has no cross-domain references to the Investment side — see `docs/prd/P11-prd-cashflow-tracking/cashflow-context.md`. See [Implemented Features (CashFlow Domain)](#implemented-features-cashflow-domain) below for the full feature breakdown.

Core entities: `Account` (built from a template — `Bank`, `CreditCard`, `Pots`, `Settlement` or `Savings` — whose capabilities decide what it can hold), `Expense`, `Income`, `Transfer`, `BalanceAdjustment`, `CardStatement`, `PotTransaction`, `SettlementTransaction`, `RecurringBill`, `InvestmentSnapshot`.

Storage mirrors the Investment domain's pattern but is entirely separate: a dedicated `data-cashflow.json` file, with its own independent LocalJson/GoogleDrive repository selection and its own configuration keys.

Import tooling: `Tools/CashFlowSpreadsheetImport` (Excel via ClosedXML) reads `Despesas.xlsx` and populates `data-cashflow.json`. It consolidates what were previously five separate migration tools into one command.

---

# Technical Requirements

## Storage

This is currently a personal project. A traditional database is not required at this stage.

Persistence uses JSON files. Two storage backends are implemented and selectable via configuration:

* **LocalJson** — reads/writes a JSON file on the local filesystem (default).
* **GoogleDrive** — reads/writes a JSON file stored in Google Drive via the Google Drive API.

**The Investment and CashFlow domains each select their storage backend independently**, with their own nested configuration element and their own JSON file — never floating at the config root:

* Investment: `Investment:Repository:Provider` (`InvestmentRepositorySettingsOptions.ConfigurationKeys`, a shared `JsonRepositoryConfigurationKeys` from `Financial.Shared.Abstractions`) → `data-investment.json`.
* CashFlow: `CashFlow:Repository:Provider` (`CashFlowRepositorySettingsOptions.ConfigurationKeys`) → `data-cashflow.json`.

The persistence layer is abstracted behind a repository interface per domain so that storage implementations can be replaced with minimal impact on the rest of the application.

All data is loaded into memory during application startup. Each write operation persists the full dataset immediately via the repository. There is no manual save step or shutdown hook.

---

## Implemented Features (Investment Domain)

The following features are currently built and available in the React web app. (For CashFlow domain features, see [Implemented Features (CashFlow Domain)](#implemented-features-cashflow-domain) below.)

* **Portfolio Navigator** — hierarchical tree (Broker → Portfolio → Asset) with per-asset detail tabs: summary (average price, quantity, current value), transactions (buy/sell CRUD), and credits (dividend/rent CRUD).
* **Dividend Check** — enter a ticker to fetch 5-year average dividend history from Google Finance, compute the maximum buy price at a 6% required yield target, and display the current discount against the live price. Supports a configured watchlist of tickers as a quick-select combobox.
* **Bulk Price Fetch** — fetches live prices for all active assets in a configured set of portfolios using the Google Finance web scraper, with a per-asset progress indicator.
* **Watchlist** — static list of tickers defined in `appsettings.json` (`Watchlist:Items`), used by the Dividend Check page.
* **Google Finance integration** — live asset prices and dividend history are obtained by scraping Google Finance pages (`WebPageParser` project). No API key is required, but the scraper depends on Google Finance's page structure.
* **Google Sheets import tool** — a separate WPF utility (`Tools/ImportGoogleSpreadSheets`) for one-time import of portfolio data from Google Sheets. Not part of the main app runtime.

---

## Implemented Features (CashFlow Domain)

The following features are built as React pages.

* **Accounts** — the CashFlow landing page, with a month picker and five tabs, the month and tab kept in the address (`?month=yyyy-MM&tab=…`): **Accounts** (every account grouped by template with its balance at the end of the chosen month), **Summary** (category totals, income by source with Total incoming, and the month's Result: total incoming minus category totals, shown as Surplus, Deficit or Balanced), **Income** (every income of the month with Source and Bank filters and a Total net footer; New income, edit and delete, where the Bank can be "No bank"), **Tithe** (the month's income base, tithe, tithe paid, carry-in with its source month and balance, owed or in credit; the checkbox that includes the previous month's balance; and a table of every month of that year with the same figures and a totals row, where choosing a month opens it) and **All transactions** (every transaction of the month, filtered by Type, Category and Description, where Description matches any part of the text ignoring case; an income with no bank shows as "No bank" with its own "Total (no bank)" footer line). An income with no bank moves no balance but counts in the month's income totals, the Result and the tithe base; it can't be split into pots. Only the open tab loads. Each account opens its account screen, whose transactions have the same Type, Category and Description filters and whose account picker groups accounts in the Accounts tab's order (by template, then by name), and where all entry happens for that account: expenses, card charges and refunds, incomes, transfers, balance adjustments, card statements (pay, unmark, due date), pot deposits and withdrawals, and settlement entries. Admin → Accounts creates and edits accounts.
* **Round-up on a bank-paid pot withdrawal** — when a pot withdrawal is paid from a bank with round-up enabled, the Withdraw form shows a "Round-up" field after Amount, filled from the amount (up to the next whole unit) exactly like a direct bank expense, until the user types in it; it can be changed, set to 0 or cleared (no round-up). The round-up is stored on the bank spend record only, so the pot and the money taken from it stay at the main amount and the bank goes down by the round-up. Editing a withdrawal can change it; opening an older one without a round-up adds none unless the amount or bank changes. Card-paid and no-payment withdrawals take no round-up.
* **Pot withdrawals by credit card** — a pot withdrawal can be paid by card instead of a bank: the pot goes down on the expense date and a linked card charge in the chosen category lands on the statement month the user picks (default: the expense date's month; never a paid or earlier statement), counting there for category totals and tithe. The withdrawal from the investment stays "Pending" until that statement is paid: marking it paid from a bank also writes, in the same save, a −X expense in the Pots account's category on that bank, dated the payment day but counted in the statement month, and the withdrawal reads "Done"; unmarking the statement paid removes it. Both Pay forms name what paying takes from each pot ("Also takes £120.00 from Reserva (Investimento) on Barclays."), and the Unmark dialog says how many investment withdrawals it removes. Once the statement is paid only the description and category can change, and the withdrawal can't be deleted until the statement is unmarked paid. The Pots account's Withdraw form offers "Paid with: None / Bank / Credit card" (card and a statement among the expense month and the next three months, leaving out paid statements), and a card account's "Withdraw from pots" opens the same form with the card locked; editing a card-paid withdrawal shows its investment withdrawal status, and once the statement is paid the form locks every field but description and category. A statement that card credit fully covers ("Nothing due") can't be paid, so its card Statements tab row offers "Settle pot withdrawals" instead: the user picks a bank and a date, and the same −X records are written without any card payment, leaving the card's credit and amount to pay untouched. The row then reads "Settled — pot withdrawals only" and counts as paid for new charges and the locks above; "Unmark settled" removes the −X records.
* **Mensais** — the monthly recurring-bill checklist.
* **Recurring Bills** — CRUD management of fixed recurring monthly bills.
* **Investment Snapshots** — monthly snapshot of quick-access accounts (savings, ISAs, Trading 212 brokerage balance, etc.) for net-worth tracking; lighter-weight than the Investment domain's full transaction-level tracking.
* **Annual Summary** — yearly rollups including gross-vs-net/after-tax salary comparisons (`SalaryAfterTaxes`/`TaxDifference`) and server-side Category Totals averages scoped to the correct months.
* **Settlement accounts** — the household ledger with my mother is the "Mãe" Settlement account (BRL and GBP): each entry records who paid, its currency, the amount and, when a rate was found, the value converted to the other currency; Settle zeroes both totals. A Settlement account with one currency (Samuel) also takes entries and Settle dated after today, for planned amounts; they count in the account's current balance and the Settle amount as soon as they are saved (the header then reads "Balance (includes planned)" and each one shows a "Planned" badge), while month balances count them from their date. An account with two currencies (Mãe) refuses future dates, because no exchange rate exists yet for the converted value. The old Reserva, Controle Mãe, Admin Banks and Admin Credit cards pages are gone; their addresses redirect to the account screen or Admin → Accounts.
* **Savings accounts** — a `Savings` account (GBP) holds money that earns interest. Its terms are how often interest is paid (monthly on a day from 1 to 31, or daily), whether it is tax-free, and a rate history where each rate has the date it takes effect; the current rate is the latest one in effect today and a future one shows as upcoming. It has an opening balance, takes transfers in and out and balance adjustments, and receives income, but has no categories, bills or round-up. Interest is entered by hand as an income: an income from a Dividendo/Juros source paid into a Savings account is interest, counted in the month's income and tithe base like any income. An Investment Snapshot account can take its suggestion from a Savings account's balance at the end of the previous month.
* **Tithe calculation** — computes 10% of a month's net income on demand (not persisted), read fresh whenever income or tithe-tagged expenses change.

---

## User Interfaces

The project has a single front end.

### React Application

A web application built using React. Covers **both the Investment and CashFlow domains**.

Because the React application requires server-side communication, the solution must include an API layer.

Business logic must never reside in the UI project.

---

## Architecture

The architecture should follow Domain-Driven Design (DDD) principles while remaining pragmatic and avoiding unnecessary complexity.

The solution is split into **two independent bounded contexts**, each with its own Domain/Application/Infrastructure projects:

* `Financial.Investment.{Domain,Application,Infrastructure}` — concepts: Broker, Portfolio, Asset, Transaction, Credit.
* `Financial.CashFlow.{Domain,Application,Infrastructure}` — concepts: Account (and its template), Expense, Income, Transfer, BalanceAdjustment, CardStatement, PotTransaction, SettlementTransaction, RecurringBill, InvestmentSnapshot.

`Financial.Shared.Infrastructure` holds cross-cutting infrastructure used by both contexts (e.g. `GoogleDriveJsonStorage`).

The primary architectural goal is separation of responsibilities, enabling the system to be:

* Easy to maintain
* Easy to test
* Easy to extend
* Easy to evolve

Each bounded context follows a clean architecture approach, with clear boundaries between:

* Domain
* Application
* Infrastructure
* Presentation

Dependencies should always point inward toward the domain. The Investment and CashFlow contexts must not reference each other.

---

## Code Quality

The project must adhere to the following principles:

* SOLID principles
* Clean Code practices
* Separation of concerns
* Dependency Injection
* High unit test coverage

All business rules should be testable without requiring a UI, database, or external services.

Unit tests should focus on domain and application behavior rather than implementation details.

---

## Guidance for AI Coding Agents

When generating code for this project:

* Prioritize maintainability over premature optimization.
* Prefer simple solutions over complex abstractions.
* Avoid overengineering.
* Follow established C# and .NET conventions.
* Keep the domain model independent of infrastructure concerns.
* Design interfaces around business needs rather than technical implementation details.
* Keep the Investment and CashFlow domains isolated — no cross-domain references; each persists to its own JSON file.
* Remove unused dependencies and unnecessary abstractions.
* Generate unit tests for all business-critical functionality.
