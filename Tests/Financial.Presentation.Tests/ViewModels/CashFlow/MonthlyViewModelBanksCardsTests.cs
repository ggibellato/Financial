using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.CashFlow;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class MonthlyViewModelBanksCardsTests
{
    private static readonly Guid BarclaysId = Guid.NewGuid();
    private static readonly Guid ChaseId = Guid.NewGuid();
    private static readonly Guid BaAmexId = Guid.NewGuid();
    private static readonly Guid ChaseCardId = Guid.NewGuid();

    private static (
        MonthlyViewModel ViewModel,
        StubExpenseService Expenses,
        StubBankService Banks,
        StubTransferService Transfers,
        StubBalanceAdjustmentService Adjustments,
        StubCardStatementService Cards) CreateViewModel(bool confirmDeletes = true)
    {
        var expenses = new StubExpenseService();
        var incomes = new StubIncomeService();
        var banks = new StubBankService
        {
            Banks =
            [
                new BankDTO { Id = BarclaysId, Name = "Barclays", RoundUpEnabled = true, OpeningBalance = 0, OpeningBalanceDate = DateOnly.FromDateTime(DateTime.Today) },
                new BankDTO { Id = ChaseId, Name = "Chase", RoundUpEnabled = false, OpeningBalance = 0, OpeningBalanceDate = DateOnly.FromDateTime(DateTime.Today) },
            ],
        };
        var incomeSources = new StubIncomeSourceService();
        var tithe = new StubTitheService();
        var transfers = new StubTransferService();
        var adjustments = new StubBalanceAdjustmentService();
        var cards = new StubCardStatementService();
        var creditCards = new StubCreditCardService();
        var categories = new StubCategoryService();

        var viewModel = new MonthlyViewModel(expenses, incomes, banks, incomeSources, tithe, transfers, adjustments, cards, creditCards, categories, confirm: _ => confirmDeletes, new RecordingTelemetryTracer());
        return (viewModel, expenses, banks, transfers, adjustments, cards);
    }

    [Fact]
    public async Task BankTotals_ComputesBalanceAndRoundUpTotalPerBank()
    {
        var (viewModel, expenses, banks, _, _, _) = CreateViewModel();
        banks.BankBalances = [new BankBalanceDTO { Bank = "Barclays", Balance = 250m }, new BankBalanceDTO { Bank = "Chase", Balance = 10m }];
        expenses.Expenses =
        [
            new ExpenseDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), Description = "A", Value = 20m, CategoryId = Guid.NewGuid(), CategoryName = "Mercado", PaymentSourceBankId = BarclaysId, PaymentSourceBankName = "Barclays", PaymentStatus = "ImmediatePayment", RoundUpAmount = 0.30m },
            new ExpenseDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), Description = "B", Value = 15m, CategoryId = Guid.NewGuid(), CategoryName = "Mercado", PaymentSourceBankId = BarclaysId, PaymentSourceBankName = "Barclays", PaymentStatus = "ImmediatePayment", RoundUpAmount = 0.20m },
        ];

        await viewModel.RefreshAsync();

        var barclaysRow = viewModel.BankTotals.Single(b => b.Bank == "Barclays");
        barclaysRow.Balance.Should().Be(250m);
        barclaysRow.RoundUpTotal.Should().Be(0.50m);
        viewModel.BankTotals.Single(b => b.Bank == "Chase").RoundUpTotal.Should().Be(0m);
        viewModel.BankTotalsSum.Should().Be(260m);
        viewModel.RoundUpTotalsSum.Should().Be(0.50m);
    }

    [Fact]
    public async Task RefreshAsync_PopulatesCreditCards()
    {
        var (viewModel, _) = CreateViewModelWithCreditCards(
            [
                new() { Id = BaAmexId, Name = "BaAmex", IsActive = true, NextInvoiceDueDate = new DateOnly(2026, 9, 5) },
                new() { Id = ChaseCardId, Name = "ChaseMaster4023", IsActive = false, NextInvoiceDueDate = null },
            ]);

        await viewModel.RefreshAsync();

        viewModel.CreditCards.Should().HaveCount(2);
        viewModel.CreditCards.Should().Contain(c => c.Name == "BaAmex" && c.IsActive);
    }

    [Fact]
    public async Task ActiveCreditCards_ExcludesInactiveCards()
    {
        var (viewModel, _) = CreateViewModelWithCreditCards(
            [
                new() { Id = BaAmexId, Name = "BaAmex", IsActive = true, NextInvoiceDueDate = new DateOnly(2026, 9, 5) },
                new() { Id = ChaseCardId, Name = "ChaseMaster4023", IsActive = false, NextInvoiceDueDate = null },
            ]);

        await viewModel.RefreshAsync();

        viewModel.Expense.ActiveCreditCards.Should().ContainSingle(c => c.Name == "BaAmex");
        viewModel.Expense.ActiveCreditCards.Should().NotContain(c => c.Name == "ChaseMaster4023");
    }

    [Fact]
    public async Task DeactivatingACard_RemovesItFromActiveCreditCards_OnNextRefresh()
    {
        var (viewModel, _) = CreateViewModelWithCreditCards(
            [
                new() { Id = BaAmexId, Name = "BaAmex", IsActive = true, NextInvoiceDueDate = new DateOnly(2026, 9, 5) },
            ]);
        await viewModel.RefreshAsync();
        var card = viewModel.CreditCards.Single(c => c.Name == "BaAmex");

        await viewModel.Cards.UpdateCreditCardAsync(card, card.NextInvoiceDueDate, isActive: false);

        viewModel.Expense.ActiveCreditCards.Should().NotContain(c => c.Name == "BaAmex");
    }

    private static (MonthlyViewModel ViewModel, StubCreditCardService CreditCards) CreateViewModelWithCreditCards(List<CreditCardDTO> creditCards)
    {
        var expenses = new StubExpenseService();
        var incomes = new StubIncomeService();
        var banks = new StubBankService();
        var incomeSources = new StubIncomeSourceService();
        var tithe = new StubTitheService();
        var transfers = new StubTransferService();
        var adjustments = new StubBalanceAdjustmentService();
        var cards = new StubCardStatementService();
        var creditCardService = new StubCreditCardService { CreditCards = creditCards };
        var categories = new StubCategoryService();

        var viewModel = new MonthlyViewModel(expenses, incomes, banks, incomeSources, tithe, transfers, adjustments, cards, creditCardService, categories, confirm: _ => true, new RecordingTelemetryTracer());
        return (viewModel, creditCardService);
    }
}
