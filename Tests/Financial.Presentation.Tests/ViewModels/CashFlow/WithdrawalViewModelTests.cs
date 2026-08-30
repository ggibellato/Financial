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
}
