using Financial.CashFlow.Application.Observability;
using Financial.Investment.Application.Observability;
using Financial.Presentation.App.Observability;
using Financial.Shared.Infrastructure.Observability;
using FluentAssertions;

namespace Financial.Presentation.Tests.DependencyInjection;

/// <summary>
/// CashFlowActivitySource/InvestmentActivitySource/AppActivitySource intentionally hardcode their
/// own name literal instead of referencing ObservabilitySourceNames, so Application/Presentation
/// stay free of a Financial.Shared.Infrastructure dependency (contracts/telemetry-semantic-conventions.md).
/// That means nothing at compile time stops the two sides drifting apart — if they do, the SDK's
/// AddSource(...) silently stops picking up that context's spans. This test is the guard rail.
/// </summary>
public class ObservabilitySourceNameConsistencyTests
{
    [Fact]
    public void CashFlowActivitySource_NameMatchesRegisteredSourceName() =>
        CashFlowActivitySource.Name.Should().Be(ObservabilitySourceNames.CashFlow);

    [Fact]
    public void InvestmentActivitySource_NameMatchesRegisteredSourceName() =>
        InvestmentActivitySource.Name.Should().Be(ObservabilitySourceNames.Investment);

    [Fact]
    public void AppActivitySource_NameMatchesRegisteredSourceName() =>
        AppActivitySource.Name.Should().Be(ObservabilitySourceNames.App);
}
