using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;
using Wpf.Ui.Controls;

namespace Financial.Presentation.Tests.ViewModels;

public class TaxWorkbookEntryRowViewModelTests
{
    private static TaxWorkbookEntryDTO Entry(CalculationStatus status) => new()
    {
        Id = Guid.NewGuid(),
        Date = new DateTime(2026, 6, 1),
        EventCategory = EventCategory.Dividend,
        GrossAmount = 100m,
        WithheldAmount = 10m,
        NetAmount = 90m,
        CalculationStatus = status,
        EvidenceReference = Guid.NewGuid(),
    };

    [Fact]
    public void Constructor_WithNullEntry_Throws()
    {
        Action act = () => new TaxWorkbookEntryRowViewModel(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("entry");
    }

    [Fact]
    public void Final_MapsToSuccessColorsAndCheckmarkIcon()
    {
        var row = new TaxWorkbookEntryRowViewModel(Entry(CalculationStatus.Final));

        row.StatusSymbol.Should().Be(SymbolRegular.CheckmarkCircle20);
        row.StatusSymbolFilled.Should().BeFalse();
        row.StatusLabel.Should().Be("Final");
        row.StatusBrush.Color.Should().Be(System.Windows.Media.Color.FromRgb(0x10, 0x7C, 0x10));
        row.StatusForeground.Color.Should().Be(System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF));
    }

    [Fact]
    public void UnknownStatus_MapsToDefaultColorsAndInfoIcon()
    {
        var row = new TaxWorkbookEntryRowViewModel(Entry((CalculationStatus)99));

        row.StatusSymbol.Should().Be(SymbolRegular.Info20);
        row.StatusSymbolFilled.Should().BeFalse();
        row.StatusLabel.Should().Be("99");
        row.StatusBrush.Color.Should().Be(System.Windows.Media.Color.FromRgb(0xEB, 0xEB, 0xEB));
        row.StatusForeground.Color.Should().Be(System.Windows.Media.Color.FromRgb(0x61, 0x61, 0x61));
    }

    [Fact]
    public void Incomplete_MapsToWarningColorsAndClockIcon()
    {
        var row = new TaxWorkbookEntryRowViewModel(Entry(CalculationStatus.Incomplete));

        row.StatusSymbol.Should().Be(SymbolRegular.Clock20);
        row.StatusSymbolFilled.Should().BeFalse();
        row.StatusLabel.Should().Be("Incomplete");
        row.StatusBrush.Color.Should().Be(System.Windows.Media.Color.FromRgb(0xFD, 0xE3, 0x00));
        row.StatusForeground.Color.Should().Be(System.Windows.Media.Color.FromRgb(0x24, 0x24, 0x24));
    }

    [Fact]
    public void RequiresReview_MapsToDangerColorsAndFilledAlertIcon()
    {
        var row = new TaxWorkbookEntryRowViewModel(Entry(CalculationStatus.RequiresReview));

        row.StatusSymbol.Should().Be(SymbolRegular.AlertUrgent20);
        row.StatusSymbolFilled.Should().BeTrue();
        row.StatusLabel.Should().Be("Requires review");
        row.StatusBrush.Color.Should().Be(System.Windows.Media.Color.FromRgb(0xD1, 0x34, 0x38));
        row.StatusForeground.Color.Should().Be(System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF));
    }

    [Fact]
    public void DisplayFields_AreCopiedFromTheEntry()
    {
        var entry = Entry(CalculationStatus.Final);
        var row = new TaxWorkbookEntryRowViewModel(entry);

        row.Date.Should().Be(entry.Date);
        row.EventCategory.Should().Be(entry.EventCategory);
        row.GrossAmount.Should().Be(entry.GrossAmount);
        row.WithheldAmount.Should().Be(entry.WithheldAmount);
        row.NetAmount.Should().Be(entry.NetAmount);
        row.EvidenceReference.Should().Be(entry.EvidenceReference);
    }
}
