using Financial.Investment.Domain.ValueObjects;
using Financial.Investment.Infrastructure.DTOs;
using Financial.Investment.Infrastructure.Services;
using FluentAssertions;

namespace Financial.Investment.Infrastructure.Tests.Services;

public class DicionarioDoInvestidorFinanceServiceTests
{
    [Fact]
    public void GetAssetValue_BlankName_ThrowsArgumentException()
    {
        var service = new DicionarioDoInvestidorFinanceService();
        var request = new AssetValueRequestDTO { Name = "" };

        Action act = () => service.GetAssetValue(request);

        act.Should().Throw<ArgumentException>().WithMessage("Name is required.*");
    }

    [Fact]
    public void Constructor_WithNullLookup_ThrowsArgumentNullException()
    {
        Action act = () => new DicionarioDoInvestidorFinanceService(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("lookup");
    }

    [Fact]
    public void GetAssetValue_ValidName_DelegatesToLookup()
    {
        var snapshot = new AssetValueSnapshot("TESOURO IPCA+ 2040", "TESOURO IPCA+ 2040", 1755.91m, DateTimeOffset.UtcNow);
        var service = new DicionarioDoInvestidorFinanceService(name => name == "TESOURO IPCA+ 2040" ? snapshot : throw new InvalidOperationException());
        var request = new AssetValueRequestDTO { Name = "TESOURO IPCA+ 2040" };

        var result = service.GetAssetValue(request);

        result.Should().Be(snapshot);
    }
}
