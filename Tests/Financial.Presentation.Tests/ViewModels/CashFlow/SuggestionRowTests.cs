using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class SuggestionRowTests
{
    private static InvestmentSnapshotSuggestionDTO CreateDto(decimal currentValue) => new()
    {
        SnapshotId = Guid.NewGuid(),
        AccountId = Guid.NewGuid(),
        AccountName = "PlatinumVisa8003",
        CurrentValue = currentValue,
        SuggestedValue = 142.17m,
        SourceDescription = "BarclaysPlatinumVisa8003 — Aug 2026 statement",
    };

    [Fact]
    public void Constructor_ZeroCurrentValue_DefaultsIncludedToTrue()
    {
        var row = new SuggestionRow(CreateDto(0m));

        row.Included.Should().BeTrue();
    }

    [Fact]
    public void Constructor_NonZeroCurrentValue_DefaultsIncludedToFalse()
    {
        var row = new SuggestionRow(CreateDto(5400m));

        row.Included.Should().BeFalse();
    }

    [Fact]
    public void IsOverwriteCandidate_IncludedWithNonZeroCurrentValue_IsTrue()
    {
        var row = new SuggestionRow(CreateDto(5400m)) { Included = true };

        row.IsOverwriteCandidate.Should().BeTrue();
    }

    [Fact]
    public void IsOverwriteCandidate_NotIncluded_IsFalse()
    {
        var row = new SuggestionRow(CreateDto(5400m)) { Included = false };

        row.IsOverwriteCandidate.Should().BeFalse();
    }

    [Fact]
    public void IsOverwriteCandidate_IncludedWithZeroCurrentValue_IsFalse()
    {
        var row = new SuggestionRow(CreateDto(0m)) { Included = true };

        row.IsOverwriteCandidate.Should().BeFalse();
    }
}
