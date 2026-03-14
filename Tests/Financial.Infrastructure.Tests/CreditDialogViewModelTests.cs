using System;
using Financial.Presentation.UI.ViewModels;
using FluentAssertions;

namespace Financial.Infrastructure.Tests;

public class CreditDialogViewModelTests
{
    [Fact]
    public void CreateForAdd_ShouldInitializeDefaults()
    {
        var viewModel = CreditDialogViewModel.CreateForAdd("XPI", "Default", "BCIA11");

        viewModel.Mode.Should().Be(CreditDialogMode.Add);
        viewModel.Type.Should().Be("Dividend");
        viewModel.Value.Should().Be(0);
        viewModel.IsReadOnly.Should().BeFalse();
        viewModel.IsEditable.Should().BeTrue();
    }

    [Fact]
    public void ConfirmCommand_ShouldBeDisabled_WhenValidationFails()
    {
        var viewModel = CreditDialogViewModel.CreateForAdd("XPI", "Default", "BCIA11");
        viewModel.Value = 0;

        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();
        viewModel.ValidationMessage.Should().NotBeEmpty();
    }

    [Fact]
    public void ConfirmCommand_ShouldBeEnabled_WhenValidationPasses()
    {
        var viewModel = CreditDialogViewModel.CreateForAdd("XPI", "Default", "BCIA11");
        viewModel.Value = 10;
        viewModel.Type = "Rent";
        viewModel.Date = DateTime.Today;

        viewModel.ConfirmCommand.CanExecute(null).Should().BeTrue();
        viewModel.ValidationMessage.Should().BeEmpty();
    }

    [Fact]
    public void DeleteMode_ShouldAllowConfirm_EvenWithInvalidValues()
    {
        var viewModel = CreditDialogViewModel.CreateForDelete(
            "XPI",
            "Default",
            "BCIA11",
            Guid.NewGuid(),
            DateTime.MinValue,
            "Invalid",
            0);

        viewModel.Mode.Should().Be(CreditDialogMode.Delete);
        viewModel.ConfirmCommand.CanExecute(null).Should().BeTrue();
        viewModel.ValidationMessage.Should().BeEmpty();
    }

    [Fact]
    public void ConfirmCommand_ShouldRaiseCloseRequested_WhenValid()
    {
        var viewModel = CreditDialogViewModel.CreateForAdd("XPI", "Default", "BCIA11");
        viewModel.Value = 1;
        viewModel.Type = "Dividend";
        viewModel.Date = DateTime.Today;

        bool? result = null;
        viewModel.CloseRequested += (_, dialogResult) => result = dialogResult;

        viewModel.ConfirmCommand.Execute(null);

        result.Should().BeTrue();
    }
}
