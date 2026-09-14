using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Exceptions;
using Financial.Investment.Domain.Rules;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Investment.Application.Tests.Services;

public class BrokerServiceTests
{
    private readonly StubInvestmentRepository _repository = new() { Investments = Domain.Entities.Investments.Create() };
    private readonly RecordingTelemetryTracer _tracer = new();
    private readonly RecordingLogger<BrokerService> _logger = new();

    [Fact]
    public async Task CreateBrokerAsync_ValidRequest_AddsActiveBrokerAndPersistsOnce()
    {
        var result = await CreateService().CreateBrokerAsync(new BrokerCreateDTO { Name = "XPI", Currency = "BRL" });

        using (new AssertionScope())
        {
            result.Name.Should().Be("XPI");
            result.Currency.Should().Be("BRL");
            result.Status.Should().Be("Active");
            result.PortfolioCount.Should().Be(0);
            _repository.Investments!.FindActiveBroker("XPI").Should().NotBeNull();
            _repository.WriteCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task CreateBrokerAsync_DuplicateName_ThrowsAndWritesNothing()
    {
        _repository.Investments!.AddActiveBroker(Broker.Create("XPI", "BRL"));

        var act = async () => await CreateService().CreateBrokerAsync(new BrokerCreateDTO { Name = "XPI", Currency = "GBP" });

        await act.Should().ThrowAsync<InvestmentRuleViolationException>();
        _repository.WriteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateBrokerAsync_WithNoCostBasisMethodRequested_DefaultsToAverageCost()
    {
        var result = await CreateService().CreateBrokerAsync(new BrokerCreateDTO { Name = "XPI", Currency = "BRL" });

        result.CostBasisMethod.Should().Be(CostBasisMethod.AverageCost);
    }

    [Fact]
    public async Task CreateBrokerAsync_WithCostBasisMethodRequested_AppliesItWithoutRegenerating()
    {
        var result = await CreateService().CreateBrokerAsync(new BrokerCreateDTO { Name = "XPI", Currency = "BRL", CostBasisMethod = CostBasisMethod.FIFO });

        result.CostBasisMethod.Should().Be(CostBasisMethod.FIFO);
    }

    [Theory]
    [InlineData("", "BRL")]
    [InlineData("XPI", "")]
    [InlineData("   ", "BRL")]
    public async Task CreateBrokerAsync_MissingRequiredField_ThrowsArgumentException(string name, string currency)
    {
        var act = async () => await CreateService().CreateBrokerAsync(new BrokerCreateDTO { Name = name, Currency = currency });

        await act.Should().ThrowAsync<ArgumentException>();
        _repository.WriteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateBrokerAsync_ValidRequest_RenamesAndPersistsOnce()
    {
        _repository.Investments!.AddActiveBroker(Broker.Create("XPI", "BRL"));

        var result = await CreateService().UpdateBrokerAsync("XPI", new BrokerUpdateDTO { Name = "XP Investimentos", Currency = "USD" });

        using (new AssertionScope())
        {
            result.Name.Should().Be("XP Investimentos");
            result.Currency.Should().Be("USD");
            result.Status.Should().Be("Active");
            _repository.WriteCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task UpdateBrokerAsync_HistoricBroker_ReturnsHistoricStatus()
    {
        _repository.Investments!.AddHistoricBroker(Broker.Create("XPI", "BRL"));

        var result = await CreateService().UpdateBrokerAsync("XPI", new BrokerUpdateDTO { Name = "XPI", Currency = "USD" });

        result.Status.Should().Be("Historic");
    }

    [Fact]
    public async Task UpdateBrokerAsync_UnknownBroker_ThrowsNotFoundAndWritesNothing()
    {
        var act = async () => await CreateService().UpdateBrokerAsync("Nope", new BrokerUpdateDTO { Name = "New", Currency = "BRL" });

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _repository.WriteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task DeleteBrokerAsync_ActiveAndEmpty_MovesToHistoricAndPersistsOnce()
    {
        _repository.Investments!.AddActiveBroker(Broker.Create("XPI", "BRL"));

        await CreateService().DeleteBrokerAsync("XPI");

        using (new AssertionScope())
        {
            _repository.Investments!.FindActiveBroker("XPI").Should().BeNull();
            _repository.Investments!.FindHistoricBroker("XPI").Should().NotBeNull();
            _repository.WriteCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task DeleteBrokerAsync_WithPortfolios_ThrowsAndWritesNothing()
    {
        var broker = Broker.Create("XPI", "BRL");
        broker.AddPortfolio("Default");
        _repository.Investments!.AddActiveBroker(broker);

        var act = async () => await CreateService().DeleteBrokerAsync("XPI");

        await act.Should().ThrowAsync<InvestmentRuleViolationException>();
        _repository.WriteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task SetCostBasisMethodAsync_ValidBroker_ChangesMethodAndRegeneratesDisposals()
    {
        var broker = Broker.Create("Trading 212", "GBP");
        var portfolio = broker.AddPortfolio("ISA");
        var asset = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 50m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 6, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.RecordTransaction(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 10m, 200m, 0m));
        portfolio.AddAsset(asset);
        _repository.Investments!.AddActiveBroker(broker);
        var originalRecord = asset.DisposalRecords.Single();

        var result = await CreateService().SetCostBasisMethodAsync("Trading 212", CostBasisMethod.FIFO);

        using (new AssertionScope())
        {
            result.Name.Should().Be("Trading 212");
            broker.CostBasisMethod.Should().Be(CostBasisMethod.FIFO);
            originalRecord.Status.Should().Be(DisposalRecordStatus.Superseded);
            asset.DisposalRecords.Should().HaveCount(2);
            _repository.WriteCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task SetCostBasisMethodAsync_UnknownBroker_ThrowsAndWritesNothing()
    {
        var act = async () => await CreateService().SetCostBasisMethodAsync("Nope", CostBasisMethod.FIFO);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _repository.WriteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task SetCostBasisMethodAsync_RegenerationThrows_MethodAndDisposalsUnchangedAndWritesNothing()
    {
        var broker = Broker.Create("Trading 212", "GBP");
        var portfolio = broker.AddPortfolio("ISA");
        var asset = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 50m, 0m));
        asset.RecordTransaction(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 10m, 200m, 0m));
        portfolio.AddAsset(asset);
        _repository.Investments!.AddActiveBroker(broker);
        var originalRecord = asset.DisposalRecords.Single();

        var act = async () => await CreateService().SetCostBasisMethodAsync("Trading 212", CostBasisMethod.SpecificId);

        await act.Should().ThrowAsync<InvestmentRuleViolationException>();
        using (new AssertionScope())
        {
            broker.CostBasisMethod.Should().Be(CostBasisMethod.AverageCost);
            originalRecord.Status.Should().Be(DisposalRecordStatus.Active);
            asset.DisposalRecords.Should().ContainSingle();
            _repository.WriteCallCount.Should().Be(0);
        }
    }

    [Fact]
    public void GetBrokers_ReturnsBothActiveAndHistoricWithStatusAndPortfolioCount()
    {
        var active = Broker.Create("XPI", "BRL");
        active.AddPortfolio("Default");
        _repository.Investments!.AddActiveBroker(active);
        _repository.Investments!.AddHistoricBroker(Broker.Create("Avenue", "USD"));

        var result = CreateService().GetBrokers();

        using (new AssertionScope())
        {
            result.Should().HaveCount(2);
            result.Should().ContainSingle(b => b.Name == "XPI" && b.Status == "Active" && b.PortfolioCount == 1);
            result.Should().ContainSingle(b => b.Name == "Avenue" && b.Status == "Historic" && b.PortfolioCount == 0);
        }
    }

    [Fact]
    public void GetBrokers_ReturnsEachBrokersConfiguredCostBasisMethod()
    {
        var broker = Broker.Create("Trading 212", "GBP");
        broker.SetCostBasisMethod(CostBasisMethod.SpecificId);
        _repository.Investments!.AddActiveBroker(broker);

        var result = CreateService().GetBrokers();

        result.Should().ContainSingle().Which.CostBasisMethod.Should().Be(CostBasisMethod.SpecificId);
    }

    private BrokerService CreateService() => new(_repository, _tracer, _logger);
}
