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

    private static InvestmentSnapshotDTO CreateSnapshot(int year, int month, string account, bool isLiability, decimal value, Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(), AccountId = Guid.NewGuid(), AccountName = account, IsLiability = isLiability, Year = year, Month = month, Value = value,
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

    private static InvestmentSnapshotSuggestionDTO CreateSuggestion(
        Guid snapshotId, string accountName, decimal currentValue, decimal suggestedValue) => new()
    {
        SnapshotId = snapshotId,
        AccountId = Guid.NewGuid(),
        AccountName = accountName,
        CurrentValue = currentValue,
        SuggestedValue = suggestedValue,
        SourceDescription = "BarclaysPlatinumVisa8003 — Aug 2026 statement",
    };

    [Fact]
    public void SuggestValuesCommand_Execute_LoadsSuggestionsWithDefaultIncludedState()
    {
        var (viewModel, service) = CreateViewModel();
        var zeroSnapshotId = Guid.NewGuid();
        var nonZeroSnapshotId = Guid.NewGuid();
        service.Suggestions = new InvestmentSnapshotSuggestionsDTO
        {
            Suggestions =
            [
                CreateSuggestion(zeroSnapshotId, "PlatinumVisa8003", currentValue: 0m, suggestedValue: 142.17m),
                CreateSuggestion(nonZeroSnapshotId, "ReservasPessoais", currentValue: 5400m, suggestedValue: 5612.30m),
            ],
            NotUpdated = [],
        };

        viewModel.SuggestValuesCommand.Execute(null);

        viewModel.IsSuggestPanelOpen.Should().BeTrue();
        viewModel.SuggestionRows.Single(r => r.SnapshotId == zeroSnapshotId).Included.Should().BeTrue();
        viewModel.SuggestionRows.Single(r => r.SnapshotId == nonZeroSnapshotId).Included.Should().BeFalse();
    }

    [Fact]
    public void SuggestValuesCommand_FetchFails_SetsSuggestionsErrorAndRetryReattempts()
    {
        var (viewModel, service) = CreateViewModel();
        service.ThrowOnGetSuggestions = new InvalidOperationException("Network down");

        viewModel.SuggestValuesCommand.Execute(null);

        viewModel.HasSuggestionsError.Should().BeTrue();
        viewModel.SuggestionsError.Should().Be("Network down");

        service.ThrowOnGetSuggestions = null;
        service.Suggestions = new InvestmentSnapshotSuggestionsDTO { Suggestions = [], NotUpdated = [] };
        viewModel.RetrySuggestionsFetchCommand.Execute(null);

        viewModel.HasSuggestionsError.Should().BeFalse();
        viewModel.ShowSuggestionsContent.Should().BeTrue();
    }

    [Fact]
    public async Task ApplySuggestionsCommand_AppliesOnlyCheckedRowsSequentially()
    {
        var (viewModel, service) = CreateViewModel();
        var included = Guid.NewGuid();
        var excluded = Guid.NewGuid();
        var today = DateTime.Today;
        service.Snapshots =
        [
            CreateSnapshot(today.Year, today.Month, "PlatinumVisa8003", true, 0m, id: included),
            CreateSnapshot(today.Year, today.Month, "ReservasPessoais", true, 5400m, id: excluded),
        ];
        service.Suggestions = new InvestmentSnapshotSuggestionsDTO
        {
            Suggestions =
            [
                CreateSuggestion(included, "PlatinumVisa8003", currentValue: 0m, suggestedValue: 142.17m),
                CreateSuggestion(excluded, "ReservasPessoais", currentValue: 5400m, suggestedValue: 5612.30m),
            ],
            NotUpdated = [],
        };
        viewModel.SuggestValuesCommand.Execute(null);

        viewModel.ApplySuggestionsCommand.Execute(null);
        await Task.Delay(50);

        service.UpdateRequests.Should().ContainSingle(r => r.Id == included);
        service.UpdateRequests.Should().NotContain(r => r.Id == excluded);
    }

    [Fact]
    public async Task ApplySuggestionsCommand_ContinuesPastFailedRow_ShowsCompletionSummary()
    {
        var (viewModel, service) = CreateViewModel();
        var willFail = Guid.NewGuid();
        var willSucceed = Guid.NewGuid();
        var today = DateTime.Today;
        service.Snapshots =
        [
            CreateSnapshot(today.Year, today.Month, "PlatinumVisa8003", true, 0m, id: willFail),
            CreateSnapshot(today.Year, today.Month, "ReservasPessoais", true, 0m, id: willSucceed),
        ];
        service.Suggestions = new InvestmentSnapshotSuggestionsDTO
        {
            Suggestions =
            [
                CreateSuggestion(willFail, "PlatinumVisa8003", currentValue: 0m, suggestedValue: 142.17m),
                CreateSuggestion(willSucceed, "ReservasPessoais", currentValue: 0m, suggestedValue: 100m),
            ],
            NotUpdated = [],
        };
        viewModel.SuggestValuesCommand.Execute(null);
        service.ThrowOnUpdateForId = willFail;

        viewModel.ApplySuggestionsCommand.Execute(null);
        await Task.Delay(50);

        viewModel.HasCompletedApply.Should().BeTrue();
        viewModel.SucceededCount.Should().Be(1);
        viewModel.FailedSuggestionRows.Should().ContainSingle(r => r.SnapshotId == willFail);
        viewModel.CompletionSummaryText.Should().Contain("Applied 1 of 2").And.Contain("PlatinumVisa8003");
    }

    [Fact]
    public async Task RetryFailedSuggestionsCommand_ResubmitsPriorValuesWithoutRefetching()
    {
        var (viewModel, service) = CreateViewModel();
        var willFailThenSucceed = Guid.NewGuid();
        var today = DateTime.Today;
        service.Snapshots = [CreateSnapshot(today.Year, today.Month, "PlatinumVisa8003", true, 0m, id: willFailThenSucceed)];
        service.Suggestions = new InvestmentSnapshotSuggestionsDTO
        {
            Suggestions = [CreateSuggestion(willFailThenSucceed, "PlatinumVisa8003", currentValue: 0m, suggestedValue: 142.17m)],
            NotUpdated = [],
        };
        viewModel.SuggestValuesCommand.Execute(null);
        service.ThrowOnUpdateForId = willFailThenSucceed;
        viewModel.ApplySuggestionsCommand.Execute(null);
        await Task.Delay(50);
        var callCountAfterFailedApply = service.GetSuggestionsForMonthCallCount;

        service.ThrowOnUpdateForId = null;
        viewModel.RetryFailedSuggestionsCommand.Execute(null);
        await Task.Delay(50);

        service.GetSuggestionsForMonthCallCount.Should().Be(callCountAfterFailedApply);
        service.UpdateRequests.Should().ContainSingle(r => r.Id == willFailThenSucceed && r.Request.Value == 142.17m);
        viewModel.IsSuggestPanelOpen.Should().BeFalse();
    }

    [Fact]
    public async Task SuggestValuesCommand_ClosesOpenEditForm_AndViceVersa()
    {
        var (viewModel, service) = CreateViewModel();
        var today = DateTime.Today;
        var snapshot = CreateSnapshot(today.Year, today.Month, "ISA", false, 1000m);
        service.Snapshots = [snapshot];
        await viewModel.RefreshAsync();
        viewModel.EditSnapshotCommand.Execute(viewModel.Snapshots.Single());
        viewModel.IsEditFormOpen.Should().BeTrue();

        viewModel.SuggestValuesCommand.Execute(null);

        viewModel.IsEditFormOpen.Should().BeFalse();
        viewModel.IsSuggestPanelOpen.Should().BeTrue();

        viewModel.EditSnapshotCommand.Execute(viewModel.Snapshots.Single());

        viewModel.IsSuggestPanelOpen.Should().BeFalse();
        viewModel.IsEditFormOpen.Should().BeTrue();
    }
}
