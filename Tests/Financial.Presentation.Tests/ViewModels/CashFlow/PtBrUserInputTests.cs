using System.Collections.ObjectModel;
using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.CashFlow;
using Financial.Tests;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

[UseCulture("pt-BR")]
public class PtBrUserInputTests
{
    private static readonly Guid CategoryId = Guid.NewGuid();

    [Theory]
    [InlineData("9,40", "0,60")]
    [InlineData("12,5", "0,99")]
    public void ExpenseForm_AcceptsTheHostCultureDecimalSeparator(string value, string roundUp)
    {
        var message = ExpenseFormValidation.BuildValidationMessage(
            TestClock.LocalToday, "Groceries", CategoryId, value, false, Guid.NewGuid(), null, true, roundUp);

        message.Should().BeEmpty();
    }

    [Fact]
    public void ExpenseForm_TreatsADotAsAThousandsSeparator_SoAnEnGbStyleRoundUpIsOutOfRange()
    {
        var message = ExpenseFormValidation.BuildValidationMessage(
            TestClock.LocalToday, "Groceries", CategoryId, "10", false, Guid.NewGuid(), null, true, "0.60");

        message.Should().NotBeEmpty();
    }

    [Fact]
    public void ExpenseWorkflow_SuggestsTheRoundUpInTheHostCultureFormat()
    {
        var viewModel = new ExpenseWorkflowViewModel(
            new StubExpenseService(),
            new ObservableCollection<CategoryDTO>([new CategoryDTO { Id = CategoryId, Name = "Mercado", Active = true, HasReferences = false, IsInvestment = false, IsTithe = false }]),
            new ObservableCollection<BankDTO>([new BankDTO { Id = Guid.NewGuid(), Name = "Barclays", RoundUpEnabled = true, OpeningBalance = 0, OpeningBalanceDate = TestClock.Today, HasReferences = false }]),
            new ObservableCollection<CreditCardDTO>(),
            _ => true, TestClock.At(), new RecordingTelemetryTracer(), () => Task.CompletedTask);
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");

        viewModel.ExpenseFormValue = "9,40";

        viewModel.ExpenseFormRoundUpAmount.Should().Be("0,60");
    }
}
