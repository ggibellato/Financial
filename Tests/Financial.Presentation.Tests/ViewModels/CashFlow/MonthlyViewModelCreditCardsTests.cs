using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class MonthlyViewModelCreditCardsTests
{
    private static readonly Guid BaAmexId = Guid.NewGuid();
    private static readonly Guid ChaseId = Guid.NewGuid();

    private static (MonthlyViewModel ViewModel, StubCreditCardService CreditCards) CreateViewModel()
    {
        var expenses = new StubExpenseService();
        var incomes = new StubIncomeService();
        var banks = new StubBankService();
        var incomeSources = new StubIncomeSourceService();
        var tithe = new StubTitheService();
        var transfers = new StubTransferService();
        var adjustments = new StubBalanceAdjustmentService();
        var cardStatements = new StubCardStatementService();
        var creditCards = new StubCreditCardService
        {
            CreditCards =
            [
                new() { Id = BaAmexId, Name = "BaAmex", IsActive = true, NextInvoiceDueDate = new DateOnly(2026, 9, 5) },
                new() { Id = ChaseId, Name = "ChaseMaster4023", IsActive = false, NextInvoiceDueDate = null },
            ],
        };

        var categories = new StubCategoryService();

        var viewModel = new MonthlyViewModel(expenses, incomes, banks, incomeSources, tithe, transfers, adjustments, cardStatements, creditCards, categories, confirm: _ => true);
        return (viewModel, creditCards);
    }

    [Fact]
    public async Task RefreshAsync_PopulatesCreditCards()
    {
        var (viewModel, _) = CreateViewModel();

        await viewModel.RefreshAsync();

        viewModel.CreditCards.Should().HaveCount(2);
        viewModel.CreditCards.Should().Contain(c => c.Name == "BaAmex" && c.IsActive);
    }

    [Fact]
    public async Task ActiveCreditCards_ExcludesInactiveCards()
    {
        var (viewModel, _) = CreateViewModel();

        await viewModel.RefreshAsync();

        viewModel.ActiveCreditCards.Should().ContainSingle(c => c.Name == "BaAmex");
        viewModel.ActiveCreditCards.Should().NotContain(c => c.Name == "ChaseMaster4023");
    }

    [Fact]
    public async Task UpdateCreditCardAsync_SendsIdAndNewFields_ThenRefreshes()
    {
        var (viewModel, creditCards) = CreateViewModel();
        await viewModel.RefreshAsync();
        var card = viewModel.CreditCards.Single(c => c.Name == "BaAmex");
        var newDueDate = new DateOnly(2026, 10, 1);

        await viewModel.UpdateCreditCardAsync(card, newDueDate, isActive: false);

        creditCards.LastUpdateRequest.Should().NotBeNull();
        creditCards.LastUpdateRequest!.Value.Id.Should().Be(BaAmexId);
        creditCards.LastUpdateRequest.Value.Request.NextInvoiceDueDate.Should().Be(newDueDate);
        creditCards.LastUpdateRequest.Value.Request.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateCreditCardAsync_ServiceThrows_SetsCreditCardUpdateError()
    {
        var (viewModel, creditCards) = CreateViewModel();
        await viewModel.RefreshAsync();
        creditCards.ThrowOnUpdate = "Credit card was not found.";
        var card = viewModel.CreditCards.Single(c => c.Name == "BaAmex");

        await viewModel.UpdateCreditCardAsync(card, card.NextInvoiceDueDate, isActive: false);

        viewModel.CreditCardUpdateError.Should().Be("Credit card was not found.");
        viewModel.UpdatingCreditCardId.Should().BeNull();
    }

    [Fact]
    public async Task DeactivatingACard_RemovesItFromActiveCreditCards_OnNextRefresh()
    {
        var (viewModel, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        var card = viewModel.CreditCards.Single(c => c.Name == "BaAmex");

        await viewModel.UpdateCreditCardAsync(card, card.NextInvoiceDueDate, isActive: false);

        viewModel.ActiveCreditCards.Should().NotContain(c => c.Name == "BaAmex");
    }
}
