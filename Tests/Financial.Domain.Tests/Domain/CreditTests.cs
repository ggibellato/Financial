using Financial.Domain.Entities;
using FluentAssertions;

namespace Financial.Domain.Tests;

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
    public void CreateWithId_EmptyGuid_StoresEmptyId()
    {
        var credit = Credit.CreateWithId(Guid.Empty, new DateTime(2024, 1, 1), Credit.CreditType.Dividend, 10m);

        credit.Id.Should().Be(Guid.Empty);
    }
}
