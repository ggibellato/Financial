# Testing

See legend in [README.md](README.md).

## Stack

**CONFIRMED — uniform across all 12 .NET test projects, no mocking framework anywhere.** xUnit + FluentAssertions + `coverlet.collector`. Zero Moq/NSubstitute references anywhere. Every collaborator that needs faking gets a hand-written fake, centralized in `Tests/Financial.TestUtilities`: `StubCashFlowRepository`/`StubInvestmentRepository` (full in-memory repository fakes, backed by real `List<T>`s, with `SaveChangesCallCount`/`ThrowOnNextSave` instrumentation), sync-status stubs, `FakeHttpMessageHandler`, `FakeTimeProvider`, `TestDataPaths`.

**OBSERVED — one package-version outlier.** `Financial.Investment.Infrastructure.Tests.csproj` pins older xunit/coverlet/Test.Sdk versions than every sibling test project. Reason **UNKNOWN**.

## Architecture enforcement

**CONFIRMED.** `Financial.Architecture.Tests` — plain reflection over `Assembly.GetReferencedAssemblies()` (not NetArchTest or a similar library), asserted with FluentAssertions. Checks only the **forbidden inward edges** per bounded context: Domain↛Application, Domain↛Infrastructure, Application↛Infrastructure. Does **not** check Presentation-layer boundaries (nothing mechanically prevents `Financial.App`/`Financial.Api` from reaching into the wrong project).

## Per-layer test style

**CONFIRMED**, sampled across both contexts:

- **Domain** — pure entity/invariant unit tests, no I/O, no test doubles.
- **Application** — services tested against the hand-written stub repositories, asserting both return values and `SaveChangesCallCount`. Constructor null-guard tests are a consistent pattern.
- **Infrastructure** — real temp-file I/O round-trips for JSON storage (`Path.GetTempPath()`); HTTP-dependent integrations (e.g. Frankfurter) use `FakeHttpMessageHandler`. **UNKNOWN** whether GoogleDrive-backed storage has any automated test coverage — not confirmed present or absent.
- **API** — real in-memory integration tests via `WebApplicationFactory<Program>` (`Tests/Financial.Api.Tests`), booting the actual API against temp-file-backed JSON data (copies of `TestDataPaths.DataJsonFile`), ~30 files, one per resource group — full HTTP round-trips, not isolated controller unit tests.
- **WPF (`Financial.Presentation.Tests`)** — references `Financial.App.csproj` directly; ~45+ files across Converters, Helpers, Input, `Navigation/NavTreeTests.cs`, and ViewModels for both contexts, including form-validation tests and at least one XAML binding test.
- **Web** — Vitest + jsdom + React Testing Library, behavior-focused (queries by role/text, not snapshots); ~90% file-to-test ratio. Plus a genuine Playwright end-to-end smoke test (`Financial.Web/scripts/smoke-test.mjs`) run against a fully published build in CI.

## Coverage tooling

**CONFIRMED, file exists and is well-formed — status in CI is unclear.** `coverlet.runsettings` configures the `XPlat code coverage` collector with one exclusion, `ExcludeByFile: **/obj/**/*.cs` (drops generated code, e.g. OpenAPI XmlComment generator output). **OBSERVED — `.github/workflows/build.yml`'s `dotnet test` step does not reference this file or collect/publish coverage at all.** Whether coverage is used locally/IDE-only, or not used at all, is **UNKNOWN**.

## Running tests

```
dotnet test                                          # all .NET test projects
dotnet test Tests/Financial.CashFlow.Domain.Tests     # one project
dotnet test --filter "FullyQualifiedName~ExpenseTests.Should_Reject_Negative_Value"  # one test

cd Financial.Web
npm test              # vitest run
npm run test:watch
npm run smoke-test    # Playwright, requires a running API + web server
```
