using System.Collections.ObjectModel;
using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class WithdrawalViewModelTests
{
    private static readonly Guid InvestimentoId = Guid.NewGuid();

    private static (WithdrawalViewModel ViewModel, StubReserveService Service) CreateViewModel(
        ObservableCollection<ReserveBucketDTO>? buckets = null, Func<string, bool>? confirm = null)
    {
        var service = new StubReserveService();
        var bucketList = buckets ?? new ObservableCollection<ReserveBucketDTO>
        {
            new() { Id = InvestimentoId, Name = "Investimento", IsActive = true, SplitPercentage = 100m },
        };
        var viewModel = new WithdrawalViewModel(service, bucketList, confirm ?? (_ => true), closeOtherForms: () => { }, refresh: () => Task.CompletedTask);
        return (viewModel, service);
    }

    [Fact]
    public async Task SubmitWithdrawal_ValidFormNoOverdraft_CallsServiceAndRefreshes()
    {
        var (viewModel, service) = CreateViewModel();
        viewModel.ShowWithdrawalFormCommand.Execute(null);
        viewModel.WithdrawalAmount = "50";
        viewModel.WithdrawalDescription = "Groceries";

        await viewModel.SubmitWithdrawalAsync();

        service.WithdrawalRequests.Should().ContainSingle();
        service.WithdrawalRequests[0].Confirmed.Should().BeFalse();
        viewModel.IsWithdrawalFormOpen.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitWithdrawal_Overdraft_ConfirmedTrue_ResubmitsWithConfirmedFlag()
    {
        var (viewModel, service) = CreateViewModel(confirm: _ => true);
        service.ThrowOverdraftOnUnconfirmedWithdrawal = true;
        viewModel.ShowWithdrawalFormCommand.Execute(null);
        viewModel.WithdrawalAmount = "5000";
        viewModel.WithdrawalDescription = "Big purchase";

        await viewModel.SubmitWithdrawalAsync();

        service.WithdrawalRequests.Should().HaveCount(2);
        service.WithdrawalRequests[0].Confirmed.Should().BeFalse();
        service.WithdrawalRequests[1].Confirmed.Should().BeTrue();
        viewModel.IsWithdrawalFormOpen.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitWithdrawal_Overdraft_ConfirmedFalse_KeepsFormOpenWithError()
    {
        var (viewModel, service) = CreateViewModel(confirm: _ => false);
        service.ThrowOverdraftOnUnconfirmedWithdrawal = true;
        service.OverdraftMessage = "This withdrawal exceeds Investimento's balance of 10.00.";
        viewModel.ShowWithdrawalFormCommand.Execute(null);
        viewModel.WithdrawalAmount = "5000";
        viewModel.WithdrawalDescription = "Big purchase";

        await viewModel.SubmitWithdrawalAsync();

        service.WithdrawalRequests.Should().ContainSingle();
        viewModel.IsWithdrawalFormOpen.Should().BeTrue();
        viewModel.WithdrawalSaveError.Should().Be(service.OverdraftMessage);
        viewModel.WithdrawalAmount.Should().Be("5000");
    }

    [Fact]
    public async Task SubmitWithdrawal_BackendRejects_KeepsFormOpenWithValuesAndShowsServerError()
    {
        var (viewModel, service) = CreateViewModel();
        service.ThrowOnWithdrawal = new InvalidOperationException("Unrecognized bucket.");
        viewModel.ShowWithdrawalFormCommand.Execute(null);
        viewModel.WithdrawalAmount = "50";
        viewModel.WithdrawalDescription = "Groceries";

        await viewModel.SubmitWithdrawalAsync();

        viewModel.IsWithdrawalFormOpen.Should().BeTrue();
        viewModel.WithdrawalSaveError.Should().Be("Unrecognized bucket.");
        viewModel.WithdrawalAmount.Should().Be("50");
    }

    [Fact]
    public void ShowWithdrawalForm_DefaultsBucketToFirstActive_SkippingALeadingInactiveOne()
    {
        var buckets = new ObservableCollection<ReserveBucketDTO>
        {
            new() { Id = Guid.NewGuid(), Name = "Retired", IsActive = false, SplitPercentage = 0m },
            new() { Id = InvestimentoId, Name = "Investimento", IsActive = true, SplitPercentage = 100m },
        };
        var (viewModel, _) = CreateViewModel(buckets);

        viewModel.ShowWithdrawalFormCommand.Execute(null);

        viewModel.WithdrawalBucketId.Should().Be(InvestimentoId);
    }

    [Fact]
    public async Task SubmitWithdrawal_NoBucketSelected_BlocksSaveWithoutServiceCall()
    {
        var (viewModel, service) = CreateViewModel(buckets: []);
        viewModel.ShowWithdrawalFormCommand.Execute(null);
        viewModel.WithdrawalAmount = "30";
        viewModel.WithdrawalDescription = "Groceries top-up";

        await viewModel.SubmitWithdrawalAsync();

        service.WithdrawalRequests.Should().BeEmpty();
        viewModel.WithdrawalSaveError.Should().Be("Bucket is required.");
        viewModel.BucketFieldError.Should().Be(viewModel.WithdrawalSaveError);
    }

    [Fact]
    public async Task DateFieldError_MissingDate_MatchesSaveError()
    {
        var (viewModel, service) = CreateViewModel();
        viewModel.ShowWithdrawalFormCommand.Execute(null);
        viewModel.WithdrawalDate = null;
        viewModel.WithdrawalAmount = "30";
        viewModel.WithdrawalDescription = "Groceries";

        await viewModel.SubmitWithdrawalAsync();

        service.WithdrawalRequests.Should().BeEmpty();
        viewModel.DateFieldError.Should().Be(viewModel.WithdrawalSaveError);
        viewModel.BucketFieldError.Should().BeNull();
    }

    [Fact]
    public async Task DescriptionFieldError_MissingDescription_MatchesSaveError()
    {
        var (viewModel, service) = CreateViewModel();
        viewModel.ShowWithdrawalFormCommand.Execute(null);
        viewModel.WithdrawalAmount = "30";
        viewModel.WithdrawalDescription = "";

        await viewModel.SubmitWithdrawalAsync();

        service.WithdrawalRequests.Should().BeEmpty();
        viewModel.DescriptionFieldError.Should().Be(viewModel.WithdrawalSaveError);
    }

    [Fact]
    public async Task AmountFieldError_ZeroAmount_MatchesSaveError()
    {
        var (viewModel, service) = CreateViewModel();
        viewModel.ShowWithdrawalFormCommand.Execute(null);
        viewModel.WithdrawalAmount = "0";
        viewModel.WithdrawalDescription = "Groceries";

        await viewModel.SubmitWithdrawalAsync();

        service.WithdrawalRequests.Should().BeEmpty();
        viewModel.AmountFieldError.Should().Be(viewModel.WithdrawalSaveError);
    }

    [Fact]
    public async Task FieldErrors_ClearAfterSuccessfulSave()
    {
        var (viewModel, _) = CreateViewModel();
        viewModel.ShowWithdrawalFormCommand.Execute(null);
        viewModel.WithdrawalAmount = "30";
        viewModel.WithdrawalDescription = "";
        await viewModel.SubmitWithdrawalAsync();
        viewModel.DescriptionFieldError.Should().NotBeNull();

        viewModel.WithdrawalDescription = "Groceries";
        await viewModel.SubmitWithdrawalAsync();

        viewModel.DescriptionFieldError.Should().BeNull();
    }

    [Fact]
    public async Task ShowWithdrawalForm_AfterSuccessfulSubmit_PersistsDateAndBucket()
    {
        var otherBucketId = Guid.NewGuid();
        var buckets = new ObservableCollection<ReserveBucketDTO>
        {
            new() { Id = InvestimentoId, Name = "Investimento", IsActive = true, SplitPercentage = 50m },
            new() { Id = otherBucketId, Name = "HouseTreats", IsActive = true, SplitPercentage = 50m },
        };
        var (viewModel, _) = CreateViewModel(buckets);
        viewModel.ShowWithdrawalFormCommand.Execute(null);
        var usedDate = DateTime.Today.AddDays(-4);
        viewModel.WithdrawalBucketId = otherBucketId;
        viewModel.WithdrawalDate = usedDate;
        viewModel.WithdrawalAmount = "30";
        viewModel.WithdrawalDescription = "Groceries";

        await viewModel.SubmitWithdrawalAsync();

        viewModel.ShowWithdrawalFormCommand.Execute(null);

        viewModel.WithdrawalDate.Should().Be(usedDate);
        viewModel.WithdrawalBucketId.Should().Be(otherBucketId);
        viewModel.WithdrawalAmount.Should().BeEmpty();
        viewModel.WithdrawalDescription.Should().BeEmpty();
    }

    private static readonly Guid ArianaId = Guid.NewGuid();
    private static readonly Guid ChaseId = Guid.NewGuid();
    private static readonly Guid ArianaCategoryId = Guid.NewGuid();
    private static readonly Guid SaudeCategoryId = Guid.NewGuid();

    private static BankDTO Bank(Guid id, string name) => new()
    {
        Id = id,
        Name = name,
        RoundUpEnabled = false,
        OpeningBalance = 0m,
        OpeningBalanceDate = new DateOnly(2026, 1, 1),
        HasReferences = false,
    };

    private static CategoryDTO Category(Guid id, string name, bool active = true, bool isInvestment = false) => new()
    {
        Id = id,
        Name = name,
        Active = active,
        IsInvestment = isInvestment,
        IsTithe = false,
        HasReferences = false,
    };

    private static (WithdrawalViewModel ViewModel, StubReserveService Service) CreateBankViewModel(
        Func<string, bool>? confirm = null, Func<Task>? refresh = null)
    {
        var service = new StubReserveService();
        var buckets = new ObservableCollection<ReserveBucketDTO>
        {
            new() { Id = InvestimentoId, Name = "Investimento", IsActive = true, SplitPercentage = 50m },
            new() { Id = ArianaId, Name = "Ariana", IsActive = true, SplitPercentage = 50m },
        };
        var viewModel = new WithdrawalViewModel(
            service, buckets, confirm ?? (_ => true), closeOtherForms: () => { }, refresh: refresh ?? (() => Task.CompletedTask));
        viewModel.LoadReferenceData(
            [Bank(ChaseId, "Chase")],
            [
                Category(ArianaCategoryId, "Ariana"),
                Category(SaudeCategoryId, "Saude"),
                Category(Guid.NewGuid(), "Investimento", isInvestment: true),
                Category(Guid.NewGuid(), "Reserva"),
                Category(Guid.NewGuid(), "Old", active: false),
            ]);
        viewModel.ShowWithdrawalFormCommand.Execute(null);
        viewModel.WithdrawalAmount = "20";
        viewModel.WithdrawalDescription = "Car service";
        return (viewModel, service);
    }

    [Fact]
    public void LoadReferenceData_ListsDirectOptionThenBanks_AndEligibleCategories()
    {
        var (viewModel, _) = CreateBankViewModel();

        viewModel.BankOptions.Select(o => o.Name).Should().Equal("No bank (direct)", "Chase");
        viewModel.CategoryOptions.Select(c => c.Name).Should().Equal("Ariana", "Saude");
        viewModel.SelectedBankOption.Should().Be(WithdrawalBankOption.Direct);
        viewModel.IsBankSelected.Should().BeFalse();
    }

    [Fact]
    public void LoadReferenceData_NeverClearsTheBankOptionsSoTheDirectSelectionSurvives()
    {
        var (viewModel, _) = CreateBankViewModel();
        var collectionActions = new List<System.Collections.Specialized.NotifyCollectionChangedAction>();
        viewModel.BankOptions.CollectionChanged += (_, e) => collectionActions.Add(e.Action);

        viewModel.LoadReferenceData([Bank(ChaseId, "Chase"), Bank(Guid.NewGuid(), "Barclays")], []);

        collectionActions.Should().NotContain(System.Collections.Specialized.NotifyCollectionChangedAction.Reset);
        viewModel.BankOptions[0].Should().BeSameAs(WithdrawalBankOption.Direct);
        viewModel.BankOptions.Select(o => o.Name).Should().Equal("No bank (direct)", "Chase", "Barclays");
        viewModel.SelectedBankOption.Should().BeSameAs(WithdrawalBankOption.Direct);
    }

    [Fact]
    public void WithdrawalBankOption_ToString_IsTheDisplayName()
    {
        new WithdrawalBankOption(Guid.NewGuid(), "Chase").ToString().Should().Be("Chase");
        WithdrawalBankOption.Direct.ToString().Should().Be("No bank (direct)");
    }

    [Fact]
    public void SelectingBank_ShowsCategoryAndDefaultsToBucketNamedCategory()
    {
        var (viewModel, _) = CreateBankViewModel();
        viewModel.WithdrawalBucketId = ArianaId;

        viewModel.SelectedBankOption = viewModel.BankOptions[1];

        viewModel.IsBankSelected.Should().BeTrue();
        viewModel.WithdrawalExpenseCategoryId.Should().Be(ArianaCategoryId);
    }

    [Fact]
    public void SelectingBank_NoCategoryMatchesTheBucket_LeavesCategoryEmpty()
    {
        var (viewModel, _) = CreateBankViewModel();

        viewModel.SelectedBankOption = viewModel.BankOptions[1];

        viewModel.WithdrawalExpenseCategoryId.Should().BeNull();
    }

    [Fact]
    public void BucketChange_UpdatesDefaultCategoryUntilTheUserChoosesOne()
    {
        var (viewModel, _) = CreateBankViewModel();
        viewModel.SelectedBankOption = viewModel.BankOptions[1];

        viewModel.WithdrawalBucketId = ArianaId;
        viewModel.WithdrawalExpenseCategoryId.Should().Be(ArianaCategoryId);

        viewModel.WithdrawalExpenseCategoryId = SaudeCategoryId;
        viewModel.WithdrawalBucketId = InvestimentoId;
        viewModel.WithdrawalExpenseCategoryId.Should().Be(SaudeCategoryId);
    }

    [Fact]
    public async Task ClearingBank_DropsExplicitCategory_AndPostsNullIds()
    {
        var (viewModel, service) = CreateBankViewModel();
        viewModel.SelectedBankOption = viewModel.BankOptions[1];
        viewModel.WithdrawalExpenseCategoryId = SaudeCategoryId;

        viewModel.SelectedBankOption = WithdrawalBankOption.Direct;
        await viewModel.SubmitWithdrawalAsync();

        viewModel.IsBankSelected.Should().BeFalse();
        service.WithdrawalRequests.Should().ContainSingle();
        service.WithdrawalRequests[0].PaymentSourceBankId.Should().BeNull();
        service.WithdrawalRequests[0].ExpenseCategoryId.Should().BeNull();
    }

    [Fact]
    public async Task SubmitWithdrawal_BankWithoutCategory_ShowsCategoryFieldError_AndPostsNothing()
    {
        var (viewModel, service) = CreateBankViewModel();
        viewModel.SelectedBankOption = viewModel.BankOptions[1];

        await viewModel.SubmitWithdrawalAsync();

        service.WithdrawalRequests.Should().BeEmpty();
        viewModel.ExpenseCategoryFieldError.Should().Be("Category is required when a bank is selected.");
        viewModel.WithdrawalGeneralSaveError.Should().BeNull();
    }

    [Fact]
    public async Task SubmitWithdrawal_BankAndCategory_PostsBothIds_ClosesFormAndRefreshes()
    {
        var refreshCount = 0;
        var (viewModel, service) = CreateBankViewModel(refresh: () =>
        {
            refreshCount++;
            return Task.CompletedTask;
        });
        viewModel.SelectedBankOption = viewModel.BankOptions[1];
        viewModel.WithdrawalExpenseCategoryId = SaudeCategoryId;

        await viewModel.SubmitWithdrawalAsync();

        service.WithdrawalRequests.Should().ContainSingle();
        service.WithdrawalRequests[0].PaymentSourceBankId.Should().Be(ChaseId);
        service.WithdrawalRequests[0].ExpenseCategoryId.Should().Be(SaudeCategoryId);
        viewModel.IsWithdrawalFormOpen.Should().BeFalse();
        refreshCount.Should().Be(1);
    }

    [Fact]
    public async Task SubmitWithdrawal_BankPathOverdraftConfirmed_ResubmitsWithBothIds()
    {
        var (viewModel, service) = CreateBankViewModel(confirm: _ => true);
        service.ThrowOverdraftOnUnconfirmedWithdrawal = true;
        viewModel.SelectedBankOption = viewModel.BankOptions[1];
        viewModel.WithdrawalExpenseCategoryId = SaudeCategoryId;

        await viewModel.SubmitWithdrawalAsync();

        service.WithdrawalRequests.Should().HaveCount(2);
        service.WithdrawalRequests[1].Confirmed.Should().BeTrue();
        service.WithdrawalRequests[1].PaymentSourceBankId.Should().Be(ChaseId);
        service.WithdrawalRequests[1].ExpenseCategoryId.Should().Be(SaudeCategoryId);
    }

    [Fact]
    public async Task SubmitWithdrawal_BankPathBackendRejects_KeepsFormOpenWithValuesAndShowsServerError()
    {
        var (viewModel, service) = CreateBankViewModel();
        service.ThrowOnWithdrawal = new InvalidOperationException("Category 'Saude' is inactive and cannot be used for new entries.");
        viewModel.SelectedBankOption = viewModel.BankOptions[1];
        viewModel.WithdrawalExpenseCategoryId = SaudeCategoryId;

        await viewModel.SubmitWithdrawalAsync();

        viewModel.IsWithdrawalFormOpen.Should().BeTrue();
        viewModel.SelectedBankOption.Id.Should().Be(ChaseId);
        viewModel.WithdrawalExpenseCategoryId.Should().Be(SaudeCategoryId);
        viewModel.WithdrawalAmount.Should().Be("20");
        viewModel.WithdrawalGeneralSaveError.Should().Be(service.ThrowOnWithdrawal.Message);
        viewModel.ExpenseCategoryFieldError.Should().BeNull();
    }

    [Fact]
    public void ShowWithdrawalForm_ResetsBankAndCategory()
    {
        var (viewModel, _) = CreateBankViewModel();
        viewModel.SelectedBankOption = viewModel.BankOptions[1];
        viewModel.WithdrawalExpenseCategoryId = SaudeCategoryId;

        viewModel.ShowWithdrawalFormCommand.Execute(null);

        viewModel.SelectedBankOption.Should().Be(WithdrawalBankOption.Direct);
        viewModel.WithdrawalExpenseCategoryId.Should().BeNull();
    }

    [Fact]
    public async Task IsWithdrawalFormEditable_IsFalseWhileSubmitting()
    {
        bool? editableDuringSave = null;
        WithdrawalViewModel? viewModel = null;
        (viewModel, _) = CreateBankViewModel(refresh: () =>
        {
            editableDuringSave = viewModel!.IsWithdrawalFormEditable;
            return Task.CompletedTask;
        });

        await viewModel.SubmitWithdrawalAsync();

        editableDuringSave.Should().BeFalse();
        viewModel.IsWithdrawalFormEditable.Should().BeTrue();
    }
}
