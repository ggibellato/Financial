using System;
using Financial.Presentation.UI.ViewModels;
using FluentAssertions;

namespace Financial.Infrastructure.Tests;

public class OperationDialogViewModelTests
{
    [Fact]
    public void CreateForAdd_ShouldInitializeDefaults()
    {
        var viewModel = OperationDialogViewModel.CreateForAdd("XPI", "Default", "BCIA11");

        viewModel.Mode.Should().Be(OperationDialogMode.Add);
        viewModel.Type.Should().Be("Buy");
        viewModel.Quantity.Should().Be(0);
        viewModel.UnitPrice.Should().Be(0);
        viewModel.Fees.Should().Be(0);
        viewModel.IsReadOnly.Should().BeFalse();
        viewModel.IsEditable.Should().BeTrue();
    }

    [Fact]
    public void ConfirmCommand_ShouldBeDisabled_WhenValidationFails()
    {
        var viewModel = OperationDialogViewModel.CreateForAdd("XPI", "Default", "BCIA11");
        viewModel.Quantity = 0;

        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();
        viewModel.ValidationMessage.Should().NotBeEmpty();
    }

    [Fact]
    public void ConfirmCommand_ShouldBeEnabled_WhenValidationPasses()
    {
        var viewModel = OperationDialogViewModel.CreateForAdd("XPI", "Default", "BCIA11");
        viewModel.Quantity = 1;
        viewModel.UnitPrice = 10;
        viewModel.Fees = 0.5m;
        viewModel.Type = "Sell";
        viewModel.Date = DateTime.Today;

        viewModel.ConfirmCommand.CanExecute(null).Should().BeTrue();
        viewModel.ValidationMessage.Should().BeEmpty();
    }

    [Fact]
    public void DeleteMode_ShouldAllowConfirm_EvenWithInvalidValues()
    {
        var viewModel = OperationDialogViewModel.CreateForDelete(
            "XPI",
            "Default",
            "BCIA11",
            Guid.NewGuid(),
            DateTime.MinValue,
            "Invalid",
            0,
            -1,
            -1);

        viewModel.Mode.Should().Be(OperationDialogMode.Delete);
        viewModel.ConfirmCommand.CanExecute(null).Should().BeTrue();
        viewModel.ValidationMessage.Should().BeEmpty();
    }

    [Fact]
    public void ConfirmCommand_ShouldRaiseCloseRequested_WhenValid()
    {
        var viewModel = OperationDialogViewModel.CreateForAdd("XPI", "Default", "BCIA11");
        viewModel.Quantity = 1;
        viewModel.UnitPrice = 1;
        viewModel.Fees = 0;
        viewModel.Type = "Buy";
        viewModel.Date = DateTime.Today;

        bool? result = null;
        viewModel.CloseRequested += (_, dialogResult) => result = dialogResult;

        viewModel.ConfirmCommand.Execute(null);

        result.Should().BeTrue();
    }
}
