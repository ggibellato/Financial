# Architecture Rules (Mandatory)

These rules are mandatory and non-negotiable for all generated code.

## Clean Code

* Follow Clean Code principles.
* Functions must have a single responsibility.
* Avoid long methods.
* Avoid code duplication.
* Use meaningful names.
* No magic strings or magic numbers.
* Keep cyclomatic complexity low.

## SOLID

All implementations must follow SOLID principles.

* Single Responsibility Principle
* Open Closed Principle
* Liskov Substitution Principle
* Interface Segregation Principle
* Dependency Inversion Principle

## Architecture

The solution follows Clean Architecture.

Layers:

* Domain
* Application
* Infrastructure
* Presentation

Dependency direction:

Presentation -> Application -> Domain

Infrastructure implements interfaces defined by Domain or Application.

Domain must never depend on Infrastructure.

## Domain Layer

Contains:

* Entities
* Value Objects
* Domain Services
* Domain Events

Must contain no framework code.

Must contain no database code.

## Application Layer

Contains:

* Use Cases
* Commands
* Queries
* DTOs
* Validators

Coordinates business workflows.

Must not contain persistence implementation details.

## Infrastructure Layer

Contains:

* Database implementations
* External APIs
* Messaging
* File system access
* Repository implementations

Must depend on abstractions.

## Presentation Layer

Contains:

* Controllers
* Endpoints
* UI
* API Contracts

Must not contain business logic.

## Testing

Every new feature must include:

* Unit tests
* Integration tests where applicable

No feature is complete without tests.

## Before Writing Code

Always:

1. Explain where the feature belongs.
2. Identify impacted layers.
3. Explain why the design follows Clean Architecture.
4. Identify SOLID principles being applied.

## Before Finishing

Perform a self-review and verify:

* Clean Code
* SOLID
* Clean Architecture
* Test coverage
* No layer violations

If any rule is violated, stop and propose a correction.

## Definition of Done

A feature is NOT complete unless:

* Architecture reviewed.
* Clean Architecture respected.
* SOLID principles respected.
* No layer violations.
* No business logic in Presentation.
* No infrastructure concerns in Domain.
* Unit tests added.
* Integration tests added when appropriate.
* Existing tests still pass.
* New code is documented where necessary.

Before marking work as complete, provide a checklist showing compliance with all Definition of Done items.

## Application details

This is a personal project and is intended to be installed a copy for each person that will use.
Does not require to scale or should not also have many updates or changes.

It should follow all the standars above but also know that it does not OVER ENGINEERING.