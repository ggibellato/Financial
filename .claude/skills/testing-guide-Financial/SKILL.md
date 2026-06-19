---
name: testing-guide-Financial
description: >
  Testing guide for Financial. Reference this skill when planning features,
  implementing code, creating tests, or reviewing changes in Financial.
  Covers what to test, at which layer, and how to set up unit tests —
  organized by artifact type for both C# (.NET) and TypeScript (React) stacks.
  Triggers on: planning Financial features, implementing Financial features,
  writing tests for Financial, reviewing Financial code, reviewing Financial tests,
  what should I test in Financial, how to test Financial, Financial test guide.
---

## §0. Purpose

This guide helps you decide **what to test** and **how to set up tests** for each artifact type in `Financial`. The project has two stacks — C# (.NET) and TypeScript (React) — each with dedicated artifact guides in `artifacts/`. Supporting references live in `references/`.

**This iteration covers unit tests only.** Integration tests (WebApplicationFactory, multi-service) are out of scope.

---

## §1. Testability Foundations

### C# (.NET)

In Clean Architecture the dependency direction (Presentation → Application → Domain) means the Domain layer has **zero external dependencies** — domain entity tests need no mocks, no file I/O, no database. They are pure logic tests.

The project has **no mocking framework** (no Moq/NSubstitute). This is intentional: since persistence is local JSON files, real implementations with temporary copies of `data.test.json` provide better confidence than fakes without adding infrastructure complexity. All Infrastructure-layer tests use this pattern.

Application-layer tests (parsers, validators) also need no mocks — they are pure transformation logic with no system boundary.

**When a C# test requires a mocking framework, it signals a layer violation** — business logic has leaked into Infrastructure, or a Domain type has an inappropriate external dependency.

### TypeScript (React)

The API client (`financialApiClient.ts`) is the single system boundary in the frontend. All component tests mock it via `vi.mock('../../api/financialApiClient')`. This is the correct boundary: mock where code crosses from your code into external HTTP.

React Testing Library tests are the TypeScript equivalent of unit tests — they test user-visible component behavior without coupling to implementation details (internal state, CSS class names, component structure).

**The mock boundary rule**: mock the API client factory once at the module level, not individual fetch calls. Utility functions and config helpers should be tested with real implementations.

---

## §2. Testing Criteria

### Worth Testing

**C#**
- Domain entity state changes after operations (e.g., `AddTransaction` updates `AveragePrice`)
- Domain entity guard clauses and validation (e.g., `ArgumentException` on empty Id)
- Value object equality, immutability, and construction validation
- Parser/validator branching: canonical normalization, null/empty handling, invalid input rejection
- Infrastructure service CRUD operations via real temp file
- Serialization round-trips (serialize → deserialize → structural equality)

**TypeScript**
- Page components: data displayed after mock API resolves, loading/error states, user interactions (form submit, navigation links)
- Shared components: render with different props (error message shown, loading spinner visible)
- Config/utility: pure functions with branching or transformation logic

### NOT Worth Testing

- C#: trivial getters/setters with no logic, xUnit framework behavior, FluentAssertions library itself
- C#: serializer property mapping with no custom transformation logic
- TypeScript: React rendering mechanics, Recharts chart rendering internals, CSS class names
- TypeScript: API client method signatures (TypeScript's type checker covers that)
- Either: mirror tests that duplicate the return value as the assertion

---

## §3. Feature Implementation Checklist

When implementing a new feature, walk through each row and check the required tests exist.

**C# (.NET)**

| Artifact created/modified | Required tests | Guide |
|---|---|---|
| Domain entity with new method | `[Fact]`: state change + guard clause | `artifacts/domain-entities.md` |
| Value object | `[Fact]`: equality, immutability, construction | `artifacts/value-objects.md` |
| Application parser/validator | `[Theory]` + `[InlineData]`: all branches, null/empty | `artifacts/application-parsers.md` |
| Infrastructure service (CRUD) | Real temp file + factory method | `artifacts/infrastructure-services.md` |
| Serialization change | Round-trip test | `artifacts/serialization.md` |

**TypeScript (React)**

| Artifact created/modified | Required tests | Guide |
|---|---|---|
| New page (`*Page.tsx`) | Render + API mock + user interactions | `artifacts/react-pages.md` |
| Shared component (`*.tsx`) | Render with variant props | `artifacts/react-components.md` |
| Utility / config function | Pure function test | `artifacts/api-client.md` |

---

## §4. Artifact Quick Reference

### C# (.NET)

| Artifact Type | Identification | Guide |
|---|---|---|
| Domain Entities | Classes in `Financial.Domain/` with factory methods | `artifacts/domain-entities.md` |
| Value Objects | Immutable types in `Financial.Domain/` | `artifacts/value-objects.md` |
| Application Parsers/Validators | `*Parser.cs`, `*Validator.cs` in `Financial.Application/` | `artifacts/application-parsers.md` |
| Infrastructure Services | `*Service.cs` in `Financial.Infrastructure/` | `artifacts/infrastructure-services.md` |
| Serialization | `*Serializer.cs`, `*Adapter.cs` in `Financial.Infrastructure/` | `artifacts/serialization.md` |

### TypeScript (React)

| Artifact Type | Identification | Guide |
|---|---|---|
| React Pages | `*Page.tsx` in `Financial.Web/src/pages/` | `artifacts/react-pages.md` |
| Shared Components | `*.tsx` in `Financial.Web/src/components/` | `artifacts/react-components.md` |
| API Client / Config / Utilities | `*.ts` in `Financial.Web/src/api/` | `artifacts/api-client.md` |
| Future types (Commands, Queries, Hooks) | — | `artifacts/future-types.md` |

---

## §5. Anti-patterns — Do NOT Do This

**C# (.NET)**
- ❌ **Mock domain entities or value objects** — they have no external dependencies; use real objects
- ❌ **Skip guard clause tests** — an unchecked invariant is an untested business rule
- ❌ **Share mutable objects across `[Theory]` data sets** — xUnit collects data before running; shared state causes cross-contamination (see `references/gotchas.md`)
- ❌ **Use `[MemberData]` with magic strings** — use `[MemberData(nameof(DataProperty))]` for rename safety
- ❌ **Put `File.Delete(tempFile)` in an Assert block** — a failing assertion skips cleanup; always use `finally` (see `references/gotchas.md`)
- ❌ **Check multiple properties without `AssertionScope`** — one failure hides all others

**TypeScript (React)**
- ❌ **Mock individual `fetch` calls** — mock the factory at the module boundary (`vi.mock('../../api/financialApiClient')`)
- ❌ **Test internal state or CSS class names** — test user-visible behavior via `screen` queries; implementation-detail tests break on refactor
- ❌ **Use `container` instead of `screen`** — `container` couples tests to DOM structure
- ❌ **Forget `mockReset()` in `beforeEach`** — mock call counts and implementations leak between tests (see `references/gotchas.md`)
- ❌ **Use `getBy*` for async content** — use `screen.findBy*` or `waitFor` for content that appears after an API call resolves

---

## §6. Scope Note

This guide covers **unit tests**: tests that run in-process with no network, no real HTTP server, and no browser. For C# Infrastructure tests, "unit" includes real JSON file I/O via temp files — this project's persistence layer has no complexity that warrants a fake.

API endpoint tests (WebApplicationFactory) and browser-level E2E tests are out of scope for this iteration.

---

## §7. References

| Topic | File |
|---|---|
| Local JSON file test strategy | `references/external-systems.md` |
| Mock boundary rules (C# and TypeScript) | `references/mock-health-rules.md` |
| File naming, directory structure, conventions | `references/file-conventions.md` |
| Stack-specific gotchas and pitfalls | `references/gotchas.md` |

---

## §8. How to Use This Guide

- **This file (SKILL.md)** — always loaded. Contains core rules, quick reference, and anti-patterns.
- **`artifacts/`** — one file per artifact type. Read when creating or modifying that artifact.
- **`references/`** — supporting content. Read for mock strategies, conventions, or gotchas.

When working on a feature:
1. Check §3 to identify which artifacts need tests
2. Read the corresponding `artifacts/*.md` file for the full recipe
3. Consult `references/` as needed for setup details, conventions, or pitfalls
