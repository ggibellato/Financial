using Financial.Investment.Domain.ValueObjects;
using Financial.Investment.Infrastructure.DTOs;
using Financial.Investment.Infrastructure.Interfaces;
using Financial.Investment.Infrastructure.Services;
using FluentAssertions;

namespace Financial.Investment.Infrastructure.Tests.Services;

[Trait("Category", "Integration")]
public class BondFinanceServicesTests
{
    public sealed record BondService(string Provider, Func<Func<string, AssetValueSnapshot>?, IFinanceService> Create, string Name, decimal Price)
    {
        public override string ToString() => Provider;
    }

    public static TheoryData<BondService> Services() => new()
    {
        new BondService("Redentia", lookup => lookup is null ? new RedentiaFinanceService() : new RedentiaFinanceService(lookup), "TESOURO PREFIXADO 2027", 955.15m),
        new BondService("StatusInvest", lookup => lookup is null ? new StatusInvestFinanceService() : new StatusInvestFinanceService(lookup), "TESOURO IPCA+ 2029", 1234.56m),
        new BondService("TesouroTransparente", lookup => lookup is null ? new TesouroTransparenteFinanceService() : new TesouroTransparenteFinanceService(lookup), "TESOURO IPCA+ 2040", 1966.48m),
    };

    [Theory]
    [MemberData(nameof(Services))]
    public void GetAssetValue_BlankName_ThrowsArgumentException(BondService bond)
    {
        var service = bond.Create(null);
        var request = new AssetValueRequestDTO { Name = "" };

        Action act = () => service.GetAssetValue(request);

        act.Should().Throw<ArgumentException>().WithMessage("Name is required.*");
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void GetAssetValue_ValidName_DelegatesToLookup(BondService bond)
    {
        var snapshot = new AssetValueSnapshot(bond.Name, bond.Name, bond.Price, TestClock.Default);
        var service = bond.Create(requested => requested == bond.Name ? snapshot : throw new InvalidOperationException());

        var result = service.GetAssetValue(new AssetValueRequestDTO { Name = bond.Name });

        result.Should().Be(snapshot);
    }
}
