using System.Collections.ObjectModel;
using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels;
using Financial.Presentation.App.ViewModels.CashFlow;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class ExpenseWorkflowViewModelTests
{
    /// <summary>Unchecks every filter option except the given values, mirroring how a user would
    /// narrow the header checklist down to a subset (see BankOperationsWorkflowViewModelTests.SelectOnly).</summary>
    private static void SelectOnly(ColumnFilterViewModel<ExpenseDTO> filter, params string[] values)
    {
        foreach (var option in filter.Options)
        {
            option.IsChecked = values.Contains(option.Value);
        }
    }

    private static readonly Guid BarclaysId = Guid.NewGuid();
    private static readonly Guid ChaseId = Guid.NewGuid();

    private static readonly List<BankDTO> DefaultBanks =
    [
        new() { Id = BarclaysId, Name = "Barclays", RoundUpEnabled = true, OpeningBalance = 0, OpeningBalanceDate = DateOnly.FromDateTime(DateTime.Today), HasReferences = false },
        new() { Id = ChaseId, Name = "Chase", RoundUpEnabled = false, OpeningBalance = 0, OpeningBalanceDate = DateOnly.FromDateTime(DateTime.Today), HasReferences = false },
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
        new() { Id = Guid.NewGuid(), Name = "Mercado", Active = true, IsInvestment = false, IsTithe = false, HasReferences = false },
        new() { Id = Guid.NewGuid(), Name = "Extras", Active = true, IsInvestment = false, IsTithe = false, HasReferences = false },
        new() { Id = Guid.NewGuid(), Name = "Viagem", Active = true, IsInvestment = false, IsTithe = false, HasReferences = false },
        new() { Id = Guid.NewGuid(), Name = "Dizimo", Active = true, IsInvestment = false, IsTithe = true, HasReferences = false },
    ];

    private static (ExpenseWorkflowViewModel ViewModel, StubExpenseService Service, ObservableCollection<BankDTO> Banks) CreateViewModel(
        bool confirmDeletes = true, RecordingTelemetryTracer? tracer = null, Func<Task>? refresh = null)
    {
        var expenseService = new StubExpenseService();
        var categories = new ObservableCollection<CategoryDTO>(DefaultCategories);
        var banks = new ObservableCollection<BankDTO>(DefaultBanks);
        var creditCards = new ObservableCollection<CreditCardDTO>(DefaultCreditCards);
        var viewModel = new ExpenseWorkflowViewModel(
            expenseService, categories, banks, creditCards,
            confirm: _ => confirmDeletes, tracer ?? new RecordingTelemetryTracer(), refresh ?? (() => Task.CompletedTask));
        return (viewModel, expenseService, banks);
    }

    [Fact]
    public void ApplyRefresh_PopulatesExpensesAndUnpaidCardCharges()
    {
        var (viewModel, _, _) = CreateViewModel();
        var expense = new ExpenseDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), Description = "Test", Value = 10m, CategoryId = Guid.NewGuid(), CategoryName = "Mercado", PaymentStatus = "ImmediatePayment" };
        var unpaidCharge = new ExpenseDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), Description = "Uber", Value = 18.4m, CategoryId = Guid.NewGuid(), CategoryName = "Extras", CreditCardId = Guid.NewGuid(), CreditCardName = "BaAmex", PaymentStatus = "CreditCardCharge" };

        viewModel.ApplyRefresh([expense], [unpaidCharge]);

        viewModel.Expenses.Should().ContainSingle().Which.Should().Be(expense);
        viewModel.UnpaidCardCharges.Should().ContainSingle().Which.Should().Be(unpaidCharge);
    }

    [Fact]
    public void ActiveCategories_ReflectsTheSharedCategoriesCollection()
    {
        var expenseService = new StubExpenseService();
        var categories = new ObservableCollection<CategoryDTO>(DefaultCategories);
        var banks = new ObservableCollection<BankDTO>(DefaultBanks);
        var creditCards = new ObservableCollection<CreditCardDTO>(DefaultCreditCards);
        var viewModel = new ExpenseWorkflowViewModel(
            expenseService, categories, banks, creditCards,
            confirm: _ => true, new RecordingTelemetryTracer(), () => Task.CompletedTask);
        var newCategory = new CategoryDTO { Id = Guid.NewGuid(), Name = "New", Active = true, IsInvestment = false, IsTithe = false, HasReferences = false };

        categories.Add(newCategory);
        viewModel.NotifyCategoriesChanged();

        viewModel.ActiveCategories.Should().Contain(newCategory);
    }

    [Fact]
    public void EditExpenseCommand_FromUnpaidCardCharges_OpensFormPrefilled()
    {
        var (viewModel, _, _) = CreateViewModel();
        var unpaidCharge = new ExpenseDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), Description = "Uber", Value = 18.4m, CategoryId = Guid.NewGuid(), CategoryName = "Extras", CreditCardId = Guid.NewGuid(), CreditCardName = "BaAmex", PaymentStatus = "CreditCardCharge" };

        viewModel.EditExpenseCommand.Execute(unpaidCharge);

        viewModel.IsExpenseFormOpen.Should().BeTrue();
        viewModel.IsEditingExpense.Should().BeTrue();
        viewModel.ExpenseFormDescription.Should().Be("Uber");
        viewModel.ExpenseFormCreditCardName.Should().Be("BaAmex");
    }

    [Fact]
    public async Task SaveExpenseAsync_WithValidRequest_RecordsSuccessfulSpan()
    {
        var tracer = new RecordingTelemetryTracer();
        var (viewModel, _, banks) = CreateViewModel(tracer: tracer);
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");
        viewModel.ExpenseFormDate = DateTime.Today;
        viewModel.ExpenseFormDescription = "Groceries";
        viewModel.ExpenseFormCategoryId = DefaultCategories[0].Id;
        viewModel.ExpenseFormValue = "25.50";
        viewModel.ExpenseFormPaymentSource = banks[1].Id;

        await viewModel.SaveExpenseAsync();

        var span = tracer.Spans.Should().ContainSingle().Which;
        span.Name.Should().Be("App.MonthlyViewModel.SaveExpense");
        span.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
    }

    [Fact]
    public async Task AddExpense_BankMode_CallsServiceWithPaymentSourceAndRefreshes()
    {
        var (viewModel, expenses, banks) = CreateViewModel();
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");
        viewModel.ExpenseFormDate = DateTime.Today;
        viewModel.ExpenseFormDescription = "Groceries";
        viewModel.ExpenseFormCategoryId = DefaultCategories[0].Id; // Mercado
        viewModel.ExpenseFormValue = "25.50";
        viewModel.ExpenseFormPaymentSource = banks[1].Id; // Chase, no round-up

        await viewModel.SaveExpenseAsync();

        expenses.LastCreateRequest.Should().NotBeNull();
        expenses.LastCreateRequest!.PaymentSourceBankId.Should().Be(ChaseId);
        expenses.LastCreateRequest.CreditCardId.Should().BeNull();
        viewModel.IsExpenseFormOpen.Should().BeFalse();
    }

    [Fact]
    public async Task AddExpense_CardMode_CallsServiceWithCreditCardId()
    {
        var (viewModel, expenses, _) = CreateViewModel();
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
    public void ShowCreateExpenseForm_DefaultsExpenseFormCountsAsTitheToTrue()
    {
        var (viewModel, _, _) = CreateViewModel();

        viewModel.ShowCreateExpenseFormCommand.Execute("bank");

        viewModel.ExpenseFormCountsAsTithe.Should().BeTrue();
    }

    [Fact]
    public void ExpenseFormCategoryId_SetToTitheCategory_ShowsCountsAsTitheField()
    {
        var (viewModel, _, _) = CreateViewModel();
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");

        viewModel.ExpenseFormCategoryId = DefaultCategories[3].Id; // Dizimo

        viewModel.ShowCountsAsTitheField.Should().BeTrue();
    }

    [Fact]
    public void ExpenseFormCategoryId_SetToNonTitheCategory_HidesCountsAsTitheField()
    {
        var (viewModel, _, _) = CreateViewModel();
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");

        viewModel.ExpenseFormCategoryId = DefaultCategories[0].Id; // Mercado

        viewModel.ShowCountsAsTitheField.Should().BeFalse();
    }

    [Fact]
    public async Task AddExpense_DizimoCategoryWithCountsAsTitheUnchecked_SendsFalseToService()
    {
        var (viewModel, expenses, banks) = CreateViewModel();
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");
        viewModel.ExpenseFormDate = DateTime.Today;
        viewModel.ExpenseFormDescription = "Charitable offer";
        viewModel.ExpenseFormCategoryId = DefaultCategories[3].Id; // Dizimo
        viewModel.ExpenseFormValue = "50";
        viewModel.ExpenseFormPaymentSource = banks[1].Id;
        viewModel.ExpenseFormCountsAsTithe = false;

        await viewModel.SaveExpenseAsync();

        expenses.LastCreateRequest.Should().NotBeNull();
        expenses.LastCreateRequest!.CountsAsTithe.Should().BeFalse();
    }

    [Fact]
    public void EditExpense_PopulatesExpenseFormCountsAsTitheFromExpense()
    {
        var (viewModel, _, banks) = CreateViewModel();
        var expense = new ExpenseDTO
        {
            Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), Description = "Offer",
            Value = 50m, CategoryId = DefaultCategories[3].Id, CategoryName = "Dizimo",
            PaymentSourceBankId = banks[0].Id, PaymentSourceBankName = banks[0].Name,
            PaymentStatus = "ImmediatePayment", CountsAsTithe = false,
        };

        viewModel.EditExpenseCommand.Execute(expense);

        viewModel.ExpenseFormCountsAsTithe.Should().BeFalse();
        viewModel.ShowCountsAsTitheField.Should().BeTrue();
    }

    [Fact]
    public void ShowCreateExpenseFormCommand_CardMode_SetsIsCardPaymentMode()
    {
        var (viewModel, _, _) = CreateViewModel();

        viewModel.ShowCreateExpenseFormCommand.Execute("card");

        viewModel.IsCardPaymentMode.Should().BeTrue();
        viewModel.IsBankPaymentMode.Should().BeFalse();
    }

    [Fact]
    public void ShowCreateExpenseFormCommand_BankMode_DefaultsToFirstBankAndEmptyCreditCardId()
    {
        var (viewModel, _, banks) = CreateViewModel();

        viewModel.ShowCreateExpenseFormCommand.Execute("bank");

        viewModel.IsCardPaymentMode.Should().BeFalse();
        viewModel.ExpenseFormPaymentSource.Should().Be(banks[0].Id);
        viewModel.ExpenseFormCreditCardId.Should().BeNull();
    }

    [Fact]
    public void ShowCreateExpenseFormCommand_CardMode_DefaultsToEmptyPaymentSourceAndCreditCardId()
    {
        var (viewModel, _, _) = CreateViewModel();

        viewModel.ShowCreateExpenseFormCommand.Execute("card");

        viewModel.ExpenseFormPaymentSource.Should().BeNull();
        viewModel.ExpenseFormCreditCardId.Should().BeNull();
    }

    [Fact]
    public void EditExpense_SettledExpense_HidesPaymentModeFieldsAndSaveButton()
    {
        var (viewModel, _, _) = CreateViewModel();
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
    public void SelectingRoundUpEnabledBank_ShowsRoundUpField()
    {
        var (viewModel, _, banks) = CreateViewModel();
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");

        viewModel.ExpenseFormPaymentSource = banks[0].Id; // Barclays, round-up enabled

        viewModel.ShowRoundUpField.Should().BeTrue();
    }

    [Fact]
    public void SelectingNonRoundUpBank_HidesRoundUpField()
    {
        var (viewModel, _, banks) = CreateViewModel();
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");

        viewModel.ExpenseFormPaymentSource = banks[1].Id; // Chase, round-up disabled

        viewModel.ShowRoundUpField.Should().BeFalse();
    }

    [Fact]
    public void NegativeValue_SelectingRoundUpEnabledBank_DoesNotSuggestRoundUp()
    {
        var (viewModel, _, banks) = CreateViewModel();
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");

        viewModel.ExpenseFormValue = "-9.40";
        viewModel.ExpenseFormPaymentSource = banks[0].Id; // Barclays, round-up enabled

        viewModel.ExpenseFormRoundUpAmount.Should().BeEmpty();
    }

    [Fact]
    public void SettledExpense_DeleteCommandCannotExecute()
    {
        var (viewModel, _, _) = CreateViewModel();
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
        var (viewModel, expenses, _) = CreateViewModel();
        var expense = new ExpenseDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), Description = "X", Value = 1m, CategoryId = Guid.NewGuid(), CategoryName = "Mercado", PaymentStatus = "ImmediatePayment" };

        await viewModel.DeleteExpenseAsync(expense);

        expenses.LastDeletedId.Should().Be(expense.Id);
    }

    [Fact]
    public async Task DeleteExpense_ConfirmationDeclined_DoesNotCallService()
    {
        var (viewModel, expenses, _) = CreateViewModel(confirmDeletes: false);
        var expense = new ExpenseDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), Description = "X", Value = 1m, CategoryId = Guid.NewGuid(), CategoryName = "Mercado", PaymentStatus = "ImmediatePayment" };

        await viewModel.DeleteExpenseAsync(expense);

        expenses.LastDeletedId.Should().BeNull();
    }

    [Fact]
    public async Task EditExpense_ValidForm_CallsUpdateServiceAndRefreshes()
    {
        var (viewModel, expenses, banks) = CreateViewModel();
        var expense = new ExpenseDTO
        {
            Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), Description = "Old",
            Value = 10m, CategoryId = Guid.NewGuid(), CategoryName = "Mercado", PaymentSourceBankId = ChaseId, PaymentSourceBankName = banks[1].Name, PaymentStatus = "ImmediatePayment",
        };

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
    public async Task SaveExpense_MissingDescription_DoesNotCallServiceAndShowsError()
    {
        var (viewModel, expenses, banks) = CreateViewModel();
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");
        viewModel.ExpenseFormDate = DateTime.Today;
        viewModel.ExpenseFormDescription = "";
        viewModel.ExpenseFormCategoryId = DefaultCategories[0].Id; // Mercado
        viewModel.ExpenseFormValue = "10";
        viewModel.ExpenseFormPaymentSource = banks[0].Id;

        await viewModel.SaveExpenseAsync();

        expenses.LastCreateRequest.Should().BeNull();
        viewModel.ExpenseSaveError.Should().NotBeNullOrEmpty();
        viewModel.IsExpenseFormOpen.Should().BeTrue();
    }

    [Fact]
    public async Task DateFieldError_MissingDate_MatchesSaveError()
    {
        var (viewModel, expenses, banks) = CreateViewModel();
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");
        viewModel.ExpenseFormDate = null;
        viewModel.ExpenseFormDescription = "Groceries";
        viewModel.ExpenseFormValue = "10";
        viewModel.ExpenseFormPaymentSource = banks[0].Id;

        await viewModel.SaveExpenseAsync();

        expenses.LastCreateRequest.Should().BeNull();
        viewModel.DateFieldError.Should().Be(viewModel.ExpenseSaveError);
        viewModel.DescriptionFieldError.Should().BeNull();
    }

    [Fact]
    public async Task DescriptionFieldError_MissingDescription_MatchesSaveErrorAndLeavesOtherFieldsNull()
    {
        var (viewModel, expenses, banks) = CreateViewModel();
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");
        viewModel.ExpenseFormDate = DateTime.Today;
        viewModel.ExpenseFormDescription = "";
        viewModel.ExpenseFormValue = "10";
        viewModel.ExpenseFormPaymentSource = banks[0].Id;

        await viewModel.SaveExpenseAsync();

        expenses.LastCreateRequest.Should().BeNull();
        viewModel.DescriptionFieldError.Should().Be(viewModel.ExpenseSaveError);
        viewModel.DateFieldError.Should().BeNull();
        viewModel.ValueFieldError.Should().BeNull();
        viewModel.PaymentModeFieldError.Should().BeNull();
    }

    [Fact]
    public async Task CategoryFieldError_MissingCategory_MatchesSaveError()
    {
        var (viewModel, expenses, banks) = CreateViewModel();
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");
        viewModel.ExpenseFormDate = DateTime.Today;
        viewModel.ExpenseFormDescription = "Groceries";
        viewModel.ExpenseFormCategoryId = null;
        viewModel.ExpenseFormValue = "10";
        viewModel.ExpenseFormPaymentSource = banks[0].Id;

        await viewModel.SaveExpenseAsync();

        expenses.LastCreateRequest.Should().BeNull();
        viewModel.CategoryFieldError.Should().Be(viewModel.ExpenseSaveError);
    }

    [Fact]
    public async Task ValueFieldError_ZeroValue_MatchesSaveError()
    {
        var (viewModel, expenses, banks) = CreateViewModel();
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");
        viewModel.ExpenseFormDate = DateTime.Today;
        viewModel.ExpenseFormDescription = "Groceries";
        viewModel.ExpenseFormValue = "0";
        viewModel.ExpenseFormPaymentSource = banks[0].Id;

        await viewModel.SaveExpenseAsync();

        expenses.LastCreateRequest.Should().BeNull();
        viewModel.ValueFieldError.Should().Be(viewModel.ExpenseSaveError);
    }

    [Fact]
    public async Task PaymentModeFieldError_BankModeMissingPaymentSource_MatchesSaveError()
    {
        var (viewModel, expenses, _) = CreateViewModel();
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");
        viewModel.ExpenseFormDate = DateTime.Today;
        viewModel.ExpenseFormDescription = "Groceries";
        viewModel.ExpenseFormValue = "10";
        viewModel.ExpenseFormPaymentSource = null;

        await viewModel.SaveExpenseAsync();

        expenses.LastCreateRequest.Should().BeNull();
        viewModel.PaymentModeFieldError.Should().Be(viewModel.ExpenseSaveError);
    }

    [Fact]
    public async Task PaymentModeFieldError_CardModeMissingCreditCard_MatchesSaveError()
    {
        var (viewModel, expenses, _) = CreateViewModel();
        viewModel.ShowCreateExpenseFormCommand.Execute("card");
        viewModel.ExpenseFormDate = DateTime.Today;
        viewModel.ExpenseFormDescription = "Flight";
        viewModel.ExpenseFormValue = "300";
        viewModel.ExpenseFormCreditCardId = null;

        await viewModel.SaveExpenseAsync();

        expenses.LastCreateRequest.Should().BeNull();
        viewModel.PaymentModeFieldError.Should().Be(viewModel.ExpenseSaveError);
    }

    [Fact]
    public async Task RoundUpAmountFieldError_OutOfRange_MatchesSaveError()
    {
        var (viewModel, expenses, banks) = CreateViewModel();
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");
        viewModel.ExpenseFormDate = DateTime.Today;
        viewModel.ExpenseFormDescription = "Groceries";
        viewModel.ExpenseFormValue = "10";
        viewModel.ExpenseFormPaymentSource = banks[0].Id; // Barclays, round-up enabled
        viewModel.ExpenseFormRoundUpAmount = "5.00"; // outside Expense.MinRoundUpAmount..MaxRoundUpAmount

        await viewModel.SaveExpenseAsync();

        expenses.LastCreateRequest.Should().BeNull();
        viewModel.RoundUpAmountFieldError.Should().Be(viewModel.ExpenseSaveError);
    }

    [Fact]
    public async Task FieldErrors_ClearAfterSuccessfulSave()
    {
        var (viewModel, _, banks) = CreateViewModel();
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");
        viewModel.ExpenseFormDate = DateTime.Today;
        viewModel.ExpenseFormDescription = "";
        viewModel.ExpenseFormValue = "10";
        viewModel.ExpenseFormPaymentSource = banks[0].Id;
        await viewModel.SaveExpenseAsync();
        viewModel.DescriptionFieldError.Should().NotBeNull();

        viewModel.ExpenseFormDescription = "Groceries";
        await viewModel.SaveExpenseAsync();

        viewModel.DescriptionFieldError.Should().BeNull();
    }

    [Fact]
    public void ShowCreateExpenseFormCommand_CardMode_DefaultsInvoiceDateFromExpenseFormDate()
    {
        var (viewModel, _, _) = CreateViewModel();

        viewModel.ShowCreateExpenseFormCommand.Execute("card");

        viewModel.ExpenseFormInvoiceYear.Should().Be(viewModel.ExpenseFormDate!.Value.Year);
        viewModel.ExpenseFormInvoiceMonth.Should().Be(viewModel.ExpenseFormDate!.Value.Month);
    }

    [Fact]
    public void ExpenseFormDate_ChangedBeforeInvoiceDateTouched_ResyncsInvoiceDefault()
    {
        var (viewModel, _, _) = CreateViewModel();
        viewModel.ShowCreateExpenseFormCommand.Execute("card");

        viewModel.ExpenseFormDate = new DateTime(2026, 3, 15);

        viewModel.ExpenseFormInvoiceYear.Should().Be(2026);
        viewModel.ExpenseFormInvoiceMonth.Should().Be(3);
    }

    [Fact]
    public void ExpenseFormInvoiceMonth_SetByUser_StopsFurtherAutoResync()
    {
        var (viewModel, _, _) = CreateViewModel();
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
        var (viewModel, _, _) = CreateViewModel();
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
        var (viewModel, _, _) = CreateViewModel();
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
        var (viewModel, expenses, _) = CreateViewModel();
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
        var (viewModel, expenses, banks) = CreateViewModel();
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");
        viewModel.ExpenseFormDate = DateTime.Today;
        viewModel.ExpenseFormDescription = "Groceries";
        viewModel.ExpenseFormCategoryId = DefaultCategories[0].Id; // Mercado
        viewModel.ExpenseFormValue = "25.50";
        viewModel.ExpenseFormPaymentSource = banks[1].Id;

        await viewModel.SaveExpenseAsync();

        expenses.LastCreateRequest.Should().NotBeNull();
        expenses.LastCreateRequest!.InvoiceDate.Should().BeNull();
    }

    [Fact]
    public async Task SaveExpenseAsync_EditingCardExpense_CallsServiceWithInvoiceDate()
    {
        var (viewModel, expenses, _) = CreateViewModel();
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

        viewModel.EditExpenseCommand.Execute(expense);
        viewModel.ExpenseFormInvoiceYear = 2026;
        viewModel.ExpenseFormInvoiceMonth = 3;

        await viewModel.SaveExpenseAsync();

        expenses.LastUpdateRequest.Should().NotBeNull();
        expenses.LastUpdateRequest!.Value.Request.InvoiceDate.Should().Be(new DateOnly(2026, 3, 1));
    }

    private static ExpenseDTO MakeExpense(string description, string categoryName, string? creditCardName = null, string? paymentSourceBankName = null) => new()
    {
        Id = Guid.NewGuid(),
        Date = DateOnly.FromDateTime(DateTime.Today),
        Description = description,
        Value = 10m,
        CategoryId = Guid.NewGuid(),
        CategoryName = categoryName,
        CreditCardId = creditCardName is null ? null : Guid.NewGuid(),
        CreditCardName = creditCardName,
        PaymentSourceBankName = paymentSourceBankName,
        PaymentStatus = creditCardName is null ? "ImmediatePayment" : "CreditCardCharge",
    };

    [Fact]
    public void ExpensesCategoryFilter_Refresh_ComputesAvailableValuesFromFullUnfilteredExpenses()
    {
        var (viewModel, _, _) = CreateViewModel();
        var mercado = MakeExpense("A", "Mercado");
        var extras = MakeExpense("B", "Extras");

        viewModel.ApplyRefresh([mercado, extras], []);

        viewModel.ExpensesCategoryFilter.Options.Select(o => o.Value).Should().BeEquivalentTo(["Mercado", "Extras"]);
    }

    [Fact]
    public void ExpensesCategoryFilter_UncheckingValue_ExcludesMatchingRowsFromFilteredExpenses()
    {
        var (viewModel, _, _) = CreateViewModel();
        var mercado = MakeExpense("A", "Mercado");
        var extras = MakeExpense("B", "Extras");
        viewModel.ApplyRefresh([mercado, extras], []);

        SelectOnly(viewModel.ExpensesCategoryFilter, "Mercado");

        viewModel.FilteredExpenses.Should().ContainSingle().Which.Should().Be(mercado);
        viewModel.Expenses.Should().HaveCount(2);
    }

    [Fact]
    public void ExpensesCategoryAndCardFilters_CombineWithAnd_OnlyRowsMatchingBothRemain()
    {
        var (viewModel, _, _) = CreateViewModel();
        var mercadoOnCardA = MakeExpense("A", "Mercado", creditCardName: "CardA");
        var mercadoOnCardB = MakeExpense("B", "Mercado", creditCardName: "CardB");
        var extrasOnCardA = MakeExpense("C", "Extras", creditCardName: "CardA");
        viewModel.ApplyRefresh([mercadoOnCardA, mercadoOnCardB, extrasOnCardA], []);

        SelectOnly(viewModel.ExpensesCategoryFilter, "Mercado");
        SelectOnly(viewModel.ExpensesCardFilter, "CardA");

        viewModel.FilteredExpenses.Should().ContainSingle().Which.Should().Be(mercadoOnCardA);
    }

    [Fact]
    public void FilteredExpensesAndFilteredUnpaidCardCharges_AreIndependent_FilteringOneDoesNotAffectTheOther()
    {
        var (viewModel, _, _) = CreateViewModel();
        var expense = MakeExpense("A", "Mercado");
        var unpaidCharge = MakeExpense("B", "Mercado", creditCardName: "BaAmex");
        viewModel.ApplyRefresh([expense], [unpaidCharge]);

        SelectOnly(viewModel.ExpensesCategoryFilter, "Extras");

        viewModel.FilteredExpenses.Should().BeEmpty();
        viewModel.FilteredUnpaidCardCharges.Should().ContainSingle().Which.Should().Be(unpaidCharge);
        viewModel.UnpaidCardChargesCategoryFilter.IsFiltered.Should().BeFalse();
    }

    [Fact]
    public void FilteredUnpaidCardCharges_FilteringDoesNotAffectFilteredExpenses()
    {
        var (viewModel, _, _) = CreateViewModel();
        var expense = MakeExpense("A", "Mercado");
        var unpaidChargeA = MakeExpense("B", "Mercado", creditCardName: "CardA");
        var unpaidChargeB = MakeExpense("C", "Mercado", creditCardName: "CardB");
        viewModel.ApplyRefresh([expense], [unpaidChargeA, unpaidChargeB]);

        SelectOnly(viewModel.UnpaidCardChargesCardFilter, "CardA");

        viewModel.FilteredUnpaidCardCharges.Should().ContainSingle().Which.Should().Be(unpaidChargeA);
        viewModel.FilteredExpenses.Should().ContainSingle().Which.Should().Be(expense);
        viewModel.ExpensesCategoryFilter.IsFiltered.Should().BeFalse();
        viewModel.ExpensesCardFilter.IsFiltered.Should().BeFalse();
    }

    [Fact]
    public async Task ShowCreateExpenseForm_AfterSuccessfulCreate_PersistsDatePaymentSourceAndCategory()
    {
        var (viewModel, _, banks) = CreateViewModel();
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");
        viewModel.ExpenseFormDate = new DateTime(2026, 3, 15);
        viewModel.ExpenseFormDescription = "Groceries";
        viewModel.ExpenseFormCategoryId = DefaultCategories[1].Id; // Extras
        viewModel.ExpenseFormValue = "25";
        viewModel.ExpenseFormPaymentSource = banks[1].Id; // Chase

        await viewModel.SaveExpenseAsync();
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");

        viewModel.ExpenseFormDate.Should().Be(new DateTime(2026, 3, 15));
        viewModel.ExpenseFormPaymentSource.Should().Be(banks[1].Id);
        viewModel.ExpenseFormCategoryId.Should().Be(DefaultCategories[1].Id);
    }

    [Fact]
    public async Task ShowCreateExpenseForm_AfterSuccessfulCreate_AmountAndDescriptionStayBlank()
    {
        var (viewModel, _, banks) = CreateViewModel();
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");
        viewModel.ExpenseFormDate = DateTime.Today;
        viewModel.ExpenseFormDescription = "Groceries";
        viewModel.ExpenseFormValue = "25";
        viewModel.ExpenseFormPaymentSource = banks[1].Id;

        await viewModel.SaveExpenseAsync();
        viewModel.ShowCreateExpenseFormCommand.Execute("bank");

        viewModel.ExpenseFormDescription.Should().BeEmpty();
        viewModel.ExpenseFormValue.Should().BeEmpty();
    }

    [Fact]
    public async Task ShowCreateExpenseForm_AfterSuccessfulCardModeCreate_PersistsCreditCardNotPaymentSource()
    {
        var (viewModel, _, _) = CreateViewModel();
        viewModel.ShowCreateExpenseFormCommand.Execute("card");
        viewModel.ExpenseFormDate = DateTime.Today;
        viewModel.ExpenseFormDescription = "Flight";
        viewModel.ExpenseFormValue = "300";
        viewModel.ExpenseFormCreditCardId = DefaultCreditCards[0].Id;

        await viewModel.SaveExpenseAsync();
        viewModel.ShowCreateExpenseFormCommand.Execute("card");

        viewModel.ExpenseFormCreditCardId.Should().Be(DefaultCreditCards[0].Id);
    }
}
