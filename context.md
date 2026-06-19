# Financial Project

## Purpose

This document provides the context and requirements for my personal Financial Project.

The application is a personal financial management tool that consolidates investment transactions across multiple brokers and portfolios.

The system must allow users to:

* Record asset purchase transactions.
* Record asset sale transactions.
* Register dividends and other forms of investment income.
* Manage brokers.
* Manage portfolios.
* Manage assets.

---

# Project Overview

The current portfolio spans two countries:

* United Kingdom
* Brazil

As a result, the system must support:

* Multiple currencies.
* Different tax regulations.
* Annual tax reporting for both jurisdictions.
* Investment performance tracking.
* Portfolio analytics.

Supported asset classes include:

* Bitcoin
* REITs (Fundos Imobiliários)
* Shares
* ETFs
* Government bonds
* ISAs

The solution must be extensible so that new asset classes can be added in the future without significant architectural changes.

---

# Technical Requirements

## Storage

This is currently a personal project. A traditional database is not required at this stage.

The initial implementation should use JSON files stored locally for persistence.

Future storage providers may include:

* Local file system
* Google Drive
* Additional cloud storage providers

The persistence layer must be abstracted so that storage implementations can be replaced with minimal impact on the rest of the application.

For the initial version:

* All data may be loaded into memory during application startup.
* Data may be saved manually by the user.
* Data should also be saved automatically during application shutdown.

The codebase should be structured to allow migration to a more sophisticated persistence model in the future.

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
