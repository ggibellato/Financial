> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Stack-Specific Gotchas and Pitfalls

---

## C# (.NET)

### Temp file leak on test failure

If `File.Delete(tempFile)` is after an assertion and the assertion fails, the file is never deleted.

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

### xUnit Theory data sharing

xUnit collects all `[Theory]` data before running any test. If data rows share a mutable object, state from one run leaks into another.

```csharp
// ❌ All rows share the same list instance
private static readonly List<string> shared = new() { "a" };
public static IEnumerable<object[]> Data => new[] { new object[] { shared } };

// ✅ New instance per row
public static IEnumerable<object[]> Data => new[]
{
    new object[] { new List<string> { "a" } }
};
```

### null in [InlineData]

`null` is not a valid C# attribute argument. Use `[MemberData]` for null test cases.

```csharp
// ❌ Does not compile
[InlineData(null)]

// ✅
public static IEnumerable<object?[]> NullCases => new[] { new object?[] { null } };
[Theory, MemberData(nameof(NullCases))]
public void Method_WithNull_ReturnsFalse(string? input) { ... }
```

### Multiple assertion failures hidden

FluentAssertions stops at the first failure by default. Wrap multiple assertions in `AssertionScope` to see all failures at once.

```csharp
// ❌ Only first failure reported
asset.Quantity.Should().Be(10);
asset.AveragePrice.Should().Be(5);

// ✅ All failures reported in one test run
using (new AssertionScope())
{
    asset.Quantity.Should().Be(10);
    asset.AveragePrice.Should().Be(5);
    asset.Active.Should().BeTrue();
}
```

### [MemberData] magic strings

```csharp
[MemberData("NullValues")]        // ❌ silently passes after rename
[MemberData(nameof(NullValues))]  // ✅ compile error after rename
```

---

## TypeScript (React)

### vi.mock hoisting

`vi.mock(...)` is hoisted to the top of the file before all imports. Variables declared in module scope with `const`/`let` are NOT hoisted. The safe pattern is to declare `vi.fn()` at module scope and reference it inside the mock factory:

```typescript
// ✅ vi.fn() declared at module scope is initialized before the hoisted mock runs
const getDataMock = vi.fn()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: () => ({ getData: getDataMock }),
}))
```

### Mock pollution between tests

`mockReset()` resets both call count and implementation. `mockClear()` resets only call count (keeps the implementation). Use `mockReset()` in `beforeEach` unless you set the implementation once for all tests in a `describe` block.

```typescript
beforeEach(() => {
  getDataMock.mockReset()   // ✅ clean slate each test
})
```

### screen vs container

```typescript
// ✅ Query by role/text/label — user-centric
screen.getByRole('button', { name: /save/i })

// ❌ Couples to DOM structure, breaks on refactor
container.querySelector('button.save-btn')
```

### Async assertions — getBy vs findBy

`getBy*` throws immediately if the element is not present. For content that appears after an API call resolves, always use `findBy*` (returns a Promise) or `waitFor`.

```typescript
// ❌ Fails immediately — component hasn't rendered async content yet
const heading = screen.getByText('Asset Name')

// ✅ Waits for the element to appear
const heading = await screen.findByText('Asset Name')
```

### ResizeObserver not available in jsdom

Recharts uses `ResizeObserver`, which jsdom does not implement. The project's `setupTests.ts` already provides a mock. Do **not** add a second mock in individual test files — it will conflict.

### Re-importing modules after vi.stubEnv

`import.meta.env.*` values are resolved at module load time. After calling `vi.stubEnv(...)`, you must reset and re-import the module to get the updated value:

```typescript
beforeEach(() => {
  vi.resetModules()
  vi.unstubAllEnvs()
})

it('reads env var', async () => {
  vi.stubEnv('API_BASE_URL', 'http://test')
  const { apiBaseUrl } = await import('./config')  // dynamic re-import
  expect(apiBaseUrl).toBe('http://test')
})
```

### mockReset vs mockClear vs mockRestore

| Method | What it resets |
|---|---|
| `mockClear()` | Call count and call arguments only |
| `mockReset()` | Call count + implementation (returns `undefined`) |
| `mockRestore()` | Restores the original implementation (for `vi.spyOn` only) |

Use `mockReset()` in `beforeEach` for standard test isolation. Use `mockRestore()` only when cleaning up a `vi.spyOn`.
