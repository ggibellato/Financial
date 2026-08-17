# System Overview

See legend in [README.md](README.md).

## What this is

A personal financial management tool, single-user and self-hosted (not built to scale to multiple tenants — **CONFIRMED**, `CLAUDE.md`). It consolidates two unrelated concerns for one household:

- **Investment tracking** — brokerage holdings across Brazil and the United Kingdom.
- **CashFlow tracking** — household income, expenses, bank/card balances, a savings-split reserve ledger, recurring bills, and an informal loan ledger with the user's mother.

Both were originally maintained in personal spreadsheets (`Despesas.xlsx` for CashFlow) before being migrated into this application — **CONFIRMED**, `context.md`, `docs/prd/P11-prd-cashflow-tracking/cashflow-context.md`. The product owner has stated the current priority is reaching an MVP sufficient to retire the spreadsheets entirely (see [10-domain-cashflow.md](10-domain-cashflow.md)).

## The two bounded contexts

**CONFIRMED** — Investment and CashFlow are two fully independent DDD bounded contexts: no cross-domain entity references, each with its own JSON data file, its own storage-provider configuration, and its own Domain/Application/Infrastructure project triad. This is explicit in `context.md` and verified empirically (zero `using Financial.CashFlow.*` in any Investment project, and vice versa).

They share real-world vocabulary in a few places without being related in code — see [09-domain-investment.md](09-domain-investment.md) and [10-domain-cashflow.md](10-domain-cashflow.md) for the specific disambiguations (e.g. "Investment Account/Snapshot" means two different, deliberately separate things; "Bank" in CashFlow and "Broker" in Investment are unrelated entities that happen to share institution names like Trading212 and Chase).

## The three front ends

**CONFIRMED**, all three exist and are actively maintained:

- **`Financial.Api`** — ASP.NET Core. Serves the REST API and hosts the built React SPA from the same process/port in production.
- **`Financial.App`** — WPF desktop client. A client for **both** bounded contexts (not Investment-only). Architecturally distinct from the other two: it hosts the Application/Infrastructure layers of both contexts **in-process** and is **not** an HTTP client of `Financial.Api` — see [04-wpf-app.md](04-wpf-app.md).
- **`Financial.Web`** — React + TypeScript SPA. The only front end that talks to `Financial.Api` over HTTP. Also covers both contexts.

`Financial.App` (WPF) is treated as the UX source of truth that `Financial.Web` is expected to stay at feature parity with — **CONFIRMED**, `CLAUDE.md`, `dev-util/ai/level-web-front-end.md`.

## No database, no auth

**CONFIRMED** — There is no relational database anywhere in the system. Each bounded context persists to a single JSON document, loaded once at process startup (see [07-data-persistence.md](07-data-persistence.md)).

**CONFIRMED** — There is no authentication or authorization anywhere in the system (no `[Authorize]` attributes, no auth middleware, no identity provider). CORS origin allowlisting is the only access-control mechanism, consistent with the single-user/self-hosted framing.

## Top-level component map

```
Financial.Web (React SPA)  ──HTTP──▶  Financial.Api (ASP.NET Core)
                                             │
                    ┌────────────────────────┴────────────────────────┐
                    ▼                                                  ▼
     Investment.Application/Infrastructure         CashFlow.Application/Infrastructure
                    │                                                  │
                    ▼                                                  ▼
          Investment.Domain (no deps)                     CashFlow.Domain (no deps)

Financial.App (WPF)  ──in-process, independent of Financial.Api──▶  same Application/Infrastructure
                                                                     layers as above, own composition root

Both Infrastructure sides ──▶ Financial.Shared.Infrastructure (JSON storage, sync-status, retry policy)
```

See [02-architecture.md](02-architecture.md) for the full dependency map and the confirmed layering inversion (`GoogleFinancialSupport`).

## Where to go next

- Building/changing backend logic → [02-architecture.md](02-architecture.md), [03-backend-dotnet.md](03-backend-dotnet.md)
- Changing the WPF app → [04-wpf-app.md](04-wpf-app.md)
- Changing the web app → [05-web-frontend.md](05-web-frontend.md)
- Adding/changing an API endpoint → [06-api.md](06-api.md)
- Anything touching how data is stored/saved → [07-data-persistence.md](07-data-persistence.md)
- Anything touching an external price/FX/storage service → [08-integrations.md](08-integrations.md)
- Understanding a business rule → [09-domain-investment.md](09-domain-investment.md) or [10-domain-cashflow.md](10-domain-cashflow.md)
- Writing tests → [11-testing.md](11-testing.md)
- CI/deploy → [12-deployment.md](12-deployment.md)
