using Financial.Investment.Domain.ValueObjects;
using Financial.Investment.Infrastructure.DTOs;
using Financial.Investment.Infrastructure.Interfaces;
using Financial.Investment.Infrastructure.Services;
using FluentAssertions;

namespace Financial.Investment.Infrastructure.Tests.Services;

public class BondFinanceServicesTests
{
    public static TheoryData<string, Func<Func<string, AssetValueSnapshot>?, IFinanceService>, string, decimal> Services() => new()
    {
        { "Redentia", lookup => lookup is null ? new RedentiaFinanceService() : new RedentiaFinanceService(lookup), "TESOURO PREFIXADO 2027", 955.15m },
        { "StatusInvest", lookup => lookup is null ? new StatusInvestFinanceService() : new StatusInvestFinanceService(lookup), "TESOURO IPCA+ 2029", 1234.56m },
        { "DicionarioDoInvestidor", lookup => lookup is null ? new DicionarioDoInvestidorFinanceService() : new DicionarioDoInvestidorFinanceService(lookup), "TESOURO IPCA+ 2040", 1755.91m },
    };

    [Theory]
    [MemberData(nameof(Services))]
    public void GetAssetValue_BlankName_ThrowsArgumentException(
        string provider, Func<Func<string, AssetValueSnapshot>?, IFinanceService> create, string name, decimal price)
    {
        var service = create(null);
        var request = new AssetValueRequestDTO { Name = "" };

        Action act = () => service.GetAssetValue(request);

        act.Should().Throw<ArgumentException>().WithMessage("Name is required.*");
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void GetAssetValue_ValidName_DelegatesToLookup(
        string provider, Func<Func<string, AssetValueSnapshot>?, IFinanceService> create, string name, decimal price)
    {
        var snapshot = new AssetValueSnapshot(name, name, price, DateTimeOffset.UtcNow);
        var service = create(requested => requested == name ? snapshot : throw new InvalidOperationException());

        var result = service.GetAssetValue(new AssetValueRequestDTO { Name = name });

        result.Should().Be(snapshot);
    }
}
