# Documentation Baseline — Current State

This folder is the durable, current-state reference for the Financial application: what exists today, how it's built, and what business rules govern it. It is written for future AI-assisted development sessions (and humans) who need to understand the system before changing it.

It describes **only** what currently exists. It does not describe an ideal architecture, does not propose refactoring, and does not invent requirements beyond what's documented or confirmed by the product owner.

## Relationship to `docs/discovery-*.md`

Three discovery documents (`discovery-repository-map.md`, `discovery-architecture.md`, `discovery-business-domains.md`) preceded this baseline and record the investigation itself — including open questions, contradictions, and the clarification log where the product owner resolved them. They remain as historical record.

This baseline is the **distilled result**: every open question from the discovery pass has been folded in as a resolved fact (or, where genuinely unresolved, explicitly marked UNKNOWN). Read this folder first; consult the discovery docs only if you need the "why" behind a resolved question.

## Classification legend

Every factual claim in this baseline is tagged with one of:

- **CONFIRMED** — explicitly documented in a PRD/`context.md`/spreadsheet-origin doc, backed by an explicit test, or explicitly confirmed by the product owner during the discovery/clarification pass.
- **OBSERVED** — implemented in code; no document or product-owner statement frames it as a deliberate requirement, but it is current, real behavior.
- **INFERRED** — a reasonable interpretation of intent from the implementation shape, not directly stated anywhere.
- **UNKNOWN** — cannot be established from the repository or the clarification pass. Treat as a question to raise, not an assumption to build on.

Untagged prose is structural/descriptive (file locations, naming, "what's where") rather than a business or architectural claim.

## Index

| File | Covers |
|---|---|
| [01-system-overview.md](01-system-overview.md) | What the system is, who it's for, the two bounded contexts, the three front ends, top-level component map |
| [02-architecture.md](02-architecture.md) | Clean Architecture layering, project dependency map, composition roots, cross-cutting concerns |
| [03-backend-dotnet.md](03-backend-dotnet.md) | Domain/Application/Infrastructure shape, DI, logging, error handling, configuration |
| [04-wpf-app.md](04-wpf-app.md) | Financial.App — MVVM pattern, navigation, Views/ViewModels, state management |
| [05-web-frontend.md](05-web-frontend.md) | Financial.Web — React/TypeScript structure, routing, state, testing |
| [06-api.md](06-api.md) | Financial.Api — controllers, routes, DTOs, middleware, auth, consumers |
| [07-data-persistence.md](07-data-persistence.md) | JSON-document persistence model, storage providers, sync/debounce behavior |
| [08-integrations.md](08-integrations.md) | External services consumed by each bounded context |
| [09-domain-investment.md](09-domain-investment.md) | Investment bounded context's business capabilities and rules |
| [10-domain-cashflow.md](10-domain-cashflow.md) | CashFlow bounded context's business capabilities and rules |
| [11-testing.md](11-testing.md) | Test project inventory, conventions, coverage tooling |
| [12-deployment.md](12-deployment.md) | CI pipeline, Docker, local deploy tooling |
