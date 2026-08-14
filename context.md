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

* Record monthly expenses.
* Record incomes.
* Control Reserve buckets — a separate running balance for each of the specified buckets (Investimento, HouseTreats, Ariana, Gleison).
* Control borrowed money between myself and my mother (an informal personal loan between us).
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

A second, fully independent domain that tracks household finances — income, expenses, bank/card balances, a savings-split reserve ledger, recurring bills, and a household ledger with my mother — separate from investment tracking. It was migrated from a personal Excel workbook (`Despesas.xlsx`) and has no cross-domain references to the Investment side — see `docs/prd/P11-prd-cashflow-tracking/cashflow-context.md`. See [Implemented Features (CashFlow Domain)](#implemented-features-cashflow-domain) below for the full feature breakdown.

Core entities: `Bank`, `Expense`, `Income`, `CardStatement`, `RecurringBill`, `ReserveMovement`, `InvestmentSnapshot`, `MaeLedgerEntry`.

Implemented as React pages only (Monthly, Income/Expenses, Banks & Cards, Reserve, Recurring Bills, Investment Snapshots, Annual Summary, Controle Mãe) — **not available in the WPF app**.

Storage mirrors the Investment domain's pattern but is entirely separate: a dedicated `data-cashflow.json` file, with its own independent LocalJson/GoogleDrive repository selection and its own configuration keys.

Import tooling: `Integrations/CashFlowSpreadsheetImport` (Excel via ClosedXML) reads `Despesas.xlsx` and populates `data-cashflow.json`. It consolidates what were previously five separate migration tools into one command.

---

# Technical Requirements

## Storage

This is currently a personal project. A traditional database is not required at this stage.

Persistence uses JSON files. Two storage backends are implemented and selectable via configuration:

* **LocalJson** — reads/writes a JSON file on the local filesystem (default).
* **GoogleDrive** — reads/writes a JSON file stored in Google Drive via the Google Drive API.

**The Investment and CashFlow domains each select their storage backend independently**, with their own nested configuration element and their own JSON file — never floating at the config root:

* Investment: `Investment:Repository:Provider` (`InvestmentRepositoryConfigurationKeys` in `Financial.Investment.Infrastructure`) → `data-investment.json`.
* CashFlow: `CashFlow:Repository:Provider` (`CashFlowRepositoryConfigurationKeys`) → `data-cashflow.json`.

The persistence layer is abstracted behind a repository interface per domain so that storage implementations can be replaced with minimal impact on the rest of the application.

All data is loaded into memory during application startup. Each write operation persists the full dataset immediately via the repository. There is no manual save step or shutdown hook.

---

## Implemented Features (Investment Domain)

The following features are currently built and available in both the React web app and the WPF desktop app unless noted. (For CashFlow domain features, see [Implemented Features (CashFlow Domain)](#implemented-features-cashflow-domain) below — that domain is React-only.)

* **Portfolio Navigator** — hierarchical tree (Broker → Portfolio → Asset) with per-asset detail tabs: summary (average price, quantity, current value), transactions (buy/sell CRUD), and credits (dividend/rent CRUD).
* **Dividend Check** — enter a ticker to fetch 5-year average dividend history from Google Finance, compute the maximum buy price at a 6% required yield target, and display the current discount against the live price. Supports a configured watchlist of tickers as a quick-select combobox.
* **Bulk Price Fetch** — fetches live prices for all active assets in a configured set of portfolios using the Google Finance web scraper, with a per-asset progress indicator.
* **Watchlist** — static list of tickers defined in `appsettings.json` (`Watchlist:Items`), used by the Dividend Check page.
* **Google Finance integration** — live asset prices and dividend history are obtained by scraping Google Finance pages (`WebPageParser` project). No API key is required, but the scraper depends on Google Finance's page structure.
* **Google Sheets import tool** — a separate WPF utility (`Integrations/ImportGoogleSpreadSheets`) for one-time import of portfolio data from Google Sheets. Not part of the main app runtime.

---

## Implemented Features (CashFlow Domain)

The following features are built as React pages only — the CashFlow domain has no WPF equivalent.

* **Monthly (Mensais)** — primary month-by-month view of income vs. expenses, with a category totals grid, gross/net income breakdown, and a dynamic credit-card-area scope.
* **Banks & Cards** — tracks bank account balances and credit card statement balances per month.
* **Reserve** — tracks movements in and out of four fixed savings-split buckets (`Investimento`, `HouseTreats`, `Ariana`, `Gleison`), each with its own running balance.
* **Recurring Bills** — CRUD management of fixed recurring monthly bills.
* **Investment Snapshots** — monthly snapshot of quick-access accounts (savings, ISAs, Trading 212 brokerage balance, etc.) for net-worth tracking; lighter-weight than the Investment domain's full transaction-level tracking.
* **Annual Summary** — yearly rollups including gross-vs-net/after-tax salary comparisons (`SalaryAfterTaxes`/`TaxDifference`) and server-side Category Totals averages scoped to the correct months.
* **Controle Mãe** — household ledger tracking informal borrowed money between myself and my mother, recorded in both BRL and GBP.
* **Tithe calculation** — computes 10% of a month's net income on demand (not persisted), read fresh whenever income or tithe-tagged expenses change.

---

## User Interfaces

The project supports two front ends, with different domain coverage:

### WPF Application

A desktop application built using WPF. Covers the **Investment domain only**.

### React Application

A web application built using React. Covers **both the Investment and CashFlow domains**.

Because the React application requires server-side communication, the solution must include an API layer.

The WPF application should use the same application services and business logic as the API, ensuring that business rules are implemented only once.

Business logic must never reside in either UI project.

---

## Architecture

The architecture should follow Domain-Driven Design (DDD) principles while remaining pragmatic and avoiding unnecessary complexity.

The solution is split into **two independent bounded contexts**, each with its own Domain/Application/Infrastructure projects:

* `Financial.Investment.{Domain,Application,Infrastructure}` — concepts: Broker, Portfolio, Asset, Transaction, Credit.
* `Financial.CashFlow.{Domain,Application,Infrastructure}` — concepts: Bank, Expense, Income, CardStatement, RecurringBill, ReserveMovement, InvestmentSnapshot, MaeLedgerEntry.

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
* Ensure new Investment-domain features remain compatible with both the WPF and React front ends; CashFlow-domain features are React-only by design.
* Remove unused dependencies and unnecessary abstractions.
* Generate unit tests for all business-critical functionality.
