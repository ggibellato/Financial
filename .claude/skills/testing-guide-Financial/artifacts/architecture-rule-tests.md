> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Architecture Rule Tests (`Tests/Financial.Architecture.Tests/*Tests.cs`)

Fitness-function tests: they assert the shape the codebase is allowed to have, not what it does.
Three mechanisms exist, each for a different granularity:

| Granularity | Mechanism | Example |
|---|---|---|
| Assembly references | `ProjectAssembly.Load(name)` + `GetReferencedAssemblyNames(assembly)` (`Infrastructure/ProjectAssembly.cs`) | `CashFlowDependencyRuleTests`, `BoundedContextIsolationTests`, `DomainPurityTests` |
| Type-level dependencies | `IArchitectureQuery` (`Infrastructure/IArchitectureQuery.cs`) implemented by `ArchUnitArchitectureQuery` — the only file allowed to reference ArchUnitNET | `AccountTemplateReadRuleTests` |
| Source-text scans | `ProductionSourceFiles` + a regex scanner, with the scanner itself unit-tested on sample lines | `ProductionClockReadsTests`, `FixedNameLookupTests` |

## What to test

- **Dependency direction per context**: Domain references neither Application nor Infrastructure;
  Application does not reference Infrastructure or a vendor integration
  (`CashFlowDependencyRuleTests`, `InvestmentDependencyRuleTests`).
- **Domain purity**: Domain assemblies reference no ASP.NET, `System.Text.Json` or Infrastructure
  (`DomainPurityTests`).
- **Bounded-context isolation**: CashFlow and Investment never reference each other, per layer
  (`BoundedContextIsolationTests`).
- **Shared-kernel rules**: `Financial.Shared.Abstractions` references nothing of ours; Integrations
  and Tools reference only Abstractions (`SharedAbstractionsDependencyRuleTests`,
  `SharedInfrastructureDependencyRuleTests`, `SharedInfrastructureIsolationRuleTests`).
- **Presentation and vendor isolation**: `PresentationDependencyRuleTests`,
  `ObservabilityIsolationRuleTests` (only `Integrations/Observability` touches OpenTelemetry),
  `FrankfurterIsolationRuleTests`.
- **Codebase rules from `docs/rules/implementation.md`**: wall-clock reads only at composition roots
  (`ProductionClockReadsTests`), no lookups by a fixed entity name (`FixedNameLookupTests`), the
  account template read only on the allow-list (`AccountTemplateReadRuleTests`), every test class
  carrying a `Category` trait (`CategoryTraitCoverageTests`), the CI time zone and culture pinned
  (`CiEnvironmentTests`).
- **The rule must be able to fail.** Type-level and scan rules ship a planted violation that the
  query must find (`Fixtures/Planted*TemplateReader.cs`, the scanner `[Theory]` rows). For an
  assembly rule, add the forbidden `<ProjectReference>` locally once and watch it fail.

## Layer assignment

- **Unit** (`Category=Unit`) — they read compiled assemblies or source text in process; no host,
  no data file. `AccountTemplateReadRuleTests` is the one `Integration` class (it loads the full
  ArchUnitNET architecture).
- No E2E.

## Setup pattern

```csharp
using System.Reflection;
using Financial.Architecture.Tests.Infrastructure;
using FluentAssertions;

namespace Financial.Architecture.Tests;

[Trait("Category", "Unit")]
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

For a rule over several projects use `TheoryData<string>` + `[MemberData(nameof(...))]`
(`SharedInfrastructureIsolationRuleTests.IsolatedProjects`) and add the new assembly when a project
is created; every project must be a `<ProjectReference>` of `Financial.Architecture.Tests.csproj` or
`Assembly.Load` cannot see it. A type-level rule goes through `IArchitectureQuery`, never ArchUnitNET
directly (`OnlyTheAdapter_ReferencesArchUnitNet` enforces it), and ships a planted fixture.

## When to skip

- Rules the compiler already enforces (a missing reference fails the build).
- Naming or styling rules with no architectural consequence.

## Examples from project

- `CashFlowDependencyRuleTests.cs`, `InvestmentDependencyRuleTests.cs`, `DomainPurityTests.cs`, `BoundedContextIsolationTests.cs` — assembly references.
- `AccountTemplateReadRuleTests.cs` + `Fixtures/` — type-level rule with planted violations.
- `ProductionClockReadsTests.cs`, `FixedNameLookupTests.cs` — source scans with their scanner tested.
- `CategoryTraitCoverageTests.cs` — the trait every test class must carry.
