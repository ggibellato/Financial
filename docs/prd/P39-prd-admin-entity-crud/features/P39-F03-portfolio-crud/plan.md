# Implementation Plan: F03 Portfolio CRUD

**Prerequisites:**
- F01 (Admin Navigation Foundation) and F02 (Broker CRUD) merged — reuses the `admin/investment/portfolios` route/view registration and the Admin CRUD screen template F02 established.
- No new libraries.

### Stage 1: Domain Rules

**1. Portfolio create/rename rules** - Add `Broker.CreatePortfolio` (duplicate-name rejection) and `Broker.RenamePortfolio` (per-broker uniqueness, self-rename no-op) without altering `AddPortfolio`'s existing find-or-create behavior used by the Move workflow.

### Stage 2: Application, API, and Contract

**2. Portfolio Application service and DTOs** - Extend `IPortfolioService`/`PortfolioService` with list/create/update, add `PortfolioDTO`/`PortfolioCreateDTO`/`PortfolioUpdateDTO`, resolving the parent broker from the Active list on create.

**3. Portfolios controller and OpenAPI** - Add GET/POST/PUT actions to `PortfoliosController`, regenerate the OpenAPI snapshot, and regenerate the frontend generated types.

### Stage 3: Web UI

**4. Web list, create/edit, delete-confirm** - Build `PortfoliosPage`, `PortfolioFormDialog`, and `usePortfolios`, wire `admin/investment/portfolios` to the new page in place of the placeholder.

### Stage 4: WPF UI

**5. WPF list, create/edit, delete-confirm** - Build `PortfoliosView`, `PortfolioFormDialog`, and their ViewModels, register `admin-portfolios` in `MainWindow.xaml.cs`'s `viewsByKey` in place of the placeholder.

### Stage 5: Cross-Platform Verification

**6. Full-suite verification and PRD acceptance criteria** - Run the complete backend and frontend test suites, confirm every F03 acceptance criterion and the F02/F03 cross-feature integration criteria against a fresh test run, and check off the satisfied boxes in the PRD.
