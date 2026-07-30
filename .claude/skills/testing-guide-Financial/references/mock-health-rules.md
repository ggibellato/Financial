> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Mock Health Rules

## The Boundary Rule

**Mock across architecturally significant boundaries, not within.** A boundary is where your code hands off to something outside your control: a file system, an HTTP endpoint, Google's SDK, the browser's `fetch`. Within a single layer, use real objects.

---

## C# (.NET) — no mocking framework, anywhere

This is a deliberate, project-wide, re-confirmed choice (no Moq/NSubstitute anywhere in the solution). Three real techniques cover every case instead:

| Situation | Technique | Example |
|---|---|---|
| Application service depends on an interface (repository, provider) | Hand-written stub class implementing the interface, only the members the test needs | `StubRepository`, `StubFinanceService`, `DividendServiceStub` |
| Infrastructure crosses a real system boundary with no useful branching to isolate | Real implementation, real temp resource | `LocalJsonStorage` + temp file, `XLWorkbook` in-memory |
| Infrastructure calls a configured library/HTTP client whose *configuration* is the thing under test | Real client, fake transport/delegate | `HttpClient` + `FakeHttpMessageHandler` (Frankfurter); `GoogleDriveJsonStorage`'s injected read/write delegates |
| DI wiring | Real `ServiceProvider` built from real `IConfiguration` | `CashFlowInfrastructureServiceCollectionExtensionsTests` |
| Whole-app HTTP behavior | Real `WebApplicationFactory<Program>`, swap only the one boundary under test via `RemoveAll<T>()`+`AddSingleton<T>()` | `ApiTestFactory` |

**When a C# test reaches for a mocking framework, that's the signal to stop and ask why** — either the unit under test has too many collaborators (split it), or business logic has leaked into a layer where it doesn't belong (Domain calling Infrastructure directly, for instance).

### Stub pattern

```csharp
internal sealed class StubRepository : IRepository
{
    private readonly List<Broker> _brokers;
    public StubRepository(IEnumerable<Broker> brokers) => _brokers = brokers.ToList();

    public IEnumerable<Broker> GetBrokerList(InvestmentScope scope = InvestmentScope.Active) => _brokers;

    // Anything the test under construction doesn't exercise stays unimplemented —
    // a NotImplementedException here is a loud signal if a test accidentally needs it.
    public IEnumerable<Asset> GetAssetsByBroker(string name, InvestmentScope scope = InvestmentScope.Active) =>
        throw new NotImplementedException();
}
```

### Fake HTTP transport pattern

```csharp
private sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
        Task.FromResult(_responder(request));
}
```

Copy this ~10-line shape for any new external-HTTP integration rather than introducing a mocking library.

---

## TypeScript (React)

| Dependency | How to handle |
|---|---|
| `financialApiClient` factory | `vi.mock` the entire module once per test file, at the module boundary |
| Individual `fetch` calls (outside `financialApiClient.ts` itself) | Never mock directly — mock the client factory instead |
| `MemoryRouter` / `Routes` | Always real — pages require router context |
| `SelectedNodeContext` | Real test provider (`createSelectedNodeWrapper`) wrapping the component/hook |
| Utility functions | Always real — they are pure functions being tested |
| Child components | Real by default; only stub if a child has side effects impossible to control in tests |

### Mock scope

One `vi.mock(...)` call per module per file. If a page/hook uses only 3 of 10 API methods, include only those 3 in the mock factory — missing methods are `undefined`, which throws if accidentally called, making an incomplete mock visible immediately rather than silently returning stale data.

### Signal: too many `vi.fn()` calls in one test

A test that creates many `vi.fn()` instances just to get one component/hook under test may be answering an integration-test question with a unit test. Consider whether the seam is in the wrong place, or whether a smaller unit should be extracted.
