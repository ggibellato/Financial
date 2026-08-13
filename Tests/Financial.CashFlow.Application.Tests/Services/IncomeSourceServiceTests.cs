using Financial.CashFlow.Application.Services;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Application.Tests.Services;

public class IncomeSourceServiceTests
{
    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new IncomeSourceService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void GetIncomeSources_MapsEveryRepositoryIncomeSourceToADto()
    {
        var repository = new StubCashFlowRepository();
        var gleison = IncomeSource.Create("Gleison", IncomeGroup.Salary);
        var lottery = IncomeSource.Create("Lottery", IncomeGroup.NonReportable, isActive: false);
        repository.IncomeSources.Add(gleison);
        repository.IncomeSources.Add(lottery);
        var service = new IncomeSourceService(repository);

        var result = service.GetIncomeSources();

        using (new AssertionScope())
        {
            result.Should().HaveCount(2);
            var gleisonDto = result.Should().ContainSingle(s => s.Name == "Gleison").Which;
            gleisonDto.Id.Should().Be(gleison.Id);
            gleisonDto.IsActive.Should().BeTrue();
            gleisonDto.Group.Should().Be("Salary");
        }
    }

    [Fact]
    public void GetIncomeSources_DoesNotFilterByIsActive()
    {
        var repository = new StubCashFlowRepository();
        repository.IncomeSources.Add(IncomeSource.Create("RetiredSource", IncomeGroup.NonReportable, isActive: false));
        var service = new IncomeSourceService(repository);

        var result = service.GetIncomeSources();

        result.Should().ContainSingle(s => s.Name == "RetiredSource" && !s.IsActive);
    }

    [Fact]
    public void GetIncomeSources_WithNoIncomeSources_ReturnsEmptyList()
    {
        var service = new IncomeSourceService(new StubCashFlowRepository());

        var result = service.GetIncomeSources();

        result.Should().BeEmpty();
    }
}
