using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests;

public class MaeLedgerEntryTests
{
    [Fact]
    public void Create_AssignsAllFieldsAndANewId()
    {
        var date = new DateOnly(2026, 7, 15);

        var entry = MaeLedgerEntry.Create(date, "School supplies", "Bought at the start of term", Currency.BRL, 350m, 51.23m);

        entry.Id.Should().NotBeEmpty();
        entry.Date.Should().Be(date);
        entry.Description.Should().Be("School supplies");
        entry.Note.Should().Be("Bought at the start of term");
        entry.SourceCurrency.Should().Be(Currency.BRL);
        entry.BrlValue.Should().Be(350m);
        entry.GbpValue.Should().Be(51.23m);
    }

    [Fact]
    public void Create_WithNullConvertedValue_AllowsIt()
    {
        var entry = MaeLedgerEntry.Create(new DateOnly(2026, 7, 16), "Medical appointment", string.Empty, Currency.GBP, null, 40m);

        entry.BrlValue.Should().BeNull();
        entry.GbpValue.Should().Be(40m);
    }

    [Fact]
    public void UpdateValues_ChangesBothCurrenciesWithoutChangingOtherFields()
    {
        var entry = MaeLedgerEntry.Create(new DateOnly(2026, 7, 16), "Medical appointment", string.Empty, Currency.GBP, null, 40m);
        var originalId = entry.Id;

        entry.UpdateValues(320.50m, 40m);

        entry.Id.Should().Be(originalId);
        entry.Date.Should().Be(new DateOnly(2026, 7, 16));
        entry.Description.Should().Be("Medical appointment");
        entry.SourceCurrency.Should().Be(Currency.GBP);
        entry.BrlValue.Should().Be(320.50m);
        entry.GbpValue.Should().Be(40m);
    }

    [Fact]
    public void Create_TwoEntries_HaveDifferentIds()
    {
        var first = MaeLedgerEntry.Create(new DateOnly(2026, 7, 1), "A", string.Empty, Currency.BRL, 10m, 1m);
        var second = MaeLedgerEntry.Create(new DateOnly(2026, 7, 1), "B", string.Empty, Currency.BRL, 10m, 1m);

        first.Id.Should().NotBe(second.Id);
    }
}
