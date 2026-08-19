using Financial.CashFlow.Application.Services;
using Financial.Shared.Abstractions;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

public class InvestmentAccountServiceTests
{
    private static readonly ITelemetryTracer Tracer = new RecordingTelemetryTracer();
    private static readonly Microsoft.Extensions.Logging.ILogger<InvestmentAccountService> Logger = NullLogger<InvestmentAccountService>.Instance;

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new InvestmentAccountService(null!, Tracer, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new InvestmentAccountService(new StubCashFlowRepository(), null!, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public void GetInvestmentAccounts_MapsEveryRepositoryAccountToADto()
    {
        var repository = new StubCashFlowRepository();
        var chaseSave = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        var platinumVisa = InvestmentAccount.Create("PlatinumVisa8003", isActive: true, isLiability: true);
        repository.InvestmentAccounts.Add(chaseSave);
        repository.InvestmentAccounts.Add(platinumVisa);
        var service = new InvestmentAccountService(repository, Tracer, Logger);

        var result = service.GetInvestmentAccounts();

        using (new AssertionScope())
        {
            result.Should().HaveCount(2);
            var chaseSaveDto = result.Should().ContainSingle(a => a.Name == "ChaseSave").Which;
            chaseSaveDto.Id.Should().Be(chaseSave.Id);
            chaseSaveDto.IsActive.Should().BeTrue();
            chaseSaveDto.IsLiability.Should().BeFalse();
            var platinumVisaDto = result.Should().ContainSingle(a => a.Name == "PlatinumVisa8003").Which;
            platinumVisaDto.IsLiability.Should().BeTrue();
        }
    }

    [Fact]
    public void GetInvestmentAccounts_DoesNotFilterByIsActive()
    {
        var repository = new StubCashFlowRepository();
        repository.InvestmentAccounts.Add(InvestmentAccount.Create("RetiredAccount", isActive: false, isLiability: false));
        var service = new InvestmentAccountService(repository, Tracer, Logger);

        var result = service.GetInvestmentAccounts();

        result.Should().ContainSingle(a => a.Name == "RetiredAccount" && !a.IsActive);
    }

    [Fact]
    public void GetInvestmentAccounts_WithNoAccounts_ReturnsEmptyList()
    {
        var service = new InvestmentAccountService(new StubCashFlowRepository(), Tracer, Logger);

        var result = service.GetInvestmentAccounts();

        result.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new InvestmentAccountService(new StubCashFlowRepository(), Tracer, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
