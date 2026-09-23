using System;
using Financial.Investment.Domain.Entities;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class CorporateActionTests
{
    [Fact]
    public void CreateSplit_ValidRatio_SetsFields()
    {
        var date = new DateTime(2026, 3, 1);

        var action = CorporateAction.CreateSplit(date, 2.0m, "2-for-1 split");

        action.Id.Should().NotBeEmpty();
        action.Type.Should().Be(CorporateAction.CorporateActionType.Split);
        action.EffectiveDate.Should().Be(date);
        action.RatioFactor.Should().Be(2.0m);
        action.Note.Should().Be("2-for-1 split");
    }

    [Fact]
    public void CreateSplit_NoNote_LeavesNoteNull()
    {
        var action = CorporateAction.CreateSplit(new DateTime(2026, 3, 1), 2.0m);

        action.Note.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1.0)]
    public void CreateSplit_InvalidRatio_ThrowsArgumentException(decimal ratioFactor)
    {
        Action act = () => CorporateAction.CreateSplit(new DateTime(2026, 3, 1), ratioFactor);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateSplit_ReverseSplitRatio_IsValid()
    {
        var action = CorporateAction.CreateSplit(new DateTime(2026, 3, 1), 0.1m, "1-for-10 reverse split");

        action.RatioFactor.Should().Be(0.1m);
    }

    [Fact]
    public void CreateSplit_NoteExceeds500Characters_ThrowsArgumentException()
    {
        var note = new string('a', 501);

        Action act = () => CorporateAction.CreateSplit(new DateTime(2026, 3, 1), 2.0m, note);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateSplit_NoteExactly500Characters_IsValid()
    {
        var note = new string('a', 500);

        var action = CorporateAction.CreateSplit(new DateTime(2026, 3, 1), 2.0m, note);

        action.Note.Should().HaveLength(500);
    }

    [Fact]
    public void CreateSplitWithId_PreservesGivenId()
    {
        var id = Guid.NewGuid();

        var action = CorporateAction.CreateSplitWithId(id, new DateTime(2026, 3, 1), 2.0m);

        action.Id.Should().Be(id);
    }

    [Fact]
    public void CreateSplitWithId_EmptyId_GeneratesNewId()
    {
        var action = CorporateAction.CreateSplitWithId(Guid.Empty, new DateTime(2026, 3, 1), 2.0m);

        action.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void CreateSplitWithId_InvalidRatio_ThrowsArgumentException()
    {
        Action act = () => CorporateAction.CreateSplitWithId(Guid.NewGuid(), new DateTime(2026, 3, 1), 1.0m);

        act.Should().Throw<ArgumentException>();
    }
}
