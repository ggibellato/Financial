using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class PriceDialogViewModelTests
{
    [Theory]
    [InlineData(PriceDialogMode.Add, "New price", "Add price")]
    [InlineData(PriceDialogMode.Update, "Edit price", "Save")]
    [InlineData(PriceDialogMode.Delete, "Delete Price", "Delete")]
    public void TitleAndConfirmLabel_ReflectMode(PriceDialogMode mode, string expectedTitle, string expectedConfirmLabel)
    {
        var viewModel = new PriceDialogViewModel(mode, "XPI", "Default", "BCIA11", DateTime.Today, 10m);

        viewModel.Title.Should().Be(expectedTitle);
        viewModel.ConfirmLabel.Should().Be(expectedConfirmLabel);
    }

    [Fact]
    public void IsReadOnlyAndIsEditable_OnDeleteMode_AreOppositeAndReadOnly()
    {
        var viewModel = PriceDialogViewModel.CreateForDelete("XPI", "Default", "BCIA11", DateTime.Today, 10m);

        viewModel.IsReadOnly.Should().BeTrue();
        viewModel.IsEditable.Should().BeFalse();
    }

    [Theory]
    [InlineData(PriceDialogMode.Add)]
    [InlineData(PriceDialogMode.Update)]
    public void IsReadOnlyAndIsEditable_OnNonDeleteMode_AreOppositeAndEditable(PriceDialogMode mode)
    {
        var viewModel = new PriceDialogViewModel(mode, "XPI", "Default", "BCIA11", DateTime.Today, 10m);

        viewModel.IsReadOnly.Should().BeFalse();
        viewModel.IsEditable.Should().BeTrue();
    }

    [Fact]
    public void CreateForAdd_DefaultZeroPrice_ConfirmCommandCannotExecute()
    {
        var viewModel = PriceDialogViewModel.CreateForAdd("XPI", "Default", "BCIA11");

        viewModel.Mode.Should().Be(PriceDialogMode.Add);
        viewModel.Date.Should().Be(DateTime.Today);
        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ConfirmCommand_CanExecute_FalseWhileValidationMessageIsNotEmpty()
    {
        var viewModel = PriceDialogViewModel.CreateForAdd("XPI", "Default", "BCIA11");

        viewModel.Price = 0m; // Invalid: price must be greater than zero.

        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ConfirmCommand_CanExecute_TrueOnceAllFieldsAreValid()
    {
        var viewModel = PriceDialogViewModel.CreateForAdd("XPI", "Default", "BCIA11");

        viewModel.Date = DateTime.Today;
        viewModel.Price = 10m;

        viewModel.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ConfirmCommand_CanExecute_AlwaysTrueOnDeleteModeRegardlessOfFieldValidity()
    {
        var viewModel = PriceDialogViewModel.CreateForDelete("XPI", "Default", "BCIA11", DateTime.MinValue, 0m);

        viewModel.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ConfirmCommand_WhenValid_RaisesCloseRequestedWithTrue()
    {
        var viewModel = PriceDialogViewModel.CreateForAdd("XPI", "Default", "BCIA11");
        viewModel.Date = DateTime.Today;
        viewModel.Price = 10m;
        bool? raisedResult = null;
        viewModel.CloseRequested += (_, result) => raisedResult = result;

        viewModel.ConfirmCommand.Execute(null);

        raisedResult.Should().BeTrue();
    }

    [Fact]
    public void ConfirmCommand_WhenInvalid_DoesNotRaiseCloseRequested()
    {
        var viewModel = PriceDialogViewModel.CreateForAdd("XPI", "Default", "BCIA11");
        viewModel.Price = 0m; // Invalid.
        var raised = false;
        viewModel.CloseRequested += (_, _) => raised = true;

        viewModel.ConfirmCommand.Execute(null);

        raised.Should().BeFalse();
    }

    [Fact]
    public void CancelCommand_Execute_RaisesCloseRequestedWithFalse()
    {
        var viewModel = PriceDialogViewModel.CreateForAdd("XPI", "Default", "BCIA11");
        bool? raisedResult = null;
        viewModel.CloseRequested += (_, result) => raisedResult = result;

        viewModel.CancelCommand.Execute(null);

        raisedResult.Should().BeFalse();
    }

    [Fact]
    public void CreateForUpdate_PreservesProvidedFieldValues()
    {
        var date = new DateTime(2026, 7, 1);

        var viewModel = PriceDialogViewModel.CreateForUpdate("XPI", "Default", "BCIA11", date, 25m);

        viewModel.Date.Should().Be(date);
        viewModel.Price.Should().Be(25m);
    }
}
