using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.CashFlow;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.CashFlow;

public class ReservaViewModelTests
{
    private static readonly Guid InvestimentoId = Guid.NewGuid();
    private static readonly Guid HouseTreatsId = Guid.NewGuid();
    private static readonly Guid ArianaId = Guid.NewGuid();
    private static readonly Guid GleisonId = Guid.NewGuid();

    private static readonly List<ReserveBucketDTO> DefaultBuckets =
    [
        new() { Id = InvestimentoId, Name = "Investimento", IsActive = true, SplitPercentage = 33.33m },
        new() { Id = HouseTreatsId, Name = "HouseTreats", IsActive = true, SplitPercentage = 33.33m },
        new() { Id = ArianaId, Name = "Ariana", IsActive = true, SplitPercentage = 16.67m },
        new() { Id = GleisonId, Name = "Gleison", IsActive = true, SplitPercentage = 16.67m },
    ];

    private static (ReservaViewModel ViewModel, StubReserveService Service) CreateViewModel(bool confirm = true) =>
        CreateViewModel(_ => confirm);

    private static (ReservaViewModel ViewModel, StubReserveService Service) CreateViewModel(
        Func<string, bool> confirm, StubReserveBucketService? bucketService = null, RecordingLogger<ReservaViewModel>? logger = null)
    {
        var service = new StubReserveService();
        var buckets = bucketService ?? new StubReserveBucketService { ReserveBuckets = DefaultBuckets };
        var viewModel = new ReservaViewModel(service, buckets, confirm, logger ?? new RecordingLogger<ReservaViewModel>());
        return (viewModel, service);
    }

    [Fact]
    public async Task Balances_ShowsFourBucketsAndCorrectTotal()
    {
        var (viewModel, service) = CreateViewModel();
        service.Balances =
        [
            new ReserveBucketBalanceDTO { BucketId = InvestimentoId, BucketName = "Investimento", Balance = 100m },
            new ReserveBucketBalanceDTO { BucketId = HouseTreatsId, BucketName = "HouseTreats", Balance = 50m },
            new ReserveBucketBalanceDTO { BucketId = ArianaId, BucketName = "Ariana", Balance = 25m },
            new ReserveBucketBalanceDTO { BucketId = GleisonId, BucketName = "Gleison", Balance = 25m },
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
            new ReserveMovementDTO { Id = Guid.NewGuid(), BucketId = InvestimentoId, BucketName = "Investimento", Amount = 10m, Date = date, Description = "Salary" },
            new ReserveMovementDTO { Id = Guid.NewGuid(), BucketId = HouseTreatsId, BucketName = "HouseTreats", Amount = 20m, Date = date, Description = "Salary" },
            new ReserveMovementDTO { Id = Guid.NewGuid(), BucketId = ArianaId, BucketName = "Ariana", Amount = 5m, Date = date, Description = "Salary" },
            new ReserveMovementDTO { Id = Guid.NewGuid(), BucketId = GleisonId, BucketName = "Gleison", Amount = 5m, Date = date, Description = "Salary" },
            new ReserveMovementDTO { Id = Guid.NewGuid(), BucketId = InvestimentoId, BucketName = "Investimento", Amount = -15m, Date = date, Description = "Standalone" },
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
    public async Task EditMovement_ValidForm_CallsUpdateServiceWithCorrectId()
    {
        var (viewModel, service) = CreateViewModel();
        var movement = new ReserveMovementRow
        {
            Id = Guid.NewGuid(), BucketId = InvestimentoId, BucketName = "Investimento", Amount = 10m,
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
    public async Task EditDateFieldError_MissingDate_MatchesSaveError()
    {
        var (viewModel, service) = CreateViewModel();
        var movement = new ReserveMovementRow
        {
            Id = Guid.NewGuid(), BucketId = InvestimentoId, BucketName = "Investimento", Amount = 10m,
            Date = DateOnly.FromDateTime(DateTime.Today), Description = "Salary",
        };
        viewModel.EditMovementCommand.Execute(movement);
        viewModel.EditDate = null;

        await viewModel.SaveMovementEditAsync();

        service.LastUpdateRequest.Should().BeNull();
        viewModel.EditDateFieldError.Should().Be(viewModel.EditSaveError);
        viewModel.EditBucketFieldError.Should().BeNull();
    }

    [Fact]
    public async Task EditBucketFieldError_MissingBucket_MatchesSaveError()
    {
        var (viewModel, service) = CreateViewModel();
        var movement = new ReserveMovementRow
        {
            Id = Guid.NewGuid(), BucketId = InvestimentoId, BucketName = "Investimento", Amount = 10m,
            Date = DateOnly.FromDateTime(DateTime.Today), Description = "Salary",
        };
        viewModel.EditMovementCommand.Execute(movement);
        viewModel.EditBucketId = null;

        await viewModel.SaveMovementEditAsync();

        service.LastUpdateRequest.Should().BeNull();
        viewModel.EditBucketFieldError.Should().Be(viewModel.EditSaveError);
    }

    [Fact]
    public async Task EditDescriptionFieldError_MissingDescription_MatchesSaveError()
    {
        var (viewModel, service) = CreateViewModel();
        var movement = new ReserveMovementRow
        {
            Id = Guid.NewGuid(), BucketId = InvestimentoId, BucketName = "Investimento", Amount = 10m,
            Date = DateOnly.FromDateTime(DateTime.Today), Description = "Salary",
        };
        viewModel.EditMovementCommand.Execute(movement);
        viewModel.EditDescription = "";

        await viewModel.SaveMovementEditAsync();

        service.LastUpdateRequest.Should().BeNull();
        viewModel.EditDescriptionFieldError.Should().Be(viewModel.EditSaveError);
    }

    [Fact]
    public async Task EditAmountFieldError_NonNumericAmount_MatchesSaveError()
    {
        var (viewModel, service) = CreateViewModel();
        var movement = new ReserveMovementRow
        {
            Id = Guid.NewGuid(), BucketId = InvestimentoId, BucketName = "Investimento", Amount = 10m,
            Date = DateOnly.FromDateTime(DateTime.Today), Description = "Salary",
        };
        viewModel.EditMovementCommand.Execute(movement);
        viewModel.EditAmount = "not-a-number";

        await viewModel.SaveMovementEditAsync();

        service.LastUpdateRequest.Should().BeNull();
        viewModel.EditAmountFieldError.Should().Be(viewModel.EditSaveError);
    }

    [Fact]
    public async Task EditFieldErrors_ClearAfterSuccessfulSave()
    {
        var (viewModel, _) = CreateViewModel();
        var movement = new ReserveMovementRow
        {
            Id = Guid.NewGuid(), BucketId = InvestimentoId, BucketName = "Investimento", Amount = 10m,
            Date = DateOnly.FromDateTime(DateTime.Today), Description = "Salary",
        };
        viewModel.EditMovementCommand.Execute(movement);
        viewModel.EditDescription = "";
        await viewModel.SaveMovementEditAsync();
        viewModel.EditDescriptionFieldError.Should().NotBeNull();

        viewModel.EditDescription = "Salary";
        await viewModel.SaveMovementEditAsync();

        viewModel.EditDescriptionFieldError.Should().BeNull();
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
            Id = Guid.NewGuid(), BucketId = InvestimentoId, BucketName = "Investimento", Amount = 10m,
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
            Id = Guid.NewGuid(), BucketId = InvestimentoId, BucketName = "Investimento", Amount = -10m,
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
        viewModel.Withdrawal.ShowWithdrawalFormCommand.Execute(null);
        viewModel.Withdrawal.IsWithdrawalFormOpen.Should().BeTrue();

        viewModel.Split.ShowSplitFormCommand.Execute(null);

        viewModel.Withdrawal.IsWithdrawalFormOpen.Should().BeFalse();
        viewModel.Split.IsSplitFormOpen.Should().BeTrue();
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
                new ReserveBucketDTO { Id = InvestimentoId, Name = "Investimento", IsActive = true, SplitPercentage = 100m },
            ],
        };
        var (viewModel, _) = CreateViewModel(_ => true, bucketService);
        await viewModel.RefreshAsync();

        viewModel.Withdrawal.ShowWithdrawalFormCommand.Execute(null);

        viewModel.Withdrawal.WithdrawalBucketId.Should().Be(InvestimentoId);
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
    public async Task DeleteMovement_DoesNotReloadBuckets()
    {
        // A mutation-triggered refresh must not rebuild the Buckets collection: doing so clears it
        // before re-adding items, which would silently reset a still-open Withdrawal/Edit form's
        // ComboBox selection (the movement grid's row actions aren't gated behind those forms being
        // closed). Buckets are seeded-only and never change mid-session, so skipping the reload is safe.
        var bucketService = new StubReserveBucketService { ReserveBuckets = DefaultBuckets };
        var (viewModel, _) = CreateViewModel(_ => true, bucketService);
        await viewModel.RefreshAsync();
        var callCountAfterInitialLoad = bucketService.GetReserveBucketsCallCount;
        var row = new ReserveMovementRow
        {
            Id = Guid.NewGuid(), BucketId = InvestimentoId, BucketName = "Investimento", Amount = 10m,
            Date = DateOnly.FromDateTime(DateTime.Today), Description = "Test",
        };

        await viewModel.DeleteMovementAsync(row);

        bucketService.GetReserveBucketsCallCount.Should().Be(callCountAfterInitialLoad);
        viewModel.Buckets.Should().HaveCount(4);
    }

    [Fact]
    public async Task SubmitWithdrawal_NoBucketSelected_BlocksSaveWithoutServiceCall()
    {
        var bucketService = new StubReserveBucketService { ThrowOnGet = new InvalidOperationException("Buckets unavailable") };
        var (viewModel, service) = CreateViewModel(_ => true, bucketService);
        await viewModel.RefreshAsync();
        viewModel.Withdrawal.ShowWithdrawalFormCommand.Execute(null);
        viewModel.Withdrawal.WithdrawalAmount = "30";
        viewModel.Withdrawal.WithdrawalDescription = "Groceries top-up";

        await viewModel.Withdrawal.SubmitWithdrawalAsync();

        service.WithdrawalRequests.Should().BeEmpty();
        viewModel.Withdrawal.WithdrawalSaveError.Should().Be("Bucket is required.");
    }

    [Fact]
    public void BuildRows_WithIncomeId_SetsIsLockedTrue()
    {
        var movement = new ReserveMovementDTO
        {
            Id = Guid.NewGuid(), BucketId = InvestimentoId, BucketName = "Investimento", Amount = 100m,
            Date = DateOnly.FromDateTime(DateTime.Today), Description = "Salary", IncomeId = Guid.NewGuid(),
        };

        var rows = ReserveMovementRow.BuildRows([movement]);

        rows.Single().IsLocked.Should().BeTrue();
    }

    [Fact]
    public void BuildRows_WithoutIncomeId_SetsIsLockedFalse()
    {
        var movement = new ReserveMovementDTO
        {
            Id = Guid.NewGuid(), BucketId = InvestimentoId, BucketName = "Investimento", Amount = 100m,
            Date = DateOnly.FromDateTime(DateTime.Today), Description = "Groceries",
        };

        var rows = ReserveMovementRow.BuildRows([movement]);

        rows.Single().IsLocked.Should().BeFalse();
    }

    [Fact]
    public void EditMovementCommand_CanExecute_FalseForLockedRow()
    {
        var (viewModel, _) = CreateViewModel();
        var row = new ReserveMovementRow
        {
            Id = Guid.NewGuid(), BucketId = InvestimentoId, BucketName = "Investimento", Amount = 10m,
            Date = DateOnly.FromDateTime(DateTime.Today), Description = "Salary", IncomeId = Guid.NewGuid(),
        };

        viewModel.EditMovementCommand.CanExecute(row).Should().BeFalse();
    }

    [Fact]
    public void EditMovementCommand_CanExecute_TrueForUnlockedRow()
    {
        var (viewModel, _) = CreateViewModel();
        var row = new ReserveMovementRow
        {
            Id = Guid.NewGuid(), BucketId = InvestimentoId, BucketName = "Investimento", Amount = 10m,
            Date = DateOnly.FromDateTime(DateTime.Today), Description = "Salary",
        };

        viewModel.EditMovementCommand.CanExecute(row).Should().BeTrue();
    }

    [Fact]
    public void DeleteMovementCommand_CanExecute_FalseForLockedRow()
    {
        var (viewModel, _) = CreateViewModel();
        var row = new ReserveMovementRow
        {
            Id = Guid.NewGuid(), BucketId = InvestimentoId, BucketName = "Investimento", Amount = 10m,
            Date = DateOnly.FromDateTime(DateTime.Today), Description = "Salary", IncomeId = Guid.NewGuid(),
        };

        viewModel.DeleteMovementCommand.CanExecute(row).Should().BeFalse();
    }

    [Fact]
    public void DeleteMovementCommand_CanExecute_TrueForUnlockedRow()
    {
        var (viewModel, _) = CreateViewModel();
        var row = new ReserveMovementRow
        {
            Id = Guid.NewGuid(), BucketId = InvestimentoId, BucketName = "Investimento", Amount = 10m,
            Date = DateOnly.FromDateTime(DateTime.Today), Description = "Salary",
        };

        viewModel.DeleteMovementCommand.CanExecute(row).Should().BeTrue();
    }

    [Fact]
    public async Task RefreshAsync_BucketLookupFails_LogsAWarningWithTheErrorTypeOnly()
    {
        var logger = new RecordingLogger<ReservaViewModel>();
        var buckets = new StubReserveBucketService { ThrowOnGet = new InvalidOperationException("bucket Ariana balance 654.27") };
        var (viewModel, _) = CreateViewModel(_ => true, buckets, logger);

        await viewModel.RefreshAsync();

        // The constructor's background refresh may hit the same failing lookup, so more than
        // one identical warning can be recorded - assert on all of them.
        var warnings = logger.Entries.Where(e => e.Level == Microsoft.Extensions.Logging.LogLevel.Warning).ToList();
        warnings.Should().NotBeEmpty();
        warnings.Should().AllSatisfy(w =>
        {
            w.Message.Should().Contain(nameof(InvalidOperationException));
            w.Message.Should().NotContain("Ariana", "exception messages may embed bucket names/balances and must stay out of the log");
        });
    }
}
