> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Architecture Rule Tests (`Tests/Financial.Architecture.Tests/*RuleTests.cs`)

Fitness-function tests: they assert the shape the codebase is allowed to have, not what it does.
They reflect over the real compiled assemblies (`Assembly.Load` +
`GetReferencedAssemblies()`) through the `ProjectAssembly` helper — no NetArchTest dependency,
and none is needed at assembly granularity.

## What to test

- **Dependency direction per context**: Domain references neither Application nor
  Infrastructure; Application does not reference Infrastructure
  (`CashFlowDependencyRuleTests`, `InvestmentDependencyRuleTests`).
- **Bounded-context isolation**: CashFlow assemblies never reference Investment ones and vice
  versa.
- **Shared-kernel rule**: `Financial.Shared.Abstractions` references nothing of ours;
  Infrastructure/Integrations/Tools projects reference `Financial.Shared.Abstractions` only,
  never `Financial.Shared.Infrastructure` (`SharedInfrastructureIsolationRuleTests`,
  `SharedAbstractionsDependencyRuleTests`, `SharedInfrastructureDependencyRuleTests`).
- **Presentation rules**: `Financial.Api` / `Financial.App` may reference Application +
  Infrastructure; nothing references Presentation (`PresentationDependencyRuleTests`).
- **Vendor SDK isolation**: only `Financial.Integrations.Observability` references
  OpenTelemetry packages (`ObservabilityIsolationRuleTests`) — the pattern from project memory
  `feedback_integration_sdk_isolation`.
- **Negative check**: when adding a rule, temporarily add the forbidden `<ProjectReference>`
  locally and confirm the test fails before removing it — a fitness test that cannot fail is
  decoration.

## Layer assignment

- **Integration** — real compiled assemblies, no mocks (the fundamentals' fitness-function
  row). The project targets `net10.0-windows` with `UseWPF` because it references
  `Financial.App`; it runs in both the `backend` and `wpf` CI jobs (the latter without coverage).
- No Unit, no E2E.

## Setup pattern

```csharp
using System.Reflection;
using Financial.Architecture.Tests.Infrastructure;
using FluentAssertions;

namespace Financial.Architecture.Tests;

public class CashFlowDependencyRuleTests
{
    private static readonly Assembly DomainAssembly = ProjectAssembly.Load("Financial.CashFlow.Domain");

    [Fact]
    public void Domain_Should_Not_Reference_Infrastructure()
    {
        ProjectAssembly.GetReferencedAssemblyNames(DomainAssembly)
            .Should().NotContain("Financial.CashFlow.Infrastructure");
    }
}
```

(From `Tests/Financial.Architecture.Tests/CashFlowDependencyRuleTests.cs`;
`ProjectAssembly.Load(string simpleAssemblyName)` and
`GetReferencedAssemblyNames(Assembly)` are in `Infrastructure/ProjectAssembly.cs`.) For a
rule over several projects use `TheoryData<string>` + `[MemberData(nameof(...))]` as
`SharedInfrastructureIsolationRuleTests.IsolatedProjects` does, and add the new assembly name
to that list when a project is created. Every new project must appear in at least one rule
test's `<ProjectReference>` list in `Financial.Architecture.Tests.csproj`, or `Assembly.Load`
cannot see it.

## When to skip

- Type-level rules (naming suffixes, sealed classes) — not enforced today; add only with an
  ADR, since `docs/rules/implementation.md` already forbids `*Service`/`*Policy` in Domain by
  review.
- Rules the compiler already enforces (a missing reference fails the build).

## Examples from project

- `Tests/Financial.Architecture.Tests/CashFlowDependencyRuleTests.cs`, `InvestmentDependencyRuleTests.cs` — layer direction.
- `Tests/Financial.Architecture.Tests/SharedInfrastructureIsolationRuleTests.cs` — seven isolated projects via `TheoryData`.
- `Tests/Financial.Architecture.Tests/ObservabilityIsolationRuleTests.cs` — vendor SDK containment.
- `Tests/Financial.Architecture.Tests/PresentationDependencyRuleTests.cs` — composition roots.
