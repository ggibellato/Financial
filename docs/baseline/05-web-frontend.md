# Web Frontend (Financial.Web)

See legend in [README.md](README.md). Financial.Web is a separate npm project, not part of `Financial.slnx`.

## Overall structure

**CONFIRMED** — SPA with client-side routing via `react-router-dom` v7 (`BrowserRouter` in `main.tsx`). Structure: `pages/` (route-level containers) → compose `components/` (presentational/reusable), driven by `hooks/` (data-fetching + form-state logic), talking to `api/` (HTTP client + hand-written types), one global React `Context` (`context/SelectedNodeContext.tsx` — currently-selected Investment tree node + scope), plus `navigation/`, `utils/`, `styles/`.

The only HTTP consumer of `Financial.Api` in this codebase — `Financial.App` (WPF) is not (see [04-wpf-app.md](04-wpf-app.md)).

## Component structure

**OBSERVED — mixed responsibility, no single rule.** Pages call one or more hooks and compose components; derived-state logic lives partly in hooks (e.g. totals computed inside `useMonthly`) and partly inline in page components (e.g. tab-switch cancel logic in `MonthlyPage`).

Co-location convention is **inconsistent across the codebase**: components/pages use a sibling `__tests__/` folder; hooks/utils use an inline `*.test.ts` next to the source file.

## TypeScript

**OBSERVED — no explicit `strict: true`** in either `tsconfig.app.json` or `tsconfig.node.json` — only granular flags (`noUnusedLocals`, `noUnusedParameters`, `noFallthroughCasesInSwitch`, `erasableSyntaxOnly`).

`api/types.ts` is **hand-written**, not generated from an OpenAPI spec or any codegen tool. Types are manually mirrored against backend Application DTOs (see [06-api.md](06-api.md)) with no build-time guarantee they stay in sync.

## State / data-fetching

**CONFIRMED — no external state or data-fetching library.** Dependencies beyond React itself are only `react-router-dom` and `recharts` (charting) — no react-query, SWR, Redux, Zustand, or axios; `fetch` is used directly.

A shared `useAsyncResource` reducer-based primitive (`{data, isLoading, error, retry}`) exists, but more complex hooks (e.g. `useMonthly`) hand-roll their own equivalent `useReducer` instead of composing it once extra action types are needed (**OBSERVED** — a duplicated, not shared, pattern).

`window.confirm` is called directly from inside data hooks (e.g. delete confirmations in `useMonthly`) — couples data logic to a browser global.

## API client

**CONFIRMED.** `api/financialApiClient.ts` — a single hand-written factory (`createFinancialApiClient`) exposing ~65 typed methods, one per backend endpoint, built on native `fetch`. Base URL resolves from `API_BASE_URL`, a non-`VITE_`-prefixed env var explicitly wired into `vite.config.ts` via Vite's `define` (baked in at build time). Must always be a relative path in Docker/production (`/api/v1/financial`) — never empty, or the SPA fallback route returns HTML instead of JSON for API calls. Dev server proxies `/api` → `http://localhost:5190`.

Errors are normalized to a typed `ApiError extends Error` (with `.status`), which attempts to parse an ASP.NET Core `ProblemDetails` body (`detail`/`title`) and falls back to a generic message.

## Routing

**OBSERVED — two independent sources of truth for the route list.** Routes are declared once, flat, in `main.tsx` (no lazy-loading, no nested route config module), and separately, by hand, in `navigation/navTree.ts` for the sidebar. Nothing keeps them mechanically in sync. `RootRedirect.tsx` restores the last-visited domain (Investments/CashFlow) from storage and redirects accordingly.

## Styling

**CONFIRMED** — plain CSS, one file per component/page (`Component.css`, imported directly — not CSS Modules, no CSS-in-JS, no framework). One shared stylesheet, `styles/data-table.css`, imported globally for tabular UI conventions reused across pages.

## Testing

**CONFIRMED — high coverage ratio.** Vitest + jsdom + `@testing-library/react` (+ `jest-dom`, `user-event`), behavior-focused (queries by role/text, not snapshots). ~70 test files against ~78 non-test source files (~90% file ratio). `setupTests.ts` registers matchers, auto-cleanup, and a `ResizeObserver` polyfill.

A separate Playwright script (`scripts/smoke-test.mjs`, `npm run smoke-test`) runs genuine end-to-end checks against a live build — also run in CI (`.github/workflows/build.yml`'s `browser-smoke-test` job). See [11-testing.md](11-testing.md) and [12-deployment.md](12-deployment.md).

## Known inconsistencies (descriptive, not a to-do list)

- Two test co-location conventions in the same codebase (components/pages vs. hooks/utils).
- Two sources of truth for the route list.
- `useAsyncResource` exists but is bypassed by hand-rolled equivalents in more complex hooks.
- `window.confirm` called directly from data hooks.
- No TypeScript `strict` mode.
- Hand-maintained API types with no codegen/shared-schema mechanism against the backend DTOs.
