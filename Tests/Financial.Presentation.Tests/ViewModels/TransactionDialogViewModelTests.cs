using System.ComponentModel;
using Financial.Presentation.App.ViewModels;
using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class TransactionDialogViewModelTests
{
    [Theory]
    [InlineData(TransactionDialogMode.Add, "Add Transaction", "Add")]
    [InlineData(TransactionDialogMode.Update, "Update Transaction", "Update")]
    [InlineData(TransactionDialogMode.Delete, "Delete Transaction", "Delete")]
    public void TitleAndConfirmLabel_ReflectMode(TransactionDialogMode mode, string expectedTitle, string expectedConfirmLabel)
    {
        var viewModel = new TransactionDialogViewModel(mode, "XPI", "Default", "PETR4", Guid.NewGuid(), DateTime.Today, "Buy", 10m, 5m, 0m);

        viewModel.Title.Should().Be(expectedTitle);
        viewModel.ConfirmLabel.Should().Be(expectedConfirmLabel);
    }

    [Fact]
    public void IsReadOnlyAndIsEditable_OnDeleteMode_AreOppositeAndReadOnly()
    {
        var viewModel = TransactionDialogViewModel.CreateForDelete("XPI", "Default", "PETR4", Guid.NewGuid(), DateTime.Today, "Buy", 10m, 5m, 0m);

        viewModel.IsReadOnly.Should().BeTrue();
        viewModel.IsEditable.Should().BeFalse();
    }

    [Fact]
    public void TotalPrice_ComputesFromQuantityUnitPriceAndFees()
    {
        var viewModel = TransactionDialogViewModel.CreateForAdd("XPI", "Default", "PETR4");

        viewModel.Quantity = 10m;
        viewModel.UnitPrice = 5m;
        viewModel.Fees = 2m;

        viewModel.TotalPrice.Should().Be(52m);
    }

    [Theory]
    [InlineData(nameof(TransactionDialogViewModel.Quantity))]
    [InlineData(nameof(TransactionDialogViewModel.UnitPrice))]
    [InlineData(nameof(TransactionDialogViewModel.Fees))]
    public void SettingQuantityUnitPriceOrFees_RaisesTotalPricePropertyChanged(string propertyToSet)
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
        }

        raisedProperties.Should().Contain(nameof(TransactionDialogViewModel.TotalPrice));
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
        var viewModel = TransactionDialogViewModel.CreateForDelete("XPI", "Default", "PETR4", Guid.NewGuid(), DateTime.MinValue, "NotAType", 0m, -1m, -1m);

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

        var viewModel = TransactionDialogViewModel.CreateForUpdate("XPI", "Default", "PETR4", id, date, "Sell", 20m, 15m, 1m);

        viewModel.TransactionId.Should().Be(id);
        viewModel.Date.Should().Be(date);
        viewModel.Type.Should().Be("Sell");
        viewModel.Quantity.Should().Be(20m);
        viewModel.UnitPrice.Should().Be(15m);
        viewModel.Fees.Should().Be(1m);
    }
}
