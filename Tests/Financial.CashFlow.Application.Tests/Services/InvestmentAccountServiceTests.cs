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
    private static readonly Microsoft.Extensions.Logging.ILogger<InvestmentAccountService> Logger = NullLogger<InvestmentAccountService>.Instance;

    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly InvestmentAccountService _sut;

    public InvestmentAccountServiceTests()
    {
        _repository = new StubCashFlowRepository();
        _tracer = new RecordingTelemetryTracer();
        _sut = CreateService();
    }

    /// <summary>Wires the SUT exactly as the test constructor does, so a test needing a differently
    /// seeded repository does not repeat the whole construction sequence.</summary>
    private InvestmentAccountService CreateService(StubCashFlowRepository? repository = null) =>
        new(repository ?? _repository, _tracer, Logger);

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new InvestmentAccountService(null!, _tracer, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new InvestmentAccountService(_repository, null!, Logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public void GetInvestmentAccounts_MapsEveryRepositoryAccountToADto()
    {
        var chaseSave = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        var platinumVisa = InvestmentAccount.Create("PlatinumVisa8003", isActive: true, isLiability: true);
        _repository.InvestmentAccounts.Add(chaseSave);
        _repository.InvestmentAccounts.Add(platinumVisa);

        var result = _sut.GetInvestmentAccounts();

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
        _repository.InvestmentAccounts.Add(InvestmentAccount.Create("RetiredAccount", isActive: false, isLiability: false));

        var result = _sut.GetInvestmentAccounts();

        result.Should().ContainSingle(a => a.Name == "RetiredAccount" && !a.IsActive);
    }

    [Fact]
    public void GetInvestmentAccounts_WithNoAccounts_ReturnsEmptyList()
    {
        var result = _sut.GetInvestmentAccounts();

        result.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new InvestmentAccountService(_repository, _tracer, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
