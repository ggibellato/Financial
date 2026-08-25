using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.CashFlow;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class MonthlyViewModelCategoriesTests
{
    private static readonly Guid MercadoId = Guid.NewGuid();
    private static readonly Guid ReservaId = Guid.NewGuid();
    private static readonly Guid BarclaysId = Guid.NewGuid();

    private static (MonthlyViewModel ViewModel, StubExpenseService Expenses) CreateViewModel()
    {
        var expenses = new StubExpenseService();
        var incomes = new StubIncomeService();
        var banks = new StubBankService
        {
            Banks = [new BankDTO { Id = BarclaysId, Name = "Barclays", RoundUpEnabled = false, OpeningBalance = 0, OpeningBalanceDate = DateOnly.FromDateTime(DateTime.Today) }],
        };
        var incomeSources = new StubIncomeSourceService();
        var tithe = new StubTitheService();
        var transfers = new StubTransferService();
        var adjustments = new StubBalanceAdjustmentService();
        var cardStatements = new StubCardStatementService();
        var creditCards = new StubCreditCardService();
        var categories = new StubCategoryService
        {
            Categories =
            [
                new() { Id = MercadoId, Name = "Mercado", Active = true, IsInvestment = false, IsTithe = false },
                new() { Id = ReservaId, Name = "Reserva", Active = false, IsInvestment = false, IsTithe = false },
            ],
        };

        var viewModel = new MonthlyViewModel(expenses, incomes, banks, incomeSources, tithe, transfers, adjustments, cardStatements, creditCards, categories, confirm: _ => true, new RecordingTelemetryTracer());
        return (viewModel, expenses);
    }

    [Fact]
    public async Task RefreshAsync_PopulatesCategories()
    {
        var (viewModel, _) = CreateViewModel();

        await viewModel.RefreshAsync();

        viewModel.Categories.Should().HaveCount(2);
        viewModel.Categories.Should().Contain(c => c.Name == "Mercado" && c.Active);
    }

    [Fact]
    public async Task ActiveCategories_ExcludesInactiveCategories()
    {
        var (viewModel, _) = CreateViewModel();

        await viewModel.RefreshAsync();

        viewModel.ActiveCategories.Should().ContainSingle(c => c.Name == "Mercado");
        viewModel.ActiveCategories.Should().NotContain(c => c.Name == "Reserva");
    }

    [Fact]
    public async Task SaveExpenseAsync_SendsSelectedCategoryId()
    {
        var (viewModel, expenses) = CreateViewModel();
        await viewModel.RefreshAsync();
        viewModel.Expense.ShowCreateExpenseFormCommand.Execute("bank");
        viewModel.Expense.ExpenseFormDate = DateTime.Today;
        viewModel.Expense.ExpenseFormDescription = "Groceries";
        viewModel.Expense.ExpenseFormValue = "10";
        viewModel.Expense.ExpenseFormCategoryId = MercadoId;

        await viewModel.Expense.SaveExpenseAsync();

        expenses.LastCreateRequest.Should().NotBeNull();
        expenses.LastCreateRequest!.CategoryId.Should().Be(MercadoId);
    }

    [Fact]
    public async Task ShowCreateExpenseFormCommand_DefaultsToFirstActiveCategory()
    {
        var (viewModel, _) = CreateViewModel();
        await viewModel.RefreshAsync();

        viewModel.Expense.ShowCreateExpenseFormCommand.Execute("bank");

        viewModel.Expense.ExpenseFormCategoryId.Should().Be(MercadoId);
    }
}
