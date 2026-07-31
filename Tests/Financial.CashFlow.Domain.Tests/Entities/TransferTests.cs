using Financial.CashFlow.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests;

public class TransferTests
{
    private static Transfer CreateValidTransfer() =>
        Transfer.Create(new DateOnly(2026, 7, 25), "Barclays", "Trading212", 500m, "Round-up top-up");

    [Fact]
    public void Create_AssignsAllFieldsAndANewId()
    {
        var date = new DateOnly(2026, 7, 25);

        var transfer = Transfer.Create(date, "Barclays", "Trading212", 500m, "Round-up top-up");

        using (new AssertionScope())
        {
            transfer.Id.Should().NotBeEmpty();
            transfer.Date.Should().Be(date);
            transfer.SourceBank.Should().Be("Barclays");
            transfer.DestinationBank.Should().Be("Trading212");
            transfer.Amount.Should().Be(500m);
            transfer.Note.Should().Be("Round-up top-up");
        }
    }

    [Fact]
    public void Create_WithoutNote_AllowsNullNote()
    {
        var transfer = Transfer.Create(new DateOnly(2026, 7, 25), "Barclays", "Trading212", 500m, null);

        transfer.Note.Should().BeNull();
    }

    [Fact]
    public void Create_TwoTransfers_HaveDifferentIds()
    {
        var first = CreateValidTransfer();
        var second = CreateValidTransfer();

        first.Id.Should().NotBe(second.Id);
    }

    [Fact]
    public void Create_WithSameSourceAndDestinationBank_Throws()
    {
        var act = () => Transfer.Create(new DateOnly(2026, 7, 25), "Barclays", "Barclays", 500m, null);

        act.Should().Throw<ArgumentException>().WithMessage("*two different banks*");
    }

    [Fact]
    public void Create_WithSameSourceAndDestinationBank_CaseInsensitive_Throws()
    {
        var act = () => Transfer.Create(new DateOnly(2026, 7, 25), "barclays", "BARCLAYS", 500m, null);

        act.Should().Throw<ArgumentException>().WithMessage("*two different banks*");
    }

    [Fact]
    public void Create_WithZeroAmount_Throws()
    {
        var act = () => Transfer.Create(new DateOnly(2026, 7, 25), "Barclays", "Trading212", 0m, null);

        act.Should().Throw<ArgumentException>().WithMessage("*greater than zero*");
    }

    [Fact]
    public void Create_WithNegativeAmount_Throws()
    {
        var act = () => Transfer.Create(new DateOnly(2026, 7, 25), "Barclays", "Trading212", -1m, null);

        act.Should().Throw<ArgumentException>().WithMessage("*greater than zero*");
    }

    [Fact]
    public void UpdateDetails_MutatesEveryFieldWithoutChangingId()
    {
        var transfer = CreateValidTransfer();
        var originalId = transfer.Id;
        var newDate = new DateOnly(2026, 8, 1);

        transfer.UpdateDetails(newDate, "Chase", "Trading212", 250m, "Updated note");

        using (new AssertionScope())
        {
            transfer.Id.Should().Be(originalId);
            transfer.Date.Should().Be(newDate);
            transfer.SourceBank.Should().Be("Chase");
            transfer.DestinationBank.Should().Be("Trading212");
            transfer.Amount.Should().Be(250m);
            transfer.Note.Should().Be("Updated note");
        }
    }

    [Fact]
    public void UpdateDetails_WithSameSourceAndDestinationBank_Throws()
    {
        var transfer = CreateValidTransfer();

        var act = () => transfer.UpdateDetails(transfer.Date, "Chase", "Chase", 100m, null);

        act.Should().Throw<ArgumentException>().WithMessage("*two different banks*");
    }

    [Fact]
    public void UpdateDetails_WithNonPositiveAmount_Throws()
    {
        var transfer = CreateValidTransfer();

        var act = () => transfer.UpdateDetails(transfer.Date, "Barclays", "Trading212", 0m, null);

        act.Should().Throw<ArgumentException>().WithMessage("*greater than zero*");
    }
}
