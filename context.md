# Financial Project

## Purpose

This document provides the context and requirements for my personal Financial Project.

The application is a personal financial management tool that consolidates investment transactions across multiple brokers and portfolios.

The system must allow users to:

* Record asset purchase transactions.
* Record asset sale transactions.
* Register dividends and other forms of investment income.
* Manage brokers, portfolios, and assets (currently done by editing `data.json` directly; no API endpoints exist for adding or removing brokers, portfolios, or assets).

---

# Project Overview

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
* Other
* Unknown (fallback when no mapping is found)

Assets are classified using a two-level system: `CountryCode` (BR, US, UK) combined with a `LocalTypeCode` string (e.g., `Acoes`, `FII`, `TesouroDireto`, `ConventionalGilt`) maps to a `GlobalAssetClass`. Cryptocurrencies (e.g. Bitcoin) are held in a dedicated broker (Coinbase) and currently fall under `Unknown` as no crypto-specific `LocalTypeCode` mapping exists.

Note: ISA is a UK tax wrapper (account type), not an asset class. ISA accounts at FreeTrade or Trading 212 hold assets of the classes above.

The solution must be extensible so that new asset classes can be added in the future without significant architectural changes.

---

# Technical Requirements

## Storage

This is currently a personal project. A traditional database is not required at this stage.

Persistence uses JSON files. Two storage backends are implemented and selectable via configuration (`Repository:Provider`):

* **LocalJson** — reads/writes a `data.json` file on the local filesystem (default).
* **GoogleDrive** — reads/writes a JSON file stored in Google Drive via the Google Drive API.

The persistence layer is abstracted behind a repository interface so that storage implementations can be replaced with minimal impact on the rest of the application.

All data is loaded into memory during application startup. Each write operation (add/update/delete transaction or credit) persists the full dataset immediately via the repository. There is no manual save step or shutdown hook.

---

## Implemented Features

The following features are currently built and available in both the React web app and the WPF desktop app unless noted:

* **Portfolio Navigator** — hierarchical tree (Broker → Portfolio → Asset) with per-asset detail tabs: summary (average price, quantity, current value), transactions (buy/sell CRUD), and credits (dividend/rent CRUD).
* **Dividend Check** — enter a ticker to fetch 5-year average dividend history from Google Finance, compute the maximum buy price at a 6% required yield target, and display the current discount against the live price. Supports a configured watchlist of tickers as a quick-select combobox.
* **Bulk Price Fetch** — fetches live prices for all active assets in a configured set of portfolios using the Google Finance web scraper, with a per-asset progress indicator.
* **Watchlist** — static list of tickers defined in `appsettings.json` (`Watchlist:Items`), used by the Dividend Check page.
* **Google Finance integration** — live asset prices and dividend history are obtained by scraping Google Finance pages (`WebPageParser` project). No API key is required, but the scraper depends on Google Finance's page structure.
* **Google Sheets import tool** — a separate WPF utility (`Integrations/ImportGoogleSpreadSheets`) for one-time import of portfolio data from Google Sheets. Not part of the main app runtime.

---

## User Interfaces

The project will support two front ends:

### WPF Application

A desktop application built using WPF.

### React Application

A web application built using React.

Because the React application requires server-side communication, the solution must include an API layer.

The WPF application should use the same application services and business logic as the API, ensuring that business rules are implemented only once.

Business logic must never reside in either UI project.

---

## Architecture

The architecture should follow Domain-Driven Design (DDD) principles while remaining pragmatic and avoiding unnecessary complexity.

The domain is relatively small and currently consists of concepts such as:

* Broker
* Portfolio
* Asset
* Transaction
* Credit

The primary architectural goal is separation of responsibilities, enabling the system to be:

* Easy to maintain
* Easy to test
* Easy to extend
* Easy to evolve

A clean architecture approach is preferred, with clear boundaries between:

* Domain
* Application
* Infrastructure
* Presentation

Dependencies should always point inward toward the domain.

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
* Ensure new features remain compatible with both the WPF and React front ends.
* Remove unused dependencies and unnecessary abstractions.
* Generate unit tests for all business-critical functionality.
