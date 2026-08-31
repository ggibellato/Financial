using Financial.Presentation.App.ViewModels.Admin;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Admin;

public class RecurringBillFormDialogViewModelTests
{
    [Fact]
    public void Constructor_NoCurrentId_IsCreateModeWithEmptyFields()
    {
        var viewModel = new RecurringBillFormDialogViewModel();

        viewModel.IsEditing.Should().BeFalse();
        viewModel.Title.Should().Be("Create Recurring Bill");
        viewModel.DueDay.Should().BeEmpty();
        viewModel.Description.Should().BeEmpty();
        viewModel.Value.Should().BeEmpty();
        viewModel.Area.Should().Be("Brasil");
    }

    [Fact]
    public void Constructor_WithCurrentId_IsEditModePreFilled()
    {
        var id = Guid.NewGuid();
        var viewModel = new RecurringBillFormDialogViewModel(
            id, 10, "INSS", 850m, "Brasil", "Direct debit", "12345678901", 1621m, "Scheduled");

        viewModel.IsEditing.Should().BeTrue();
        viewModel.Title.Should().Be("Edit Recurring Bill");
        viewModel.DueDay.Should().Be("10");
        viewModel.Description.Should().Be("INSS");
        viewModel.Value.Should().Be("850");
        viewModel.Note.Should().Be("Direct debit");
        viewModel.NitNumber.Should().Be("12345678901");
        viewModel.MinimumWageValue.Should().Be("1621");
        viewModel.Status.Should().Be("Scheduled");
    }

    [Fact]
    public void Constructor_BlankFields_StartsInvalid()
    {
        var viewModel = new RecurringBillFormDialogViewModel();

        viewModel.ValidationMessage.Should().NotBeEmpty();
        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ValidFields_BecomesValid()
    {
        var viewModel = new RecurringBillFormDialogViewModel
        {
            DueDay = "10",
            Description = "Rent",
            Value = "1500",
        };

        viewModel.ValidationMessage.Should().BeEmpty();
        viewModel.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Theory]
    [InlineData("0")]
    [InlineData("32")]
    [InlineData("not-a-number")]
    public void InvalidDueDay_StaysInvalid(string dueDay)
    {
        var viewModel = new RecurringBillFormDialogViewModel { DueDay = dueDay, Description = "Rent", Value = "100" };

        viewModel.ValidationMessage.Should().NotBeEmpty();
    }

    [Fact]
    public void ParsedDueDay_ReturnsTheParsedInteger()
    {
        var viewModel = new RecurringBillFormDialogViewModel { DueDay = "15", Description = "Rent", Value = "100" };

        viewModel.ParsedDueDay.Should().Be(15);
    }

    [Fact]
    public void ParsedValue_ReturnsTheParsedDecimal()
    {
        var viewModel = new RecurringBillFormDialogViewModel { DueDay = "15", Description = "Rent", Value = "1500.50" };

        viewModel.ParsedValue.Should().Be(1500.50m);
    }

    [Fact]
    public void NormalizedNitNumber_BlankReturnsNull()
    {
        var viewModel = new RecurringBillFormDialogViewModel { NitNumber = "   " };

        viewModel.NormalizedNitNumber.Should().BeNull();
    }

    [Fact]
    public void ParsedMinimumWageValue_BlankReturnsNull()
    {
        var viewModel = new RecurringBillFormDialogViewModel();

        viewModel.ParsedMinimumWageValue.Should().BeNull();
    }

    [Fact]
    public void ConfirmCommand_ValidForm_TrimsDescriptionAndRaisesCloseRequestedTrue()
    {
        var viewModel = new RecurringBillFormDialogViewModel { DueDay = "10", Description = "  Rent  ", Value = "1500" };
        bool? result = null;
        viewModel.CloseRequested += (_, r) => result = r;

        viewModel.ConfirmCommand.Execute(null);

        result.Should().Be(true);
        viewModel.Description.Should().Be("Rent");
    }

    [Fact]
    public void ConfirmCommand_InvalidForm_DoesNotRaiseCloseRequested()
    {
        var viewModel = new RecurringBillFormDialogViewModel();
        var raised = false;
        viewModel.CloseRequested += (_, _) => raised = true;

        viewModel.ConfirmCommand.Execute(null);

        raised.Should().BeFalse();
    }

    [Fact]
    public void CancelCommand_RaisesCloseRequestedFalse()
    {
        var viewModel = new RecurringBillFormDialogViewModel();
        bool? result = null;
        viewModel.CloseRequested += (_, r) => result = r;

        viewModel.CancelCommand.Execute(null);

        result.Should().Be(false);
    }
}
