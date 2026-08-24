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
        new() { Id = GleisonSourceId, Name = "Gleison", IsActive = true, Group = "Salary", AutoSplitToReserve = false },
        new() { Id = ArianaSourceId, Name = "Ariana", IsActive = true, Group = "Salary", AutoSplitToReserve = true },
        new() { Id = LotterySourceId, Name = "Lottery", IsActive = true, Group = "NonReportable", AutoSplitToReserve = false },
        new() { Id = DividendoJurosSourceId, Name = "DividendoJuros", IsActive = true, Group = "DividendoJuros", AutoSplitToReserve = false },
    ];

    private static readonly Guid BaAmexId = Guid.NewGuid();

    /// <summary>The 5 cards seeded in a real deployment (F01), pre-loaded so the ComboBox-driven
    /// expense form has something to select from in tests.</summary>
    private static readonly List<CreditCardDTO> DefaultCreditCards =
    [
        new() { Id = Guid.NewGuid(), Name = "BarclaysPlatinumVisa8003", IsActive = true },
        new() { Id = Guid.NewGuid(), Name = "BarclaysPlatinumVisa6007", IsActive = true },
        new() { Id = Guid.NewGuid(), Name = "ChaseMaster4023", IsActive = true },
        new() { Id = BaAmexId, Name = "BaAmex", IsActive = true },
        new() { Id = Guid.NewGuid(), Name = "PaypalCredit", IsActive = true },
    ];

    /// <summary>The categories seeded in a real deployment (F01), pre-loaded so the expense
    /// form's live category picklist (F05) has something to select from in tests.</summary>
    private static readonly List<CategoryDTO> DefaultCategories =
    [
        new() { Id = Guid.NewGuid(), Name = "Mercado", Active = true, IsInvestment = false, IsTithe = false },
        new() { Id = Guid.NewGuid(), Name = "Extras", Active = true, IsInvestment = false, IsTithe = false },
        new() { Id = Guid.NewGuid(), Name = "Viagem", Active = true, IsInvestment = false, IsTithe = false },
        new() { Id = Guid.NewGuid(), Name = "Dizimo", Active = true, IsInvestment = false, IsTithe = true },
    ];

    private static (MonthlyViewModel ViewModel, StubExpenseService Expenses, StubIncomeService Incomes, StubBankService Banks, StubTitheService Tithe, StubCreditCardService CreditCards) CreateViewModel(
        bool confirmDeletes = true, StubIncomeSourceService? incomeSourceService = null, RecordingTelemetryTracer? tracer = null)
    {
        var expenses = new StubExpenseService();
        var incomes = new StubIncomeService();
        var banks = new StubBankService { Banks = [new BankDTO { Id = BarclaysId, Name = "Barclays", RoundUpEnabled = true, OpeningBalance = 0, OpeningBalanceDate = DateOnly.FromDateTime(DateTime.Today) }, new BankDTO { Id = ChaseId, Name = "Chase", RoundUpEnabled = false, OpeningBalance = 0, OpeningBalanceDate = DateOnly.FromDateTime(DateTime.Today) }] };
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
        incomes.Incomes = [new IncomeDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), IncomeSourceId = Guid.NewGuid(), IncomeSourceName = "Gleison", NetValue = 100m, BankId = BarclaysId, BankName = "Barclays", SplitToReserve = false, ReserveSplitMovements = [] }];

        await viewModel.RefreshAsync();

        viewModel.Expenses.Should().ContainSingle();
        viewModel.Incomes.Should().ContainSingle();
        viewModel.CategoryTotals.Should().ContainSingle();
        viewModel.TitheSummary.Should().Be(tithe.Summary);
        viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshAsync_GroupsIncomesBySourceAndSumsGrossOnlyWhenPresent()
    {
        var (viewModel, _, incomes, _, _, _) = CreateViewModel();
        incomes.Incomes =
        [
            new IncomeDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), IncomeSourceId = Guid.NewGuid(), IncomeSourceName = "Gleison", GrossValue = 120m, NetValue = 100m, BankId = BarclaysId, BankName = "Barclays", SplitToReserve = false, ReserveSplitMovements = [] },
            new IncomeDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), IncomeSourceId = Guid.NewGuid(), IncomeSourceName = "Gleison", GrossValue = 60m, NetValue = 50m, BankId = BarclaysId, BankName = "Barclays", SplitToReserve = false, ReserveSplitMovements = [] },
            new IncomeDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), IncomeSourceId = Guid.NewGuid(), IncomeSourceName = "Ariana", NetValue = 30m, BankId = ChaseId, BankName = "Chase", SplitToReserve = false, ReserveSplitMovements = [] },
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

        viewModel.UnpaidCardCharges.Should().ContainSingle().Which.Description.Should().Be("Uber");
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
    public void EditExpenseCommand_FromUnpaidCardCharges_OpensFormPrefilled()
    {
        var (viewModel, _, _, _, _, _) = CreateViewModel();
        var unpaidCharge = new ExpenseDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), Description = "Uber", Value = 18.4m, CategoryId = Guid.NewGuid(), CategoryName = "Extras", CreditCardId = Guid.NewGuid(), CreditCardName = "BaAmex", PaymentStatus = "CreditCardCharge" };

        viewModel.EditExpenseCommand.Execute(unpaidCharge);

        viewModel.IsExpenseFormOpen.Should().BeTrue();
        viewModel.IsEditingExpense.Should().BeTrue();
        viewModel.ExpenseFormDescription.Should().Be("Uber");
        viewModel.ExpenseFormCreditCardName.Should().Be("BaAmex");
    }

    [Fact]
    public async Task DeleteExpenseCommand_FromUnpaidCardCharges_ConfirmedCallsDeleteAndRefreshes()
    {
        var (viewModel, expenses, _, _, _, _) = CreateViewModel();
        var unpaidCharge = new ExpenseDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), Description = "Uber", Value = 18.4m, CategoryId = Guid.NewGuid(), CategoryName = "Extras", CreditCardId = Guid.NewGuid(), CreditCardName = "BaAmex", PaymentStatus = "CreditCardCharge" };
        await viewModel.RefreshAsync();
        var callsBefore = expenses.GetUnpaidCardChargesByMonthCallCount;

        await viewModel.DeleteExpenseAsync(unpaidCharge);

        expenses.LastDeletedId.Should().Be(unpaidCharge.Id);
        expenses.GetUnpaidCardChargesByMonthCallCount.Should().BeGreaterThan(callsBefore);
    }

    [Fact]
    public async Task SaveExpenseAsync_WithValidRequest_RecordsSuccessfulSpan()
    {
        var tracer = new RecordingTelemetryTracer();
        var (viewModel, _, _, banks, _, _) = CreateViewModel(tracer: tracer);
        await viewModel.RefreshAsync();
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");
        viewModel.ExpenseFormDate = DateTime.Today;
        viewModel.ExpenseFormDescription = "Groceries";
        viewModel.ExpenseFormCategoryId = DefaultCategories[0].Id;
        viewModel.ExpenseFormValue = "25.50";
        viewModel.ExpenseFormPaymentSource = banks.Banks[1].Id;

        await viewModel.SaveExpenseAsync();

        var span = tracer.Spans.Should().ContainSingle().Which;
        span.Name.Should().Be("App.MonthlyViewModel.SaveExpense");
        span.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
    }

    [Fact]
    public async Task AddExpense_BankMode_CallsServiceWithPaymentSourceAndRefreshes()
    {
        var (viewModel, expenses, _, banks, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");
        viewModel.ExpenseFormDate = DateTime.Today;
        viewModel.ExpenseFormDescription = "Groceries";
        viewModel.ExpenseFormCategoryId = DefaultCategories[0].Id; // Mercado
        viewModel.ExpenseFormValue = "25.50";
        viewModel.ExpenseFormPaymentSource = banks.Banks[1].Id; // Chase, no round-up

        await viewModel.SaveExpenseAsync();

        expenses.LastCreateRequest.Should().NotBeNull();
        expenses.LastCreateRequest!.PaymentSourceBankId.Should().Be(ChaseId);
        expenses.LastCreateRequest.CreditCardId.Should().BeNull();
        viewModel.IsExpenseFormOpen.Should().BeFalse();
    }

    [Fact]
    public async Task AddExpense_CardMode_CallsServiceWithCreditCardId()
    {
        var (viewModel, expenses, _, _, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        viewModel.ShowCreateExpenseFormCommand.Execute("card");
        viewModel.ExpenseFormDate = DateTime.Today;
        viewModel.ExpenseFormDescription = "Flight";
        viewModel.ExpenseFormCategoryId = DefaultCategories[2].Id; // Viagem
        viewModel.ExpenseFormValue = "300";
        viewModel.ExpenseFormCreditCardId = DefaultCreditCards[0].Id;

        await viewModel.SaveExpenseAsync();

        expenses.LastCreateRequest.Should().NotBeNull();
        expenses.LastCreateRequest!.CreditCardId.Should().Be(DefaultCreditCards[0].Id);
        expenses.LastCreateRequest.PaymentSourceBankId.Should().BeNull();
    }

    [Fact]
    public async Task ShowCreateExpenseForm_DefaultsExpenseFormCountsAsTitheToTrue()
    {
        var (viewModel, _, _, _, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();

        viewModel.ShowCreateExpenseFormCommand.Execute("bank");

        viewModel.ExpenseFormCountsAsTithe.Should().BeTrue();
    }

    [Fact]
    public async Task ExpenseFormCategoryId_SetToTitheCategory_ShowsCountsAsTitheField()
    {
        var (viewModel, _, _, _, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");

        viewModel.ExpenseFormCategoryId = DefaultCategories[3].Id; // Dizimo

        viewModel.ShowCountsAsTitheField.Should().BeTrue();
    }

    [Fact]
    public async Task ExpenseFormCategoryId_SetToNonTitheCategory_HidesCountsAsTitheField()
    {
        var (viewModel, _, _, _, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");

        viewModel.ExpenseFormCategoryId = DefaultCategories[0].Id; // Mercado

        viewModel.ShowCountsAsTitheField.Should().BeFalse();
    }

    [Fact]
    public async Task AddExpense_DizimoCategoryWithCountsAsTitheUnchecked_SendsFalseToService()
    {
        var (viewModel, expenses, _, banks, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");
        viewModel.ExpenseFormDate = DateTime.Today;
        viewModel.ExpenseFormDescription = "Charitable offer";
        viewModel.ExpenseFormCategoryId = DefaultCategories[3].Id; // Dizimo
        viewModel.ExpenseFormValue = "50";
        viewModel.ExpenseFormPaymentSource = banks.Banks[1].Id;
        viewModel.ExpenseFormCountsAsTithe = false;

        await viewModel.SaveExpenseAsync();

        expenses.LastCreateRequest.Should().NotBeNull();
        expenses.LastCreateRequest!.CountsAsTithe.Should().BeFalse();
    }

    [Fact]
    public async Task EditExpense_PopulatesExpenseFormCountsAsTitheFromExpense()
    {
        var (viewModel, _, _, banks, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        var expense = new ExpenseDTO
        {
            Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), Description = "Offer",
            Value = 50m, CategoryId = DefaultCategories[3].Id, CategoryName = "Dizimo",
            PaymentSourceBankId = banks.Banks[0].Id, PaymentSourceBankName = banks.Banks[0].Name,
            PaymentStatus = "ImmediatePayment", CountsAsTithe = false,
        };

        viewModel.EditExpenseCommand.Execute(expense);

        viewModel.ExpenseFormCountsAsTithe.Should().BeFalse();
        viewModel.ShowCountsAsTitheField.Should().BeTrue();
    }

    [Fact]
    public void ShowCreateExpenseFormCommand_CardMode_SetsIsCardPaymentMode()
    {
        var (viewModel, _, _, _, _, _) = CreateViewModel();

        viewModel.ShowCreateExpenseFormCommand.Execute("card");

        viewModel.IsCardPaymentMode.Should().BeTrue();
        viewModel.IsBankPaymentMode.Should().BeFalse();
    }

    [Fact]
    public async Task ShowCreateExpenseFormCommand_BankMode_DefaultsToFirstBankAndEmptyCreditCardId()
    {
        var (viewModel, _, _, banks, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();

        viewModel.ShowCreateExpenseFormCommand.Execute("bank");

        viewModel.IsCardPaymentMode.Should().BeFalse();
        viewModel.ExpenseFormPaymentSource.Should().Be(banks.Banks[0].Id);
        viewModel.ExpenseFormCreditCardId.Should().BeNull();
    }

    [Fact]
    public void ShowCreateExpenseFormCommand_CardMode_DefaultsToEmptyPaymentSourceAndCreditCardId()
    {
        var (viewModel, _, _, _, _, _) = CreateViewModel();

        viewModel.ShowCreateExpenseFormCommand.Execute("card");

        viewModel.ExpenseFormPaymentSource.Should().BeNull();
        viewModel.ExpenseFormCreditCardId.Should().BeNull();
    }

    [Fact]
    public void EditExpense_SettledExpense_HidesPaymentModeFieldsAndSaveButton()
    {
        var (viewModel, _, _, _, _, _) = CreateViewModel();
        var settledExpense = new ExpenseDTO
        {
            Id = Guid.NewGuid(),
            Date = DateOnly.FromDateTime(DateTime.Today),
            Description = "Settled",
            Value = 10m,
            CategoryId = Guid.NewGuid(), CategoryName = "Mercado",
            CreditCardId = Guid.NewGuid(), CreditCardName = "BaAmex",
            PaymentStatus = "CreditCardSettled",
        };

        viewModel.EditExpenseCommand.Execute(settledExpense);

        viewModel.ExpenseFormIsSettled.Should().BeTrue();
        viewModel.ShowPaymentModeFields.Should().BeFalse();
    }

    [Fact]
    public async Task SelectingRoundUpEnabledBank_ShowsRoundUpField()
    {
        var (viewModel, _, _, banks, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");

        viewModel.ExpenseFormPaymentSource = banks.Banks[0].Id; // Barclays, round-up enabled

        viewModel.ShowRoundUpField.Should().BeTrue();
    }

    [Fact]
    public async Task SelectingNonRoundUpBank_HidesRoundUpField()
    {
        var (viewModel, _, _, banks, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");

        viewModel.ExpenseFormPaymentSource = banks.Banks[1].Id; // Chase, round-up disabled

        viewModel.ShowRoundUpField.Should().BeFalse();
    }

    [Fact]
    public async Task NegativeValue_SelectingRoundUpEnabledBank_DoesNotSuggestRoundUp()
    {
        var (viewModel, _, _, banks, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");

        viewModel.ExpenseFormValue = "-9.40";
        viewModel.ExpenseFormPaymentSource = banks.Banks[0].Id; // Barclays, round-up enabled

        viewModel.ExpenseFormRoundUpAmount.Should().BeEmpty();
    }

    [Fact]
    public void SettledExpense_DeleteCommandCannotExecute()
    {
        var (viewModel, _, _, _, _, _) = CreateViewModel();
        var settledExpense = new ExpenseDTO
        {
            Id = Guid.NewGuid(),
            Date = DateOnly.FromDateTime(DateTime.Today),
            Description = "Settled",
            Value = 10m,
            CategoryId = Guid.NewGuid(), CategoryName = "Mercado",
            CreditCardId = Guid.NewGuid(), CreditCardName = "BaAmex",
            PaymentStatus = "CreditCardSettled",
        };

        viewModel.DeleteExpenseCommand.CanExecute(settledExpense).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteExpense_CallsServiceAndRefreshes()
    {
        var (viewModel, expenses, _, _, _, _) = CreateViewModel();
        var expense = new ExpenseDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), Description = "X", Value = 1m, CategoryId = Guid.NewGuid(), CategoryName = "Mercado", PaymentStatus = "ImmediatePayment" };

        await viewModel.DeleteExpenseAsync(expense);

        expenses.LastDeletedId.Should().Be(expense.Id);
    }

    [Fact]
    public async Task DeleteExpense_ConfirmationDeclined_DoesNotCallService()
    {
        var (viewModel, expenses, _, _, _, _) = CreateViewModel(confirmDeletes: false);
        var expense = new ExpenseDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), Description = "X", Value = 1m, CategoryId = Guid.NewGuid(), CategoryName = "Mercado", PaymentStatus = "ImmediatePayment" };

        await viewModel.DeleteExpenseAsync(expense);

        expenses.LastDeletedId.Should().BeNull();
    }

    [Fact]
    public async Task IncomeSourceOptions_MatchesActiveFetchedSourcesInDisplayOrder()
    {
        var incomeSourceService = new StubIncomeSourceService
        {
            IncomeSources =
            [
                new IncomeSourceDTO { Id = Guid.NewGuid(), Name = "DividendoJuros", IsActive = true, Group = "DividendoJuros", AutoSplitToReserve = false },
                new IncomeSourceDTO { Id = Guid.NewGuid(), Name = "Lottery", IsActive = true, Group = "NonReportable", AutoSplitToReserve = false },
                new IncomeSourceDTO { Id = Guid.NewGuid(), Name = "Ariana", IsActive = true, Group = "Salary", AutoSplitToReserve = true },
                new IncomeSourceDTO { Id = Guid.NewGuid(), Name = "Gleison", IsActive = true, Group = "Salary", AutoSplitToReserve = false },
            ],
        };
        var (viewModel, _, _, _, _, _) = CreateViewModel(incomeSourceService: incomeSourceService);

        await viewModel.RefreshAsync();

        viewModel.IncomeSourceOptions.Select(s => s.Name).Should().Equal("Gleison", "Ariana", "Lottery", "DividendoJuros");
    }

    [Fact]
    public async Task IncomeSourceOptions_ExcludesInactiveSources()
    {
        var incomeSourceService = new StubIncomeSourceService
        {
            IncomeSources =
            [
                new IncomeSourceDTO { Id = Guid.NewGuid(), Name = "Gleison", IsActive = true, Group = "Salary", AutoSplitToReserve = false },
                new IncomeSourceDTO { Id = Guid.NewGuid(), Name = "RetiredSource", IsActive = false, Group = "NonReportable", AutoSplitToReserve = false },
            ],
        };
        var (viewModel, _, _, _, _, _) = CreateViewModel(incomeSourceService: incomeSourceService);

        await viewModel.RefreshAsync();

        viewModel.IncomeSourceOptions.Select(s => s.Name).Should().Equal("Gleison");
    }

    [Fact]
    public async Task ShowCreateIncomeForm_DefaultsSourceToFirstActiveOption()
    {
        var (viewModel, _, _, _, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();

        viewModel.ShowCreateIncomeFormCommand.Execute(null);

        viewModel.IncomeFormSource.Should().Be(GleisonSourceId);
    }

    [Fact]
    public async Task ShowCreateIncomeForm_WithNoActiveSources_DefaultsToEmpty()
    {
        var incomeSourceService = new StubIncomeSourceService { IncomeSources = [] };
        var (viewModel, _, _, _, _, _) = CreateViewModel(incomeSourceService: incomeSourceService);
        await viewModel.RefreshAsync();

        viewModel.ShowCreateIncomeFormCommand.Execute(null);

        viewModel.IncomeFormSource.Should().BeNull();
    }

    [Fact]
    public async Task RefreshAsync_IncomeSourceFetchFails_LeavesOptionsEmptyAndSetsError()
    {
        var incomeSourceService = new StubIncomeSourceService { ThrowOnGet = new InvalidOperationException("Unavailable") };
        var (viewModel, _, _, _, _, _) = CreateViewModel(incomeSourceService: incomeSourceService);

        await viewModel.RefreshAsync();

        viewModel.HasError.Should().BeTrue();
        viewModel.IncomeSourceOptions.Should().BeEmpty();
        IncomeFormValidation.BuildValidationMessage(DateTime.Today, incomeSource: null, netValue: "10")
            .Should().Contain("Source is required.");
    }

    [Fact]
    public async Task AddIncome_GleisonSource_ShowsGrossValueField()
    {
        var (viewModel, _, _, _, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        viewModel.ShowCreateIncomeFormCommand.Execute(null);

        viewModel.IncomeFormSource = GleisonSourceId;

        viewModel.ShowIncomeGrossValueField.Should().BeTrue();
    }

    [Fact]
    public void AddIncome_LotterySource_HidesGrossValueField()
    {
        var (viewModel, _, _, _, _, _) = CreateViewModel();
        viewModel.ShowCreateIncomeFormCommand.Execute(null);

        viewModel.IncomeFormSource = LotterySourceId;

        viewModel.ShowIncomeGrossValueField.Should().BeFalse();
    }

    [Fact]
    public async Task AddIncome_ValidForm_CallsServiceAndRefreshes()
    {
        var (viewModel, _, incomes, banks, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        viewModel.ShowCreateIncomeFormCommand.Execute(null);
        viewModel.IncomeFormDate = DateTime.Today;
        viewModel.IncomeFormSource = LotterySourceId;
        viewModel.IncomeFormNetValue = "50";
        viewModel.IncomeFormBank = banks.Banks[0].Id;

        await viewModel.SaveIncomeAsync();

        incomes.LastCreateRequest.Should().NotBeNull();
        incomes.LastCreateRequest!.NetValue.Should().Be(50m);
        incomes.LastCreateRequest.GrossValue.Should().BeNull();
        viewModel.IsIncomeFormOpen.Should().BeFalse();
    }

    [Fact]
    public async Task AddIncome_WithDescription_SendsDescriptionToService()
    {
        var (viewModel, _, incomes, banks, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        viewModel.ShowCreateIncomeFormCommand.Execute(null);
        viewModel.IncomeFormDate = DateTime.Today;
        viewModel.IncomeFormSource = LotterySourceId;
        viewModel.IncomeFormNetValue = "50";
        viewModel.IncomeFormBank = banks.Banks[0].Id;
        viewModel.IncomeFormDescription = "Chip ISA dividend";

        await viewModel.SaveIncomeAsync();

        incomes.LastCreateRequest.Should().NotBeNull();
        incomes.LastCreateRequest!.Description.Should().Be("Chip ISA dividend");
    }

    [Fact]
    public async Task ShowEditIncomeForm_PopulatesDescription()
    {
        var (viewModel, _, _, banks, _, _) = CreateViewModel();
        var income = new IncomeDTO
        {
            Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), IncomeSourceId = Guid.NewGuid(), IncomeSourceName = "Lottery",
            NetValue = 50m, BankId = banks.Banks[0].Id, BankName = banks.Banks[0].Name, Description = "Chip ISA dividend",
            SplitToReserve = false, ReserveSplitMovements = [],
        };
        await viewModel.RefreshAsync();

        viewModel.EditIncomeCommand.Execute(income);

        viewModel.IncomeFormDescription.Should().Be("Chip ISA dividend");
    }

    [Fact]
    public async Task EditExpense_ValidForm_CallsUpdateServiceAndRefreshes()
    {
        var (viewModel, expenses, _, banks, _, _) = CreateViewModel();
        var expense = new ExpenseDTO
        {
            Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), Description = "Old",
            Value = 10m, CategoryId = Guid.NewGuid(), CategoryName = "Mercado", PaymentSourceBankId = ChaseId, PaymentSourceBankName = banks.Banks[1].Name, PaymentStatus = "ImmediatePayment",
        };
        await viewModel.RefreshAsync();

        viewModel.EditExpenseCommand.Execute(expense);
        viewModel.ExpenseFormDescription = "Updated";
        viewModel.ExpenseFormValue = "20";

        await viewModel.SaveExpenseAsync();

        expenses.LastUpdateRequest.Should().NotBeNull();
        expenses.LastUpdateRequest!.Value.Id.Should().Be(expense.Id);
        expenses.LastUpdateRequest.Value.Request.Description.Should().Be("Updated");
        expenses.LastUpdateRequest.Value.Request.Value.Should().Be(20m);
        viewModel.IsExpenseFormOpen.Should().BeFalse();
    }

    [Fact]
    public async Task EditIncome_ValidForm_CallsUpdateServiceAndRefreshes()
    {
        var (viewModel, _, incomes, banks, _, _) = CreateViewModel();
        var income = new IncomeDTO
        {
            Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), IncomeSourceId = Guid.NewGuid(), IncomeSourceName = "Lottery",
            NetValue = 50m, BankId = BarclaysId, BankName = banks.Banks[0].Name,
            SplitToReserve = false, ReserveSplitMovements = [],
        };
        await viewModel.RefreshAsync();

        viewModel.EditIncomeCommand.Execute(income);
        viewModel.IncomeFormNetValue = "75";

        await viewModel.SaveIncomeAsync();

        incomes.LastUpdateRequest.Should().NotBeNull();
        incomes.LastUpdateRequest!.Value.Id.Should().Be(income.Id);
        incomes.LastUpdateRequest.Value.Request.NetValue.Should().Be(75m);
        viewModel.IsIncomeFormOpen.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteIncome_CallsServiceAndRefreshes()
    {
        var (viewModel, _, incomes, _, _, _) = CreateViewModel();
        var income = new IncomeDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), IncomeSourceId = Guid.NewGuid(), IncomeSourceName = "Lottery", NetValue = 10m, BankId = BarclaysId, BankName = "Barclays", SplitToReserve = false, ReserveSplitMovements = [] };

        await viewModel.DeleteIncomeAsync(income);

        incomes.LastDeletedId.Should().Be(income.Id);
    }

    [Fact]
    public async Task DeleteIncome_ConfirmationDeclined_DoesNotCallService()
    {
        var (viewModel, _, incomes, _, _, _) = CreateViewModel(confirmDeletes: false);
        var income = new IncomeDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), IncomeSourceId = Guid.NewGuid(), IncomeSourceName = "Lottery", NetValue = 10m, BankId = BarclaysId, BankName = "Barclays", SplitToReserve = false, ReserveSplitMovements = [] };

        await viewModel.DeleteIncomeAsync(income);

        incomes.LastDeletedId.Should().BeNull();
    }

    [Fact]
    public async Task SaveExpense_MissingDescription_DoesNotCallServiceAndShowsError()
    {
        var (viewModel, expenses, _, banks, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");
        viewModel.ExpenseFormDate = DateTime.Today;
        viewModel.ExpenseFormDescription = "";
        viewModel.ExpenseFormCategoryId = DefaultCategories[0].Id; // Mercado
        viewModel.ExpenseFormValue = "10";
        viewModel.ExpenseFormPaymentSource = banks.Banks[0].Id;

        await viewModel.SaveExpenseAsync();

        expenses.LastCreateRequest.Should().BeNull();
        viewModel.ExpenseSaveError.Should().NotBeNullOrEmpty();
        viewModel.IsExpenseFormOpen.Should().BeTrue();
    }

    [Fact]
    public async Task SaveIncome_WithoutBank_CallsServiceWithNullBankAndRefreshes()
    {
        var (viewModel, _, incomes, _, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        viewModel.ShowCreateIncomeFormCommand.Execute(null);
        viewModel.IncomeFormDate = DateTime.Today;
        viewModel.IncomeFormSource = LotterySourceId;
        viewModel.IncomeFormNetValue = "50";
        viewModel.IncomeFormBank = null;

        await viewModel.SaveIncomeAsync();

        incomes.LastCreateRequest.Should().NotBeNull();
        incomes.LastCreateRequest!.BankId.Should().BeNull();
        viewModel.IsIncomeFormOpen.Should().BeFalse();
    }

    [Fact]
    public async Task ShowCreateIncomeForm_DefaultsBankToNone()
    {
        var (viewModel, _, _, _, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();

        viewModel.ShowCreateIncomeFormCommand.Execute(null);

        viewModel.IncomeFormBank.Should().BeNull();
    }

    [Fact]
    public async Task RefreshAsync_PopulatesIncomeBankOptionsWithANoneOptionFirst()
    {
        var (viewModel, _, _, _, _, _) = CreateViewModel();

        await viewModel.RefreshAsync();

        viewModel.IncomeBankOptions.Should().HaveCount(3);
        viewModel.IncomeBankOptions[0].Id.Should().BeNull();
        viewModel.IncomeBankOptions[0].Name.Should().Be(MonthlyViewModel.NoBankOptionLabel);
        viewModel.IncomeBankOptions.Should().Contain(o => o.Id == BarclaysId && o.Name == "Barclays");
        viewModel.IncomeBankOptions.Should().Contain(o => o.Id == ChaseId && o.Name == "Chase");
    }

    [Fact]
    public void ShowCreateExpenseFormCommand_CardMode_DefaultsInvoiceDateFromExpenseFormDate()
    {
        var (viewModel, _, _, _, _, _) = CreateViewModel();

        viewModel.ShowCreateExpenseFormCommand.Execute("card");

        viewModel.ExpenseFormInvoiceYear.Should().Be(viewModel.ExpenseFormDate!.Value.Year);
        viewModel.ExpenseFormInvoiceMonth.Should().Be(viewModel.ExpenseFormDate!.Value.Month);
    }

    [Fact]
    public void ExpenseFormDate_ChangedBeforeInvoiceDateTouched_ResyncsInvoiceDefault()
    {
        var (viewModel, _, _, _, _, _) = CreateViewModel();
        viewModel.ShowCreateExpenseFormCommand.Execute("card");

        viewModel.ExpenseFormDate = new DateTime(2026, 3, 15);

        viewModel.ExpenseFormInvoiceYear.Should().Be(2026);
        viewModel.ExpenseFormInvoiceMonth.Should().Be(3);
    }

    [Fact]
    public void ExpenseFormInvoiceMonth_SetByUser_StopsFurtherAutoResync()
    {
        var (viewModel, _, _, _, _, _) = CreateViewModel();
        viewModel.ShowCreateExpenseFormCommand.Execute("card");

        viewModel.ExpenseFormInvoiceYear = 2026;
        viewModel.ExpenseFormInvoiceMonth = 4;
        viewModel.ExpenseFormDate = new DateTime(2026, 3, 15);

        viewModel.ExpenseFormInvoiceYear.Should().Be(2026);
        viewModel.ExpenseFormInvoiceMonth.Should().Be(4);
    }

    [Fact]
    public void EditExpenseCommand_FromUnpaidCardCharges_PrefillsInvoiceDateFromExpense()
    {
        var (viewModel, _, _, _, _, _) = CreateViewModel();
        var unpaidCharge = new ExpenseDTO
        {
            Id = Guid.NewGuid(),
            Date = new DateOnly(2026, 1, 20),
            ChargeDate = new DateOnly(2026, 1, 20),
            InvoiceDate = new DateOnly(2026, 2, 1),
            Description = "Uber",
            Value = 18.4m,
            CategoryId = Guid.NewGuid(), CategoryName = "Extras",
            CreditCardId = Guid.NewGuid(), CreditCardName = "BaAmex",
            PaymentStatus = "CreditCardCharge",
        };

        viewModel.EditExpenseCommand.Execute(unpaidCharge);

        viewModel.ExpenseFormInvoiceYear.Should().Be(2026);
        viewModel.ExpenseFormInvoiceMonth.Should().Be(2);
    }

    [Fact]
    public void EditExpense_SettledExpense_InvoiceDateFieldPresentButPaymentModeFieldsGated()
    {
        var (viewModel, _, _, _, _, _) = CreateViewModel();
        var settledExpense = new ExpenseDTO
        {
            Id = Guid.NewGuid(),
            Date = new DateOnly(2026, 2, 1),
            ChargeDate = new DateOnly(2026, 1, 20),
            InvoiceDate = new DateOnly(2026, 2, 1),
            Description = "Settled",
            Value = 10m,
            CategoryId = Guid.NewGuid(), CategoryName = "Mercado",
            CreditCardId = Guid.NewGuid(), CreditCardName = "BaAmex",
            PaymentStatus = "CreditCardSettled",
        };

        viewModel.EditExpenseCommand.Execute(settledExpense);

        viewModel.ExpenseFormInvoiceYear.Should().Be(2026);
        viewModel.ExpenseFormInvoiceMonth.Should().Be(2);
        viewModel.ShowPaymentModeFields.Should().BeFalse();
    }

    [Fact]
    public async Task AddExpense_CardMode_CallsServiceWithInvoiceDate()
    {
        var (viewModel, expenses, _, _, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        viewModel.ShowCreateExpenseFormCommand.Execute("card");
        viewModel.ExpenseFormDate = new DateTime(2026, 3, 15);
        viewModel.ExpenseFormDescription = "Flight";
        viewModel.ExpenseFormCategoryId = DefaultCategories[2].Id; // Viagem
        viewModel.ExpenseFormValue = "300";
        viewModel.ExpenseFormCreditCardId = DefaultCreditCards[0].Id;

        await viewModel.SaveExpenseAsync();

        expenses.LastCreateRequest.Should().NotBeNull();
        expenses.LastCreateRequest!.InvoiceDate.Should().Be(new DateOnly(2026, 3, 1));
    }

    [Fact]
    public async Task AddExpense_BankMode_CallsServiceWithNullInvoiceDate()
    {
        var (viewModel, expenses, _, banks, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");
        viewModel.ExpenseFormDate = DateTime.Today;
        viewModel.ExpenseFormDescription = "Groceries";
        viewModel.ExpenseFormCategoryId = DefaultCategories[0].Id; // Mercado
        viewModel.ExpenseFormValue = "25.50";
        viewModel.ExpenseFormPaymentSource = banks.Banks[1].Id;

        await viewModel.SaveExpenseAsync();

        expenses.LastCreateRequest.Should().NotBeNull();
        expenses.LastCreateRequest!.InvoiceDate.Should().BeNull();
    }

    [Fact]
    public async Task SaveExpenseAsync_EditingCardExpense_CallsServiceWithInvoiceDate()
    {
        var (viewModel, expenses, _, _, _, _) = CreateViewModel();
        var expense = new ExpenseDTO
        {
            Id = Guid.NewGuid(),
            Date = new DateOnly(2026, 1, 20),
            ChargeDate = new DateOnly(2026, 1, 20),
            InvoiceDate = new DateOnly(2026, 2, 1),
            Description = "Uber",
            Value = 18.4m,
            CategoryId = Guid.NewGuid(), CategoryName = "Extras",
            CreditCardId = Guid.NewGuid(), CreditCardName = "BaAmex",
            PaymentStatus = "CreditCardCharge",
        };
        await viewModel.RefreshAsync();

        viewModel.EditExpenseCommand.Execute(expense);
        viewModel.ExpenseFormInvoiceYear = 2026;
        viewModel.ExpenseFormInvoiceMonth = 3;

        await viewModel.SaveExpenseAsync();

        expenses.LastUpdateRequest.Should().NotBeNull();
        expenses.LastUpdateRequest!.Value.Request.InvoiceDate.Should().Be(new DateOnly(2026, 3, 1));
    }
}
