# Plan: React UI alongside WPF

Task Understanding:
- Add a new React UI for Financial.App while keeping the existing WPF UI and C# backend intact.
- Other WPF tools (import utilities) remain WPF-only and are out of scope for React.
- Follow layered architecture (Domain/Application/Infrastructure/Presentation) and existing integrations.
- Use latest React version 19.2 with best practices and align with current testing/naming conventions.
- Use a consistent API route prefix `/api/v1/financial` to avoid clashes with other APIs.
- Break work into small, testable steps for validation and review after each change.
- Match WPF feature parity and UX where possible: left navigation tree + detail panel, tabbed asset details, CRUD for operations/credits, dividend/price tools, and credits chart.

Affected Areas:
- Presentation (new Web API + React client)
- Financial.Application (DTOs/interfaces)
- Financial.Infrastructure (repositories/services)
- Integrations (Google/web providers, dividend/price parsing)
- Tests (backend + frontend)
- Solution configuration (new projects and appsettings)

Implementation Plan:
1. Inventory WPF use-cases, Application services/DTOs, repository selection/config keys, and integrations to expose; list initial API endpoints.  
   Reason: establishes the parity baseline and avoids missing behaviors during the migration.
2. Decide and document new project locations/names in the solution (Financial.Api and Financial.Web), and confirm frontend tooling choices (React 19.2, TypeScript, Vite, Vitest).  
   Reason: locks the structure and toolchain early so later steps stay consistent.
3. Add an ASP.NET Core Web API presentation project shell with DI composition for Application/Infrastructure; set base route prefix `/api/v1/financial`, include a health endpoint and build/test validation.  
   Reason: provides a stable API host for all later UI slices and tests.
4. Add configuration binding for repository selection and storage paths in the API (appsettings + environment overrides); validate with a simple endpoint.  
   Reason: ensures data access is correct across environments before adding features.
5. Define API contracts and mapping strategy (reuse Application DTOs or API-specific DTOs); implement the first endpoint plus its unit/integration tests.  
   Reason: sets the contract patterns and test harness for subsequent endpoints.
6. Incrementally add endpoints one by one, each with tests and focused validation before moving on.  
   Reason: keeps risk low and failures isolated.
7. Create the React app scaffold (React 19.2 + TypeScript + Vite), set up lint/test scripts, and verify build/test locally.  
   Reason: provides a clean, tested frontend baseline.
8. Add a typed API client layer with error handling and unit tests; configure base URL/CORS alignment with the API.  
   Reason: centralizes HTTP behavior and avoids duplicated fetch logic.
9. Implement UI features in slices, matching WPF structure and behavior, each with component tests and manual validation.  
   Reason: delivers parity incrementally without breaking navigation flows.
   9.1. Build WPF-style shell: left navigation tree, right details panel, and in-panel tabs (Summary, Operations, Credits).  
        Reason: replicates the primary UX pattern users rely on.
   9.2. Expose missing API endpoints for operations/credits update/delete (and add tests), then wire React UI to allow edit/delete with confirmation.  
        Reason: WPF supports full CRUD; parity requires backend + UI support.
   9.3. Add credits chart in the Credits tab with WPF filters and stacked/grouped modes (Recharts).  
        Reason: the chart is a key insight view in WPF and should behave the same.
   9.4. Add “Shares Dividend check” UI in React, backed by API endpoints for dividend lookup and summary calculations.  
        Reason: keeps the tool available in the new UI without relying on WPF.
   9.5. Add “Read Assets current values” UI with progress and results, backed by API endpoints for current prices.  
        Reason: parity for the price-check utility workflow.
10. Decide on frontend integration/e2e testing scope and, if needed, add a minimal smoke test for a critical flow.  
    Reason: validates the most important flows once parity is achieved.
11. Document run/build/test workflows for both backend and frontend and keep WPF unaffected.  
    Reason: helps contributors operate both UIs safely in parallel.
