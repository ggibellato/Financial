using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Exceptions;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Investment.Application.Tests.Services;

public class AssetAdminServiceTests
{
    private readonly StubInvestmentRepository _repository = new();
    private readonly RecordingTelemetryTracer _tracer = new();
    private readonly RecordingLogger<AssetAdminService> _logger = new();

    [Fact]
    public void GetAssets_ReturnsAcrossActiveAndHistoricBrokers()
    {
        _repository.Investments = Investments.Create();
        var active = Broker.Create("XPI", "BRL");
        active.CreatePortfolio("Default").RegisterAsset(Asset.Create("AAAA", "ISIN123", "NYSE", "AAA"));
        _repository.Investments.AddActiveBroker(active);
        var historic = Broker.Create("Avenue", "USD");
        historic.CreatePortfolio("Old").RegisterAsset(Asset.Create("BBBB", "ISIN456", "NYSE", "BBB"));
        _repository.Investments.AddHistoricBroker(historic);

        var result = CreateService().GetAssets();

        using (new AssertionScope())
        {
            result.Should().HaveCount(2);
            result.Should().ContainSingle(a => a.Name == "AAAA" && a.BrokerStatus == "Active");
            result.Should().ContainSingle(a => a.Name == "BBBB" && a.BrokerStatus == "Historic");
        }
    }

    [Fact]
    public async Task CreateAssetAsync_ValidRequest_AddsAssetWithZeroQuantityAndPersistsOnce()
    {
        _repository.Investments = Investments.Create();
        var broker = Broker.Create("XPI", "BRL");
        broker.CreatePortfolio("Default");
        _repository.Investments.AddActiveBroker(broker);

        var result = await CreateService().CreateAssetAsync(new AssetAdminCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            Name = "AAAA",
            ISIN = "US0378331005",
            Ticker = "AAA"
        });

        using (new AssertionScope())
        {
            result.Name.Should().Be("AAAA");
            result.Quantity.Should().Be(0);
            _repository.Investments!.FindActiveBroker("XPI")!.FindPortfolio("Default")!.FindAsset("AAAA").Should().NotBeNull();
            _repository.WriteCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task CreateAssetAsync_BrokerIsHistoricNotActive_ThrowsNotFoundAndWritesNothing()
    {
        _repository.Investments = Investments.Create();
        var broker = Broker.Create("XPI", "BRL");
        broker.CreatePortfolio("Default");
        _repository.Investments.AddHistoricBroker(broker);

        var act = async () => await CreateService().CreateAssetAsync(new AssetAdminCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            Name = "AAAA"
        });

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _repository.WriteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateAssetAsync_DuplicateNameInPortfolio_ThrowsRuleViolationAndWritesNothing()
    {
        _repository.Investments = Investments.Create();
        var broker = Broker.Create("XPI", "BRL");
        broker.CreatePortfolio("Default").RegisterAsset(Asset.Create("AAAA", "ISIN123", "NYSE", "AAA"));
        _repository.Investments.AddActiveBroker(broker);

        var act = async () => await CreateService().CreateAssetAsync(new AssetAdminCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            Name = "AAAA"
        });

        await act.Should().ThrowAsync<InvestmentRuleViolationException>();
        _repository.WriteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateAssetAsync_InvalidIsinFormat_ThrowsArgumentExceptionAndWritesNothing()
    {
        _repository.Investments = Investments.Create();
        var broker = Broker.Create("XPI", "BRL");
        broker.CreatePortfolio("Default");
        _repository.Investments.AddActiveBroker(broker);

        var act = async () => await CreateService().CreateAssetAsync(new AssetAdminCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            Name = "AAAA",
            ISIN = "NOT-AN-ISIN"
        });

        await act.Should().ThrowAsync<ArgumentException>();
        _repository.WriteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateAssetAsync_ClassLeftUnset_AutoResolvesFromCountryAndLocalTypeCode()
    {
        _repository.Investments = Investments.Create();
        var broker = Broker.Create("XPI", "BRL");
        broker.CreatePortfolio("Default");
        _repository.Investments.AddActiveBroker(broker);

        var result = await CreateService().CreateAssetAsync(new AssetAdminCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            Name = "AAAA",
            Country = CountryCode.BR,
            LocalTypeCode = "FII"
        });

        result.Class.Should().Be(GlobalAssetClass.RealEstate);
    }

    [Fact]
    public async Task UpdateAssetAsync_ValidRequest_UpdatesIdentityAndPersistsOnce()
    {
        _repository.Investments = Investments.Create();
        var broker = Broker.Create("XPI", "BRL");
        broker.CreatePortfolio("Default").RegisterAsset(Asset.Create("AAAA", "ISIN123", "NYSE", "AAA"));
        _repository.Investments.AddActiveBroker(broker);

        var result = await CreateService().UpdateAssetAsync("XPI", "Default", "AAAA", new AssetAdminUpdateDTO
        {
            Name = "AAAB",
            ISIN = "GB0002374006",
            Exchange = "LSE",
            Ticker = "ZZZ",
            Country = CountryCode.UK,
            Class = GlobalAssetClass.Equity
        });

        using (new AssertionScope())
        {
            result.Name.Should().Be("AAAB");
            result.ISIN.Should().Be("GB0002374006");
            _repository.WriteCallCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task UpdateAssetAsync_AssetMissing_ThrowsNotFoundAndWritesNothing()
    {
        _repository.Investments = Investments.Create();
        var broker = Broker.Create("XPI", "BRL");
        broker.CreatePortfolio("Default");
        _repository.Investments.AddActiveBroker(broker);

        var act = async () => await CreateService().UpdateAssetAsync("XPI", "Default", "Missing", new AssetAdminUpdateDTO { Name = "Missing" });

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _repository.WriteCallCount.Should().Be(0);
    }

    private AssetAdminService CreateService() => new(_repository, _tracer, _logger);
}
