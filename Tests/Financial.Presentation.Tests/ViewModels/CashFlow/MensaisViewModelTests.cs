using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.Controls;
using Financial.Presentation.App.ViewModels.CashFlow;
using Financial.Presentation.Tests.ViewModels.Admin;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class MensaisViewModelTests
{
    private static (MensaisViewModel ViewModel, StubMensaisService Service, StubExpenseService ExpenseService, StubBankService BankService, StubCategoryService CategoryService, StubDialogService DialogService) CreateViewModel(bool confirm = true) =>
        CreateViewModel(_ => confirm);

    private static (MensaisViewModel ViewModel, StubMensaisService Service, StubExpenseService ExpenseService, StubBankService BankService, StubCategoryService CategoryService, StubDialogService DialogService) CreateViewModel(Func<string, bool> confirm, RecordingLogger<MensaisViewModel>? logger = null)
    {
        var service = new StubMensaisService();
        var expenseService = new StubExpenseService();
        var bankService = new StubBankService();
        var categoryService = new StubCategoryService();
        var dialogService = new StubDialogService
        {
            // Most tests don't exercise the UK Paid-to-Expense prompt: default to Skip so a
            // direct status commit (F03's pre-existing behavior) still happens when it does trigger.
            OnShowUkExpensePromptDialog = vm => vm.SkipCommand.Execute(null),
        };
        var viewModel = new MensaisViewModel(
            service, expenseService, bankService, categoryService, dialogService,
            confirm, logger ?? new RecordingLogger<MensaisViewModel>());
        return (viewModel, service, expenseService, bankService, categoryService, dialogService);
    }

    private static RecurringBillDTO CreateBill(string area, string status = "Unset", string description = "Rent") => new()
    {
        Id = Guid.NewGuid(), DueDay = 10, Description = description, Value = 100m,
        Area = area, Note = string.Empty, NitNumber = area == "Brasil" ? "123" : null,
        MinimumWageValue = area == "Brasil" ? 1500m : null, Status = status,
    };

    [Fact]
    public async Task RefreshAsync_SplitsBillsIntoBrasilAndUkByArea()
    {
        var (viewModel, service, _, _, _, _) = CreateViewModel();
        service.Bills = [CreateBill("Brasil"), CreateBill("Brasil"), CreateBill("UK")];

        await viewModel.RefreshAsync();

        viewModel.BrasilBills.Should().HaveCount(2);
        viewModel.UkBills.Should().HaveCount(1);
    }

    [Theory]
    [InlineData("Brasil")]
    [InlineData("UK")]
    public async Task AddBill_ValidForm_CallsServiceWithCorrectAreaAndClosesForm(string area)
    {
        var (viewModel, service, _, _, _, _) = CreateViewModel();
        viewModel.ShowAddFormCommand.Execute(null);
        viewModel.NewDescription = "Electricity";
        viewModel.NewDueDay = "15";
        viewModel.NewValue = "80.50";
        viewModel.NewArea = area;

        await viewModel.SubmitAddAsync();

        service.LastCreateRequest.Should().NotBeNull();
        service.LastCreateRequest!.Description.Should().Be("Electricity");
        service.LastCreateRequest.DueDay.Should().Be(15);
        service.LastCreateRequest.Value.Should().Be(80.50m);
        service.LastCreateRequest.Area.Should().Be(area);
        viewModel.IsAddFormOpen.Should().BeFalse();
    }

    [Fact]
    public async Task ShowAddForm_AfterSuccessfulAdd_PersistsAreaButNotOtherFields()
    {
        var (viewModel, _, _, _, _, _) = CreateViewModel();
        viewModel.ShowAddFormCommand.Execute(null);
        viewModel.NewDescription = "Council Tax";
        viewModel.NewDueDay = "15";
        viewModel.NewValue = "80.50";
        viewModel.NewArea = "UK";
        viewModel.NewNote = "Monthly";

        await viewModel.SubmitAddAsync();

        viewModel.ShowAddFormCommand.Execute(null);

        viewModel.NewArea.Should().Be("UK");
        viewModel.NewDescription.Should().BeEmpty();
        viewModel.NewDueDay.Should().BeEmpty();
        viewModel.NewValue.Should().BeEmpty();
        viewModel.NewNote.Should().BeEmpty();
    }

    [Theory]
    [InlineData("", "15", "80")]
    [InlineData("Electricity", "0", "80")]
    [InlineData("Electricity", "32", "80")]
    [InlineData("Electricity", "15", "abc")]
    public async Task AddBill_InvalidForm_BlocksSaveWithoutServiceCall(string description, string dueDay, string value)
    {
        var (viewModel, service, _, _, _, _) = CreateViewModel();
        viewModel.ShowAddFormCommand.Execute(null);
        viewModel.NewDescription = description;
        viewModel.NewDueDay = dueDay;
        viewModel.NewValue = value;

        await viewModel.SubmitAddAsync();

        service.LastCreateRequest.Should().BeNull();
        viewModel.AddSaveError.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task EditBill_ValidForm_CallsUpdateServiceWithCorrectId()
    {
        var (viewModel, service, _, _, _, _) = CreateViewModel();
        var bill = CreateBill("Brasil");
        service.Bills = [bill];
        await viewModel.RefreshAsync();

        viewModel.EditBillCommand.Execute(bill);
        viewModel.EditValue = "150";
        viewModel.EditStatus = "Paid";

        await viewModel.SaveEditAsync();

        service.LastUpdateRequest.Should().NotBeNull();
        service.LastUpdateRequest!.Value.Id.Should().Be(bill.Id);
        service.LastUpdateRequest.Value.Request.Value.Should().Be(150m);
        service.LastUpdateRequest.Value.Request.Status.Should().Be("Paid");
        service.LastUpdateRequest.Value.Request.DueDay.Should().Be(bill.DueDay);
        service.LastUpdateRequest.Value.Request.Description.Should().Be(bill.Description);
        service.LastUpdateRequest.Value.Request.Area.Should().Be(bill.Area);
        viewModel.IsEditFormOpen.Should().BeFalse();
    }

    [Fact]
    public async Task EditBill_InvalidForm_BlocksSaveWithoutServiceCall()
    {
        var (viewModel, service, _, _, _, _) = CreateViewModel();
        var bill = CreateBill("Brasil");
        viewModel.EditBillCommand.Execute(bill);
        viewModel.EditValue = "not-a-number";

        await viewModel.SaveEditAsync();

        service.LastUpdateRequest.Should().BeNull();
        viewModel.EditSaveError.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeleteBill_ConfirmedAndDeclined_CallsOrSkipsService(bool confirmed)
    {
        var (viewModel, service, _, _, _, _) = CreateViewModel(confirmed);
        var bill = CreateBill("Brasil");

        await viewModel.DeleteBillAsync(bill);

        if (confirmed)
        {
            service.LastDeletedId.Should().Be(bill.Id);
        }
        else
        {
            service.LastDeletedId.Should().BeNull();
        }
    }

    [Fact]
    public async Task ResetAllToUnset_Confirmed_CallsServiceAndRefreshesBills()
    {
        var (viewModel, service, _, _, _, _) = CreateViewModel(confirm: true);
        service.Bills = [CreateBill("Brasil", status: "Paid"), CreateBill("UK", status: "Scheduled")];
        await viewModel.RefreshAsync();

        await viewModel.ResetAllToUnsetAsync();

        service.ResetAllToUnsetCallCount.Should().Be(1);
        viewModel.BrasilBills.Should().OnlyContain(b => b.Status == "Unset");
        viewModel.UkBills.Should().OnlyContain(b => b.Status == "Unset");
    }

    [Fact]
    public async Task ResetAllToUnset_Declined_DoesNotCallService()
    {
        var (viewModel, service, _, _, _, _) = CreateViewModel(confirm: false);

        await viewModel.ResetAllToUnsetAsync();

        service.ResetAllToUnsetCallCount.Should().Be(0);
    }

    [Theory]
    [InlineData("Brasil")]
    [InlineData("UK")]
    public async Task ChangeStatusAsync_ValidRequest_UpdatesTheMatchingBillInPlace(string area)
    {
        // A UK bill still triggers the F05 prompt on Unset->Paid; the shared default stub
        // (Skip) lets this test assert F03's original direct-commit outcome either way.
        var (viewModel, service, expenseService, _, _, _) = CreateViewModel();
        var bill = CreateBill(area);
        var otherBill = CreateBill(area, description: "Other");
        service.Bills = [bill, otherBill];
        await viewModel.RefreshAsync();

        await viewModel.ChangeStatusAsync(new StatusChangeRequest(bill, "Paid"));

        service.LastStatusChangeRequest.Should().NotBeNull();
        service.LastStatusChangeRequest!.Value.Id.Should().Be(bill.Id);
        service.LastStatusChangeRequest.Value.Request.Status.Should().Be("Paid");
        expenseService.AddExpenseCallCount.Should().Be(0);
        var collection = area == "Brasil" ? viewModel.BrasilBills : viewModel.UkBills;
        collection.Should().ContainSingle(b => b.Id == bill.Id && b.Status == "Paid");
        collection.Should().ContainSingle(b => b.Id == otherBill.Id && b.Status == "Unset");
    }

    [Fact]
    public async Task ChangeStatusAsync_ServiceThrows_SetsErrorAndLeavesCollectionsUntouched()
    {
        var (viewModel, service, _, _, _, _) = CreateViewModel();
        var bill = CreateBill("UK");
        service.Bills = [bill];
        await viewModel.RefreshAsync();
        service.ThrowOnUpdateStatus = new InvalidOperationException("Recurring bill not found.");

        await viewModel.ChangeStatusAsync(new StatusChangeRequest(bill, "Paid"));

        viewModel.StatusChangeError.Should().Be("Recurring bill not found.");
        viewModel.UkBills.Should().ContainSingle(b => b.Id == bill.Id && b.Status == "Unset");
    }

    [Fact]
    public async Task ChangeStatusAsync_RequestWithoutARecurringBill_DoesNothing()
    {
        var (viewModel, service, _, _, _, _) = CreateViewModel();

        await viewModel.ChangeStatusAsync(new StatusChangeRequest("not a bill", "Paid"));

        service.LastStatusChangeRequest.Should().BeNull();
    }

    [Fact]
    public async Task RefreshAsync_ServiceFails_LogsAnErrorWithTheErrorTypeOnly()
    {
        var logger = new RecordingLogger<MensaisViewModel>();
        var (viewModel, service, _, _, _, _) = CreateViewModel(_ => true, logger);
        service.ThrowOnGetBills = new InvalidOperationException("bill Rent value 1234.56");

        await viewModel.RefreshAsync();

        // The constructor's background refresh may also hit the failing service, so more than
        // one identical error can be recorded - assert on all of them.
        var errors = logger.Entries.Where(e => e.Level == Microsoft.Extensions.Logging.LogLevel.Error).ToList();
        errors.Should().NotBeEmpty();
        errors.Should().AllSatisfy(e =>
        {
            e.Message.Should().Contain(nameof(InvalidOperationException));
            e.Message.Should().NotContain("1234.56", "exception messages may embed bill values and must stay out of the log");
        });
    }

    #region UK Paid-to-Expense Prompt (F05)

    private static (Guid BankId, Guid CategoryId) SetUpDialogToConfirm(StubDialogService dialogService)
    {
        var bankId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        dialogService.OnShowUkExpensePromptDialog = vm =>
        {
            vm.BankId = bankId;
            vm.CategoryId = categoryId;
            vm.ConfirmCommand.Execute(null);
        };
        return (bankId, categoryId);
    }

    private static void SetUpDialogToSkip(StubDialogService dialogService) =>
        dialogService.OnShowUkExpensePromptDialog = vm => vm.SkipCommand.Execute(null);

    private static void SetUpDialogToCancel(StubDialogService dialogService) =>
        dialogService.ShowUkExpensePromptDialogResult = false;

    [Theory]
    [InlineData("Unset")]
    [InlineData("Scheduled")]
    public async Task ChangeStatusAsync_UkBillTransitionToPaid_ShowsThePromptInsteadOfCallingApiDirectly(string priorStatus)
    {
        var (viewModel, service, _, _, _, dialogService) = CreateViewModel();
        var bill = CreateBill("UK", status: priorStatus, description: "Council Tax");
        service.Bills = [bill];
        await viewModel.RefreshAsync();
        SetUpDialogToCancel(dialogService);

        await viewModel.ChangeStatusAsync(new StatusChangeRequest(bill, "Paid"));

        dialogService.LastUkExpensePromptDialog.Should().NotBeNull();
        dialogService.LastUkExpensePromptDialog!.BillDescription.Should().Be("Council Tax");
        dialogService.LastUkExpensePromptDialog.Value.Should().Be(bill.Value.ToString());
        service.LastStatusChangeRequest.Should().BeNull();
    }

    [Theory]
    [InlineData("Brasil", "Unset")]
    [InlineData("Brasil", "Scheduled")]
    [InlineData("UK", "Paid")]
    public async Task ChangeStatusAsync_BrasilBillOrAlreadyPaid_UpdatesDirectlyWithoutShowingThePrompt(string area, string priorStatus)
    {
        var (viewModel, service, _, _, _, dialogService) = CreateViewModel();
        var bill = CreateBill(area, status: priorStatus);
        service.Bills = [bill];
        await viewModel.RefreshAsync();

        await viewModel.ChangeStatusAsync(new StatusChangeRequest(bill, "Paid"));

        dialogService.LastUkExpensePromptDialog.Should().BeNull();
        service.LastStatusChangeRequest.Should().NotBeNull();
        service.LastStatusChangeRequest!.Value.Request.Status.Should().Be("Paid");
    }

    [Fact]
    public async Task ChangeStatusAsync_Confirmed_CreatesExpenseThenCommitsStatus()
    {
        var (viewModel, service, expenseService, _, _, dialogService) = CreateViewModel();
        var bill = CreateBill("UK", description: "Council Tax");
        service.Bills = [bill];
        await viewModel.RefreshAsync();
        var (bankId, categoryId) = SetUpDialogToConfirm(dialogService);

        await viewModel.ChangeStatusAsync(new StatusChangeRequest(bill, "Paid"));

        expenseService.AddExpenseCallCount.Should().Be(1);
        expenseService.LastCreateRequest.Should().NotBeNull();
        expenseService.LastCreateRequest!.PaymentSourceBankId.Should().Be(bankId);
        expenseService.LastCreateRequest.CreditCardId.Should().BeNull();
        expenseService.LastCreateRequest.CategoryId.Should().Be(categoryId);
        expenseService.LastCreateRequest.Description.Should().Be("Council Tax");
        service.LastStatusChangeRequest.Should().NotBeNull();
        service.LastStatusChangeRequest!.Value.Request.Status.Should().Be("Paid");
        viewModel.UkBills.Should().ContainSingle(b => b.Id == bill.Id && b.Status == "Paid");
    }

    [Fact]
    public async Task ChangeStatusAsync_ExpenseCreationFails_DoesNotCommitStatus()
    {
        var (viewModel, service, expenseService, _, _, dialogService) = CreateViewModel();
        var bill = CreateBill("UK");
        service.Bills = [bill];
        await viewModel.RefreshAsync();
        SetUpDialogToConfirm(dialogService);
        expenseService.ThrowOnAdd = "Category is inactive.";

        await viewModel.ChangeStatusAsync(new StatusChangeRequest(bill, "Paid"));

        viewModel.StatusChangeError.Should().Be("Category is inactive.");
        service.LastStatusChangeRequest.Should().BeNull();
        viewModel.UkBills.Should().ContainSingle(b => b.Id == bill.Id && b.Status == "Unset");
    }

    [Fact]
    public async Task ChangeStatusAsync_StatusCommitFailsAfterExpenseCreated_RetriesViaConfirmWithoutRecreatingExpense()
    {
        var confirmCallCount = 0;
        StubMensaisService? serviceRef = null;
        var (viewModel, service, expenseService, _, _, dialogService) = CreateViewModel(_ =>
        {
            confirmCallCount++;
            // The retry loop only calls UpdateBillStatusAsync again - clear the stubbed failure
            // here so the retried attempt succeeds, proving AddExpenseAsync is never called twice.
            serviceRef!.ThrowOnUpdateStatus = null;
            return true;
        });
        serviceRef = service;
        var bill = CreateBill("UK");
        service.Bills = [bill];
        await viewModel.RefreshAsync();
        SetUpDialogToConfirm(dialogService);
        service.ThrowOnUpdateStatus = new InvalidOperationException("Recurring bill not found.");

        await viewModel.ChangeStatusAsync(new StatusChangeRequest(bill, "Paid"));

        confirmCallCount.Should().Be(1);
        expenseService.AddExpenseCallCount.Should().Be(1);
        viewModel.StatusChangeError.Should().BeNull();
        viewModel.UkBills.Should().ContainSingle(b => b.Id == bill.Id && b.Status == "Paid");
    }

    [Fact]
    public async Task ChangeStatusAsync_DeclinesRetry_LeavesStatusChangeErrorSetAndBillUnchanged()
    {
        var (viewModel, service, expenseService, _, _, dialogService) = CreateViewModel(_ => false);
        var bill = CreateBill("UK");
        service.Bills = [bill];
        await viewModel.RefreshAsync();
        SetUpDialogToConfirm(dialogService);
        service.ThrowOnUpdateStatus = new InvalidOperationException("Recurring bill not found.");

        await viewModel.ChangeStatusAsync(new StatusChangeRequest(bill, "Paid"));

        expenseService.AddExpenseCallCount.Should().Be(1);
        viewModel.StatusChangeError.Should().Be("Recurring bill not found.");
        viewModel.UkBills.Should().ContainSingle(b => b.Id == bill.Id && b.Status == "Unset");
    }

    [Fact]
    public async Task ChangeStatusAsync_Skipped_CommitsStatusWithoutCreatingExpense()
    {
        var (viewModel, service, expenseService, _, _, dialogService) = CreateViewModel();
        var bill = CreateBill("UK");
        service.Bills = [bill];
        await viewModel.RefreshAsync();
        SetUpDialogToSkip(dialogService);

        await viewModel.ChangeStatusAsync(new StatusChangeRequest(bill, "Paid"));

        expenseService.AddExpenseCallCount.Should().Be(0);
        service.LastStatusChangeRequest.Should().NotBeNull();
        service.LastStatusChangeRequest!.Value.Request.Status.Should().Be("Paid");
        viewModel.UkBills.Should().ContainSingle(b => b.Id == bill.Id && b.Status == "Paid");
    }

    [Fact]
    public async Task ChangeStatusAsync_Cancelled_MakesNoServiceCalls()
    {
        var (viewModel, service, expenseService, _, _, dialogService) = CreateViewModel();
        var bill = CreateBill("UK");
        service.Bills = [bill];
        await viewModel.RefreshAsync();
        SetUpDialogToCancel(dialogService);

        await viewModel.ChangeStatusAsync(new StatusChangeRequest(bill, "Paid"));

        expenseService.AddExpenseCallCount.Should().Be(0);
        service.LastStatusChangeRequest.Should().BeNull();
        viewModel.UkBills.Should().ContainSingle(b => b.Id == bill.Id && b.Status == "Unset");
    }

    [Fact]
    public async Task SaveEditAsync_MarkingAUkBillPaidViaTheEditFormDrawer_NeverOpensThePrompt()
    {
        var (viewModel, service, expenseService, _, _, dialogService) = CreateViewModel();
        var bill = CreateBill("UK");
        service.Bills = [bill];
        await viewModel.RefreshAsync();

        viewModel.EditBillCommand.Execute(bill);
        viewModel.EditValue = "150";
        viewModel.EditStatus = "Paid";

        await viewModel.SaveEditAsync();

        dialogService.LastUkExpensePromptDialog.Should().BeNull();
        expenseService.AddExpenseCallCount.Should().Be(0);
        service.LastUpdateRequest.Should().NotBeNull();
        service.LastUpdateRequest!.Value.Request.Status.Should().Be("Paid");
    }

    #endregion
}
