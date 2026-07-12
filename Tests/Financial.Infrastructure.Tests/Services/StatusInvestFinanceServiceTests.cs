using Financial.Infrastructure.Interfaces;
using Financial.Infrastructure.Services;
using FluentAssertions;

namespace Financial.Infrastructure.Tests.Services;

public class StatusInvestFinanceServiceTests
{
    [Fact]
    public void GetAssetValue_BlankName_ThrowsArgumentException()
    {
        var service = new StatusInvestFinanceService();
        var request = new AssetValueRequest { Name = "" };

        Action act = () => service.GetAssetValue(request);

        act.Should().Throw<ArgumentException>().WithMessage("Name is required.*");
    }
}
