using Financial.Infrastructure.Interfaces;
using Financial.Infrastructure.Services;
using FluentAssertions;

namespace Financial.Infrastructure.Tests.Services;

public class GoogleFinanceServiceTests
{
    [Fact]
    public void GetQuote_BlankTicker_ThrowsArgumentException()
    {
        var service = new GoogleFinanceService();
        var request = new FinanceQuoteRequest { Exchange = "BVMF", Ticker = "" };

        Action act = () => service.GetQuote(request);

        act.Should().Throw<ArgumentException>().WithMessage("Ticker is required.*");
    }

    [Fact]
    public void GetQuote_NeitherExchangeNorCurrencyProvided_ThrowsArgumentException()
    {
        var service = new GoogleFinanceService();
        var request = new FinanceQuoteRequest { Ticker = "BCIA11" };

        Action act = () => service.GetQuote(request);

        act.Should().Throw<ArgumentException>().WithMessage("Either Exchange or Currency must be provided.*");
    }
}
