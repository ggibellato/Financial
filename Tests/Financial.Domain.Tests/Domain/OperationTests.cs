using System;
using FluentAssertions;
using Financial.Domain.Entities;

namespace Financial.Domain.Tests;

public class OperationTests
{
    [Fact]
    public void Create_AssignsIdAndTotalPrice()
    {
        var operation = Operation.Create(new DateTime(2024, 1, 1), Operation.OperationType.Buy, 2m, 10m, 1m);

        operation.Id.Should().NotBe(Guid.Empty);
        operation.TotalPrice.Should().Be(21m);
    }

    [Fact]
    public void CreateWithId_UsesProvidedId()
    {
        var id = Guid.NewGuid();

        var operation = Operation.CreateWithId(id, new DateTime(2024, 1, 1), Operation.OperationType.Sell, 1m, 5m, 0m);

        operation.Id.Should().Be(id);
    }

    [Fact]
    public void CreateWithId_WhenEmpty_AssignsNewId()
    {
        var operation = Operation.CreateWithId(Guid.Empty, new DateTime(2024, 1, 1), Operation.OperationType.Buy, 1m, 5m, 0m);

        operation.Id.Should().NotBe(Guid.Empty);
    }
}
