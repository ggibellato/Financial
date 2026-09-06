> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Negative-Path Testing

The rule: every branch, every external-provider call, every storage write and every critical
journey needs its failure case tested at the same layer as its success case — never inferred
from the happy path passing. Track it per artifact row in `../SKILL.md` §3.

## Unit

**Guard clauses and invalid input (C#)** — one test per `throw`, asserting the type and the
parameter name where there is one:

```csharp
[Fact]
public void Constructor_WithNullRepository_Throws()
{
    Action act = () => new ExpenseService(null!, _tracer, Logger);
    act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
}

[Fact]
public async Task AddExpenseAsync_WithZeroValue_RecordsFailedSpanWithException()
{
    var request = ToCreateDto(_repository, ValidCreateRequest() with { Value = 0m });

    var act = async () => await _sut.AddExpenseAsync(request);

    await act.Should().ThrowAsync<ArgumentException>();
}
```

(`Tests/Financial.CashFlow.Application.Tests/Services/ExpenseServiceTests.cs`.) Pair the
exception assertion with the observability assertion: `_tracer.Spans.Should().ContainSingle().Which.RecordedException.Should().NotBeNull()`
and, for swallowed exceptions, `logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Warning && e.Message.Contains(nameof(HttpRequestException)))`.

**Boundary values** — `[Theory]` with both sides of the edge (`Expense.MaxRoundUpAmount` is
`0.99m`: test `0.99m` accepted and `1.00m` rejected; `PaymentsDueService` window: due in 5 days
included, 6 days excluded; overdue excluded).

**WPF ViewModels** — the stub service throws (`StubExpenseService` with a configured
exception) → error text set, `IsSaving` back to false, entered values preserved, span
recorded as failed, no `refresh` call.

**React hooks/components** — `getBanksMock.mockRejectedValue(new Error('Network down'))` →
`result.current.error` set, `data` null; a component given `onSave` that rejects keeps its
field values and shows the error under the named field
(`TransferForm.test.tsx: renders a backend error under the field named in saveErrorFields`).

## Integration

**External provider failure modes** — for every HTTP-backed provider, all four:

| Failure | Fake | Expected (Frankfurter) |
|---|---|---|
| Non-2xx | `_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)` | `null` |
| Malformed body | `Content = new StringContent("not json")` | `null` |
| Missing key | body with `"rates":{"EUR":0.15}` when GBP was asked | `null` |
| Transport exception | `_ => throw new HttpRequestException("network down")` | `null` + `Warning` log naming `HttpRequestException` |

(`FrankfurterExchangeRateProviderTests`.) At host level the same is proven with
`new ApiTestFactory(new StubExchangeRateProvider(null))` →
`CreateEntry_WhenRateLookupFails_ReturnsOkWithOnlyEnteredCurrency` (`ControleMaeEndpointsTests`):
the feature degrades, it does not 500.

**Rejected storage write** — `new LocalJsonStorage(Path.Combine(Path.GetTempPath(), $"cashflow-missing-dir-{Guid.NewGuid()}", "data.json"))`
→ `ApplyAndSaveAsync` throws `DirectoryNotFoundException`
(`CashFlowJsonRepositoryTests.ApplyAndSaveAsync_WhenWriteFails_PropagatesException`);
`ReadAsync` on a missing file → `FileNotFoundException` (`LocalJsonStorageTests`). For the
debounced path: `ControllableJsonStorage` configured to throw `TransientStorageException`
→ retried, then `GetStatus().LastError` set after the retry budget.

**Rejected request through the host** — one per endpoint for each mapped exception the
endpoint can raise: `PostAsJsonAsync` with an unknown id → 404; a duplicate bank name → 409
`DuplicateNameException`; a withdrawal over balance → 409 with the message in the problem
details and **without** the amount in the log (`DomainExceptionLoggingTests`).

**Unsupported configuration** — `["CashFlow:Repository:Provider"] = "NotARealProvider"` →
`GetRequiredService<ICashFlowRepository>()` throws `InvalidOperationException` `*is not supported*`.

**Unreachable collector** — `Observability:Enabled=true` + `Endpoint=http://localhost:4319`
→ endpoints still 200, spans never throw (`ObservabilityBackendUnreachableTests`).

**Contract drift** — an intentional snapshot change without `npm run generate-api-types` must
fail `openapiFreshness.test.ts`; a DTO rename must fail `OpenApiContractTests` naming the
endpoint.

**Frontend page** — the faked client rejects → `role="alert"` with "Try again"; clicking it
calls the client again (`BanksPage.test.tsx: shows an error state with retry on load failure`).
An `ApiError` 409 on save → message shown, form kept open with values.

**AC-tracing** — every negative §9 bullet ("… are excluded", "fails silently", "returns 409")
gets its own tagged test (`feature-traceability.md`).

## E2E (smoke run)

Required per critical flow: one failure journey. Today `Financial.Web/scripts/smoke-test.mjs`
has one success journey (seed three expenses → Historic Summary Average shows `25.00`) and
**no failure journey** — the next change to the script adds one. The realistic options with the
published API on seeded JSON:

- **Rejected submission end to end**: POST an expense with `value: 0` through the UI form and
  assert the inline validation / 400 message renders and the row does not appear.
- **Domain conflict surfaced**: attempt a reserve withdrawal over the seeded balance and assert
  the 409 problem-details message is shown, the form keeps its values, and no console error is
  logged.
- **Degraded dependency**: the FX provider is unreachable in CI already (no network to
  Frankfurter is guaranteed) — a Controle Mãe entry must still save with only the entered
  currency, which is exactly the fail-safe the API promises.

Stopping the API mid-journey is not a useful E2E negative here (single process); the
frontend's own error state for that is covered at the page layer.

## Examples from the project

- Unit: `ExpenseServiceTests`, `YahooFinanceServiceTests.GetAssetValue_UnsupportedExchange_ThrowsInvalidOperationException_WithoutCallingHttp`, `PaymentDueBannerViewModelTests.Constructor_WithEmptyResponse_IsVisibleIsFalse`.
- Integration: `FrankfurterExchangeRateProviderTests` (all four modes), `CashFlowJsonRepositoryTests.ApplyAndSaveAsync_WhenWriteFails_PropagatesException`, `ControleMaeEndpointsTests.CreateEntry_WhenRateLookupFails_ReturnsOkWithOnlyEnteredCurrency`, `DomainExceptionLoggingTests`, `ObservabilityBackendUnreachableTests`, `BanksPage.test.tsx`.
- E2E: none yet — the first failure journey added should follow the options above.
