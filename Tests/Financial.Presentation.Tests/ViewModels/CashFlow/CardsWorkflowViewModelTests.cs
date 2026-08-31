using System.Collections.ObjectModel;
using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels;
using Financial.Presentation.App.ViewModels.CashFlow;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

/// <summary>Covers CardsWorkflowViewModel.CreditCardManagementRows, the merged Credit Card tab grid
/// (statement + due-date/active management in one row per card) - mirrors Financial.Web's
/// CardsGrid merge tests.</summary>
public class CardsWorkflowViewModelTests
{
    private static readonly Guid BarclaysId = Guid.NewGuid();
    private static readonly Guid BaAmexId = Guid.NewGuid();
    private static readonly Guid ChaseId = Guid.NewGuid();
    private static readonly Guid PaypalId = Guid.NewGuid();

    /// <summary>Unchecks every filter option except the given values, mirroring how a user would
    /// narrow the header checklist down to a single card.</summary>
    private static void SelectOnly(ColumnFilterViewModel<CreditCardManagementRow> filter, params string[] values)
    {
        foreach (var option in filter.Options)
        {
            option.IsChecked = values.Contains(option.Value);
        }
    }

    private static (CardsWorkflowViewModel ViewModel, StubCardStatementService CardStatementService, StubCreditCardService CreditCardService, ObservableCollection<BankDTO> Banks, ObservableCollection<CreditCardDTO> CreditCards) CreateViewModel(
        Func<Task>? refresh = null)
    {
        var cardStatementService = new StubCardStatementService();
        var creditCardService = new StubCreditCardService();
        var banks = new ObservableCollection<BankDTO> { new() { Id = BarclaysId, Name = "Barclays", RoundUpEnabled = true, OpeningBalance = 0, OpeningBalanceDate = DateOnly.FromDateTime(DateTime.Today), HasReferences = false } };
        var creditCards = new ObservableCollection<CreditCardDTO>();
        var viewModel = new CardsWorkflowViewModel(cardStatementService, creditCardService, banks, creditCards, refresh ?? (() => Task.CompletedTask));
        return (viewModel, cardStatementService, creditCardService, banks, creditCards);
    }

    [Fact]
    public void CreditCardManagementRows_HasOneRowPerCreditCard_IncludingOneWithNoStatementThisMonth()
    {
        var (viewModel, _, _, _, creditCards) = CreateViewModel();
        creditCards.Add(new() { Id = BaAmexId, Name = "BaAmex", IsActive = true, NextInvoiceDueDate = new DateOnly(2026, 9, 5), HasReferences = false });
        creditCards.Add(new() { Id = ChaseId, Name = "ChaseMaster4023", IsActive = true, NextInvoiceDueDate = null, HasReferences = false });
        creditCards.Add(new() { Id = PaypalId, Name = "PaypalCredit", IsActive = false, NextInvoiceDueDate = null, HasReferences = false });
        viewModel.ApplyRefresh([new CardStatementDTO { Id = Guid.NewGuid(), CreditCardId = BaAmexId, CreditCardName = "BaAmex", Year = DateTime.Today.Year, Month = DateTime.Today.Month, IsPaid = false, OutstandingTotal = 100m }]);

        viewModel.CreditCardManagementRows.Should().HaveCount(3);
        viewModel.CreditCardManagementRows.Select(r => r.CreditCardName).Should().Contain(["BaAmex", "ChaseMaster4023", "PaypalCredit"]);
    }

    [Fact]
    public void CreditCardManagementRows_CardWithNoStatement_HasStatementFalseAndZeroOutstanding()
    {
        var (viewModel, _, _, _, creditCards) = CreateViewModel();
        creditCards.Add(new() { Id = PaypalId, Name = "PaypalCredit", IsActive = false, NextInvoiceDueDate = null, HasReferences = false });
        viewModel.ApplyRefresh([]);

        var paypalRow = viewModel.CreditCardManagementRows.Single(r => r.CreditCardName == "PaypalCredit");
        paypalRow.HasStatement.Should().BeFalse();
        paypalRow.OutstandingTotal.Should().Be(0m);
        paypalRow.IsPaid.Should().BeFalse();
    }

    [Fact]
    public void CreditCardManagementRows_CardWithStatement_MergesStatementDataOntoTheCard()
    {
        var (viewModel, _, _, _, creditCards) = CreateViewModel();
        creditCards.Add(new() { Id = ChaseId, Name = "ChaseMaster4023", IsActive = true, NextInvoiceDueDate = null, HasReferences = false });
        viewModel.ApplyRefresh([new CardStatementDTO { Id = Guid.NewGuid(), CreditCardId = ChaseId, CreditCardName = "ChaseMaster4023", Year = DateTime.Today.Year, Month = DateTime.Today.Month, IsPaid = true, OutstandingTotal = 42.5m }]);

        var chaseRow = viewModel.CreditCardManagementRows.Single(r => r.CreditCardName == "ChaseMaster4023");
        chaseRow.HasStatement.Should().BeTrue();
        chaseRow.OutstandingTotal.Should().Be(42.5m);
        chaseRow.IsPaid.Should().BeTrue();
        chaseRow.CreditCard.Id.Should().Be(ChaseId);
    }

    [Fact]
    public void CreditCardManagementRows_StillExposesTheUnderlyingCreditCard_ForDueDateAndActiveManagement()
    {
        var (viewModel, _, _, _, creditCards) = CreateViewModel();
        creditCards.Add(new() { Id = BaAmexId, Name = "BaAmex", IsActive = true, NextInvoiceDueDate = new DateOnly(2026, 9, 5), HasReferences = false });
        creditCards.Add(new() { Id = PaypalId, Name = "PaypalCredit", IsActive = false, NextInvoiceDueDate = null, HasReferences = false });

        var baAmexRow = viewModel.CreditCardManagementRows.Single(r => r.CreditCardName == "BaAmex");
        baAmexRow.CreditCard.NextInvoiceDueDate.Should().Be(new DateOnly(2026, 9, 5));
        baAmexRow.CreditCard.IsActive.Should().BeTrue();

        var paypalRow = viewModel.CreditCardManagementRows.Single(r => r.CreditCardName == "PaypalCredit");
        paypalRow.CreditCard.IsActive.Should().BeFalse();
    }

    [Fact]
    public void CardFilter_AvailableValuesComeFromFullUnfilteredData()
    {
        var (viewModel, _, _, _, creditCards) = CreateViewModel();
        creditCards.Add(new() { Id = BaAmexId, Name = "BaAmex", IsActive = true, NextInvoiceDueDate = null, HasReferences = false });
        creditCards.Add(new() { Id = ChaseId, Name = "ChaseMaster4023", IsActive = true, NextInvoiceDueDate = null, HasReferences = false });

        viewModel.NotifyCreditCardsChanged();

        viewModel.CardFilter.Options.Select(o => o.Value).Should().BeEquivalentTo(["BaAmex", "ChaseMaster4023"]);
    }

    [Fact]
    public void CardFilter_UncheckingCard_ExcludesItFromFilteredCreditCardManagementRows()
    {
        var (viewModel, _, _, _, creditCards) = CreateViewModel();
        creditCards.Add(new() { Id = BaAmexId, Name = "BaAmex", IsActive = true, NextInvoiceDueDate = null, HasReferences = false });
        creditCards.Add(new() { Id = ChaseId, Name = "ChaseMaster4023", IsActive = true, NextInvoiceDueDate = null, HasReferences = false });
        viewModel.NotifyCreditCardsChanged();

        SelectOnly(viewModel.CardFilter, "BaAmex");

        viewModel.FilteredCreditCardManagementRows.Should().ContainSingle(r => r.CreditCardName == "BaAmex");
        viewModel.FilteredCreditCardManagementRows.Should().NotContain(r => r.CreditCardName == "ChaseMaster4023");
    }

    [Fact]
    public void ApplyRefresh_UpdatesAdjustmentTotal()
    {
        var (viewModel, _, _, _, _) = CreateViewModel();

        viewModel.ApplyRefresh([
            new CardStatementDTO { Id = Guid.NewGuid(), CreditCardId = Guid.NewGuid(), CreditCardName = "BaAmex", Year = DateTime.Today.Year, Month = DateTime.Today.Month, IsPaid = false, OutstandingTotal = 100m },
            new CardStatementDTO { Id = Guid.NewGuid(), CreditCardId = Guid.NewGuid(), CreditCardName = "ChaseMaster4023", Year = DateTime.Today.Year, Month = DateTime.Today.Month, IsPaid = false, OutstandingTotal = 50m },
        ]);

        viewModel.AdjustmentTotal.Should().Be(150m);
    }

    [Fact]
    public async Task MarkCardStatementPaid_RequiresBankSelected_ThenCallsService()
    {
        var (viewModel, cardStatementService, _, _, _) = CreateViewModel();
        var statement = new CardStatementDTO { Id = Guid.NewGuid(), CreditCardId = Guid.NewGuid(), CreditCardName = "BaAmex", Year = DateTime.Today.Year, Month = DateTime.Today.Month, IsPaid = false, OutstandingTotal = 100m };
        cardStatementService.Statements = [statement];
        viewModel.ApplyRefresh([statement]);

        viewModel.MarkStatementPaidCommand.CanExecute(statement).Should().BeFalse();

        viewModel.SetMarkPaidSource(statement.Id, BarclaysId);

        viewModel.MarkStatementPaidCommand.CanExecute(statement).Should().BeTrue();
        await viewModel.MarkStatementPaidAsync(statement);

        cardStatementService.LastMarkPaidRequest.Should().NotBeNull();
        cardStatementService.LastMarkPaidRequest!.Value.Id.Should().Be(statement.Id);
        cardStatementService.LastMarkPaidRequest.Value.Request.PaymentSourceBankId.Should().Be(BarclaysId);
    }

    /// <summary>
    /// Web shows the same string through listActionWarning. Without it the WPF user gets the
    /// silence the backlog item is about: the click reports nothing and nothing changed.
    /// </summary>
    [Fact]
    public async Task MarkCardStatementPaid_WhenTheServerWarnsNothingChanged_SurfacesItSeparatelyFromErrors()
    {
        var (viewModel, cardStatementService, _, _, _) = CreateViewModel();
        var statement = new CardStatementDTO { Id = Guid.NewGuid(), CreditCardId = Guid.NewGuid(), CreditCardName = "BaAmex", Year = DateTime.Today.Year, Month = DateTime.Today.Month, IsPaid = true, OutstandingTotal = 0m };
        cardStatementService.Statements = [statement];
        cardStatementService.NextWarning = "This statement was already marked paid; nothing changed.";
        viewModel.SetMarkPaidSource(statement.Id, BarclaysId);

        await viewModel.MarkStatementPaidAsync(statement);

        viewModel.CardStatementWarning.Should().Contain("already marked paid");
        viewModel.CardStatementError.Should().BeNull("a no-op is not a failure and must not show in red");
    }

    [Fact]
    public async Task UnmarkCardStatementPaid_WhenTheServerWarnsNothingChanged_SurfacesTheWarning()
    {
        var (viewModel, cardStatementService, _, _, _) = CreateViewModel();
        var statement = new CardStatementDTO { Id = Guid.NewGuid(), CreditCardId = Guid.NewGuid(), CreditCardName = "BaAmex", Year = DateTime.Today.Year, Month = DateTime.Today.Month, IsPaid = false, OutstandingTotal = 0m };
        cardStatementService.Statements = [statement];
        cardStatementService.NextWarning = "This statement was not marked paid; nothing changed.";

        await viewModel.UnmarkStatementPaidAsync(statement);

        viewModel.CardStatementWarning.Should().Contain("not marked paid");
    }

    [Fact]
    public async Task MarkCardStatementPaid_WhenTheServerReportsNoWarning_LeavesTheWarningClear()
    {
        var (viewModel, cardStatementService, _, _, _) = CreateViewModel();
        var statement = new CardStatementDTO { Id = Guid.NewGuid(), CreditCardId = Guid.NewGuid(), CreditCardName = "BaAmex", Year = DateTime.Today.Year, Month = DateTime.Today.Month, IsPaid = false, OutstandingTotal = 100m };
        cardStatementService.Statements = [statement];
        viewModel.SetMarkPaidSource(statement.Id, BarclaysId);

        await viewModel.MarkStatementPaidAsync(statement);

        viewModel.CardStatementWarning.Should().BeNull();
    }

    [Fact]
    public async Task UnmarkCardStatementPaid_CallsService()
    {
        var (viewModel, cardStatementService, _, _, _) = CreateViewModel();
        var statement = new CardStatementDTO { Id = Guid.NewGuid(), CreditCardId = Guid.NewGuid(), CreditCardName = "BaAmex", Year = DateTime.Today.Year, Month = DateTime.Today.Month, IsPaid = true, OutstandingTotal = 0m };
        cardStatementService.Statements = [statement];

        await viewModel.UnmarkStatementPaidAsync(statement);

        cardStatementService.LastUnmarkedId.Should().Be(statement.Id);
    }

    [Fact]
    public async Task UpdateCreditCardAsync_SendsIdAndNewFields_ThenRefreshes()
    {
        var (viewModel, _, creditCardService, _, creditCards) = CreateViewModel();
        var card = new CreditCardDTO { Id = BaAmexId, Name = "BaAmex", IsActive = true, NextInvoiceDueDate = new DateOnly(2026, 9, 5), HasReferences = false };
        creditCards.Add(card);
        creditCardService.CreditCards = [card];
        var newDueDate = new DateOnly(2026, 10, 1);

        await viewModel.UpdateCreditCardAsync(card, newDueDate, isActive: false);

        creditCardService.LastUpdateRequest.Should().NotBeNull();
        creditCardService.LastUpdateRequest!.Value.Id.Should().Be(BaAmexId);
        creditCardService.LastUpdateRequest.Value.Request.NextInvoiceDueDate.Should().Be(newDueDate);
        creditCardService.LastUpdateRequest.Value.Request.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateCreditCardAsync_ServiceThrows_SetsCreditCardUpdateError()
    {
        var (viewModel, _, creditCardService, _, creditCards) = CreateViewModel();
        var card = new CreditCardDTO { Id = BaAmexId, Name = "BaAmex", IsActive = true, NextInvoiceDueDate = new DateOnly(2026, 9, 5), HasReferences = false };
        creditCards.Add(card);
        creditCardService.CreditCards = [card];
        creditCardService.ThrowOnUpdate = "Credit card was not found.";

        await viewModel.UpdateCreditCardAsync(card, card.NextInvoiceDueDate, isActive: false);

        viewModel.CreditCardUpdateError.Should().Be("Credit card was not found.");
        viewModel.UpdatingCreditCardId.Should().BeNull();
    }

    [Fact]
    public async Task UpdateCreditCardAsync_ValuesUnchanged_DoesNotCallServiceOrRefresh()
    {
        // Regression test: the grid's DatePicker/CheckBox bind one-way and call this method on
        // their change events, but WPF also raises those same events when the coordinator's
        // refresh rebinds a row to its own current value - not just on a real user edit. Without
        // a guard, that echo would call the update service, which refreshes, which rebinds the
        // row again, forever (the reported "Credit Card tab keeps reloading" bug). Calling with
        // the card's own current values must be a no-op.
        var (viewModel, _, creditCardService, _, creditCards) = CreateViewModel();
        var card = new CreditCardDTO { Id = BaAmexId, Name = "BaAmex", IsActive = true, NextInvoiceDueDate = new DateOnly(2026, 9, 5), HasReferences = false };
        creditCards.Add(card);
        creditCardService.CreditCards = [card];

        await viewModel.UpdateCreditCardAsync(card, card.NextInvoiceDueDate, card.IsActive);

        creditCardService.LastUpdateRequest.Should().BeNull();
    }
}
