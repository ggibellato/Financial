using System.ComponentModel;
using Financial.Investment.Application.DTOs;
using Financial.Presentation.App.ViewModels;
using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class TransactionDialogViewModelTests
{
    [Theory]
    [InlineData(TransactionDialogMode.Add, "New transaction", "Add transaction")]
    [InlineData(TransactionDialogMode.Update, "Edit transaction", "Save")]
    [InlineData(TransactionDialogMode.Delete, "Delete Transaction", "Delete")]
    public void TitleAndConfirmLabel_ReflectMode(TransactionDialogMode mode, string expectedTitle, string expectedConfirmLabel)
    {
        var viewModel = new TransactionDialogViewModel(mode, "XPI", "Default", "PETR4", Guid.NewGuid(), DateTime.Today, "Buy", 10m, 5m, 0m, 0m);

        viewModel.Title.Should().Be(expectedTitle);
        viewModel.ConfirmLabel.Should().Be(expectedConfirmLabel);
    }

    [Fact]
    public void IsReadOnlyAndIsEditable_OnDeleteMode_AreOppositeAndReadOnly()
    {
        var viewModel = TransactionDialogViewModel.CreateForDelete("XPI", "Default", "PETR4", Guid.NewGuid(), DateTime.Today, "Buy", 10m, 5m, 0m, 0m);

        viewModel.IsReadOnly.Should().BeTrue();
        viewModel.IsEditable.Should().BeFalse();
    }

    [Fact]
    public void NetCash_ComputesFromQuantityUnitPriceFeesAndWithheld()
    {
        var viewModel = TransactionDialogViewModel.CreateForAdd("XPI", "Default", "PETR4");

        viewModel.Quantity = 10m;
        viewModel.UnitPrice = 5m;
        viewModel.Fees = 2m;
        viewModel.Withheld = 1m;

        viewModel.NetCash.Should().Be(-53m);
    }

    [Theory]
    [InlineData(nameof(TransactionDialogViewModel.Quantity))]
    [InlineData(nameof(TransactionDialogViewModel.UnitPrice))]
    [InlineData(nameof(TransactionDialogViewModel.Fees))]
    [InlineData(nameof(TransactionDialogViewModel.Withheld))]
    public void SettingQuantityUnitPriceFeesOrWithheld_RaisesNetCashPropertyChanged(string propertyToSet)
    {
        var viewModel = TransactionDialogViewModel.CreateForAdd("XPI", "Default", "PETR4");
        var raisedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, e) => raisedProperties.Add(e.PropertyName);

        switch (propertyToSet)
        {
            case nameof(TransactionDialogViewModel.Quantity):
                viewModel.Quantity = 10m;
                break;
            case nameof(TransactionDialogViewModel.UnitPrice):
                viewModel.UnitPrice = 5m;
                break;
            case nameof(TransactionDialogViewModel.Fees):
                viewModel.Fees = 1m;
                break;
            case nameof(TransactionDialogViewModel.Withheld):
                viewModel.Withheld = 1m;
                break;
        }

        raisedProperties.Should().Contain(nameof(TransactionDialogViewModel.NetCash));
    }

    [Fact]
    public void CreateForAdd_DefaultZeroQuantity_ConfirmCommandCannotExecute()
    {
        var viewModel = TransactionDialogViewModel.CreateForAdd("XPI", "Default", "PETR4");

        viewModel.Mode.Should().Be(TransactionDialogMode.Add);
        viewModel.TransactionId.Should().Be(Guid.Empty);
        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ConfirmCommand_CanExecute_TrueOnceAllFieldsAreValid()
    {
        var viewModel = TransactionDialogViewModel.CreateForAdd("XPI", "Default", "PETR4");

        viewModel.Date = DateTime.Today;
        viewModel.Type = "Buy";
        viewModel.Quantity = 10m;
        viewModel.UnitPrice = 5m;
        viewModel.Fees = 0m;

        viewModel.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ConfirmCommand_CanExecute_AlwaysTrueOnDeleteModeRegardlessOfFieldValidity()
    {
        var viewModel = TransactionDialogViewModel.CreateForDelete("XPI", "Default", "PETR4", Guid.NewGuid(), DateTime.MinValue, "NotAType", 0m, -1m, -1m, -1m);

        viewModel.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ConfirmCommand_WhenValid_RaisesCloseRequestedWithTrue()
    {
        var viewModel = TransactionDialogViewModel.CreateForAdd("XPI", "Default", "PETR4");
        viewModel.Type = "Buy";
        viewModel.Quantity = 10m;
        viewModel.UnitPrice = 5m;
        bool? raisedResult = null;
        viewModel.CloseRequested += (_, result) => raisedResult = result;

        viewModel.ConfirmCommand.Execute(null);

        raisedResult.Should().BeTrue();
    }

    [Fact]
    public void ConfirmCommand_WhenInvalid_DoesNotRaiseCloseRequested()
    {
        var viewModel = TransactionDialogViewModel.CreateForAdd("XPI", "Default", "PETR4"); // Quantity defaults to 0, invalid.
        var raised = false;
        viewModel.CloseRequested += (_, _) => raised = true;

        viewModel.ConfirmCommand.Execute(null);

        raised.Should().BeFalse();
    }

    [Fact]
    public void CancelCommand_Execute_RaisesCloseRequestedWithFalse()
    {
        var viewModel = TransactionDialogViewModel.CreateForAdd("XPI", "Default", "PETR4");
        bool? raisedResult = null;
        viewModel.CloseRequested += (_, result) => raisedResult = result;

        viewModel.CancelCommand.Execute(null);

        raisedResult.Should().BeFalse();
    }

    [Fact]
    public void CreateForUpdate_PreservesProvidedIdAndFieldValues()
    {
        var id = Guid.NewGuid();
        var date = new DateTime(2026, 7, 1);

        var viewModel = TransactionDialogViewModel.CreateForUpdate("XPI", "Default", "PETR4", id, date, "Sell", 20m, 15m, 1m, 2m);

        viewModel.TransactionId.Should().Be(id);
        viewModel.Date.Should().Be(date);
        viewModel.Type.Should().Be("Sell");
        viewModel.Quantity.Should().Be(20m);
        viewModel.UnitPrice.Should().Be(15m);
        viewModel.Fees.Should().Be(1m);
        viewModel.Withheld.Should().Be(2m);
    }

    private static OpenLotDTO Lot(decimal remaining, decimal unitCost = 10m) => new()
    {
        SourceTransactionId = Guid.NewGuid(),
        Date = new DateTime(2024, 1, 1),
        RemainingQuantity = remaining,
        UnitCost = unitCost
    };

    [Fact]
    public void RequiresLotAllocation_AddModeSellTypeSpecificIdBroker_IsTrue()
    {
        var viewModel = TransactionDialogViewModel.CreateForAdd("XPI", "Default", "PETR4", DateTime.Today, "Sell", isSpecificIdBroker: true);

        viewModel.RequiresLotAllocation.Should().BeTrue();
    }

    [Fact]
    public void RequiresLotAllocation_AddModeBuyTypeSpecificIdBroker_IsFalse()
    {
        var viewModel = TransactionDialogViewModel.CreateForAdd("XPI", "Default", "PETR4", DateTime.Today, "Buy", isSpecificIdBroker: true);

        viewModel.RequiresLotAllocation.Should().BeFalse();
    }

    [Fact]
    public void RequiresLotAllocation_NotSpecificIdBroker_IsFalseEvenForSellType()
    {
        var viewModel = TransactionDialogViewModel.CreateForAdd("XPI", "Default", "PETR4", DateTime.Today, "Sell", isSpecificIdBroker: false);

        viewModel.RequiresLotAllocation.Should().BeFalse();
    }

    [Fact]
    public void RequiresLotAllocation_UpdateMode_IsAlwaysFalse()
    {
        var viewModel = TransactionDialogViewModel.CreateForUpdate("XPI", "Default", "PETR4", Guid.NewGuid(), DateTime.Today, "Sell", 10m, 5m, 0m, 0m);

        viewModel.RequiresLotAllocation.Should().BeFalse();
    }

    [Fact]
    public void ChangingTypeToSell_ForSpecificIdBroker_TriggersOpenLotsFetchOnce()
    {
        var fetchCount = 0;
        var viewModel = TransactionDialogViewModel.CreateForAdd("XPI", "Default", "PETR4", DateTime.Today, "Buy", true, () => fetchCount++);

        viewModel.Type = "Sell";
        viewModel.Type = "Redemption";

        fetchCount.Should().Be(1);
    }

    [Fact]
    public void CreateForAdd_InitialTypeAlreadyRequiresLotAllocation_FetchesImmediately()
    {
        var fetchCount = 0;
        TransactionDialogViewModel.CreateForAdd("XPI", "Default", "PETR4", DateTime.Today, "Sell", true, () => fetchCount++);

        fetchCount.Should().Be(1);
    }

    [Fact]
    public void SetOpenLots_PopulatesRowsAndComputesAllocatedTotal()
    {
        var viewModel = TransactionDialogViewModel.CreateForAdd("XPI", "Default", "PETR4", DateTime.Today, "Sell", isSpecificIdBroker: true);
        var lot = Lot(15m);

        viewModel.SetOpenLots([lot]);
        viewModel.OpenLots.Single().Quantity = 5m;

        viewModel.AllocatedTotal.Should().Be(5m);
    }

    [Fact]
    public void CanConfirm_RequiresLotAllocation_FalseUntilAllocationExactlyMatchesQuantity()
    {
        var viewModel = TransactionDialogViewModel.CreateForAdd("XPI", "Default", "PETR4", DateTime.Today, "Sell", isSpecificIdBroker: true);
        viewModel.Quantity = 10m;
        viewModel.UnitPrice = 5m;
        viewModel.SetOpenLots([Lot(15m)]);

        viewModel.OpenLots.Single().Quantity = 6m;
        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();

        viewModel.OpenLots.Single().Quantity = 10m;
        viewModel.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void CanConfirm_RequiresLotAllocation_FalseWhenALotIsOverAllocated()
    {
        var viewModel = TransactionDialogViewModel.CreateForAdd("XPI", "Default", "PETR4", DateTime.Today, "Sell", isSpecificIdBroker: true);
        viewModel.Quantity = 10m;
        viewModel.UnitPrice = 5m;
        viewModel.SetOpenLots([Lot(5m)]);

        viewModel.OpenLots.Single().Quantity = 8m;

        viewModel.OpenLots.Single().IsOverAllocated.Should().BeTrue();
        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void CanConfirm_RequiresLotAllocation_FalseWhileLoadingOpenLots()
    {
        var viewModel = TransactionDialogViewModel.CreateForAdd("XPI", "Default", "PETR4", DateTime.Today, "Sell", isSpecificIdBroker: true);
        viewModel.Quantity = 10m;
        viewModel.UnitPrice = 5m;

        viewModel.SetOpenLotsLoading();

        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void CanConfirm_RequiresLotAllocation_FalseOnOpenLotsError()
    {
        var viewModel = TransactionDialogViewModel.CreateForAdd("XPI", "Default", "PETR4", DateTime.Today, "Sell", isSpecificIdBroker: true);
        viewModel.Quantity = 10m;
        viewModel.UnitPrice = 5m;

        viewModel.SetOpenLotsError("Unable to load open lots.");

        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void RetryOpenLotsCommand_InvokesFetchCallbackEvenAfterInitialFetch()
    {
        var fetchCount = 0;
        var viewModel = TransactionDialogViewModel.CreateForAdd("XPI", "Default", "PETR4", DateTime.Today, "Sell", true, () => fetchCount++);

        viewModel.RetryOpenLotsCommand.Execute(null);

        fetchCount.Should().Be(2);
    }

    [Fact]
    public void ReportSubmitFailed_PreservesEnteredLotAllocation()
    {
        var viewModel = TransactionDialogViewModel.CreateForAdd("XPI", "Default", "PETR4", DateTime.Today, "Sell", isSpecificIdBroker: true);
        viewModel.SetOpenLots([Lot(15m)]);
        viewModel.OpenLots.Single().Quantity = 7m;

        viewModel.ReportSubmitFailed("Lot already consumed by a concurrent edit.");

        viewModel.OpenLots.Single().Quantity.Should().Be(7m);
    }
}
