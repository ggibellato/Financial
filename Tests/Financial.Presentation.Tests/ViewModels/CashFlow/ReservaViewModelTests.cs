using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.CashFlow;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class ReservaViewModelTests
{
    private static readonly List<ReserveBucketDTO> DefaultBuckets =
    [
        new() { Id = Guid.NewGuid(), Name = "Investimento", IsActive = true, SplitPercentage = 33.33m },
        new() { Id = Guid.NewGuid(), Name = "HouseTreats", IsActive = true, SplitPercentage = 33.33m },
        new() { Id = Guid.NewGuid(), Name = "Ariana", IsActive = true, SplitPercentage = 16.67m },
        new() { Id = Guid.NewGuid(), Name = "Gleison", IsActive = true, SplitPercentage = 16.67m },
    ];

    private static (ReservaViewModel ViewModel, StubReserveService Service) CreateViewModel(bool confirm = true) =>
        CreateViewModel(_ => confirm);

    private static (ReservaViewModel ViewModel, StubReserveService Service) CreateViewModel(
        Func<string, bool> confirm, StubReserveBucketService? bucketService = null)
    {
        var service = new StubReserveService();
        var buckets = bucketService ?? new StubReserveBucketService { ReserveBuckets = DefaultBuckets };
        var viewModel = new ReservaViewModel(service, buckets, confirm);
        return (viewModel, service);
    }

    [Fact]
    public async Task Balances_ShowsFourBucketsAndCorrectTotal()
    {
        var (viewModel, service) = CreateViewModel();
        service.Balances =
        [
            new ReserveBucketBalanceDTO { Bucket = "Investimento", Balance = 100m },
            new ReserveBucketBalanceDTO { Bucket = "HouseTreats", Balance = 50m },
            new ReserveBucketBalanceDTO { Bucket = "Ariana", Balance = 25m },
            new ReserveBucketBalanceDTO { Bucket = "Gleison", Balance = 25m },
        ];

        await viewModel.RefreshAsync();

        viewModel.Balances.Should().HaveCount(4);
        viewModel.TotalBalance.Should().Be(200m);
    }

    [Fact]
    public async Task Movements_GroupsSameDateDescriptionSplitWithCorrectSubtotal()
    {
        var (viewModel, service) = CreateViewModel();
        var date = DateOnly.FromDateTime(DateTime.Today);
        service.Movements =
        [
            new ReserveMovementDTO { Id = Guid.NewGuid(), Bucket = "Investimento", Amount = 10m, Date = date, Description = "Salary" },
            new ReserveMovementDTO { Id = Guid.NewGuid(), Bucket = "HouseTreats", Amount = 20m, Date = date, Description = "Salary" },
            new ReserveMovementDTO { Id = Guid.NewGuid(), Bucket = "Ariana", Amount = 5m, Date = date, Description = "Salary" },
            new ReserveMovementDTO { Id = Guid.NewGuid(), Bucket = "Gleison", Amount = 5m, Date = date, Description = "Salary" },
            new ReserveMovementDTO { Id = Guid.NewGuid(), Bucket = "Investimento", Amount = -15m, Date = date, Description = "Standalone" },
        ];

        await viewModel.RefreshAsync();

        var splitRows = viewModel.Movements.Where(m => m.Description == "Salary").ToList();
        splitRows.Should().HaveCount(4);
        splitRows.Should().OnlyContain(r => r.IsPartOfGroup);
        splitRows.Count(r => r.GroupTotal != null).Should().Be(1);
        splitRows.Single(r => r.GroupTotal != null).GroupTotal.Should().Be(40m);

        var standaloneRow = viewModel.Movements.Single(m => m.Description == "Standalone");
        standaloneRow.IsPartOfGroup.Should().BeFalse();
        standaloneRow.GroupTotal.Should().BeNull();
    }

    [Fact]
    public async Task SubmitIncomeSplit_ValidForm_CallsServiceAndShowsResultPanel()
    {
        var (viewModel, service) = CreateViewModel();
        await viewModel.RefreshAsync();
        viewModel.ShowSplitFormCommand.Execute(null);
        viewModel.SplitDate = DateTime.Today;
        viewModel.SplitAmount = "100";
        viewModel.SplitDescription = "Salary";

        await viewModel.SubmitSplitAsync();

        service.LastSplitRequest.Should().NotBeNull();
        service.LastSplitRequest!.Amount.Should().Be(100m);
        service.LastSplitRequest.Description.Should().Be("Salary");
        viewModel.LastSplitResult.Should().Be(service.SplitResult);
        viewModel.HasSplitResult.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, "100", "Salary")]
    [InlineData("2026-01-01", "0", "Salary")]
    [InlineData("2026-01-01", "100", "")]
    public async Task SubmitIncomeSplit_InvalidForm_BlocksSaveWithoutServiceCall(string? date, string amount, string description)
    {
        var (viewModel, service) = CreateViewModel();
        viewModel.ShowSplitFormCommand.Execute(null);
        viewModel.SplitDate = date is null ? null : DateTime.Parse(date);
        viewModel.SplitAmount = amount;
        viewModel.SplitDescription = description;

        await viewModel.SubmitSplitAsync();

        service.LastSplitRequest.Should().BeNull();
        viewModel.SplitSaveError.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SubmitWithdrawal_ValidFormNoOverdraft_CallsServiceAndRefreshes()
    {
        var (viewModel, service) = CreateViewModel();
        await viewModel.RefreshAsync();
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
        var (viewModel, service) = CreateViewModel(confirm: true);
        await viewModel.RefreshAsync();
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
        var (viewModel, service) = CreateViewModel(confirm: false);
        await viewModel.RefreshAsync();
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
        await viewModel.RefreshAsync();
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
    public async Task EditMovement_ValidForm_CallsUpdateServiceWithCorrectId()
    {
        var (viewModel, service) = CreateViewModel();
        var movement = new ReserveMovementRow
        {
            Id = Guid.NewGuid(), Bucket = "Investimento", Amount = 10m,
            Date = DateOnly.FromDateTime(DateTime.Today), Description = "Salary",
        };

        viewModel.EditMovementCommand.Execute(movement);
        viewModel.EditAmount = "15";

        await viewModel.SaveMovementEditAsync();

        service.LastUpdateRequest.Should().NotBeNull();
        service.LastUpdateRequest!.Value.Id.Should().Be(movement.Id);
        service.LastUpdateRequest.Value.Request.Amount.Should().Be(15m);
        viewModel.IsEditFormOpen.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteMovement_SplitGroupMember_ShowsSplitWarningAndCallsService()
    {
        var capturedMessage = string.Empty;
        var (viewModel, service) = CreateViewModel(confirm: msg =>
        {
            capturedMessage = msg;
            return true;
        });
        var row = new ReserveMovementRow
        {
            Id = Guid.NewGuid(), Bucket = "Investimento", Amount = 10m,
            Date = DateOnly.FromDateTime(DateTime.Today), Description = "Salary", IsPartOfGroup = true,
        };

        await viewModel.DeleteMovementAsync(row);

        capturedMessage.Should().Contain("part of a split");
        service.LastDeletedId.Should().Be(row.Id);
    }

    [Fact]
    public async Task DeleteMovement_Standalone_ShowsStandardWarningAndCallsService()
    {
        var capturedMessage = string.Empty;
        var (viewModel, service) = CreateViewModel(confirm: msg =>
        {
            capturedMessage = msg;
            return true;
        });
        var row = new ReserveMovementRow
        {
            Id = Guid.NewGuid(), Bucket = "Investimento", Amount = -10m,
            Date = DateOnly.FromDateTime(DateTime.Today), Description = "Groceries", IsPartOfGroup = false,
        };

        await viewModel.DeleteMovementAsync(row);

        capturedMessage.Should().NotContain("part of a split");
        capturedMessage.Should().Contain("removes it for good");
        service.LastDeletedId.Should().Be(row.Id);
    }

    [Fact]
    public void ShowIncomeSplitForm_ClosesOtherOpenForms()
    {
        var (viewModel, _) = CreateViewModel();
        viewModel.ShowWithdrawalFormCommand.Execute(null);
        viewModel.IsWithdrawalFormOpen.Should().BeTrue();

        viewModel.ShowSplitFormCommand.Execute(null);

        viewModel.IsWithdrawalFormOpen.Should().BeFalse();
        viewModel.IsSplitFormOpen.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshAsync_LoadsAllBucketsIncludingInactive()
    {
        var bucketService = new StubReserveBucketService
        {
            ReserveBuckets =
            [
                .. DefaultBuckets,
                new ReserveBucketDTO { Id = Guid.NewGuid(), Name = "Retired", IsActive = false, SplitPercentage = 0m },
            ],
        };
        var (viewModel, _) = CreateViewModel(_ => true, bucketService);

        await viewModel.RefreshAsync();

        viewModel.Buckets.Should().HaveCount(5);
        viewModel.Buckets.Should().Contain(b => b.Name == "Retired" && !b.IsActive);
    }

    [Fact]
    public async Task ShowWithdrawalForm_DefaultsBucketToFirstActive_SkippingALeadingInactiveOne()
    {
        var bucketService = new StubReserveBucketService
        {
            ReserveBuckets =
            [
                new ReserveBucketDTO { Id = Guid.NewGuid(), Name = "Retired", IsActive = false, SplitPercentage = 0m },
                new ReserveBucketDTO { Id = Guid.NewGuid(), Name = "Investimento", IsActive = true, SplitPercentage = 100m },
            ],
        };
        var (viewModel, _) = CreateViewModel(_ => true, bucketService);
        await viewModel.RefreshAsync();

        viewModel.ShowWithdrawalFormCommand.Execute(null);

        viewModel.WithdrawalBucket.Should().Be("Investimento");
    }

    [Fact]
    public async Task SplitPercentageWarning_EmptyWhenActiveBucketsSumTo100Percent()
    {
        var (viewModel, _) = CreateViewModel();

        await viewModel.RefreshAsync();

        viewModel.SplitPercentageWarning.Should().BeEmpty();
    }

    [Fact]
    public async Task SplitPercentageWarning_SetWhenActiveBucketsDoNotSumTo100Percent()
    {
        var bucketService = new StubReserveBucketService
        {
            ReserveBuckets =
            [
                new ReserveBucketDTO { Id = Guid.NewGuid(), Name = "Investimento", IsActive = true, SplitPercentage = 50m },
                new ReserveBucketDTO { Id = Guid.NewGuid(), Name = "HouseTreats", IsActive = true, SplitPercentage = 48.5m },
            ],
        };
        var (viewModel, _) = CreateViewModel(_ => true, bucketService);

        await viewModel.RefreshAsync();

        viewModel.SplitPercentageWarning.Should().Be("Active bucket percentages sum to 98.50%, not 100%");
    }

    [Fact]
    public async Task RefreshAsync_BucketServiceThrows_LeavesBucketsEmptyWithoutSettingPageError()
    {
        var bucketService = new StubReserveBucketService { ThrowOnGet = new InvalidOperationException("Buckets unavailable") };
        var (viewModel, _) = CreateViewModel(_ => true, bucketService);

        await viewModel.RefreshAsync();

        viewModel.Buckets.Should().BeEmpty();
        viewModel.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitWithdrawal_NoBucketSelected_BlocksSaveWithoutServiceCall()
    {
        var bucketService = new StubReserveBucketService { ThrowOnGet = new InvalidOperationException("Buckets unavailable") };
        var (viewModel, service) = CreateViewModel(_ => true, bucketService);
        await viewModel.RefreshAsync();
        viewModel.ShowWithdrawalFormCommand.Execute(null);
        viewModel.WithdrawalAmount = "30";
        viewModel.WithdrawalDescription = "Groceries top-up";

        await viewModel.SubmitWithdrawalAsync();

        service.WithdrawalRequests.Should().BeEmpty();
        viewModel.WithdrawalSaveError.Should().Be("Bucket is required.");
    }
}
