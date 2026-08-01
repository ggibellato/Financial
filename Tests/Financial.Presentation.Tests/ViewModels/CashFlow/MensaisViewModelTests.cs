using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class MensaisViewModelTests
{
    private static (MensaisViewModel ViewModel, StubMensaisService Service) CreateViewModel(bool confirm = true) =>
        CreateViewModel(_ => confirm);

    private static (MensaisViewModel ViewModel, StubMensaisService Service) CreateViewModel(Func<string, bool> confirm)
    {
        var service = new StubMensaisService();
        var viewModel = new MensaisViewModel(service, confirm);
        return (viewModel, service);
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
        var (viewModel, service) = CreateViewModel();
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
        var (viewModel, service) = CreateViewModel();
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

    [Theory]
    [InlineData("", "15", "80")]
    [InlineData("Electricity", "0", "80")]
    [InlineData("Electricity", "32", "80")]
    [InlineData("Electricity", "15", "abc")]
    public async Task AddBill_InvalidForm_BlocksSaveWithoutServiceCall(string description, string dueDay, string value)
    {
        var (viewModel, service) = CreateViewModel();
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
        var (viewModel, service) = CreateViewModel();
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
        viewModel.IsEditFormOpen.Should().BeFalse();
    }

    [Fact]
    public async Task EditBill_InvalidForm_BlocksSaveWithoutServiceCall()
    {
        var (viewModel, service) = CreateViewModel();
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
        var (viewModel, service) = CreateViewModel(confirmed);
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
        var (viewModel, service) = CreateViewModel(confirm: true);
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
        var (viewModel, service) = CreateViewModel(confirm: false);

        await viewModel.ResetAllToUnsetAsync();

        service.ResetAllToUnsetCallCount.Should().Be(0);
    }
}
