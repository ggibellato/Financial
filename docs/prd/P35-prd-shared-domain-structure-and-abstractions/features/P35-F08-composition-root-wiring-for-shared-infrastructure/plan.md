# Implementation Plan: F08. Composition Root Wiring for Shared Infrastructure

**Prerequisites:**
- F01, F06, and F07 merged to `main` (F08's PRD dependencies — all merged; F06/F07 already implemented F08's Core Scope as a necessary pull-forward, documented in their own specs)

### Stage 1: Verify the already-shipped wiring

**1. Confirm composition-root registrations** - Re-read `Financial.Api/Program.cs` and `Financial.App/App.xaml.cs` to confirm `IJsonStorageFactory` and both `ShutdownFlushHostedService<T>` registrations are present, correctly ordered, and unchanged since F06/F07.

**2. Confirm the reference boundary** - Search the repo for every `ProjectReference` to `Financial.Shared.Infrastructure.csproj` and confirm only the two composition roots and the PRD-exempt test/Tools projects have one.

### Stage 2: Full verification

**3. Full solution verification** - Run a full solution build and the full test suite (with coverage settings), confirming `ShutdownFlushHostedServiceRegistrationTests` still proves both hosted services resolve from the real host.

**4. Manual docker-compose smoke check** - Bring the stack up via `docker-compose up --build`, confirm the API starts cleanly, then bring it down — the one PRD acceptance criterion this feature scope explicitly calls out as a manual check.
