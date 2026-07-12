using Financial.Infrastructure.Interfaces;
using Financial.Infrastructure.Services;
using FluentAssertions;

namespace Financial.Infrastructure.Tests.Services;

public class GoogleFinanceServiceTests
{
    [Fact]
    public void GetAssetValue_BlankTicker_ThrowsArgumentException()
    {
        var service = new GoogleFinanceService();
        var request = new AssetValueRequest { Exchange = "BVMF", Ticker = "" };

        Action act = () => service.GetAssetValue(request);

        act.Should().Throw<ArgumentException>().WithMessage("Ticker is required.*");
    }

    [Fact]
    public void GetAssetValue_NeitherExchangeNorCurrencyProvided_ThrowsArgumentException()
    {
        var service = new GoogleFinanceService();
        var request = new AssetValueRequest { Ticker = "BCIA11" };

        Action act = () => service.GetAssetValue(request);

        act.Should().Throw<ArgumentException>().WithMessage("Either Exchange or Currency must be provided.*");
    }
}
