using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.Investment.Infrastructure.Tests.Services;

[Trait("Category", "Integration")]
public class CreditServiceTests : IDisposable
{
    private readonly PersistedInvestmentFile _file = new();
    private readonly CreditService _service;

    public CreditServiceTests()
    {
        var repository = _file.OpenRepository();
        var tracer = new RecordingTelemetryTracer();
        var navigationService = new NavigationService(repository, TestHoldingValuationService.Create(), tracer, NullLogger<NavigationService>.Instance);
        IExchangeRateProvider exchangeRateProvider = new StubExchangeRateProvider(0.15m);
        _service = new CreditService(repository, navigationService, exchangeRateProvider, new StubReportingCurrencyProvider(), TestClock.At(), tracer, NullLogger<CreditService>.Instance);
    }

    public void Dispose() => _file.Dispose();

    private static CreditCreateDTO NewCredit(DateTime date, string type, decimal value, decimal withheld = 0m) => new()
    {
        BrokerName = "XPI",
        PortfolioName = "Default",
        AssetName = "BCIA11",
        Date = date,
        Type = type,
        Value = value,
        Withheld = withheld
    };

    private IReadOnlyCollection<Credit> PersistedCredits() => _file.ReloadAsset("XPI", "Default", "BCIA11").Credits;

    [Fact]
    public async Task AddCredit_PersistsTheCreditToDisk()
    {
        var existingIds = PersistedCredits().Select(credit => credit.Id).ToHashSet();

        var result = await _service.AddCreditAsync(NewCredit(new DateTime(2024, 2, 1), "Dividend", 12.5m));

        var persisted = PersistedCredits().Single(credit => !existingIds.Contains(credit.Id));
        persisted.Date.Should().Be(new DateTime(2024, 2, 1));
        persisted.Type.Should().Be(Credit.CreditType.Dividend);
        persisted.Value.Should().Be(12.5m);
        result!.Credits.Should().Contain(credit => credit.Id == persisted.Id);
    }

    [Fact]
    public async Task UpdateCredit_PersistsTheChangedFieldsToDisk()
    {
        var created = await _service.AddCreditAsync(NewCredit(new DateTime(2024, 2, 2), "Dividend", 5m));
        var creditId = created!.Credits.Single(credit => credit.Date == new DateTime(2024, 2, 2)).Id;

        await _service.UpdateCreditAsync(new CreditUpdateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Id = creditId,
            Date = new DateTime(2024, 2, 2),
            Type = "SecuritiesLendingIncome",
            Value = 8.75m
        });

        var persisted = PersistedCredits().Single(credit => credit.Id == creditId);
        persisted.Type.Should().Be(Credit.CreditType.SecuritiesLendingIncome);
        persisted.Value.Should().Be(8.75m);
    }

    [Fact]
    public async Task DeleteCredit_RemovesTheCreditFromDisk()
    {
        var created = await _service.AddCreditAsync(NewCredit(new DateTime(2024, 2, 3), "Dividend", 4m));
        var creditId = created!.Credits.Single(credit => credit.Date == new DateTime(2024, 2, 3)).Id;
        PersistedCredits().Should().Contain(credit => credit.Id == creditId);

        await _service.DeleteCreditAsync(new CreditDeleteDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Id = creditId
        });

        PersistedCredits().Should().NotContain(credit => credit.Id == creditId);
    }

    [Fact]
    public async Task AddCredit_CouponWithWithheld_PersistsWithheldAndNetAmount()
    {
        await _service.AddCreditAsync(NewCredit(new DateTime(2024, 2, 4), "Coupon", 100m, withheld: 15m));

        var persisted = PersistedCredits().Single(credit => credit.Date == new DateTime(2024, 2, 4));
        persisted.Type.Should().Be(Credit.CreditType.Coupon);
        persisted.Withheld.Should().Be(15m);
        persisted.NetAmount.Should().Be(85m);
    }

    [Fact]
    public async Task AddCredit_NegativeValue_PersistsAsACorrection()
    {
        await _service.AddCreditAsync(NewCredit(new DateTime(2024, 2, 5), "Dividend", -20m));

        PersistedCredits().Should().Contain(credit => credit.Date == new DateTime(2024, 2, 5) && credit.Value == -20m);
    }
}
