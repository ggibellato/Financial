# Implementation Plan: API Host Boundary Tests

**Prerequisites:**
- An up-to-date `main`, `gh` authenticated
- F01's `ApiTestFactory` (stub FX provider) and F08's delay seam on `GoogleRetryPolicy` are on `main`
- Stages merge in order; each is one PR that leaves `main` deployable and the suite green
- A defect found while writing a test is fixed in the stage that owns that behaviour, or reported in the PR if it falls outside F05

### Stage 1: SPA Fallback Excludes the API (PR1)

**1. Test host web root** - Let `ApiTestFactory` serve a temporary web root with a known `index.html`, deleted on dispose, and fail setup explicitly when the file is missing.

**2. Fallback fix** - Constrain the SPA fallback in `Program.cs` so `/api` and `/api/**` never reach `index.html`, while wrong-method calls on real routes keep returning 405.

**3. Fallback tests** - Add the host tests for the unknown API route, the bare `/api` path, client routes, the `/apiary` look-alike and the unchanged real-route behaviour.

### Stage 2: Exception Mapping and Production Boundary (PR2)

**4. Transient mapping** - Map `TransientStorageException` to 503 with a fixed `Retry-After`, logging the type only, following the existing handler shape.

**5. Mapping table and completeness** - Add one table-driven test over every mapped exception type, plus the completeness check that names any exception type neither mapped nor on the service-layer allowlist, with a self-test of the scanner.

**6. Production boundary** - Add host tests in the `Production` environment proving an unmapped exception returns a ProblemDetails 500 with no message, stack trace or financial value, and that a transient failure returns 503 through the real pipeline.

### Stage 3: GoogleDriveClient Contract Tests and Ticks (PR3)

**7. Drive seam** - Give `GoogleDriveClient` an internal construction path that accepts a service factory and a delay function, thread the delay through every retry call, add an internal constructor on `GoogleDriveFileClient`, and expose internals to the existing integration test project.

**8. Contract tests** - Add the fake HTTP handler and the contract tests for listing, download, name and shortcut resolution, id caching, not-found and multiple-match errors, upload, the 429 backoff, the 503 translation and the credential-rejection path.

**9. Documentation** - Record the fake-handler pattern for Google SDK clients in the testing guide.

**10. PRD ticks** - Tick the satisfied F05 boxes and the F05 cross-feature box in their own commit.
