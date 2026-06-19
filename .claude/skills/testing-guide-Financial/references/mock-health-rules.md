> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Mock Health Rules

## The Boundary Rule

**Mock across architecturally significant boundaries, not within.**

A boundary is where your code hands off to something outside your control: a file system, an HTTP endpoint, a message broker. Within a single layer, use real objects.

---

## C# (.NET)

This project has **no mocking framework** — and that is correct for the current architecture. The only external dependency is a local JSON file, which is simple enough to use real implementations with temp files.

| Dependency | How to handle |
|---|---|
| Domain entities and value objects | Always real — these are the things being tested |
| `JSONRepository` | Real instance with temp file (Infrastructure tests) |
| `NavigationService` | Real instance — construct it with the same repository |
| `LocalJsonStorage` | Real — it is just file I/O on the temp file |
| `InvestmentsSerializerAdapter` | Real — serialization logic under test |
| External HTTP API (if added in future) | Define a manual stub implementing the interface |

### When a C# test needs many setup steps

If constructing a service requires assembling many dependencies, the test is a signal — not a problem to solve with mocks. Consider:
1. Does the factory method `CreateService()` cover reuse of this setup?
2. Is the class doing too many things (SRP violation)?

### Inline stubs for future Command/Query handlers

If Application-layer handlers are added and need isolation from the real repository, use a minimal inline stub instead of a mocking framework:

```csharp
private sealed class StubRepository : IRepository
{
    public Investments Data { get; set; } = Investments.Create();
    public Task<Investments> GetAsync() => Task.FromResult(Data);
    public Task SaveAsync(Investments data) { Data = data; return Task.CompletedTask; }
}
```

This is simpler than Moq for this project's scale and keeps the "no mocking framework" pattern.

---

## TypeScript (React)

| Dependency | How to handle |
|---|---|
| `financialApiClient` factory | `vi.mock` the entire module once per test file |
| `MemoryRouter` / `Routes` | Always real — pages require router context |
| React context (if added) | Real test provider wrapping the component |
| Individual `fetch` calls | Never — mock at the client factory, not lower |
| Utility functions | Always real — they are pure functions being tested |
| Child components | Real by default; only mock if a child has heavy side effects that are impossible to control in tests |

### Mock scope

One `vi.mock(...)` call per module per file. Do not add multiple partial mocks for the same module. If a page uses only 3 of 10 API methods, include only those 3 in the mock factory — missing methods are `undefined`, which will throw if accidentally called, making missing mocks visible.

### Signal: too many vi.fn() calls in one test

A test that creates many `vi.fn()` instances may be an integration test in disguise. Consider whether a real implementation with controlled input would be simpler and more meaningful.
