using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class MonthlyViewModelBanksCardsTests
{
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
                new BankDTO { Name = "Barclays", RoundUpEnabled = true, OpeningBalance = 0, OpeningBalanceDate = DateOnly.FromDateTime(DateTime.Today) },
                new BankDTO { Name = "Chase", RoundUpEnabled = false, OpeningBalance = 0, OpeningBalanceDate = DateOnly.FromDateTime(DateTime.Today) },
            ],
        };
        var tithe = new StubTitheService();
        var transfers = new StubTransferService();
        var adjustments = new StubBalanceAdjustmentService();
        var cards = new StubCardStatementService();

        var viewModel = new MonthlyViewModel(expenses, incomes, banks, tithe, transfers, adjustments, cards, confirm: _ => confirmDeletes);
        return (viewModel, expenses, banks, transfers, adjustments, cards);
    }

    [Fact]
    public async Task BankTotals_ComputesBalanceAndRoundUpTotalPerBank()
    {
        var (viewModel, expenses, banks, _, _, _) = CreateViewModel();
        banks.BankBalances = [new BankBalanceDTO { Bank = "Barclays", Balance = 250m }, new BankBalanceDTO { Bank = "Chase", Balance = 10m }];
        expenses.Expenses =
        [
            new ExpenseDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), Description = "A", Value = 20m, Category = "Mercado", PaymentSource = "Barclays", PaymentStatus = "ImmediatePayment", RoundUpAmount = 0.30m },
            new ExpenseDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), Description = "B", Value = 15m, Category = "Mercado", PaymentSource = "Barclays", PaymentStatus = "ImmediatePayment", RoundUpAmount = 0.20m },
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
    public async Task ToggleBankExpand_ExpandsThenCollapses()
    {
        var (viewModel, _, _, _, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        var row = viewModel.BankTotals.Single(b => b.Bank == "Barclays");
        row.IsExpanded.Should().BeFalse();

        viewModel.ToggleBankExpandCommand.Execute(row);
        row.IsExpanded.Should().BeTrue();

        viewModel.ToggleBankExpandCommand.Execute(row);
        row.IsExpanded.Should().BeFalse();
    }

    [Fact]
    public async Task BankHistory_MergesTransfersAndAdjustmentsSortedByDateDescending()
    {
        var (viewModel, _, _, transfers, adjustments, _) = CreateViewModel();
        var today = DateOnly.FromDateTime(DateTime.Today);
        transfers.Transfers = [new TransferDTO { Id = Guid.NewGuid(), Date = today.AddDays(-1), SourceBank = "Barclays", DestinationBank = "Chase", Amount = 50m }];
        adjustments.AdjustmentsByBank["Barclays"] = [new BalanceAdjustmentDTO { Id = Guid.NewGuid(), Date = today, Bank = "Barclays", TargetBalance = 100m, Delta = 5m }];

        await viewModel.RefreshAsync();

        var history = viewModel.BankTotals.Single(b => b.Bank == "Barclays").History;
        history.Should().HaveCount(2);
        history[0].Kind.Should().Be(BankHistoryEntryKind.Adjustment);
        history[1].Kind.Should().Be(BankHistoryEntryKind.TransferOut);
    }

    [Fact]
    public async Task BankHistory_TransferAppearsInBothSourceAndDestinationBankHistory()
    {
        var (viewModel, _, _, transfers, _, _) = CreateViewModel();
        transfers.Transfers = [new TransferDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), SourceBank = "Barclays", DestinationBank = "Chase", Amount = 50m }];

        await viewModel.RefreshAsync();

        viewModel.BankTotals.Single(b => b.Bank == "Barclays").History.Should().ContainSingle(e => e.Kind == BankHistoryEntryKind.TransferOut);
        viewModel.BankTotals.Single(b => b.Bank == "Chase").History.Should().ContainSingle(e => e.Kind == BankHistoryEntryKind.TransferIn);
    }

    [Fact]
    public async Task AddTransfer_ValidForm_CallsServiceAndRefreshes()
    {
        var (viewModel, _, banks, transfers, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        viewModel.ShowMoveMoneyFormCommand.Execute(banks.Banks[0].Name);
        viewModel.TransferFormDate = DateTime.Today;
        viewModel.TransferFormDestinationBank = banks.Banks[1].Name;
        viewModel.TransferFormAmount = "75";

        await viewModel.SaveTransferAsync();

        transfers.LastCreateRequest.Should().NotBeNull();
        transfers.LastCreateRequest!.SourceBank.Should().Be("Barclays");
        transfers.LastCreateRequest.DestinationBank.Should().Be("Chase");
        transfers.LastCreateRequest.Amount.Should().Be(75m);
        viewModel.IsTransferFormOpen.Should().BeFalse();
    }

    [Fact]
    public async Task AddTransfer_SameSourceAndDestination_BlocksSaveWithoutServiceCall()
    {
        var (viewModel, _, banks, transfers, _, _) = CreateViewModel();
        viewModel.ShowMoveMoneyFormCommand.Execute(banks.Banks[0].Name);
        viewModel.TransferFormDate = DateTime.Today;
        viewModel.TransferFormDestinationBank = banks.Banks[0].Name;
        viewModel.TransferFormAmount = "75";

        viewModel.SaveTransferCommand.CanExecute(null).Should().BeFalse();
        viewModel.SameBankTransferError.Should().NotBeNullOrEmpty();

        await viewModel.SaveTransferAsync();

        transfers.LastCreateRequest.Should().BeNull();
        viewModel.TransferSaveError.Should().NotBeNullOrEmpty();

        viewModel.TransferFormDestinationBank = banks.Banks[1].Name;
        viewModel.SaveTransferCommand.CanExecute(null).Should().BeTrue();
        viewModel.SameBankTransferError.Should().BeEmpty();
    }

    [Fact]
    public async Task AddTransfer_BackendRejects_KeepsFormOpenWithValuesAndShowsServerError()
    {
        var (viewModel, _, banks, transfers, _, _) = CreateViewModel();
        transfers.ThrowOnAdd = "Insufficient funds in source bank.";
        await viewModel.RefreshAsync();
        viewModel.ShowMoveMoneyFormCommand.Execute(banks.Banks[0].Name);
        viewModel.TransferFormDate = DateTime.Today;
        viewModel.TransferFormDestinationBank = banks.Banks[1].Name;
        viewModel.TransferFormAmount = "75";

        await viewModel.SaveTransferAsync();

        viewModel.IsTransferFormOpen.Should().BeTrue();
        viewModel.TransferSaveError.Should().Be("Insufficient funds in source bank.");
        viewModel.TransferFormAmount.Should().Be("75");
        viewModel.TransferFormDestinationBank.Should().Be(banks.Banks[1].Name);
    }

    [Fact]
    public async Task EditTransfer_ValidForm_CallsUpdateServiceWithCorrectId()
    {
        var (viewModel, _, _, transfers, _, _) = CreateViewModel();
        var transfer = new TransferDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), SourceBank = "Barclays", DestinationBank = "Chase", Amount = 50m };

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
        var transfer = new TransferDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), SourceBank = "Barclays", DestinationBank = "Chase", Amount = 50m };
        var entry = BankHistoryEntry.FromTransferOut(transfer);

        await viewModel.DeleteHistoryEntryAsync(entry);

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
        var row = viewModel.BankTotals.Single(b => b.Bank == "Barclays");

        viewModel.ShowCorrectBalanceFormCommand.Execute(row);
        viewModel.AdjustmentFormCurrentBalance.Should().Be(42.5m);
        viewModel.AdjustmentFormDate = DateTime.Today;
        viewModel.AdjustmentFormTargetBalance = "50";

        await viewModel.SaveAdjustmentAsync();

        adjustments.LastCreateRequest.Should().NotBeNull();
        adjustments.LastCreateRequest!.Value.Bank.Should().Be("Barclays");
        adjustments.LastCreateRequest.Value.Request.TargetBalance.Should().Be(50m);
        viewModel.AdjustmentSavedDelta.Should().NotBeNull();
    }

    [Fact]
    public async Task EditAdjustment_ValidForm_CallsUpdateServiceWithCorrectBankAndId()
    {
        var (viewModel, _, _, _, adjustments, _) = CreateViewModel();
        var adjustment = new BalanceAdjustmentDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), Bank = "Barclays", TargetBalance = 100m, Delta = 5m };
        var entry = BankHistoryEntry.FromAdjustment(adjustment);

        viewModel.EditAdjustmentCommand.Execute(entry);
        viewModel.AdjustmentFormTargetBalance = "120";

        await viewModel.SaveAdjustmentAsync();

        adjustments.LastUpdateRequest.Should().NotBeNull();
        adjustments.LastUpdateRequest!.Value.Bank.Should().Be("Barclays");
        adjustments.LastUpdateRequest.Value.Id.Should().Be(adjustment.Id);
        adjustments.LastUpdateRequest.Value.Request.TargetBalance.Should().Be(120m);
    }

    [Fact]
    public async Task DeleteAdjustment_Confirmed_CallsServiceWithBankAndId()
    {
        var (viewModel, _, _, _, adjustments, _) = CreateViewModel();
        var adjustment = new BalanceAdjustmentDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), Bank = "Barclays", TargetBalance = 100m, Delta = 5m };
        var entry = BankHistoryEntry.FromAdjustment(adjustment);

        await viewModel.DeleteHistoryEntryAsync(entry);

        adjustments.LastDeleted.Should().Be(("Barclays", adjustment.Id));
    }

    [Fact]
    public async Task CardStatements_FooterShowsCombinedOutstandingTotal()
    {
        var (viewModel, _, _, _, _, cards) = CreateViewModel();
        cards.Statements =
        [
            new CardStatementDTO { Id = Guid.NewGuid(), Card = "BaAmex", Year = DateTime.Today.Year, Month = DateTime.Today.Month, IsPaid = false, OutstandingTotal = 100m },
            new CardStatementDTO { Id = Guid.NewGuid(), Card = "ChaseMaster4023", Year = DateTime.Today.Year, Month = DateTime.Today.Month, IsPaid = false, OutstandingTotal = 50m },
        ];

        await viewModel.RefreshAsync();

        viewModel.AdjustmentTotal.Should().Be(150m);
    }

    [Fact]
    public async Task MarkCardStatementPaid_RequiresBankSelected_ThenCallsService()
    {
        var (viewModel, _, banks, _, _, cards) = CreateViewModel();
        var statement = new CardStatementDTO { Id = Guid.NewGuid(), Card = "BaAmex", Year = DateTime.Today.Year, Month = DateTime.Today.Month, IsPaid = false, OutstandingTotal = 100m };
        cards.Statements = [statement];
        await viewModel.RefreshAsync();

        viewModel.MarkStatementPaidCommand.CanExecute(statement).Should().BeFalse();

        viewModel.SetMarkPaidSource(statement.Id, banks.Banks[0].Name);

        viewModel.MarkStatementPaidCommand.CanExecute(statement).Should().BeTrue();
        await viewModel.MarkStatementPaidAsync(statement);

        cards.LastMarkPaidRequest.Should().NotBeNull();
        cards.LastMarkPaidRequest!.Value.Id.Should().Be(statement.Id);
        cards.LastMarkPaidRequest.Value.Request.PaymentSource.Should().Be("Barclays");
    }

    [Fact]
    public async Task UnmarkCardStatementPaid_CallsService()
    {
        var (viewModel, _, _, _, _, cards) = CreateViewModel();
        var statement = new CardStatementDTO { Id = Guid.NewGuid(), Card = "BaAmex", Year = DateTime.Today.Year, Month = DateTime.Today.Month, IsPaid = true, OutstandingTotal = 0m };
        cards.Statements = [statement];

        await viewModel.UnmarkStatementPaidAsync(statement);

        cards.LastUnmarkedId.Should().Be(statement.Id);
    }
}
