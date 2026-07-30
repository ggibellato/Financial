> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Stack-Specific Gotchas and Pitfalls

---

## C# (.NET)

### Temp file / factory leak on test failure

If cleanup runs after an assertion and the assertion fails, cleanup is skipped.

```csharp
// ❌ File leaks if assertion throws
var result = await service.AddAsync(request);
result.Should().NotBeNull();
File.Delete(tempFile);   // skipped on failure

// ✅ Always in finally
try
{
    var result = await service.AddAsync(request);
    result.Should().NotBeNull();
}
finally
{
    File.Delete(tempFile);
}
```

Same principle applies to `ApiTestFactory` — always `await using var factory = ...` (or `using` if not awaited) so its `Dispose` (which deletes its own temp JSON files) runs even when an assertion fails mid-test.

### DI override must happen before `CreateClient()`

`ApiTestFactory().WithWebHostBuilder(builder => builder.ConfigureServices(services => { services.RemoveAll<T>(); services.AddSingleton<T>(stub); }))` must be set up before `factory.CreateClient()` is called — the host builds lazily on first use, but the override registration itself must be chained onto the factory before that first request.

### xUnit Theory data sharing

xUnit collects all `[Theory]` data before running any test. If data rows share a mutable object, state from one run leaks into another.

```csharp
// ❌ All rows share the same list instance
private static readonly List<string> shared = new() { "a" };
public static IEnumerable<object[]> Data => new[] { new object[] { shared } };

// ✅ New instance per row
public static IEnumerable<object[]> Data => new[] { new object[] { new List<string> { "a" } } };
```

### `null` in `[InlineData]`

`null` is not a valid C# attribute argument by itself in every position; combine it with non-null cases via `[Theory]` parameters typed as nullable, or use `[MemberData]`:

```csharp
[Theory]
[InlineData(null)]
[InlineData("")]
public void TryResolve_BlankOrNullLabel_ReturnsFalse(string? label)
{
    var result = CategoryResolver.TryResolve(label, out _);
    result.Should().BeFalse();
}
```

### Multiple assertion failures hidden

FluentAssertions stops at the first failure by default. Wrap multiple assertions in `AssertionScope` to see all failures in one run.

```csharp
using (new AssertionScope())
{
    asset.Quantity.Should().Be(10);
    asset.AveragePrice.Should().Be(5);
    asset.Active.Should().BeTrue();
}
```

### `[MemberData]` magic strings

```csharp
[MemberData("NullValues")]        // ❌ silently breaks after rename
[MemberData(nameof(NullValues))]  // ✅ compile error after rename
```

### ClosedXML workbook disposal

`XLWorkbook` holds unmanaged resources — always `using var workbook = new XLWorkbook();` in spreadsheet-import tests (`artifacts/spreadsheet-import.md`), even though nothing is written to disk.

### `FrankfurterExchangeRateProvider`-style fakes: set `BaseAddress`

The fake `HttpMessageHandler` ignores the request URL by default (`_ => new HttpResponseMessage(...)`), so a bug that changes the request path silently won't be caught unless the test also asserts on the captured `HttpRequestMessage`. If a new HTTP-backed provider's URL/params have their own branching, capture and assert the request inside the responder delegate, not just the response.

---

## TypeScript (React)

### `vi.mock` hoisting

`vi.mock(...)` is hoisted to the top of the file before all imports. Variables declared in module scope with `const`/`let` are NOT hoisted the same way, but a `vi.fn()` assigned to a `const` at module scope works because the mock factory function body only *runs* later, by which point the const is initialized:

```typescript
const getDataMock = vi.fn<FinancialApiClient['getAssetDetails']>()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({ getAssetDetails: getDataMock }),
}))
```

### Mock pollution between tests

`mockReset()` resets both call count and implementation. `mockClear()` resets only call count. Use `mockReset()` in `beforeEach` unless the implementation is deliberately shared across an entire `describe` block.

| Method | What it resets |
|---|---|
| `mockClear()` | Call count and call arguments only |
| `mockReset()` | Call count + implementation (returns `undefined`) |
| `mockRestore()` | Restores the original implementation (`vi.spyOn` only) |

### `screen` vs `container`

```typescript
// ✅ Query by role/text/label — user-centric, doubles as an a11y check
screen.getByRole('button', { name: /save/i })

// ❌ Couples to DOM structure, breaks on refactor
container.querySelector('button.save-btn')
```

### Async assertions — `getBy` vs `findBy`

`getBy*` throws immediately if the element isn't present yet. For content that appears after a mocked promise resolves (API call, `renderHook` effect), use `findBy*` or `waitFor`.

```typescript
// ❌ Fails immediately — async content hasn't rendered yet
const heading = screen.getByText('Asset Name')

// ✅ Waits for the element to appear
const heading = await screen.findByText('Asset Name')
```

### `renderHook` doesn't wait for effects

`renderHook(() => useMyHook())` returns immediately; if the hook fetches data in a `useEffect`, always follow with `waitFor` before asserting on post-fetch state — and re-read `result.current` fresh each time rather than destructuring it once, since destructuring at hook-render time captures a stale snapshot.

```typescript
const { result } = renderHook(() => useAggregatedSummary(BROKER_NODE), { wrapper })

await waitFor(() => expect(result.current.loading).toBe(false))
expect(result.current.data).toEqual(SUMMARY_DTO) // re-read result.current here, not destructured earlier
```

### `ResizeObserver` not available in jsdom

Recharts uses `ResizeObserver`, which jsdom doesn't implement. The project's `setupTests.ts` already provides a mock — don't add a second one in individual test files, it will conflict.

### `API_BASE_URL` must never resolve to empty

`config.ts` falls back to `''` if `import.meta.env.API_BASE_URL` is unset, but the *real* default is injected by `vite.config.ts`'s `define` (`/api/v1/financial`). This has bitten the project before in Docker: an empty base URL causes requests to be relative to the current page, which the SPA's own catch-all route intercepts and returns HTML for — instead of a clear connection error, API calls silently receive HTML and fail to parse as JSON. Don't write a test that treats the empty-string fallback as a valid, working configuration.
