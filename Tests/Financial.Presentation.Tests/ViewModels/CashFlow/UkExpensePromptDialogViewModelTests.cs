using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class UkExpensePromptDialogViewModelTests
{
    private static RecurringBillDTO Bill() => new()
    {
        Id = Guid.NewGuid(), DueDay = 5, Description = "Council Tax", Value = 120m,
        Area = "UK", Note = string.Empty, NitNumber = null, MinimumWageValue = null, Status = "Unset",
    };

    private static BankDTO Bank(Guid id, string name) => new()
    {
        Id = id, Name = name, RoundUpEnabled = false, OpeningBalance = 0, OpeningBalanceDate = default, HasReferences = false,
    };

    private static CategoryDTO Category(Guid id, string name) => new()
    {
        Id = id, Name = name, Active = true, IsInvestment = false, IsTithe = false, HasReferences = false,
    };

    [Fact]
    public void Constructor_PrefillsFromBillAndDefaultsDateToToday()
    {
        var bill = Bill();

        var viewModel = new UkExpensePromptDialogViewModel(bill, [], []);

        viewModel.Description.Should().Be("Council Tax");
        viewModel.Value.Should().Be("120");
        viewModel.Date.Date.Should().Be(DateTime.Today);
        viewModel.BankId.Should().BeNull();
        viewModel.CategoryId.Should().BeNull();
    }

    [Fact]
    public void ConfirmCommand_StartsDisabled_NoBankOrCategorySelected()
    {
        var viewModel = new UkExpensePromptDialogViewModel(Bill(), [], []);

        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ConfirmCommand_EnabledOnceBankAndCategorySelected()
    {
        var bankId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var viewModel = new UkExpensePromptDialogViewModel(Bill(), [Bank(bankId, "Barclays")], [Category(categoryId, "Bills")]);

        viewModel.BankId = bankId;
        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();

        viewModel.CategoryId = categoryId;
        viewModel.ConfirmCommand.CanExecute(null).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("not-a-number")]
    public void ConfirmCommand_DisabledForInvalidValue(string value)
    {
        var bankId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var viewModel = new UkExpensePromptDialogViewModel(Bill(), [Bank(bankId, "Barclays")], [Category(categoryId, "Bills")])
        {
            BankId = bankId,
            CategoryId = categoryId,
            Value = value,
        };

        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ConfirmCommand_Execute_SetsDecisionConfirmAndRaisesCloseRequestedTrue()
    {
        var bankId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var viewModel = new UkExpensePromptDialogViewModel(Bill(), [Bank(bankId, "Barclays")], [Category(categoryId, "Bills")])
        {
            BankId = bankId,
            CategoryId = categoryId,
        };
        bool? result = null;
        viewModel.CloseRequested += (_, r) => result = r;

        viewModel.ConfirmCommand.Execute(null);

        viewModel.Decision.Should().Be(UkExpensePromptDecision.Confirm);
        result.Should().BeTrue();
    }

    [Fact]
    public void SkipCommand_Execute_SetsDecisionSkipAndRaisesCloseRequestedTrue()
    {
        var viewModel = new UkExpensePromptDialogViewModel(Bill(), [], []);
        bool? result = null;
        viewModel.CloseRequested += (_, r) => result = r;

        viewModel.SkipCommand.Execute(null);

        viewModel.Decision.Should().Be(UkExpensePromptDecision.Skip);
        result.Should().BeTrue();
    }

    [Fact]
    public void CancelCommand_Execute_SetsDecisionCancelAndRaisesCloseRequestedFalse()
    {
        var viewModel = new UkExpensePromptDialogViewModel(Bill(), [], []);
        bool? result = null;
        viewModel.CloseRequested += (_, r) => result = r;

        viewModel.CancelCommand.Execute(null);

        viewModel.Decision.Should().Be(UkExpensePromptDecision.Cancel);
        result.Should().BeFalse();
    }
}
