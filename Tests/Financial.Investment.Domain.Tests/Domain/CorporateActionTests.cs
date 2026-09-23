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
        var note = new string('a', CorporateAction.MaxNoteLength + 1);

        Action act = () => CorporateAction.CreateSplit(new DateTime(2026, 3, 1), 2.0m, note);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateSplit_NoteExactly500Characters_IsValid()
    {
        var note = new string('a', CorporateAction.MaxNoteLength);

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

    [Fact]
    public void CreateSplit_LeavesMergerFieldsNull()
    {
        var action = CorporateAction.CreateSplit(new DateTime(2026, 3, 1), 2.0m);

        action.Role.Should().BeNull();
        action.CorrelationId.Should().BeNull();
        action.LinkedAssetName.Should().BeNull();
        action.ExchangeRatio.Should().BeNull();
        action.CashInLieu.Should().BeNull();
        action.ConvertedQuantity.Should().BeNull();
        action.CarriedCostBasis.Should().BeNull();
    }

    [Fact]
    public void CreateMergerSource_ValidFields_SetsFields()
    {
        var date = new DateTime(2026, 4, 1);
        var correlationId = Guid.NewGuid();

        var action = CorporateAction.CreateMergerSource(date, 0.5m, 3.25m, "Acquisition", correlationId, "XCORP", 40m, 1000m);

        action.Id.Should().NotBeEmpty();
        action.Type.Should().Be(CorporateAction.CorporateActionType.Merger);
        action.Role.Should().Be(CorporateAction.CorporateActionRole.Source);
        action.EffectiveDate.Should().Be(date);
        action.ExchangeRatio.Should().Be(0.5m);
        action.CashInLieu.Should().Be(3.25m);
        action.Note.Should().Be("Acquisition");
        action.CorrelationId.Should().Be(correlationId);
        action.LinkedAssetName.Should().Be("XCORP");
        action.ConvertedQuantity.Should().Be(40m);
        action.CarriedCostBasis.Should().Be(1000m);
        action.RatioFactor.Should().BeNull();
    }

    [Fact]
    public void CreateMergerSource_NoCashInLieu_LeavesCashInLieuNull()
    {
        var action = CorporateAction.CreateMergerSource(
            new DateTime(2026, 4, 1), 0.5m, null, null, Guid.NewGuid(), "XCORP", 40m, 1000m);

        action.CashInLieu.Should().BeNull();
    }

    [Fact]
    public void CreateMergerSource_OneToOneExchangeRatio_IsValid()
    {
        var action = CorporateAction.CreateMergerSource(
            new DateTime(2026, 4, 1), 1.0m, null, null, Guid.NewGuid(), "XCORP", 40m, 1000m);

        action.ExchangeRatio.Should().Be(1.0m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateMergerSource_ExchangeRatioNotPositive_ThrowsArgumentException(decimal exchangeRatio)
    {
        Action act = () => CorporateAction.CreateMergerSource(
            new DateTime(2026, 4, 1), exchangeRatio, null, null, Guid.NewGuid(), "XCORP", 40m, 1000m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateMergerSource_NegativeCashInLieu_ThrowsArgumentException()
    {
        Action act = () => CorporateAction.CreateMergerSource(
            new DateTime(2026, 4, 1), 0.5m, -1m, null, Guid.NewGuid(), "XCORP", 40m, 1000m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateMergerSource_NoteExceeds500Characters_ThrowsArgumentException()
    {
        var note = new string('a', CorporateAction.MaxNoteLength + 1);

        Action act = () => CorporateAction.CreateMergerSource(
            new DateTime(2026, 4, 1), 0.5m, null, note, Guid.NewGuid(), "XCORP", 40m, 1000m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateMergerSourceWithId_PreservesGivenId()
    {
        var id = Guid.NewGuid();

        var action = CorporateAction.CreateMergerSourceWithId(
            id, new DateTime(2026, 4, 1), 0.5m, null, null, Guid.NewGuid(), "XCORP", 40m, 1000m);

        action.Id.Should().Be(id);
    }

    [Fact]
    public void CreateMergerTarget_ValidFields_SetsFields()
    {
        var date = new DateTime(2026, 4, 1);
        var correlationId = Guid.NewGuid();

        var action = CorporateAction.CreateMergerTarget(date, "Acquisition", correlationId, "TWTR", 40m, 1000m);

        action.Id.Should().NotBeEmpty();
        action.Type.Should().Be(CorporateAction.CorporateActionType.Merger);
        action.Role.Should().Be(CorporateAction.CorporateActionRole.Target);
        action.EffectiveDate.Should().Be(date);
        action.Note.Should().Be("Acquisition");
        action.CorrelationId.Should().Be(correlationId);
        action.LinkedAssetName.Should().Be("TWTR");
        action.ConvertedQuantity.Should().Be(40m);
        action.CarriedCostBasis.Should().Be(1000m);
        action.ExchangeRatio.Should().BeNull();
        action.CashInLieu.Should().BeNull();
        action.RatioFactor.Should().BeNull();
    }

    [Fact]
    public void CreateMergerTarget_NoteExceeds500Characters_ThrowsArgumentException()
    {
        var note = new string('a', CorporateAction.MaxNoteLength + 1);

        Action act = () => CorporateAction.CreateMergerTarget(
            new DateTime(2026, 4, 1), note, Guid.NewGuid(), "TWTR", 40m, 1000m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateMergerTargetWithId_PreservesGivenId()
    {
        var id = Guid.NewGuid();

        var action = CorporateAction.CreateMergerTargetWithId(
            id, new DateTime(2026, 4, 1), null, Guid.NewGuid(), "TWTR", 40m, 1000m);

        action.Id.Should().Be(id);
    }

    [Fact]
    public void CreateMergerSourceAndTarget_ShareCorrelationId()
    {
        var correlationId = Guid.NewGuid();

        var source = CorporateAction.CreateMergerSource(
            new DateTime(2026, 4, 1), 0.5m, null, null, correlationId, "XCORP", 40m, 1000m);
        var target = CorporateAction.CreateMergerTarget(
            new DateTime(2026, 4, 1), null, correlationId, "TWTR", 40m, 1000m);

        source.CorrelationId.Should().Be(target.CorrelationId);
    }

    [Fact]
    public void CreateSpinOffParent_ValidFields_SetsFields()
    {
        var date = new DateTime(2026, 4, 1);
        var correlationId = Guid.NewGuid();

        var action = CorporateAction.CreateSpinOffParent(date, 15m, "Spin-off completed", correlationId, "SPINCO", 5m, 150m);

        action.Id.Should().NotBeEmpty();
        action.Type.Should().Be(CorporateAction.CorporateActionType.SpinOff);
        action.Role.Should().Be(CorporateAction.CorporateActionRole.Parent);
        action.EffectiveDate.Should().Be(date);
        action.AllocationPercentage.Should().Be(15m);
        action.Note.Should().Be("Spin-off completed");
        action.CorrelationId.Should().Be(correlationId);
        action.LinkedAssetName.Should().Be("SPINCO");
        action.ConvertedQuantity.Should().Be(5m);
        action.CarriedCostBasis.Should().Be(150m);
        action.RatioFactor.Should().BeNull();
        action.ExchangeRatio.Should().BeNull();
        action.CashInLieu.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void CreateSpinOffParent_BoundaryAllocationPercentage_IsValid(decimal allocationPercentage)
    {
        var action = CorporateAction.CreateSpinOffParent(
            new DateTime(2026, 4, 1), allocationPercentage, null, Guid.NewGuid(), "SPINCO", 5m, 150m);

        action.AllocationPercentage.Should().Be(allocationPercentage);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100.01)]
    public void CreateSpinOffParent_AllocationPercentageOutsideRange_ThrowsArgumentException(decimal allocationPercentage)
    {
        Action act = () => CorporateAction.CreateSpinOffParent(
            new DateTime(2026, 4, 1), allocationPercentage, null, Guid.NewGuid(), "SPINCO", 5m, 150m);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateSpinOffParent_QuantityReceivedNotPositive_ThrowsArgumentException(decimal quantityReceived)
    {
        Action act = () => CorporateAction.CreateSpinOffParent(
            new DateTime(2026, 4, 1), 15m, null, Guid.NewGuid(), "SPINCO", quantityReceived, 150m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateSpinOffParent_NoteExceeds500Characters_ThrowsArgumentException()
    {
        var note = new string('a', CorporateAction.MaxNoteLength + 1);

        Action act = () => CorporateAction.CreateSpinOffParent(
            new DateTime(2026, 4, 1), 15m, note, Guid.NewGuid(), "SPINCO", 5m, 150m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateSpinOffParentWithId_PreservesGivenId()
    {
        var id = Guid.NewGuid();

        var action = CorporateAction.CreateSpinOffParentWithId(
            id, new DateTime(2026, 4, 1), 15m, null, Guid.NewGuid(), "SPINCO", 5m, 150m);

        action.Id.Should().Be(id);
    }

    [Fact]
    public void CreateSpinOffNew_ValidFields_SetsFields()
    {
        var date = new DateTime(2026, 4, 1);
        var correlationId = Guid.NewGuid();

        var action = CorporateAction.CreateSpinOffNew(date, "Spin-off completed", correlationId, "GEHC", 5m, 150m);

        action.Id.Should().NotBeEmpty();
        action.Type.Should().Be(CorporateAction.CorporateActionType.SpinOff);
        action.Role.Should().Be(CorporateAction.CorporateActionRole.New);
        action.EffectiveDate.Should().Be(date);
        action.Note.Should().Be("Spin-off completed");
        action.CorrelationId.Should().Be(correlationId);
        action.LinkedAssetName.Should().Be("GEHC");
        action.ConvertedQuantity.Should().Be(5m);
        action.CarriedCostBasis.Should().Be(150m);
        action.AllocationPercentage.Should().BeNull();
        action.ExchangeRatio.Should().BeNull();
        action.CashInLieu.Should().BeNull();
        action.RatioFactor.Should().BeNull();
    }

    [Fact]
    public void CreateSpinOffNew_PerformsNoValidation_AcceptsAnyQuantity()
    {
        var action = CorporateAction.CreateSpinOffNew(
            new DateTime(2026, 4, 1), null, Guid.NewGuid(), "GEHC", -5m, -150m);

        action.ConvertedQuantity.Should().Be(-5m);
        action.CarriedCostBasis.Should().Be(-150m);
    }

    [Fact]
    public void CreateSpinOffNew_NoteExceeds500Characters_ThrowsArgumentException()
    {
        var note = new string('a', CorporateAction.MaxNoteLength + 1);

        Action act = () => CorporateAction.CreateSpinOffNew(
            new DateTime(2026, 4, 1), note, Guid.NewGuid(), "GEHC", 5m, 150m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateSpinOffNewWithId_PreservesGivenId()
    {
        var id = Guid.NewGuid();

        var action = CorporateAction.CreateSpinOffNewWithId(
            id, new DateTime(2026, 4, 1), null, Guid.NewGuid(), "GEHC", 5m, 150m);

        action.Id.Should().Be(id);
    }

    [Fact]
    public void CreateSpinOffParentAndNew_ShareCorrelationId()
    {
        var correlationId = Guid.NewGuid();

        var parent = CorporateAction.CreateSpinOffParent(
            new DateTime(2026, 4, 1), 15m, null, correlationId, "SPINCO", 5m, 150m);
        var newRecord = CorporateAction.CreateSpinOffNew(
            new DateTime(2026, 4, 1), null, correlationId, "GEHC", 5m, 150m);

        parent.CorrelationId.Should().Be(newRecord.CorrelationId);
    }
}
