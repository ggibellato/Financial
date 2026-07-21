using Financial.Investment.Domain.Entities;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class TransactionTests
{
    [Fact]
    public void Create_AssignsIdAndTotalPrice()
    {
        var transaction = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 2m, 10m, 1m);

        transaction.Id.Should().NotBe(Guid.Empty);
        transaction.TotalPrice.Should().Be(21m);
    }

    [Fact]
    public void CreateWithId_UsesProvidedId()
    {
        var id = Guid.NewGuid();

        var transaction = Transaction.CreateWithId(id, new DateTime(2024, 1, 1), Transaction.TransactionType.Sell, 1m, 5m, 0m);

        transaction.Id.Should().Be(id);
    }

    [Fact]
    public void CreateWithId_WhenEmpty_AssignsNewId()
    {
        var transaction = Transaction.CreateWithId(Guid.Empty, new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 1m, 5m, 0m);

        transaction.Id.Should().NotBe(Guid.Empty);
    }
}
