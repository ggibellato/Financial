using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class MonthlyViewModelTests
{
    private static (MonthlyViewModel ViewModel, StubExpenseService Expenses, StubIncomeService Incomes, StubBankService Banks, StubTitheService Tithe) CreateViewModel()
    {
        var expenses = new StubExpenseService();
        var incomes = new StubIncomeService();
        var banks = new StubBankService { Banks = [new BankDTO { Name = "Barclays", RoundUpEnabled = true, OpeningBalance = 0, OpeningBalanceDate = DateOnly.FromDateTime(DateTime.Today) }, new BankDTO { Name = "Chase", RoundUpEnabled = false, OpeningBalance = 0, OpeningBalanceDate = DateOnly.FromDateTime(DateTime.Today) }] };
        var tithe = new StubTitheService { Summary = new TitheSummaryDTO { CalculatedTithe = 100m, TitheBalance = 50m } };

        var viewModel = new MonthlyViewModel(expenses, incomes, banks, tithe);
        return (viewModel, expenses, incomes, banks, tithe);
    }

    [Fact]
    public async Task LoadsExpensesIncomesCategoryTotalsAndTitheForCurrentMonth()
    {
        var (viewModel, expenses, incomes, _, tithe) = CreateViewModel();
        expenses.Expenses = [new ExpenseDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), Description = "Test", Value = 10m, Category = "Mercado", PaymentSource = "Barclays", PaymentStatus = "ImmediatePayment" }];
        expenses.CategoryTotals = [new CategoryTotalDTO { Category = "Mercado", TotalValue = 10m }];
        incomes.Incomes = [new IncomeDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), IncomeSource = "Gleison", NetValue = 100m, Bank = "Barclays" }];

        await viewModel.RefreshAsync();

        viewModel.Expenses.Should().ContainSingle();
        viewModel.Incomes.Should().ContainSingle();
        viewModel.CategoryTotals.Should().ContainSingle();
        viewModel.TitheSummary.Should().Be(tithe.Summary);
        viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task ChangingYearOrMonth_RefetchesAllFour()
    {
        var (viewModel, expenses, _, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        var callsBefore = expenses.GetExpensesByMonthCallCount;

        viewModel.Year = viewModel.Year - 1;
        await viewModel.RefreshAsync();

        expenses.GetExpensesByMonthCallCount.Should().BeGreaterThan(callsBefore);
    }

    [Fact]
    public async Task AddExpense_BankMode_CallsServiceWithPaymentSourceAndRefreshes()
    {
        var (viewModel, expenses, _, banks, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        viewModel.ShowCreateExpenseFormCommand.Execute(null);
        viewModel.ExpenseFormDate = DateTime.Today;
        viewModel.ExpenseFormDescription = "Groceries";
        viewModel.ExpenseFormCategory = "Mercado";
        viewModel.ExpenseFormValue = "25.50";
        viewModel.ExpenseFormPaymentSource = banks.Banks[1].Name; // Chase, no round-up

        await viewModel.SaveExpenseAsync();

        expenses.LastCreateRequest.Should().NotBeNull();
        expenses.LastCreateRequest!.PaymentSource.Should().Be("Chase");
        expenses.LastCreateRequest.CardTag.Should().BeNull();
        viewModel.IsExpenseFormOpen.Should().BeFalse();
    }

    [Fact]
    public async Task AddExpense_CardMode_CallsServiceWithCardTag()
    {
        var (viewModel, expenses, _, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        viewModel.ShowCreateExpenseFormCommand.Execute(null);
        viewModel.ExpenseFormDate = DateTime.Today;
        viewModel.ExpenseFormDescription = "Flight";
        viewModel.ExpenseFormCategory = "Viagem";
        viewModel.ExpenseFormValue = "300";
        viewModel.SetCardPaymentModeCommand.Execute(null);
        viewModel.ExpenseFormCardTag = MonthlyViewModel.Cards[0];

        await viewModel.SaveExpenseAsync();

        expenses.LastCreateRequest.Should().NotBeNull();
        expenses.LastCreateRequest!.CardTag.Should().Be(MonthlyViewModel.Cards[0]);
        expenses.LastCreateRequest.PaymentSource.Should().BeNull();
    }

    [Fact]
    public void SelectingRoundUpEnabledBank_ShowsRoundUpField()
    {
        var (viewModel, _, _, banks, _) = CreateViewModel();
        viewModel.ShowCreateExpenseFormCommand.Execute(null);

        viewModel.ExpenseFormPaymentSource = banks.Banks[0].Name; // Barclays, round-up enabled

        viewModel.ShowRoundUpField.Should().BeTrue();
    }

    [Fact]
    public void SelectingNonRoundUpBank_HidesRoundUpField()
    {
        var (viewModel, _, _, banks, _) = CreateViewModel();
        viewModel.ShowCreateExpenseFormCommand.Execute(null);

        viewModel.ExpenseFormPaymentSource = banks.Banks[1].Name; // Chase, round-up disabled

        viewModel.ShowRoundUpField.Should().BeFalse();
    }

    [Fact]
    public void SettledExpense_DeleteCommandCannotExecute()
    {
        var (viewModel, _, _, _, _) = CreateViewModel();
        var settledExpense = new ExpenseDTO
        {
            Id = Guid.NewGuid(),
            Date = DateOnly.FromDateTime(DateTime.Today),
            Description = "Settled",
            Value = 10m,
            Category = "Mercado",
            CardTag = "BaAmex",
            PaymentStatus = "CreditCardSettled",
        };

        viewModel.DeleteExpenseCommand.CanExecute(settledExpense).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteExpense_CallsServiceAndRefreshes()
    {
        var (viewModel, expenses, _, _, _) = CreateViewModel();
        var expense = new ExpenseDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), Description = "X", Value = 1m, Category = "Mercado", PaymentStatus = "ImmediatePayment" };

        await viewModel.DeleteExpenseAsync(expense);

        expenses.LastDeletedId.Should().Be(expense.Id);
    }

    [Fact]
    public void AddIncome_GleisonSource_ShowsGrossValueField()
    {
        var (viewModel, _, _, _, _) = CreateViewModel();
        viewModel.ShowCreateIncomeFormCommand.Execute(null);

        viewModel.IncomeFormSource = "Gleison";

        viewModel.ShowIncomeGrossValueField.Should().BeTrue();
    }

    [Fact]
    public void AddIncome_LotterySource_HidesGrossValueField()
    {
        var (viewModel, _, _, _, _) = CreateViewModel();
        viewModel.ShowCreateIncomeFormCommand.Execute(null);

        viewModel.IncomeFormSource = "Lottery";

        viewModel.ShowIncomeGrossValueField.Should().BeFalse();
    }

    [Fact]
    public async Task AddIncome_ValidForm_CallsServiceAndRefreshes()
    {
        var (viewModel, _, incomes, banks, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        viewModel.ShowCreateIncomeFormCommand.Execute(null);
        viewModel.IncomeFormDate = DateTime.Today;
        viewModel.IncomeFormSource = "Lottery";
        viewModel.IncomeFormNetValue = "50";
        viewModel.IncomeFormBank = banks.Banks[0].Name;

        await viewModel.SaveIncomeAsync();

        incomes.LastCreateRequest.Should().NotBeNull();
        incomes.LastCreateRequest!.NetValue.Should().Be(50m);
        incomes.LastCreateRequest.GrossValue.Should().BeNull();
        viewModel.IsIncomeFormOpen.Should().BeFalse();
    }
}
