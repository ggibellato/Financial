I've revised the text for grammar, spelling, clarity, cohesion, and consistency while keeping the original meaning and intent.

# Financial Project

## Purpose

This document provides the context for my personal Financial Project.

The application should allow users to record asset purchase and sale transactions, as well as register dividends and any other forms of investment income or profit.

The system should also support the creation and management of brokers, portfolios, and assets.

---

# Project Overview

The project is a **personal financial management tool** designed to consolidate financial transactions across multiple Brokers.

The current portfolio spans **two countries**:

* United Kingdom
* Brazil

This introduces the need to manage:

* Multiple currencies
* Different tax regulations

The system must support:

* **Annual tax reporting** for both jurisdictions
* **Investment performance tracking**
* **Portfolio analytics**

Supported asset classes include:

* Bitcoin
* REITs (Fundos Imobiliários)
* Shares
* ETFs
* Government bonds
* ISAs

The system must be designed with flexibility in mind, allowing **new asset classes to be added in the future**.

---

# Technical Requirements

As this is currently a personal project, a database is not required. A simple JSON file stored on the local hard drive is sufficient for persisting data.

However, the design should also support storing data in Google Drive as an alternative to local storage. The storage mechanism should be abstracted so that future changes can be implemented with minimal impact on the rest of the application.

For the initial version, all data can be loaded into memory when the application starts and saved either on user request or automatically when the application closes. Although this approach may evolve in the future, the codebase should be structured to make such changes as straightforward as possible.

The project must adhere to the following development principles:

* SOLID principles
* Clean Code practices
* Comprehensive unit test coverage

The architecture should follow Domain-Driven Design (DDD) principles to provide a clear and maintainable structure. However, given the relatively simple domain model, consisting of concepts such as Brokers, Portfolios, Assets, Transactions, and Credits, the architecture should remain pragmatic and avoid unnecessary complexity.

The primary goal of the architecture is to separate responsibilities effectively, making the system easier to maintain, extend, and evolve over time.