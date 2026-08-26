using Financial.Investment.Domain.Entities;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class CreditTests
{
    [Fact]
    public void Create_AssignsId()
    {
        var credit = Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.Dividend, 10m);

        credit.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void CreateWithId_UsesProvidedId()
    {
        var id = Guid.NewGuid();

        var credit = Credit.CreateWithId(id, new DateTime(2024, 1, 1), Credit.CreditType.Rent, 12m);

        credit.Id.Should().Be(id);
    }

    [Fact]
    public void Create_WithJcpType_AssignsType()
    {
        var credit = Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.JCP, 10m);

        credit.Type.Should().Be(Credit.CreditType.JCP);
    }

    [Fact]
    public void CreateWithId_EmptyGuid_StoresEmptyId()
    {
        var credit = Credit.CreateWithId(Guid.Empty, new DateTime(2024, 1, 1), Credit.CreditType.Dividend, 10m);

        credit.Id.Should().Be(Guid.Empty);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithAZeroOrNegativeValue_Throws(decimal value)
    {
        var act = () => Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.Dividend, value);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateWithId_WithAZeroOrNegativeValue_Throws(decimal value)
    {
        var act = () => Credit.CreateWithId(Guid.NewGuid(), new DateTime(2024, 1, 1), Credit.CreditType.Dividend, value);

        act.Should().Throw<ArgumentException>();
    }
}
