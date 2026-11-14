> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Feature Traceability — the mandatory AC-tracing subset of Integration

## Where acceptance criteria live

- PRDs: `docs/prd/P{NN}-prd-{slug}/prd-{slug}.md`, section `## 9. Acceptance Criteria`, grouped
  under `### F0N. Feature name` headings plus a `### Cross-Feature Integration` group. Each
  criterion is a `- [ ]` / `- [x]` checkbox. Example: `docs/prd/P42-prd-payment-due-banner/prd-payment-due-banner.md`.
- Per-feature specs: `docs/prd/P{NN}-prd-{slug}/features/P{NN}-F{NN}-{feature-slug}/spec.md`
  (§7 "Testing Strategy" names the test files) and `plan.md`. Example folder:
  `docs/prd/P42-prd-payment-due-banner/features/P42-F01-payments-due-aggregation-backend`.
- Speckit specs under `specs/{NNN}-{slug}/spec.md` use numbered Given/When/Then "Acceptance
  Scenarios" per user story (two exist: `001-application-observability`,
  `002-move-assets-between-portfolios`).
- Work with no PRD cites the pull request or issue instead (see "No PRD" below).

## The stable AC id (decided 2026-09-06)

Existing §9 bullets carry no id, and a positional id silently renumbers whenever a bullet is
inserted or reordered. From now on every §9 bullet gets a literal id **written into the PRD**,
and tests cite that literal:

```
P{NN}-F{NN}-{feature-slug}-{SS}
```

- `P{NN}` — PRD number, `F{NN}` — feature number, `{feature-slug}` — the slug of the feature
  folder under `features/` (so `P42-F01-payments-due-aggregation-backend`), `{SS}` — two-digit
  sequence assigned once and never reused, even if the bullet is later deleted.
- Cross-feature bullets: `P{NN}-CF-cross-feature-{SS}` (e.g. `P42-CF-cross-feature-01`).
- The PRD line becomes `- [ ] **P42-F01-payments-due-aggregation-backend-08** Payments with due
  dates before today (overdue) are excluded.` Ids stay when the checkbox is ticked.
- Extraction regex for the id, used by every audit below:
  `P[0-9]{2}-(F[0-9]{2}|CF)-[a-z0-9-]+-[0-9]{2}`.

Existing PRDs (P01–P44) are not retro-numbered; add ids to a PRD the first time one of its
features gets an AC-tracing test, and only to that feature's group.

## Setup pattern — identical to any other Integration test

Wire the real feature graph; fake only external providers.

**Backend feature (any feature with an endpoint — all of them today):** derive from
`ApiEndpointTests` (`Tests/Financial.Api.Tests/ApiEndpointTests.cs`). It boots the real
`Program` host on a temp copy of `TestData/data.test.json` and the seeded CashFlow JSON, with
the real `CashFlowJsonRepository`, `LocalJsonStorage`, serializers and services. Pass "today",
an FX stub or a calendar fake through the base constructor
(`ApiEndpointTests(exchangeRateProvider:, timeProvider:, calendarProvider:, useRealExchangeRates:)`)
— the only overrides `ApiTestFactory` accepts — and seed the feature's own data with
`ReplaceCashFlowDataBeforeStart(json)`.

Condensed from `Tests/Financial.Api.Tests/Acceptance/BanklessIncomeAcceptanceTests.cs`:

```csharp
[Trait("Category", "Integration")]
public class BanklessIncomeAcceptanceTests : ApiEndpointTests
{
    public BanklessIncomeAcceptanceTests()
        : base(timeProvider: TestClock.At(new DateTimeOffset(2026, 11, 15, 12, 0, 0, TimeSpan.Zero)))
    {
        ReplaceCashFlowDataBeforeStart(SeededData);
    }

    [Fact]
    [Trait("AC", "P61-F02-income-tab-and-bankless-income-02")]
    [Trait("AC", "P61-F02-income-tab-and-bankless-income-10")]
    public async Task CreateIncome_NoBank_CountsInTotalsAndTitheAndMovesNoBalance()
    {
        var before = await MonthFiguresAsync();

        var income = await CreateAsync(NewIncome(bankId: null));
        var after = await MonthFiguresAsync();

        using (new AssertionScope())
        {
            income.BankId.Should().BeNull();
            after.BarclaysBalance.Should().Be(before.BarclaysBalance);
            after.TotalIncoming.Should().Be(before.TotalIncoming + 42.10m);
            after.CalculatedTithe.Should().Be(before.CalculatedTithe + 4.21m);
        }
    }
}
```

Everything goes through HTTP on the real host; the expected figures are literals worked out by
hand; one test may carry two AC traits when one observable outcome proves both criteria.

**Web feature:** the SPA's real graph is page + hooks + components + `financialApiClient`; the
API is a separate deployable, so `vi.mock('../../api/financialApiClient')` is the fake and
nothing else is (`references/mock-health-rules.md`). Render through
`src/test/renderWithFluent.tsx` when Fluent components are involved.

## Tagging convention (mechanically queryable)

| Stack | Tag | Why this form |
|---|---|---|
| C# (xUnit 2.9.3) | `[Trait("AC", "<id>")]` on the `[Fact]`/`[Theory]` | VSTest filters traits natively (`AC=` exact, `AC~` contains) |
| TypeScript (Vitest 4.1) | test title starts with `[AC <id>] ` — `it('[AC P42-F02-payment-due-banner-web-03] renders nothing when empty', …)` | Vitest 4.1 `--tagsFilter` throws unless every tag is declared in `test.tags` (verified: "The Vitest config does't define any tags"); the title is filterable with `-t` and greppable without touching `vite.config.ts` |

Both forms carry the same id grammar, so one grep answers both directions.

## One test per AC, dedicated files per feature

- One `[Fact]`/`it` per criterion, asserting that criterion directly. A criterion with a
  natural table of cases may be a `[Theory]` — still one method, one trait.
- Negative criteria ("… are excluded", "fails silently and renders nothing", "returns 409")
  get their own test; they are not implied by the positive ones.
- Dedicated files, never interleaved. No `P{NN}F{NN}` prefix on the filename - the AC id
  already carries the PRD/feature numbering in its `[Trait]`/title, so repeating it in the
  filename is redundant (decided 2026-09-07, after the first real example shipped it that way
  and then dropped the prefix):
  - `Tests/Financial.Api.Tests/Acceptance/{FeatureName}AcceptanceTests.cs`
  - `Financial.Web/src/acceptance/{feature-slug}.test.tsx`
- The PR body's §9 checklist (see `feedback_pr_body_acceptance_criteria` in project memory)
  lists each id next to the test that proves it.

## Audit commands (dry-run verified 2026-09-06)

**Given an AC id (or a feature), which tests prove it?**

```powershell
# one feature, all its criteria — parsed and executed with --list-tests, no syntax error
dotnet test Tests/Financial.Api.Tests --filter "AC~P42-F01" --list-tests
# one exact criterion
dotnet test Tests/Financial.Api.Tests --filter "AC=P42-F01-payments-due-aggregation-backend-08" --list-tests
# web: list matching test titles without running them
cd Financial.Web; npx vitest list -t "\[AC P42-F02"
```

`dotnet test Tests/Financial.Api.Tests --filter "AC~P45-F01" --list-tests` lists the real tests in
`GoogleCalendarAccountConnectionAcceptanceTests.cs`. As of 2026-11-14 there are 356 `AC` traits in
`Tests/` (38 files under `Acceptance/`) and 220 `[AC …]` web titles, most of them inline in the
component/page test that owns the behaviour rather than in `src/acceptance/`. The nightly
`ac-traceability` job reports §9 criteria with no tagged test.

**Given a test, which AC does it prove?** — read its trait / title prefix. **Whole-repo
listing of every AC-tracing test:**

```bash
grep -rnE 'Trait\("AC", *"[^"]+"\)|\[AC [^]]+\]' Tests Financial.Web/src \
  --include=*.cs --include=*.ts --include=*.tsx --exclude-dir=bin --exclude-dir=obj
```

Real example, from the P45-F01 Google Calendar connection feature:

```
Tests/Financial.Api.Tests/Acceptance/GoogleCalendarAccountConnectionAcceptanceTests.cs:29:    [Trait("AC", "P45-F01-google-calendar-account-connection-01")]
```

A Web line, from the account screen's page test:

```
Financial.Web/src/pages/__tests__/AccountScreenPage.test.tsx:  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-03] saves a deposit from t…
```

## An AC test proves the wired criterion, not the branches again

The AC test asserts the criterion's observable outcome through the real graph — the stored value,
the status code, what the screen shows. The rule's branch combinations stay in the Unit tests; an
AC test that re-runs them through the host duplicates coverage without proving anything new
(`../SKILL.md` §2). One AC test per criterion, plus its negative bullet.

## No PRD? Cite the PR or issue the same way

`[Trait("AC", "PR751-01")]` / `it('[AC ISSUE45-02] …')` — grammar `PR[0-9]+-[0-9]{2}` or
`ISSUE[0-9]+-[0-9]{2}`, the sequence numbering the acceptance bullets in that PR/issue body.
The audit grep above matches these too because it keys on the `AC` tag, not the id shape.

## When a general Integration test is NOT enough

`ExpenseEndpointsTests.AddExpense_ValidRequest_ReturnsOk` exercises the same code as several
P12/P13 criteria, but it asserts only a 200. It is not AC coverage for "an expense with a
round-up bank stores Value + RoundUp" — that needs its own tagged test asserting the stored
value. Likewise `PaymentsDueServiceTests` (Unit, stub repository) covers the F01 logic but
not the wired feature: a criterion is proven at Integration, through the host.

## Examples from the project

`Tests/Financial.Api.Tests/Acceptance/GoogleCalendarAccountConnectionAcceptanceTests.cs` (P45-F01,
Google Calendar account connection) is the first AC-tracing suite - 6 tests, one per §9
criterion, `IExchangeRateProvider`'s external-provider role played here by a faked
`ICalendarProvider` (`FakeCalendarProvider` in `Tests/Financial.TestUtilities`) while the real
`ICalendarIntegrationService`/`ICalendarConnectionStore` graph runs through the host. The Web
form is `Financial.Web/src/acceptance/reporting-currency-react.test.tsx` (P49-F04, `[AC …]` titles).
