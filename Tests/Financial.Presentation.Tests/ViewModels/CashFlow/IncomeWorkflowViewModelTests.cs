using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels;
using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class IncomeWorkflowViewModelTests
{
    /// <summary>Unchecks every filter option except the given values, mirroring how a user would
    /// narrow the header checklist down to a subset (see BankOperationsWorkflowViewModelTests.SelectOnly).</summary>
    private static void SelectOnly(ColumnFilterViewModel<IncomeDTO> filter, params string[] values)
    {
        foreach (var option in filter.Options)
        {
            option.IsChecked = values.Contains(option.Value);
        }
    }

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

    private static readonly List<BankDTO> DefaultBanks =
    [
        new() { Id = BarclaysId, Name = "Barclays", RoundUpEnabled = true, OpeningBalance = 0, OpeningBalanceDate = DateOnly.FromDateTime(DateTime.Today) },
        new() { Id = ChaseId, Name = "Chase", RoundUpEnabled = false, OpeningBalance = 0, OpeningBalanceDate = DateOnly.FromDateTime(DateTime.Today) },
    ];

    private static (IncomeWorkflowViewModel ViewModel, StubIncomeService Service) CreateViewModel(
        bool confirmDeletes = true, Func<Task>? refresh = null)
    {
        var incomeService = new StubIncomeService();
        var viewModel = new IncomeWorkflowViewModel(incomeService, confirm: _ => confirmDeletes, refresh ?? (() => Task.CompletedTask));
        return (viewModel, incomeService);
    }

    [Fact]
    public void IncomeSourceOptions_MatchesActiveFetchedSourcesInDisplayOrder()
    {
        var (viewModel, _) = CreateViewModel();

        viewModel.ApplyRefresh([],
        [
            new IncomeSourceDTO { Id = Guid.NewGuid(), Name = "DividendoJuros", IsActive = true, Group = "DividendoJuros", AutoSplitToReserve = false },
            new IncomeSourceDTO { Id = Guid.NewGuid(), Name = "Lottery", IsActive = true, Group = "NonReportable", AutoSplitToReserve = false },
            new IncomeSourceDTO { Id = Guid.NewGuid(), Name = "Ariana", IsActive = true, Group = "Salary", AutoSplitToReserve = true },
            new IncomeSourceDTO { Id = Guid.NewGuid(), Name = "Gleison", IsActive = true, Group = "Salary", AutoSplitToReserve = false },
        ], []);

        viewModel.IncomeSourceOptions.Select(s => s.Name).Should().Equal("Gleison", "Ariana", "Lottery", "DividendoJuros");
    }

    [Fact]
    public void IncomeSourceOptions_ExcludesInactiveSources()
    {
        var (viewModel, _) = CreateViewModel();

        viewModel.ApplyRefresh([],
        [
            new IncomeSourceDTO { Id = Guid.NewGuid(), Name = "Gleison", IsActive = true, Group = "Salary", AutoSplitToReserve = false },
            new IncomeSourceDTO { Id = Guid.NewGuid(), Name = "RetiredSource", IsActive = false, Group = "NonReportable", AutoSplitToReserve = false },
        ], []);

        viewModel.IncomeSourceOptions.Select(s => s.Name).Should().Equal("Gleison");
    }

    [Fact]
    public void ShowCreateIncomeForm_DefaultsSourceToFirstActiveOption()
    {
        var (viewModel, _) = CreateViewModel();
        viewModel.ApplyRefresh([], DefaultIncomeSources, DefaultBanks);

        viewModel.ShowCreateIncomeFormCommand.Execute(null);

        viewModel.IncomeFormSource.Should().Be(GleisonSourceId);
    }

    [Fact]
    public void ShowCreateIncomeForm_WithNoActiveSources_DefaultsToEmpty()
    {
        var (viewModel, _) = CreateViewModel();
        viewModel.ApplyRefresh([], [], DefaultBanks);

        viewModel.ShowCreateIncomeFormCommand.Execute(null);

        viewModel.IncomeFormSource.Should().BeNull();
    }

    [Fact]
    public void AddIncome_GleisonSource_ShowsGrossValueField()
    {
        var (viewModel, _) = CreateViewModel();
        viewModel.ApplyRefresh([], DefaultIncomeSources, DefaultBanks);
        viewModel.ShowCreateIncomeFormCommand.Execute(null);

        viewModel.IncomeFormSource = GleisonSourceId;

        viewModel.ShowIncomeGrossValueField.Should().BeTrue();
    }

    [Fact]
    public void AddIncome_LotterySource_HidesGrossValueField()
    {
        var (viewModel, _) = CreateViewModel();
        viewModel.ShowCreateIncomeFormCommand.Execute(null);

        viewModel.IncomeFormSource = LotterySourceId;

        viewModel.ShowIncomeGrossValueField.Should().BeFalse();
    }

    [Fact]
    public async Task AddIncome_ValidForm_CallsServiceAndRefreshes()
    {
        var (viewModel, incomes) = CreateViewModel();
        viewModel.ApplyRefresh([], DefaultIncomeSources, DefaultBanks);
        viewModel.ShowCreateIncomeFormCommand.Execute(null);
        viewModel.IncomeFormDate = DateTime.Today;
        viewModel.IncomeFormSource = LotterySourceId;
        viewModel.IncomeFormNetValue = "50";
        viewModel.IncomeFormBank = BarclaysId;

        await viewModel.SaveIncomeAsync();

        incomes.LastCreateRequest.Should().NotBeNull();
        incomes.LastCreateRequest!.NetValue.Should().Be(50m);
        incomes.LastCreateRequest.GrossValue.Should().BeNull();
        viewModel.IsIncomeFormOpen.Should().BeFalse();
    }

    [Fact]
    public async Task AddIncome_WithDescription_SendsDescriptionToService()
    {
        var (viewModel, incomes) = CreateViewModel();
        viewModel.ApplyRefresh([], DefaultIncomeSources, DefaultBanks);
        viewModel.ShowCreateIncomeFormCommand.Execute(null);
        viewModel.IncomeFormDate = DateTime.Today;
        viewModel.IncomeFormSource = LotterySourceId;
        viewModel.IncomeFormNetValue = "50";
        viewModel.IncomeFormBank = BarclaysId;
        viewModel.IncomeFormDescription = "Chip ISA dividend";

        await viewModel.SaveIncomeAsync();

        incomes.LastCreateRequest.Should().NotBeNull();
        incomes.LastCreateRequest!.Description.Should().Be("Chip ISA dividend");
    }

    [Fact]
    public void ShowEditIncomeForm_PopulatesDescription()
    {
        var (viewModel, _) = CreateViewModel();
        var income = new IncomeDTO
        {
            Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), IncomeSourceId = Guid.NewGuid(), IncomeSourceName = "Lottery",
            NetValue = 50m, BankId = BarclaysId, BankName = "Barclays", Description = "Chip ISA dividend",
            SplitToReserve = false,
        };
        viewModel.ApplyRefresh([], DefaultIncomeSources, DefaultBanks);

        viewModel.EditIncomeCommand.Execute(income);

        viewModel.IncomeFormDescription.Should().Be("Chip ISA dividend");
    }

    [Fact]
    public void ShowIncomeSplitField_ForEligibleSource_IsTrue()
    {
        var (viewModel, _) = CreateViewModel();
        viewModel.ApplyRefresh([], DefaultIncomeSources, DefaultBanks);
        viewModel.ShowCreateIncomeFormCommand.Execute(null);

        viewModel.IncomeFormSource = ArianaSourceId;

        viewModel.ShowIncomeSplitField.Should().BeTrue();
    }

    [Fact]
    public void ShowIncomeSplitField_ForIneligibleSource_IsFalse()
    {
        var (viewModel, _) = CreateViewModel();
        viewModel.ApplyRefresh([], DefaultIncomeSources, DefaultBanks);
        viewModel.ShowCreateIncomeFormCommand.Execute(null);

        viewModel.IncomeFormSource = GleisonSourceId;

        viewModel.ShowIncomeSplitField.Should().BeFalse();
    }

    [Fact]
    public void ShowCreateIncomeForm_DefaultsIncomeFormSplitToReserve_FromInitialSourceEligibility()
    {
        var (viewModel, _) = CreateViewModel();
        viewModel.ApplyRefresh([], DefaultIncomeSources, DefaultBanks);

        viewModel.ShowCreateIncomeFormCommand.Execute(null);

        viewModel.IncomeFormSource.Should().Be(GleisonSourceId); // not eligible
        viewModel.IncomeFormSplitToReserve.Should().BeFalse();
    }

    [Fact]
    public void SettingIncomeFormSource_ToEligibleSource_SetsIncomeFormSplitToReserveTrue()
    {
        var (viewModel, _) = CreateViewModel();
        viewModel.ApplyRefresh([], DefaultIncomeSources, DefaultBanks);
        viewModel.ShowCreateIncomeFormCommand.Execute(null);

        viewModel.IncomeFormSource = ArianaSourceId;

        viewModel.IncomeFormSplitToReserve.Should().BeTrue();
    }

    [Fact]
    public void SettingIncomeFormSource_BackToIneligibleSource_SetsIncomeFormSplitToReserveFalse()
    {
        var (viewModel, _) = CreateViewModel();
        viewModel.ApplyRefresh([], DefaultIncomeSources, DefaultBanks);
        viewModel.ShowCreateIncomeFormCommand.Execute(null);
        viewModel.IncomeFormSource = ArianaSourceId;

        viewModel.IncomeFormSource = GleisonSourceId;

        viewModel.IncomeFormSplitToReserve.Should().BeFalse();
    }

    [Fact]
    public void ShowEditIncomeForm_PopulatesIncomeFormSplitToReserve_FromIncome()
    {
        var (viewModel, _) = CreateViewModel();
        var income = new IncomeDTO
        {
            Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), IncomeSourceId = ArianaSourceId, IncomeSourceName = "Ariana",
            NetValue = 2450m, BankId = BarclaysId, BankName = "Barclays", Description = null,
            SplitToReserve = true,
        };
        viewModel.ApplyRefresh([], DefaultIncomeSources, DefaultBanks);

        viewModel.EditIncomeCommand.Execute(income);

        viewModel.IncomeFormSplitToReserve.Should().BeTrue();
    }

    [Fact]
    public async Task SaveIncomeAsync_WithSplitChecked_SendsSplitToReserveTrue()
    {
        var (viewModel, incomes) = CreateViewModel();
        viewModel.ApplyRefresh([], DefaultIncomeSources, DefaultBanks);
        viewModel.ShowCreateIncomeFormCommand.Execute(null);
        viewModel.IncomeFormDate = DateTime.Today;
        viewModel.IncomeFormSource = ArianaSourceId;
        viewModel.IncomeFormNetValue = "2450";
        viewModel.IncomeFormBank = BarclaysId;

        await viewModel.SaveIncomeAsync();

        incomes.LastCreateRequest.Should().NotBeNull();
        incomes.LastCreateRequest!.SplitToReserve.Should().BeTrue();
    }

    [Fact]
    public async Task SaveIncomeAsync_WhenResponseSplitToReserveTrue_SetsConfirmationMessage()
    {
        var (viewModel, _) = CreateViewModel();
        viewModel.ApplyRefresh([], DefaultIncomeSources, DefaultBanks);
        viewModel.ShowCreateIncomeFormCommand.Execute(null);
        viewModel.IncomeFormDate = DateTime.Today;
        viewModel.IncomeFormSource = ArianaSourceId;
        viewModel.IncomeFormNetValue = "2450";
        viewModel.IncomeFormBank = BarclaysId;

        var saveTask = viewModel.SaveIncomeAsync();
        // The confirmation message is set before the hide-delay awaits, so it's observable
        // immediately once the save itself (not the delay) has completed.
        await Task.Delay(50);

        viewModel.IncomeSplitConfirmationMessage.Should().Be("Income saved and split to reserve");

        await saveTask;
    }

    [Fact]
    public async Task SaveIncomeAsync_WhenResponseSplitToReserveFalse_LeavesConfirmationMessageNull()
    {
        var (viewModel, incomes) = CreateViewModel();
        viewModel.ApplyRefresh([], DefaultIncomeSources, DefaultBanks);
        viewModel.ShowCreateIncomeFormCommand.Execute(null);
        viewModel.IncomeFormDate = DateTime.Today;
        viewModel.IncomeFormSource = LotterySourceId;
        viewModel.IncomeFormNetValue = "50";
        viewModel.IncomeFormBank = BarclaysId;

        await viewModel.SaveIncomeAsync();

        incomes.LastCreateRequest!.SplitToReserve.Should().BeFalse();
        viewModel.IncomeSplitConfirmationMessage.Should().BeNull();
    }

    [Fact]
    public async Task EditIncome_ValidForm_CallsUpdateServiceAndRefreshes()
    {
        var (viewModel, incomes) = CreateViewModel();
        var income = new IncomeDTO
        {
            Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), IncomeSourceId = Guid.NewGuid(), IncomeSourceName = "Lottery",
            NetValue = 50m, BankId = BarclaysId, BankName = "Barclays",
            SplitToReserve = false,
        };
        viewModel.ApplyRefresh([], DefaultIncomeSources, DefaultBanks);

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
        var (viewModel, incomes) = CreateViewModel();
        var income = new IncomeDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), IncomeSourceId = Guid.NewGuid(), IncomeSourceName = "Lottery", NetValue = 10m, BankId = BarclaysId, BankName = "Barclays", SplitToReserve = false };

        await viewModel.DeleteIncomeAsync(income);

        incomes.LastDeletedId.Should().Be(income.Id);
    }

    [Fact]
    public async Task DeleteIncome_ConfirmationDeclined_DoesNotCallService()
    {
        var (viewModel, incomes) = CreateViewModel(confirmDeletes: false);
        var income = new IncomeDTO { Id = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), IncomeSourceId = Guid.NewGuid(), IncomeSourceName = "Lottery", NetValue = 10m, BankId = BarclaysId, BankName = "Barclays", SplitToReserve = false };

        await viewModel.DeleteIncomeAsync(income);

        incomes.LastDeletedId.Should().BeNull();
    }

    [Fact]
    public async Task SaveIncome_WithoutBank_CallsServiceWithNullBankAndRefreshes()
    {
        var (viewModel, incomes) = CreateViewModel();
        viewModel.ApplyRefresh([], DefaultIncomeSources, DefaultBanks);
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
    public void ShowCreateIncomeForm_DefaultsBankToNone()
    {
        var (viewModel, _) = CreateViewModel();
        viewModel.ApplyRefresh([], DefaultIncomeSources, DefaultBanks);

        viewModel.ShowCreateIncomeFormCommand.Execute(null);

        viewModel.IncomeFormBank.Should().BeNull();
    }

    [Fact]
    public void ApplyRefresh_PopulatesIncomeBankOptionsWithANoneOptionFirst()
    {
        var (viewModel, _) = CreateViewModel();

        viewModel.ApplyRefresh([], [], DefaultBanks);

        viewModel.IncomeBankOptions.Should().HaveCount(3);
        viewModel.IncomeBankOptions[0].Id.Should().BeNull();
        viewModel.IncomeBankOptions[0].Name.Should().Be(IncomeWorkflowViewModel.NoBankOptionLabel);
        viewModel.IncomeBankOptions.Should().Contain(o => o.Id == BarclaysId && o.Name == "Barclays");
        viewModel.IncomeBankOptions.Should().Contain(o => o.Id == ChaseId && o.Name == "Chase");
    }

    [Fact]
    public async Task DateFieldError_MissingDate_MatchesSaveError()
    {
        var (viewModel, incomes) = CreateViewModel();
        viewModel.ApplyRefresh([], DefaultIncomeSources, DefaultBanks);
        viewModel.ShowCreateIncomeFormCommand.Execute(null);
        viewModel.IncomeFormDate = null;
        viewModel.IncomeFormSource = LotterySourceId;
        viewModel.IncomeFormNetValue = "50";

        await viewModel.SaveIncomeAsync();

        incomes.LastCreateRequest.Should().BeNull();
        viewModel.DateFieldError.Should().Be(viewModel.IncomeSaveError);
        viewModel.SourceFieldError.Should().BeNull();
    }

    [Fact]
    public async Task SourceFieldError_MissingSource_MatchesSaveError()
    {
        var (viewModel, incomes) = CreateViewModel();
        viewModel.ApplyRefresh([], DefaultIncomeSources, DefaultBanks);
        viewModel.ShowCreateIncomeFormCommand.Execute(null);
        viewModel.IncomeFormDate = DateTime.Today;
        viewModel.IncomeFormSource = null;
        viewModel.IncomeFormNetValue = "50";

        await viewModel.SaveIncomeAsync();

        incomes.LastCreateRequest.Should().BeNull();
        viewModel.SourceFieldError.Should().Be(viewModel.IncomeSaveError);
    }

    [Fact]
    public async Task NetValueFieldError_NonNumeric_MatchesSaveError()
    {
        var (viewModel, incomes) = CreateViewModel();
        viewModel.ApplyRefresh([], DefaultIncomeSources, DefaultBanks);
        viewModel.ShowCreateIncomeFormCommand.Execute(null);
        viewModel.IncomeFormDate = DateTime.Today;
        viewModel.IncomeFormSource = LotterySourceId;
        viewModel.IncomeFormNetValue = "abc";

        await viewModel.SaveIncomeAsync();

        incomes.LastCreateRequest.Should().BeNull();
        viewModel.NetValueFieldError.Should().Be(viewModel.IncomeSaveError);
    }

    [Fact]
    public async Task FieldErrors_ClearAfterSuccessfulSave()
    {
        var (viewModel, _) = CreateViewModel();
        viewModel.ApplyRefresh([], DefaultIncomeSources, DefaultBanks);
        viewModel.ShowCreateIncomeFormCommand.Execute(null);
        viewModel.IncomeFormDate = null;
        viewModel.IncomeFormSource = LotterySourceId;
        viewModel.IncomeFormNetValue = "50";
        await viewModel.SaveIncomeAsync();
        viewModel.DateFieldError.Should().NotBeNull();

        viewModel.IncomeFormDate = DateTime.Today;
        await viewModel.SaveIncomeAsync();

        viewModel.DateFieldError.Should().BeNull();
    }

    private static IncomeDTO MakeIncome(string description, string? bankName) => new()
    {
        Id = Guid.NewGuid(),
        Date = DateOnly.FromDateTime(DateTime.Today),
        IncomeSourceId = Guid.NewGuid(),
        IncomeSourceName = "Lottery",
        NetValue = 10m,
        BankId = bankName is null ? null : Guid.NewGuid(),
        BankName = bankName,
        Description = description,
        SplitToReserve = false,
    };

    [Fact]
    public void BankFilter_Refresh_ComputesAvailableValuesFromFullUnfilteredIncomes()
    {
        var (viewModel, _) = CreateViewModel();
        var barclaysIncome = MakeIncome("A", "Barclays");
        var chaseIncome = MakeIncome("B", "Chase");

        viewModel.ApplyRefresh([barclaysIncome, chaseIncome], [], DefaultBanks);

        viewModel.BankFilter.Options.Select(o => o.Value).Should().BeEquivalentTo(["Barclays", "Chase"]);
    }

    [Fact]
    public void BankFilter_UncheckingValue_ExcludesMatchingRowsFromFilteredIncomes()
    {
        var (viewModel, _) = CreateViewModel();
        var barclaysIncome = MakeIncome("A", "Barclays");
        var chaseIncome = MakeIncome("B", "Chase");
        viewModel.ApplyRefresh([barclaysIncome, chaseIncome], [], DefaultBanks);

        SelectOnly(viewModel.BankFilter, "Barclays");

        viewModel.FilteredIncomes.Should().ContainSingle().Which.Should().Be(barclaysIncome);
        viewModel.Incomes.Should().HaveCount(2);
    }

    [Fact]
    public void BankFilter_ToggleAll_RestoresFullFilteredIncomesList()
    {
        var (viewModel, _) = CreateViewModel();
        var barclaysIncome = MakeIncome("A", "Barclays");
        var chaseIncome = MakeIncome("B", "Chase");
        viewModel.ApplyRefresh([barclaysIncome, chaseIncome], [], DefaultBanks);
        SelectOnly(viewModel.BankFilter, "Barclays");

        viewModel.BankFilter.ToggleAllCommand.Execute(null);

        viewModel.FilteredIncomes.Count.Should().Be(viewModel.Incomes.Count);
    }
}
