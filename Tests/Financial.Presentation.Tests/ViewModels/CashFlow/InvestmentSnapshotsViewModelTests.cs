using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class InvestmentSnapshotsViewModelTests
{
    private static (InvestmentSnapshotsViewModel ViewModel, StubInvestmentSnapshotService Service) CreateViewModel()
    {
        var service = new StubInvestmentSnapshotService();
        var viewModel = new InvestmentSnapshotsViewModel(service);
        return (viewModel, service);
    }

    private static InvestmentSnapshotDTO CreateSnapshot(int year, int month, string account, bool isLiability, decimal value) => new()
    {
        Id = Guid.NewGuid(), Account = account, IsLiability = isLiability, Year = year, Month = month, Value = value,
    };

    [Fact]
    public async Task RefreshAsync_LoadsSnapshotsForSelectedYearMonth()
    {
        var (viewModel, service) = CreateViewModel();
        var today = DateTime.Today;
        var previousMonth = today.AddMonths(-1);
        service.Snapshots =
        [
            CreateSnapshot(today.Year, today.Month, "ISA", false, 1000m),
            CreateSnapshot(previousMonth.Year, previousMonth.Month, "ISA", false, 900m),
        ];

        await viewModel.RefreshAsync();

        viewModel.Snapshots.Should().ContainSingle(s => s.Account == "ISA" && s.Value == 1000m);
    }

    [Fact]
    public async Task SettingYearOrMonth_RefetchesSnapshots()
    {
        var (viewModel, service) = CreateViewModel();
        await viewModel.RefreshAsync();
        var callsAfterInitial = service.GetSnapshotsForMonthCallCount;

        viewModel.Year = viewModel.Year - 1;
        await viewModel.RefreshAsync();

        service.GetSnapshotsForMonthCallCount.Should().BeGreaterThan(callsAfterInitial);
    }

    [Fact]
    public async Task SnapshotRow_LiabilityAccount_ShowsSuffixedLabel()
    {
        var (viewModel, service) = CreateViewModel();
        var today = DateTime.Today;
        service.Snapshots =
        [
            CreateSnapshot(today.Year, today.Month, "ISA", false, 1000m),
            CreateSnapshot(today.Year, today.Month, "Credit Card", true, 200m),
        ];

        await viewModel.RefreshAsync();

        viewModel.Snapshots.Single(s => s.Account == "ISA").DisplayLabel.Should().Be("ISA");
        viewModel.Snapshots.Single(s => s.Account == "Credit Card").DisplayLabel.Should().Be("Credit Card (liability)");
    }

    [Fact]
    public async Task NetTotal_SubtractsLiabilityValues()
    {
        var (viewModel, service) = CreateViewModel();
        var today = DateTime.Today;
        service.Snapshots =
        [
            CreateSnapshot(today.Year, today.Month, "ISA", false, 1000m),
            CreateSnapshot(today.Year, today.Month, "Savings", false, 500m),
            CreateSnapshot(today.Year, today.Month, "Credit Card", true, 200m),
        ];

        await viewModel.RefreshAsync();

        viewModel.NetTotal.Should().Be(1300m);
    }

    [Fact]
    public async Task EditSnapshot_ValidForm_CallsUpdateServiceAndClosesForm()
    {
        var (viewModel, service) = CreateViewModel();
        var today = DateTime.Today;
        var snapshot = CreateSnapshot(today.Year, today.Month, "ISA", false, 1000m);
        service.Snapshots = [snapshot];
        await viewModel.RefreshAsync();
        var row = viewModel.Snapshots.Single();

        viewModel.EditSnapshotCommand.Execute(row);
        viewModel.EditValue = "1200";

        await viewModel.SaveEditAsync();

        service.LastUpdateRequest.Should().NotBeNull();
        service.LastUpdateRequest!.Value.Id.Should().Be(snapshot.Id);
        service.LastUpdateRequest.Value.Request.Value.Should().Be(1200m);
        viewModel.IsEditFormOpen.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("-5")]
    public async Task EditSnapshot_InvalidForm_BlocksSaveWithoutServiceCall(string value)
    {
        var (viewModel, service) = CreateViewModel();
        var today = DateTime.Today;
        var snapshot = CreateSnapshot(today.Year, today.Month, "ISA", false, 1000m);
        viewModel.EditSnapshotCommand.Execute(SnapshotRow.FromDto(snapshot));
        viewModel.EditValue = value;

        await viewModel.SaveEditAsync();

        service.LastUpdateRequest.Should().BeNull();
        viewModel.EditSaveError.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task EditSnapshot_BackendRejects_KeepsFormOpenWithValueIntactAndShowsServerError()
    {
        var (viewModel, service) = CreateViewModel();
        var today = DateTime.Today;
        var snapshot = CreateSnapshot(today.Year, today.Month, "ISA", false, 1000m);
        service.ThrowOnUpdate = new InvalidOperationException("Value must not be negative.");
        viewModel.EditSnapshotCommand.Execute(SnapshotRow.FromDto(snapshot));
        viewModel.EditValue = "1200";

        await viewModel.SaveEditAsync();

        viewModel.IsEditFormOpen.Should().BeTrue();
        viewModel.EditSaveError.Should().Be("Value must not be negative.");
        viewModel.EditValue.Should().Be("1200");
    }
}
