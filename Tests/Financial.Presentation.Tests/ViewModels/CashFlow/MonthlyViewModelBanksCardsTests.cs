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
    public async Task AddTransfer_ValidForm_CallsServiceAndRefreshes()
    {
        var (viewModel, _, banks, transfers, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        viewModel.ShowMoveMoneyFormCommand.Execute(banks.Banks[0].Id);
        viewModel.TransferFormDate = DateTime.Today;
        viewModel.TransferFormDestinationBank = banks.Banks[1].Id;
        viewModel.TransferFormAmount = "75";

        await viewModel.SaveTransferAsync();

        transfers.LastCreateRequest.Should().NotBeNull();
        transfers.LastCreateRequest!.SourceBankId.Should().Be(BarclaysId);
        transfers.LastCreateRequest.DestinationBankId.Should().Be(ChaseId);
        transfers.LastCreateRequest.Amount.Should().Be(75m);
        viewModel.IsTransferFormOpen.Should().BeFalse();
    }

    [Fact]
    public async Task AddTransfer_SameSourceAndDestination_BlocksSaveWithoutServiceCall()
    {
        var (viewModel, _, banks, transfers, _, _) = CreateViewModel();
        viewModel.ShowMoveMoneyFormCommand.Execute(banks.Banks[0].Id);
        viewModel.TransferFormDate = DateTime.Today;
        viewModel.TransferFormDestinationBank = banks.Banks[0].Id;
        viewModel.TransferFormAmount = "75";

        viewModel.SaveTransferCommand.CanExecute(null).Should().BeFalse();
        viewModel.SameBankTransferError.Should().NotBeNullOrEmpty();

        await viewModel.SaveTransferAsync();

        transfers.LastCreateRequest.Should().BeNull();
        viewModel.TransferSaveError.Should().NotBeNullOrEmpty();

        viewModel.TransferFormDestinationBank = banks.Banks[1].Id;
        viewModel.SaveTransferCommand.CanExecute(null).Should().BeTrue();
        viewModel.SameBankTransferError.Should().BeEmpty();
    }

    [Fact]
    public async Task AddTransfer_BackendRejects_KeepsFormOpenWithValuesAndShowsServerError()
    {
        var (viewModel, _, banks, transfers, _, _) = CreateViewModel();
        transfers.ThrowOnAdd = "Insufficient funds in source bank.";
        await viewModel.RefreshAsync();
        viewModel.ShowMoveMoneyFormCommand.Execute(banks.Banks[0].Id);
        viewModel.TransferFormDate = DateTime.Today;
        viewModel.TransferFormDestinationBank = banks.Banks[1].Id;
        viewModel.TransferFormAmount = "75";

        await viewModel.SaveTransferAsync();

        viewModel.IsTransferFormOpen.Should().BeTrue();
        viewModel.TransferSaveError.Should().Be("Insufficient funds in source bank.");
        viewModel.TransferFormAmount.Should().Be("75");
        viewModel.TransferFormDestinationBank.Should().Be(banks.Banks[1].Id);
    }

    [Fact]
    public async Task EditTransfer_ValidForm_CallsUpdateServiceWithCorrectId()
    {
        var (viewModel, _, _, transfers, _, _) = CreateViewModel();
        var transfer = new TransferDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), SourceBankId = BarclaysId, SourceBankName = "Barclays", DestinationBankId = ChaseId, DestinationBankName = "Chase", Amount = 50m };

        viewModel.EditTransferCommand.Execute(transfer);
        viewModel.TransferFormAmount = "60";

        await viewModel.SaveTransferAsync();

        transfers.LastUpdateRequest.Should().NotBeNull();
        transfers.LastUpdateRequest!.Value.Id.Should().Be(transfer.Id);
        transfers.LastUpdateRequest.Value.Request.Amount.Should().Be(60m);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeleteTransfer_ConfirmedAndDeclined_CallsOrSkipsService(bool confirmed)
    {
        var (viewModel, _, _, transfers, _, _) = CreateViewModel(confirmDeletes: confirmed);
        var transfer = new TransferDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), SourceBankId = BarclaysId, SourceBankName = "Barclays", DestinationBankId = ChaseId, DestinationBankName = "Chase", Amount = 50m };
        var row = BankOperationRow.FromTransfer(transfer);

        await viewModel.DeleteBankOperationAsync(row);

        if (confirmed)
        {
            transfers.LastDeletedId.Should().Be(transfer.Id);
        }
        else
        {
            transfers.LastDeletedId.Should().BeNull();
        }
    }

    [Fact]
    public async Task AddBalanceAdjustment_ValidForm_CallsServiceAndShowsDelta()
    {
        var (viewModel, _, banks, _, adjustments, _) = CreateViewModel();
        banks.BankBalances = [new BankBalanceDTO { Bank = "Barclays", Balance = 42.5m }];
        await viewModel.RefreshAsync();

        viewModel.ShowCorrectBalanceFormCommand.Execute(null);
        viewModel.AdjustmentFormBankName = BarclaysId;
        viewModel.AdjustmentFormCurrentBalance.Should().Be(42.5m);
        viewModel.AdjustmentFormDate = DateTime.Today;
        viewModel.AdjustmentFormTargetBalance = "50";

        await viewModel.SaveAdjustmentAsync();

        adjustments.LastCreateRequest.Should().NotBeNull();
        adjustments.LastCreateRequest!.Value.BankId.Should().Be(BarclaysId);
        adjustments.LastCreateRequest.Value.Request.TargetBalance.Should().Be(50m);
        viewModel.AdjustmentSavedDelta.Should().NotBeNull();
    }

    [Fact]
    public async Task EditAdjustment_ValidForm_CallsUpdateServiceWithCorrectBankAndId()
    {
        var (viewModel, _, _, _, adjustments, _) = CreateViewModel();
        var adjustment = new BalanceAdjustmentDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), BankId = BarclaysId, BankName = "Barclays", TargetBalance = 100m, Delta = 5m };

        viewModel.EditAdjustmentCommand.Execute(adjustment);
        viewModel.AdjustmentFormTargetBalance = "120";

        await viewModel.SaveAdjustmentAsync();

        adjustments.LastUpdateRequest.Should().NotBeNull();
        adjustments.LastUpdateRequest!.Value.BankId.Should().Be(BarclaysId);
        adjustments.LastUpdateRequest.Value.Id.Should().Be(adjustment.Id);
        adjustments.LastUpdateRequest.Value.Request.TargetBalance.Should().Be(120m);
    }

    [Fact]
    public async Task DeleteAdjustment_Confirmed_CallsServiceWithBankAndId()
    {
        var (viewModel, _, _, _, adjustments, _) = CreateViewModel();
        var adjustment = new BalanceAdjustmentDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), BankId = BarclaysId, BankName = "Barclays", TargetBalance = 100m, Delta = 5m };
        var row = BankOperationRow.FromAdjustment(adjustment);

        await viewModel.DeleteBankOperationAsync(row);

        adjustments.LastDeleted.Should().Be((adjustment.BankId, adjustment.Id));
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
