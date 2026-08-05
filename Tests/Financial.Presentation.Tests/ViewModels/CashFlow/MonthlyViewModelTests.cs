using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class MonthlyViewModelTests
{
    private static (MonthlyViewModel ViewModel, StubExpenseService Expenses, StubIncomeService Incomes, StubBankService Banks, StubTitheService Tithe) CreateViewModel(bool confirmDeletes = true)
    {
        var expenses = new StubExpenseService();
        var incomes = new StubIncomeService();
        var banks = new StubBankService { Banks = [new BankDTO { Name = "Barclays", RoundUpEnabled = true, OpeningBalance = 0, OpeningBalanceDate = DateOnly.FromDateTime(DateTime.Today) }, new BankDTO { Name = "Chase", RoundUpEnabled = false, OpeningBalance = 0, OpeningBalanceDate = DateOnly.FromDateTime(DateTime.Today) }] };
        var tithe = new StubTitheService { Summary = new TitheSummaryDTO { CalculatedTithe = 100m, TitheBalance = 50m } };
        var transfers = new StubTransferService();
        var adjustments = new StubBalanceAdjustmentService();
        var cardStatements = new StubCardStatementService();

        var viewModel = new MonthlyViewModel(expenses, incomes, banks, tithe, transfers, adjustments, cardStatements, confirm: _ => confirmDeletes);
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
    public async Task RefreshAsync_GroupsIncomesBySourceAndSumsGrossOnlyWhenPresent()
    {
        var (viewModel, _, incomes, _, _) = CreateViewModel();
        incomes.Incomes =
        [
            new IncomeDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), IncomeSource = "Gleison", GrossValue = 120m, NetValue = 100m, Bank = "Barclays" },
            new IncomeDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), IncomeSource = "Gleison", GrossValue = 60m, NetValue = 50m, Bank = "Barclays" },
            new IncomeDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), IncomeSource = "Ariana", NetValue = 30m, Bank = "Chase" },
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
    public void CategoryAndCardOptions_ExposeStaticListsAsInstanceMembers()
    {
        // WPF's {Binding} only resolves instance members, never static fields — these
        // instance-level wrappers are what the Category/Card ComboBoxes actually bind to.
        var (viewModel, _, _, _, _) = CreateViewModel();

        viewModel.CategoryOptions.Should().BeSameAs(MonthlyViewModel.Categories);
        viewModel.CardOptions.Should().BeSameAs(MonthlyViewModel.Cards);
    }

    [Fact]
    public void SettingCardPaymentMode_TogglesIsCardPaymentModeAndExposesFiveCards()
    {
        var (viewModel, _, _, _, _) = CreateViewModel();
        viewModel.ShowCreateExpenseFormCommand.Execute(null);

        viewModel.SetCardPaymentModeCommand.Execute(null);
        viewModel.IsCardPaymentMode.Should().BeTrue();
        viewModel.IsBankPaymentMode.Should().BeFalse();
        MonthlyViewModel.Cards.Should().HaveCount(5);

        viewModel.SetBankPaymentModeCommand.Execute(null);
        viewModel.IsCardPaymentMode.Should().BeFalse();
        viewModel.IsBankPaymentMode.Should().BeTrue();
    }

    [Fact]
    public void EditExpense_SettledExpense_HidesPaymentModeFieldsAndSaveButton()
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

        viewModel.EditExpenseCommand.Execute(settledExpense);

        viewModel.ExpenseFormIsSettled.Should().BeTrue();
        viewModel.ShowPaymentModeFields.Should().BeFalse();
    }

    [Fact]
    public async Task SelectingRoundUpEnabledBank_ShowsRoundUpField()
    {
        var (viewModel, _, _, banks, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        viewModel.ShowCreateExpenseFormCommand.Execute(null);

        viewModel.ExpenseFormPaymentSource = banks.Banks[0].Name; // Barclays, round-up enabled

        viewModel.ShowRoundUpField.Should().BeTrue();
    }

    [Fact]
    public async Task SelectingNonRoundUpBank_HidesRoundUpField()
    {
        var (viewModel, _, _, banks, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        viewModel.ShowCreateExpenseFormCommand.Execute(null);

        viewModel.ExpenseFormPaymentSource = banks.Banks[1].Name; // Chase, round-up disabled

        viewModel.ShowRoundUpField.Should().BeFalse();
    }

    [Fact]
    public async Task NegativeValue_SelectingRoundUpEnabledBank_DoesNotSuggestRoundUp()
    {
        var (viewModel, _, _, banks, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        viewModel.ShowCreateExpenseFormCommand.Execute(null);

        viewModel.ExpenseFormValue = "-9.40";
        viewModel.ExpenseFormPaymentSource = banks.Banks[0].Name; // Barclays, round-up enabled

        viewModel.ExpenseFormRoundUpAmount.Should().BeEmpty();
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
    public async Task DeleteExpense_ConfirmationDeclined_DoesNotCallService()
    {
        var (viewModel, expenses, _, _, _) = CreateViewModel(confirmDeletes: false);
        var expense = new ExpenseDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), Description = "X", Value = 1m, Category = "Mercado", PaymentStatus = "ImmediatePayment" };

        await viewModel.DeleteExpenseAsync(expense);

        expenses.LastDeletedId.Should().BeNull();
    }

    [Fact]
    public void IncomeSourceOptions_ExposesStaticListAsInstanceMember()
    {
        // WPF's {Binding} only resolves instance members, never static fields — this
        // instance-level wrapper is what the Income form's Source ComboBox actually binds to.
        var (viewModel, _, _, _, _) = CreateViewModel();

        viewModel.IncomeSourceOptions.Should().BeSameAs(MonthlyViewModel.IncomeSources);
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

    [Fact]
    public async Task EditExpense_ValidForm_CallsUpdateServiceAndRefreshes()
    {
        var (viewModel, expenses, _, banks, _) = CreateViewModel();
        var expense = new ExpenseDTO
        {
            Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), Description = "Old",
            Value = 10m, Category = "Mercado", PaymentSource = banks.Banks[1].Name, PaymentStatus = "ImmediatePayment",
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
        var (viewModel, _, incomes, banks, _) = CreateViewModel();
        var income = new IncomeDTO
        {
            Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), IncomeSource = "Lottery",
            NetValue = 50m, Bank = banks.Banks[0].Name,
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
        var (viewModel, _, incomes, _, _) = CreateViewModel();
        var income = new IncomeDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), IncomeSource = "Lottery", NetValue = 10m, Bank = "Barclays" };

        await viewModel.DeleteIncomeAsync(income);

        incomes.LastDeletedId.Should().Be(income.Id);
    }

    [Fact]
    public async Task DeleteIncome_ConfirmationDeclined_DoesNotCallService()
    {
        var (viewModel, _, incomes, _, _) = CreateViewModel(confirmDeletes: false);
        var income = new IncomeDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), IncomeSource = "Lottery", NetValue = 10m, Bank = "Barclays" };

        await viewModel.DeleteIncomeAsync(income);

        incomes.LastDeletedId.Should().BeNull();
    }

    [Fact]
    public async Task SaveExpense_MissingDescription_DoesNotCallServiceAndShowsError()
    {
        var (viewModel, expenses, _, banks, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        viewModel.ShowCreateExpenseFormCommand.Execute(null);
        viewModel.ExpenseFormDate = DateTime.Today;
        viewModel.ExpenseFormDescription = "";
        viewModel.ExpenseFormCategory = "Mercado";
        viewModel.ExpenseFormValue = "10";
        viewModel.ExpenseFormPaymentSource = banks.Banks[0].Name;

        await viewModel.SaveExpenseAsync();

        expenses.LastCreateRequest.Should().BeNull();
        viewModel.ExpenseSaveError.Should().NotBeNullOrEmpty();
        viewModel.IsExpenseFormOpen.Should().BeTrue();
    }

    [Fact]
    public async Task SaveIncome_MissingBank_DoesNotCallServiceAndShowsError()
    {
        var (viewModel, _, incomes, _, _) = CreateViewModel();
        await viewModel.RefreshAsync();
        viewModel.ShowCreateIncomeFormCommand.Execute(null);
        viewModel.IncomeFormDate = DateTime.Today;
        viewModel.IncomeFormSource = "Lottery";
        viewModel.IncomeFormNetValue = "50";
        viewModel.IncomeFormBank = "";

        await viewModel.SaveIncomeAsync();

        incomes.LastCreateRequest.Should().BeNull();
        viewModel.IncomeSaveError.Should().NotBeNullOrEmpty();
        viewModel.IsIncomeFormOpen.Should().BeTrue();
    }
}
