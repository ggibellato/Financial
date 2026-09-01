using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.CashFlow;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class MonthlyViewModelTests
{
    private static readonly Guid BarclaysId = Guid.NewGuid();
    private static readonly Guid ChaseId = Guid.NewGuid();

    private static readonly Guid GleisonSourceId = Guid.NewGuid();
    private static readonly Guid ArianaSourceId = Guid.NewGuid();
    private static readonly Guid LotterySourceId = Guid.NewGuid();
    private static readonly Guid DividendoJurosSourceId = Guid.NewGuid();

    private static readonly List<IncomeSourceDTO> DefaultIncomeSources =
    [
        new() { Id = GleisonSourceId, Name = "Gleison", IsActive = true, Group = "Salary", AutoSplitToReserve = false, HasReferences = false },
        new() { Id = ArianaSourceId, Name = "Ariana", IsActive = true, Group = "Salary", AutoSplitToReserve = true, HasReferences = false },
        new() { Id = LotterySourceId, Name = "Lottery", IsActive = true, Group = "NonReportable", AutoSplitToReserve = false, HasReferences = false },
        new() { Id = DividendoJurosSourceId, Name = "DividendoJuros", IsActive = true, Group = "DividendoJuros", AutoSplitToReserve = false, HasReferences = false },
    ];

    private static readonly Guid BaAmexId = Guid.NewGuid();

    /// <summary>The 5 cards seeded in a real deployment (F01), pre-loaded so the ComboBox-driven
    /// expense form has something to select from in tests.</summary>
    private static readonly List<CreditCardDTO> DefaultCreditCards =
    [
        new() { Id = Guid.NewGuid(), Name = "BarclaysPlatinumVisa8003", IsActive = true, HasReferences = false },
        new() { Id = Guid.NewGuid(), Name = "BarclaysPlatinumVisa6007", IsActive = true, HasReferences = false },
        new() { Id = Guid.NewGuid(), Name = "ChaseMaster4023", IsActive = true, HasReferences = false },
        new() { Id = BaAmexId, Name = "BaAmex", IsActive = true, HasReferences = false },
        new() { Id = Guid.NewGuid(), Name = "PaypalCredit", IsActive = true, HasReferences = false },
    ];

    /// <summary>The categories seeded in a real deployment (F01), pre-loaded so the expense
    /// form's live category picklist (F05) has something to select from in tests.</summary>
    private static readonly List<CategoryDTO> DefaultCategories =
    [
        new() { Id = Guid.NewGuid(), Name = "Mercado", Active = true, IsInvestment = false, IsTithe = false, HasReferences = false },
        new() { Id = Guid.NewGuid(), Name = "Extras", Active = true, IsInvestment = false, IsTithe = false, HasReferences = false },
        new() { Id = Guid.NewGuid(), Name = "Viagem", Active = true, IsInvestment = false, IsTithe = false, HasReferences = false },
        new() { Id = Guid.NewGuid(), Name = "Dizimo", Active = true, IsInvestment = false, IsTithe = true, HasReferences = false },
    ];

    private static (MonthlyViewModel ViewModel, StubExpenseService Expenses, StubIncomeService Incomes, StubBankService Banks, StubTitheService Tithe, StubCreditCardService CreditCards) CreateViewModel(
        bool confirmDeletes = true, StubIncomeSourceService? incomeSourceService = null, RecordingTelemetryTracer? tracer = null)
    {
        var expenses = new StubExpenseService();
        var incomes = new StubIncomeService();
        var banks = new StubBankService { Banks = [new BankDTO { Id = BarclaysId, Name = "Barclays", RoundUpEnabled = true, OpeningBalance = 0, OpeningBalanceDate = DateOnly.FromDateTime(DateTime.Today), HasReferences = false }, new BankDTO { Id = ChaseId, Name = "Chase", RoundUpEnabled = false, OpeningBalance = 0, OpeningBalanceDate = DateOnly.FromDateTime(DateTime.Today), HasReferences = false }] };
        var incomeSources = incomeSourceService ?? new StubIncomeSourceService { IncomeSources = DefaultIncomeSources };
        var tithe = new StubTitheService { Summary = new TitheSummaryDTO { CalculatedTithe = 100m, TitheBalance = 50m } };
        var transfers = new StubTransferService();
        var adjustments = new StubBalanceAdjustmentService();
        var cardStatements = new StubCardStatementService();
        var creditCards = new StubCreditCardService { CreditCards = new List<CreditCardDTO>(DefaultCreditCards) };
        var categories = new StubCategoryService { Categories = new List<CategoryDTO>(DefaultCategories) };

        var viewModel = new MonthlyViewModel(expenses, incomes, banks, incomeSources, tithe, transfers, adjustments, cardStatements, creditCards, categories, confirm: _ => confirmDeletes, tracer ?? new RecordingTelemetryTracer());
        return (viewModel, expenses, incomes, banks, tithe, creditCards);
    }

    [Fact]
    public async Task LoadsExpensesIncomesCategoryTotalsAndTitheForCurrentMonth()
    {
        var (viewModel, expenses, incomes, _, tithe, _) = CreateViewModel();
        expenses.Expenses = [new ExpenseDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), Description = "Test", Value = 10m, CategoryId = Guid.NewGuid(), CategoryName = "Mercado", PaymentSourceBankId = BarclaysId, PaymentSourceBankName = "Barclays", PaymentStatus = "ImmediatePayment" }];
        expenses.CategoryTotals = [new CategoryTotalDTO { Category = "Mercado", TotalValue = 10m }];
        incomes.Incomes = [new IncomeDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), IncomeSourceId = Guid.NewGuid(), IncomeSourceName = "Gleison", NetValue = 100m, BankId = BarclaysId, BankName = "Barclays", SplitToReserve = false }];

        await viewModel.RefreshAsync();

        viewModel.Expense.Expenses.Should().ContainSingle();
        viewModel.Income.Incomes.Should().ContainSingle();
        viewModel.CategoryTotals.Should().ContainSingle();
        viewModel.TitheSummary.Should().Be(tithe.Summary);
        viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task HasTitheCarryForward_ReflectsWhetherTheSummaryHasACarryForward()
    {
        var (viewModel, _, _, _, tithe, _) = CreateViewModel();
        tithe.Summary = new TitheSummaryDTO
        {
            CalculatedTithe = 100m,
            TitheBalance = 150m,
            CarryForward = new TitheCarryForwardDTO { Amount = 50m, Included = true, FromYear = 2026, FromMonth = 8 },
        };

        await viewModel.RefreshAsync();

        viewModel.HasTitheCarryForward.Should().BeTrue();
    }

    [Fact]
    public async Task HasTitheCarryForward_FalseWhenNothingToCarry()
    {
        var (viewModel, _, _, _, _, _) = CreateViewModel();

        await viewModel.RefreshAsync();

        viewModel.HasTitheCarryForward.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateTitheCarryForwardAsync_TogglesInclusionAndRefreshes()
    {
        var (viewModel, _, _, _, tithe, _) = CreateViewModel();
        tithe.Summary = new TitheSummaryDTO
        {
            CalculatedTithe = 100m,
            TitheBalance = 150m,
            CarryForward = new TitheCarryForwardDTO { Amount = 50m, Included = true, FromYear = 2026, FromMonth = 8 },
        };
        await viewModel.RefreshAsync();

        await viewModel.UpdateTitheCarryForwardAsync(false);

        tithe.LastUpdateRequest.Should().Be((viewModel.Year, viewModel.Month, false));
        viewModel.TitheCarryForwardUpdateError.Should().BeNull();
        viewModel.IsUpdatingTitheCarryForward.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateTitheCarryForwardAsync_NoCarryForwardAvailable_DoesNothing()
    {
        var (viewModel, _, _, _, tithe, _) = CreateViewModel();
        await viewModel.RefreshAsync();

        await viewModel.UpdateTitheCarryForwardAsync(false);

        tithe.LastUpdateRequest.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTitheCarryForwardAsync_SameValueAsCurrent_DoesNothing()
    {
        var (viewModel, _, _, _, tithe, _) = CreateViewModel();
        tithe.Summary = new TitheSummaryDTO
        {
            CalculatedTithe = 100m,
            TitheBalance = 150m,
            CarryForward = new TitheCarryForwardDTO { Amount = 50m, Included = true, FromYear = 2026, FromMonth = 8 },
        };
        await viewModel.RefreshAsync();

        await viewModel.UpdateTitheCarryForwardAsync(true);

        tithe.LastUpdateRequest.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTitheCarryForwardAsync_WhenServiceThrows_SetsErrorAndClearsBusyFlag()
    {
        var (viewModel, _, _, _, tithe, _) = CreateViewModel();
        tithe.Summary = new TitheSummaryDTO
        {
            CalculatedTithe = 100m,
            TitheBalance = 150m,
            CarryForward = new TitheCarryForwardDTO { Amount = 50m, Included = true, FromYear = 2026, FromMonth = 8 },
        };
        await viewModel.RefreshAsync();
        tithe.ThrowOnUpdate = new InvalidOperationException("No carry-forward is available for this month.");

        await viewModel.UpdateTitheCarryForwardAsync(false);

        viewModel.TitheCarryForwardUpdateError.Should().Be("No carry-forward is available for this month.");
        viewModel.IsUpdatingTitheCarryForward.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshAsync_GroupsIncomesBySourceAndSumsGrossOnlyWhenPresent()
    {
        var (viewModel, _, incomes, _, _, _) = CreateViewModel();
        incomes.Incomes =
        [
            new IncomeDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), IncomeSourceId = Guid.NewGuid(), IncomeSourceName = "Gleison", GrossValue = 120m, NetValue = 100m, BankId = BarclaysId, BankName = "Barclays", SplitToReserve = false },
            new IncomeDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), IncomeSourceId = Guid.NewGuid(), IncomeSourceName = "Gleison", GrossValue = 60m, NetValue = 50m, BankId = BarclaysId, BankName = "Barclays", SplitToReserve = false },
            new IncomeDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), IncomeSourceId = Guid.NewGuid(), IncomeSourceName = "Ariana", NetValue = 30m, BankId = ChaseId, BankName = "Chase", SplitToReserve = false },
        ];

        await viewModel.RefreshAsync();

        viewModel.IncomeTotals.Should().HaveCount(2);
        var gleison = viewModel.IncomeTotals.Single(i => i.Source == "Gleison");
        gleison.GrossValue.Should().Be(180m);
        gleison.NetValue.Should().Be(150m);
        var ariana = viewModel.IncomeTotals.Single(i => i.Source == "Ariana");
        ariana.GrossValue.Should().BeNull();
        ariana.NetValue.Should().Be(30m);
        viewModel.TotalIncoming.Should().Be(180m);
    }

    [Fact]
    public async Task ChangingYearOrMonth_RefetchesAllFour()
    {
        var (viewModel, expenses, _, _, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        var callsBefore = expenses.GetExpensesByMonthCallCount;

        viewModel.Year = viewModel.Year - 1;
        await viewModel.RefreshAsync();

        expenses.GetExpensesByMonthCallCount.Should().BeGreaterThan(callsBefore);
    }

    [Fact]
    public async Task RefreshAsync_PopulatesUnpaidCardCharges()
    {
        var (viewModel, expenses, _, _, _, _) = CreateViewModel();
        expenses.UnpaidCardCharges = [new ExpenseDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), Description = "Uber", Value = 18.4m, CategoryId = Guid.NewGuid(), CategoryName = "Extras", CreditCardId = Guid.NewGuid(), CreditCardName = "BaAmex", PaymentStatus = "CreditCardCharge" }];

        await viewModel.RefreshAsync();

        viewModel.Expense.UnpaidCardCharges.Should().ContainSingle().Which.Description.Should().Be("Uber");
    }

    [Fact]
    public async Task ChangingYearOrMonth_RefetchesUnpaidCardCharges()
    {
        var (viewModel, expenses, _, _, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        var callsBefore = expenses.GetUnpaidCardChargesByMonthCallCount;

        viewModel.Year = viewModel.Year - 1;
        await viewModel.RefreshAsync();

        expenses.GetUnpaidCardChargesByMonthCallCount.Should().BeGreaterThan(callsBefore);
    }

    [Fact]
    public async Task DeleteExpenseCommand_FromUnpaidCardCharges_ConfirmedCallsDeleteAndRefreshes()
    {
        var (viewModel, expenses, _, _, _, _) = CreateViewModel();
        var unpaidCharge = new ExpenseDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), Description = "Uber", Value = 18.4m, CategoryId = Guid.NewGuid(), CategoryName = "Extras", CreditCardId = Guid.NewGuid(), CreditCardName = "BaAmex", PaymentStatus = "CreditCardCharge" };
        await viewModel.RefreshAsync();
        var callsBefore = expenses.GetUnpaidCardChargesByMonthCallCount;

        await viewModel.Expense.DeleteExpenseAsync(unpaidCharge);

        expenses.LastDeletedId.Should().Be(unpaidCharge.Id);
        expenses.GetUnpaidCardChargesByMonthCallCount.Should().BeGreaterThan(callsBefore);
    }

    [Fact]
    public async Task RefreshAsync_IncomeSourceFetchFails_LeavesOptionsEmptyAndSetsError()
    {
        var incomeSourceService = new StubIncomeSourceService { ThrowOnGet = new InvalidOperationException("Unavailable") };
        var (viewModel, _, _, _, _, _) = CreateViewModel(incomeSourceService: incomeSourceService);

        await viewModel.RefreshAsync();

        viewModel.HasError.Should().BeTrue();
        viewModel.Income.IncomeSourceOptions.Should().BeEmpty();
        IncomeFormValidation.BuildValidationMessage(DateTime.Today, incomeSource: null, netValue: "10")
            .Should().Contain("Source is required.");
    }

}
