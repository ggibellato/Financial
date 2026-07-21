using Financial.Investment.Domain.ValueObjects;
using Financial.Infrastructure.DTOs;
using Financial.Infrastructure.Services;
using FluentAssertions;

namespace Financial.Infrastructure.Tests.Services;

public class StatusInvestFinanceServiceTests
{
    [Fact]
    public void GetAssetValue_BlankName_ThrowsArgumentException()
    {
        var service = new StatusInvestFinanceService();
        var request = new AssetValueRequestDTO { Name = "" };

        Action act = () => service.GetAssetValue(request);

        act.Should().Throw<ArgumentException>().WithMessage("Name is required.*");
    }

    [Fact]
    public void Constructor_WithNullLookup_ThrowsArgumentNullException()
    {
        Action act = () => new StatusInvestFinanceService(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("lookup");
    }

    [Fact]
    public void GetAssetValue_ValidName_DelegatesToLookup()
    {
        var snapshot = new AssetValueSnapshot("TESOURO IPCA+ 2029", "TESOURO IPCA+ 2029", 1234.56m, DateTimeOffset.UtcNow);
        var service = new StatusInvestFinanceService(name => name == "TESOURO IPCA+ 2029" ? snapshot : throw new InvalidOperationException());
        var request = new AssetValueRequestDTO { Name = "TESOURO IPCA+ 2029" };

        var result = service.GetAssetValue(request);

        result.Should().Be(snapshot);
    }
}
