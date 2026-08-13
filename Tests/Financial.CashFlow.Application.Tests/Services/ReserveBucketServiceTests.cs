using Financial.CashFlow.Application.Services;
using Financial.TestUtilities;
using Financial.CashFlow.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Application.Tests.Services;

public class ReserveBucketServiceTests
{
    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new ReserveBucketService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void GetReserveBuckets_MapsEveryRepositoryBucketToADto()
    {
        var repository = new StubCashFlowRepository();
        var investimento = ReserveBucket.Create("Investimento", 33.33m);
        var ariana = ReserveBucket.Create("Ariana", 16.67m, isActive: false);
        repository.ReserveBuckets.Add(investimento);
        repository.ReserveBuckets.Add(ariana);
        var service = new ReserveBucketService(repository);

        var result = service.GetReserveBuckets();

        using (new AssertionScope())
        {
            result.Should().HaveCount(2);
            var investimentoDto = result.Should().ContainSingle(b => b.Name == "Investimento").Which;
            investimentoDto.Id.Should().Be(investimento.Id);
            investimentoDto.IsActive.Should().BeTrue();
            investimentoDto.SplitPercentage.Should().Be(33.33m);
        }
    }

    [Fact]
    public void GetReserveBuckets_DoesNotFilterByIsActive()
    {
        var repository = new StubCashFlowRepository();
        repository.ReserveBuckets.Add(ReserveBucket.Create("Retired", 0m, isActive: false));
        var service = new ReserveBucketService(repository);

        var result = service.GetReserveBuckets();

        result.Should().ContainSingle(b => b.Name == "Retired" && !b.IsActive);
    }

    [Fact]
    public void GetReserveBuckets_WithNoBuckets_ReturnsEmptyList()
    {
        var service = new ReserveBucketService(new StubCashFlowRepository());

        var result = service.GetReserveBuckets();

        result.Should().BeEmpty();
    }
}
