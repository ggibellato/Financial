using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Presentation.App.ViewModels.Investment.Dashboard;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Investment.Dashboard;

public class UpcomingIncomeViewModelTests
{
    private static UpcomingIncomeDTO Entry(string assetName, int daysAhead, decimal amount = 10m) => new(
        assetName,
        "Chase",
        DateTime.Today.AddDays(daysAhead).AddMonths(-1),
        DateTime.Today.AddDays(daysAhead),
        amount);

    private static (UpcomingIncomeViewModel ViewModel, StubUpcomingIncomeService Service) CreateViewModel(
        params UpcomingIncomeDTO[] entries)
    {
        var service = new StubUpcomingIncomeService { Entries = entries };
        var viewModel = new UpcomingIncomeViewModel(service, new RecordingLogger<UpcomingIncomeViewModel>());
        return (viewModel, service);
    }

    [Fact]
    public void SelectedWindow_DefaultsToNinetyDays()
    {
        var (viewModel, _) = CreateViewModel();

        viewModel.SelectedWindow.Should().Be(UpcomingIncomeWindow.Days90);
        viewModel.SelectedWindowDays.Should().Be(90);
        viewModel.WindowOptions.Select(option => (option.Label, option.Value)).Should().Equal(
            ("30 days", UpcomingIncomeWindow.Days30),
            ("90 days", UpcomingIncomeWindow.Days90),
            ("180 days", UpcomingIncomeWindow.Days180));
        viewModel.WindowOptions.Single(option => option.IsSelected).Value.Should().Be(UpcomingIncomeWindow.Days90);
    }

    [Fact]
    public async Task LoadAsync_PopulatesTheEntriesInsideTheDefaultWindow()
    {
        var (viewModel, service) = CreateViewModel(Entry("VUSA", 10), Entry("VWRL", 60), Entry("BCIA11", 150));

        await viewModel.LoadAsync();

        service.GetUpcomingIncomeCallCount.Should().Be(1);
        viewModel.Entries.Select(entry => entry.AssetName).Should().Equal("VUSA", "VWRL");
        viewModel.IsLoading.Should().BeFalse();
        viewModel.ShowContent.Should().BeTrue();
        viewModel.ShowEntries.Should().BeTrue();
        viewModel.ShowEmptyState.Should().BeFalse();
    }

    [Fact]
    public async Task ChangingTheWindow_RefiltersTheAlreadyFetchedListWithoutCallingTheServiceAgain()
    {
        var (viewModel, service) = CreateViewModel(Entry("VUSA", 10), Entry("VWRL", 60), Entry("BCIA11", 150));
        await viewModel.LoadAsync();

        viewModel.SelectedWindow = UpcomingIncomeWindow.Days30;
        viewModel.Entries.Select(entry => entry.AssetName).Should().Equal("VUSA");

        viewModel.SelectedWindow = UpcomingIncomeWindow.Days180;
        viewModel.Entries.Select(entry => entry.AssetName).Should().Equal("VUSA", "VWRL", "BCIA11");

        service.GetUpcomingIncomeCallCount.Should().Be(1);
    }

    [Fact]
    public async Task ChangingTheWindow_LeavesTheProjectedDatesAndTheBackendsSortOrderUntouched()
    {
        var third = Entry("BCIA11", 150);
        var first = Entry("VUSA", 10);
        var second = Entry("VWRL", 60);
        var (viewModel, _) = CreateViewModel(first, second, third);
        await viewModel.LoadAsync();

        viewModel.SelectedWindow = UpcomingIncomeWindow.Days180;

        viewModel.Entries.Should().Equal(first, second, third);
        viewModel.Entries.Select(entry => entry.ProjectedNextDate).Should().Equal(
            first.ProjectedNextDate, second.ProjectedNextDate, third.ProjectedNextDate);
    }

    [Fact]
    public async Task SelectWindowCommand_SelectsTheChosenChipAndDeselectsTheRest()
    {
        var (viewModel, _) = CreateViewModel(Entry("VUSA", 10));
        await viewModel.LoadAsync();

        viewModel.SelectWindowCommand.Execute(
            viewModel.WindowOptions.Single(option => option.Value == UpcomingIncomeWindow.Days30));

        viewModel.SelectedWindow.Should().Be(UpcomingIncomeWindow.Days30);
        viewModel.WindowOptions.Where(option => option.IsSelected).Should().ContainSingle()
            .Which.Value.Should().Be(UpcomingIncomeWindow.Days30);
    }

    [Fact]
    public async Task AProjectionAlreadyInThePast_StaysVisibleInEveryWindow()
    {
        var (viewModel, _) = CreateViewModel(Entry("OVERDUE", -40));
        await viewModel.LoadAsync();

        viewModel.Entries.Should().ContainSingle();

        viewModel.SelectedWindow = UpcomingIncomeWindow.Days30;

        viewModel.Entries.Should().ContainSingle();
    }

    [Fact]
    public async Task NoEntryInsideTheWindow_ShowsTheEmptyStateWordedWithTheSelectedDayCount()
    {
        var (viewModel, _) = CreateViewModel(Entry("BCIA11", 150));

        await viewModel.LoadAsync();

        viewModel.EmptyStateText.Should().Be("No upcoming payments detected in the next 90 days");
        viewModel.ShowEmptyState.Should().BeTrue();
        viewModel.ShowEntries.Should().BeFalse();

        viewModel.SelectedWindow = UpcomingIncomeWindow.Days180;

        viewModel.ShowEmptyState.Should().BeFalse();
        viewModel.EmptyStateText.Should().Be("No upcoming payments detected in the next 180 days");
    }

    [Fact]
    public async Task AFailedLoad_ReportsTheErrorAndKeepsTheContentHidden()
    {
        var service = new StubUpcomingIncomeService { ThrowOnGetUpcomingIncome = new InvalidOperationException("boom") };
        var viewModel = new UpcomingIncomeViewModel(service, new RecordingLogger<UpcomingIncomeViewModel>());

        await viewModel.LoadAsync();

        viewModel.HasError.Should().BeTrue();
        viewModel.ErrorMessage.Should().Be("boom");
        viewModel.ShowContent.Should().BeFalse();
        viewModel.ShowEntries.Should().BeFalse();
        viewModel.ShowEmptyState.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshCommand_ClearsAnEarlierErrorAndReloads()
    {
        var service = new StubUpcomingIncomeService { ThrowOnGetUpcomingIncome = new InvalidOperationException("boom") };
        var viewModel = new UpcomingIncomeViewModel(service, new RecordingLogger<UpcomingIncomeViewModel>());
        await viewModel.LoadAsync();
        viewModel.HasError.Should().BeTrue();

        service.ThrowOnGetUpcomingIncome = null;
        service.Entries = [Entry("VUSA", 10)];
        viewModel.RefreshCommand.Execute(null);

        viewModel.HasError.Should().BeFalse();
        viewModel.Entries.Should().ContainSingle();
        service.GetUpcomingIncomeCallCount.Should().Be(2);
    }

    [Fact]
    public void Constructor_RejectsAMissingService()
    {
        var act = () => new UpcomingIncomeViewModel(null!, new RecordingLogger<UpcomingIncomeViewModel>());

        act.Should().Throw<ArgumentNullException>();
    }
}

internal sealed class StubUpcomingIncomeService : IUpcomingIncomeService
{
    public IReadOnlyList<UpcomingIncomeDTO> Entries { get; set; } = [];

    public Exception? ThrowOnGetUpcomingIncome { get; set; }

    public int GetUpcomingIncomeCallCount { get; private set; }

    public IReadOnlyList<UpcomingIncomeDTO> GetUpcomingIncome()
    {
        GetUpcomingIncomeCallCount++;

        if (ThrowOnGetUpcomingIncome is not null)
        {
            throw ThrowOnGetUpcomingIncome;
        }

        return Entries;
    }
}
