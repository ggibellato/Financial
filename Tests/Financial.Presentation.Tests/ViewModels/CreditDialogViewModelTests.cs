using Financial.Presentation.App.ViewModels;
using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class CreditDialogViewModelTests
{
    [Theory]
    [InlineData(CreditDialogMode.Add, "New credit", "Add credit")]
    [InlineData(CreditDialogMode.Update, "Edit credit", "Save")]
    [InlineData(CreditDialogMode.Delete, "Delete Credit", "Delete")]
    public void TitleAndConfirmLabel_ReflectMode(CreditDialogMode mode, string expectedTitle, string expectedConfirmLabel)
    {
        var viewModel = new CreditDialogViewModel(mode, "XPI", "Default", "PETR4", Guid.NewGuid(), DateTime.Today, "Dividend", 10m);

        viewModel.Title.Should().Be(expectedTitle);
        viewModel.ConfirmLabel.Should().Be(expectedConfirmLabel);
    }

    [Fact]
    public void IsReadOnlyAndIsEditable_OnDeleteMode_AreOppositeAndReadOnly()
    {
        var viewModel = CreditDialogViewModel.CreateForDelete("XPI", "Default", "PETR4", Guid.NewGuid(), DateTime.Today, "Dividend", 10m);

        viewModel.IsReadOnly.Should().BeTrue();
        viewModel.IsEditable.Should().BeFalse();
    }

    [Theory]
    [InlineData(CreditDialogMode.Add)]
    [InlineData(CreditDialogMode.Update)]
    public void IsReadOnlyAndIsEditable_OnNonDeleteMode_AreOppositeAndEditable(CreditDialogMode mode)
    {
        var viewModel = new CreditDialogViewModel(mode, "XPI", "Default", "PETR4", Guid.NewGuid(), DateTime.Today, "Dividend", 10m);

        viewModel.IsReadOnly.Should().BeFalse();
        viewModel.IsEditable.Should().BeTrue();
    }

    [Fact]
    public void CreateForAdd_DefaultZeroValue_ConfirmCommandCannotExecute()
    {
        var viewModel = CreditDialogViewModel.CreateForAdd("XPI", "Default", "PETR4");

        viewModel.Mode.Should().Be(CreditDialogMode.Add);
        viewModel.CreditId.Should().Be(Guid.Empty);
        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ConfirmCommand_CanExecute_FalseWhileValidationMessageIsNotEmpty()
    {
        var viewModel = CreditDialogViewModel.CreateForAdd("XPI", "Default", "PETR4");

        viewModel.Value = 0m; // Invalid: value must be greater than zero.

        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ConfirmCommand_CanExecute_TrueOnceAllFieldsAreValid()
    {
        var viewModel = CreditDialogViewModel.CreateForAdd("XPI", "Default", "PETR4");

        viewModel.Date = DateTime.Today;
        viewModel.Type = "Dividend";
        viewModel.Value = 10m;

        viewModel.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ConfirmCommand_CanExecute_AlwaysTrueOnDeleteModeRegardlessOfFieldValidity()
    {
        var viewModel = CreditDialogViewModel.CreateForDelete("XPI", "Default", "PETR4", Guid.NewGuid(), DateTime.MinValue, "NotAType", 0m);

        viewModel.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ConfirmCommand_WhenValid_RaisesCloseRequestedWithTrue()
    {
        var viewModel = CreditDialogViewModel.CreateForAdd("XPI", "Default", "PETR4");
        viewModel.Date = DateTime.Today;
        viewModel.Type = "Dividend";
        viewModel.Value = 10m;
        bool? raisedResult = null;
        viewModel.CloseRequested += (_, result) => raisedResult = result;

        viewModel.ConfirmCommand.Execute(null);

        raisedResult.Should().BeTrue();
    }

    [Fact]
    public void ConfirmCommand_WhenInvalid_DoesNotRaiseCloseRequested()
    {
        var viewModel = CreditDialogViewModel.CreateForAdd("XPI", "Default", "PETR4");
        viewModel.Value = 0m; // Invalid.
        var raised = false;
        viewModel.CloseRequested += (_, _) => raised = true;

        viewModel.ConfirmCommand.Execute(null);

        raised.Should().BeFalse();
    }

    [Fact]
    public void CancelCommand_Execute_RaisesCloseRequestedWithFalse()
    {
        var viewModel = CreditDialogViewModel.CreateForAdd("XPI", "Default", "PETR4");
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

        var viewModel = CreditDialogViewModel.CreateForUpdate("XPI", "Default", "PETR4", id, date, "Rent", 25m);

        viewModel.CreditId.Should().Be(id);
        viewModel.Date.Should().Be(date);
        viewModel.Type.Should().Be("Rent");
        viewModel.Value.Should().Be(25m);
    }
}
