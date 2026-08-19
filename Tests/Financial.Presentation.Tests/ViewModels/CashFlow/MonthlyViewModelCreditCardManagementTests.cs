using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.CashFlow;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

/// <summary>Covers MonthlyViewModel.CreditCardManagementRows, the merged Credit Card tab grid
/// (statement + due-date/active management in one row per card) - mirrors Financial.Web's
/// CardsGrid merge tests.</summary>
public class MonthlyViewModelCreditCardManagementTests
{
    private static readonly Guid BaAmexId = Guid.NewGuid();
    private static readonly Guid ChaseId = Guid.NewGuid();
    private static readonly Guid PaypalId = Guid.NewGuid();

    private static (MonthlyViewModel ViewModel, StubCardStatementService Cards) CreateViewModel()
    {
        var expenses = new StubExpenseService();
        var incomes = new StubIncomeService();
        var banks = new StubBankService();
        var incomeSources = new StubIncomeSourceService();
        var tithe = new StubTitheService();
        var transfers = new StubTransferService();
        var adjustments = new StubBalanceAdjustmentService();
        var cards = new StubCardStatementService();
        var creditCards = new StubCreditCardService
        {
            CreditCards =
            [
                new() { Id = BaAmexId, Name = "BaAmex", IsActive = true, NextInvoiceDueDate = new DateOnly(2026, 9, 5) },
                new() { Id = ChaseId, Name = "ChaseMaster4023", IsActive = true, NextInvoiceDueDate = null },
                new() { Id = PaypalId, Name = "PaypalCredit", IsActive = false, NextInvoiceDueDate = null },
            ],
        };
        var categories = new StubCategoryService();

        var viewModel = new MonthlyViewModel(expenses, incomes, banks, incomeSources, tithe, transfers, adjustments, cards, creditCards, categories, confirm: _ => true, new RecordingTelemetryTracer());
        return (viewModel, cards);
    }

    [Fact]
    public async Task CreditCardManagementRows_HasOneRowPerCreditCard_IncludingOneWithNoStatementThisMonth()
    {
        var (viewModel, cards) = CreateViewModel();
        cards.Statements =
        [
            new CardStatementDTO { Id = Guid.NewGuid(), CreditCardId = BaAmexId, CreditCardName = "BaAmex", Year = DateTime.Today.Year, Month = DateTime.Today.Month, IsPaid = false, OutstandingTotal = 100m },
        ];

        await viewModel.RefreshAsync();

        viewModel.CreditCardManagementRows.Should().HaveCount(3);
        viewModel.CreditCardManagementRows.Select(r => r.CreditCardName).Should().Contain(["BaAmex", "ChaseMaster4023", "PaypalCredit"]);
    }

    [Fact]
    public async Task CreditCardManagementRows_CardWithNoStatement_HasStatementFalseAndZeroOutstanding()
    {
        var (viewModel, cards) = CreateViewModel();
        cards.Statements = [];

        await viewModel.RefreshAsync();

        var paypalRow = viewModel.CreditCardManagementRows.Single(r => r.CreditCardName == "PaypalCredit");
        paypalRow.HasStatement.Should().BeFalse();
        paypalRow.OutstandingTotal.Should().Be(0m);
        paypalRow.IsPaid.Should().BeFalse();
    }

    [Fact]
    public async Task CreditCardManagementRows_CardWithStatement_MergesStatementDataOntoTheCard()
    {
        var (viewModel, cards) = CreateViewModel();
        cards.Statements =
        [
            new CardStatementDTO { Id = Guid.NewGuid(), CreditCardId = ChaseId, CreditCardName = "ChaseMaster4023", Year = DateTime.Today.Year, Month = DateTime.Today.Month, IsPaid = true, OutstandingTotal = 42.5m },
        ];

        await viewModel.RefreshAsync();

        var chaseRow = viewModel.CreditCardManagementRows.Single(r => r.CreditCardName == "ChaseMaster4023");
        chaseRow.HasStatement.Should().BeTrue();
        chaseRow.OutstandingTotal.Should().Be(42.5m);
        chaseRow.IsPaid.Should().BeTrue();
        chaseRow.CreditCard.Id.Should().Be(ChaseId);
    }

    [Fact]
    public async Task CreditCardManagementRows_StillExposesTheUnderlyingCreditCard_ForDueDateAndActiveManagement()
    {
        var (viewModel, _) = CreateViewModel();

        await viewModel.RefreshAsync();

        var baAmexRow = viewModel.CreditCardManagementRows.Single(r => r.CreditCardName == "BaAmex");
        baAmexRow.CreditCard.NextInvoiceDueDate.Should().Be(new DateOnly(2026, 9, 5));
        baAmexRow.CreditCard.IsActive.Should().BeTrue();

        var paypalRow = viewModel.CreditCardManagementRows.Single(r => r.CreditCardName == "PaypalCredit");
        paypalRow.CreditCard.IsActive.Should().BeFalse();
    }
}
