using Financial.Investment.Domain.ValueObjects;
using Financial.Investment.Infrastructure.DTOs;
using Financial.Investment.Infrastructure.Services;
using FluentAssertions;

namespace Financial.Investment.Infrastructure.Tests.Services;

public class RedentiaFinanceServiceTests
{
    [Fact]
    public void GetAssetValue_BlankName_ThrowsArgumentException()
    {
        var service = new RedentiaFinanceService();
        var request = new AssetValueRequestDTO { Name = "" };

        Action act = () => service.GetAssetValue(request);

        act.Should().Throw<ArgumentException>().WithMessage("Name is required.*");
    }

    [Fact]
    public void Constructor_WithNullLookup_ThrowsArgumentNullException()
    {
        Action act = () => new RedentiaFinanceService(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("lookup");
    }

    [Fact]
    public void GetAssetValue_ValidName_DelegatesToLookup()
    {
        var snapshot = new AssetValueSnapshot("TESOURO PREFIXADO 2027", "TESOURO PREFIXADO 2027", 955.15m, DateTimeOffset.UtcNow);
        var service = new RedentiaFinanceService(name => name == "TESOURO PREFIXADO 2027" ? snapshot : throw new InvalidOperationException());
        var request = new AssetValueRequestDTO { Name = "TESOURO PREFIXADO 2027" };

        var result = service.GetAssetValue(request);

        result.Should().Be(snapshot);
    }
}
