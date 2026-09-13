using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Investment.Application.Tests.Services;

public class ReportingCurrencySettingServiceTests
{
    [Fact]
    public void GetReportingCurrency_ReflectsTheAggregatesCurrentValue()
    {
        var investments = Investments.Create();
        investments.SetReportingCurrency(Currency.BRL);
        var repository = new StubInvestmentRepository { Investments = investments };
        var service = new ReportingCurrencySettingService(repository);

        service.GetReportingCurrency().Should().Be(Currency.BRL);
    }

    [Fact]
    public async Task SetReportingCurrencyAsync_PersistsThroughTheRepository()
    {
        var investments = Investments.Create();
        var repository = new StubInvestmentRepository { Investments = investments };
        var service = new ReportingCurrencySettingService(repository);

        await service.SetReportingCurrencyAsync(Currency.USD);

        investments.ReportingCurrency.Should().Be(Currency.USD);
        repository.WriteCallCount.Should().Be(1);
    }

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new ReportingCurrencySettingService(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }
}
