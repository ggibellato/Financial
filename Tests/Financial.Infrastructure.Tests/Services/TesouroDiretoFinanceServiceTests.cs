using Financial.Domain.ValueObjects;
using Financial.Infrastructure.Interfaces;
using Financial.Infrastructure.Services;
using FluentAssertions;

namespace Financial.Infrastructure.Tests.Services;

public class TesouroDiretoFinanceServiceTests
{
    [Fact]
    public void GetAssetValue_BlankName_ThrowsArgumentException()
    {
        var service = new TesouroDiretoFinanceService(_ => null);
        var request = new AssetValueRequest { Name = "" };

        Action act = () => service.GetAssetValue(request);

        act.Should().Throw<ArgumentException>().WithMessage("Name is required.*");
    }

    [Fact]
    public void GetAssetValue_NoMatch_ThrowsAssetValueNotFoundException()
    {
        var service = new TesouroDiretoFinanceService(_ => null);
        var request = new AssetValueRequest { Name = "TESOURO PREFIXADO 2027" };

        Action act = () => service.GetAssetValue(request);

        act.Should().Throw<AssetValueNotFoundException>().WithMessage("*TESOURO PREFIXADO 2027*");
    }

    [Fact]
    public void GetAssetValue_Match_ReturnsSnapshot()
    {
        var snapshot = new AssetValueSnapshot("TESOURO IPCA+ 2029", "TESOURO IPCA+ 2029", 3775.97m, DateTimeOffset.UtcNow);
        var service = new TesouroDiretoFinanceService(_ => snapshot);
        var request = new AssetValueRequest { Name = "TESOURO IPCA+ 2029" };

        var result = service.GetAssetValue(request);

        result.Should().Be(snapshot);
    }
}
