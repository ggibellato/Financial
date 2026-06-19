> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Future Artifact Types

Proactive guidance for types not yet present but likely to be added.

---

## C# — Application Commands / Queries (CQRS handlers)

If the Application layer introduces Commands and Queries (e.g., handled by MediatR or a custom dispatcher):

- **Unit test the handler**: if the handler contains branching, use a manual inline stub for `IRepository` — no mocking framework needed.
- **Integration test** (if warranted): use the same temp-file pattern as Infrastructure services.

```csharp
// Inline repository stub — define inside the test file
private sealed class StubRepository : IRepository
{
    public Investments Data { get; set; } = Investments.Create();
    public Task<Investments> GetAsync() => Task.FromResult(Data);
    public Task SaveAsync(Investments investments) { Data = investments; return Task.CompletedTask; }
}

[Fact]
public async Task Handler_WhenCondition_ExpectedResult()
{
    var repo = new StubRepository();
    repo.Data = BuildTestData();
    var handler = new SomeCommandHandler(repo);

    var result = await handler.Handle(new SomeCommand { /* params */ }, CancellationToken.None);

    result.Should().NotBeNull();
}
```

---

## C# — Domain Services

Test like domain entities: pure unit tests with no I/O, no mocks. Instantiate the service with its real dependencies (domain objects, value objects), call the method, assert the result.

---

## TypeScript — Custom React Hooks (`use*.ts`)

If pages extract data-fetching or state logic into custom hooks, test them with `renderHook` from `@testing-library/react` (included in the project's `@testing-library/react` 16.x).

```typescript
import { renderHook, waitFor } from '@testing-library/react'
import { vi } from 'vitest'
import { useMyHook } from '../hooks/useMyHook'

const fetchMock = vi.fn()

describe('useMyHook', () => {
  beforeEach(() => fetchMock.mockReset())

  it('fetches data on mount and returns it', async () => {
    fetchMock.mockResolvedValue({ id: '1', name: 'Test' })

    const { result } = renderHook(() => useMyHook(fetchMock))

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.data).toEqual({ id: '1', name: 'Test' })
  })

  it('sets error when fetch rejects', async () => {
    fetchMock.mockRejectedValue(new Error('network error'))

    const { result } = renderHook(() => useMyHook(fetchMock))

    await waitFor(() => expect(result.current.error).toBeTruthy())
  })
})
```

---

## TypeScript — Pure Utility Functions

Any pure TypeScript function added to the codebase:

```typescript
import { describe, it, expect } from 'vitest'
import { myUtility } from '../utils/myUtility'

describe('myUtility', () => {
  it('transforms input correctly', () => {
    expect(myUtility('input')).toBe('expected output')
  })

  it('handles edge case', () => {
    expect(myUtility('')).toBe('')
  })
})
```

Place test files in a `__tests__/` directory next to the source file being tested.

---

## C# — Application DTOs with Validation Attributes

If DTOs gain validation attributes (e.g., `[Required]`, `[Range]`), test them through the API endpoint tests (WebApplicationFactory), not as unit tests. Validation attribute behavior is a framework concern.
