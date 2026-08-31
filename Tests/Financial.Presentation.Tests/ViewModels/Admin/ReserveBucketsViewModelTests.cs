using Financial.CashFlow.Application.DTOs;
using Financial.Presentation.App.ViewModels.Admin;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels.Admin;

public class ReserveBucketsViewModelTests
{
    private static (ReserveBucketsViewModel ViewModel, StubReserveBucketService Service, StubDialogService Dialog) CreateViewModel()
    {
        var service = new StubReserveBucketService();
        var dialog = new StubDialogService();
        var viewModel = new ReserveBucketsViewModel(service, dialog, new RecordingLogger<ReserveBucketsViewModel>());
        return (viewModel, service, dialog);
    }

    private static ReserveBucketDTO Bucket(Guid id, string name, decimal splitPercentage, bool isActive = true) => new()
    {
        Id = id,
        Name = name,
        SplitPercentage = splitPercentage,
        IsActive = isActive,
        Warning = null,
    };

    [Fact]
    public async Task RefreshAsync_PopulatesReserveBucketsFromService()
    {
        var (viewModel, service, _) = CreateViewModel();
        service.ReserveBuckets = [Bucket(Guid.NewGuid(), "Investimento", 100m)];

        await viewModel.RefreshAsync();

        viewModel.ReserveBuckets.Should().ContainSingle(b => b.Name == "Investimento");
    }

    [Fact]
    public async Task RefreshAsync_NoSplitWarningWhenActiveBucketsSumTo100()
    {
        var (viewModel, service, _) = CreateViewModel();
        service.ReserveBuckets = [Bucket(Guid.NewGuid(), "Investimento", 100m)];

        await viewModel.RefreshAsync();

        viewModel.SplitPercentageWarning.Should().BeEmpty();
    }

    [Fact]
    public async Task RefreshAsync_SplitWarningWhenActiveBucketsDoNotSumTo100()
    {
        var (viewModel, service, _) = CreateViewModel();
        service.ReserveBuckets = [Bucket(Guid.NewGuid(), "Investimento", 60m)];

        await viewModel.RefreshAsync();

        viewModel.SplitPercentageWarning.Should().Contain("60").And.Contain("review your split percentages");
    }

    [Fact]
    public async Task CreateReserveBucketAsync_DialogConfirmed_CallsServiceAndRefreshes()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.ShowReserveBucketFormDialogResult = true;
        dialog.OnShowReserveBucketFormDialog = vm =>
        {
            vm.Name = "Ferias";
            vm.SplitPercentage = "20";
        };

        await viewModel.CreateReserveBucketAsync();

        service.LastCreateRequest.Should().NotBeNull();
        service.LastCreateRequest!.Name.Should().Be("Ferias");
        service.LastCreateRequest.SplitPercentage.Should().Be(20m);
        viewModel.ReserveBuckets.Should().ContainSingle(b => b.Name == "Ferias");
    }

    [Fact]
    public async Task CreateReserveBucketAsync_DialogCancelled_DoesNotCallService()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.ShowReserveBucketFormDialogResult = false;

        await viewModel.CreateReserveBucketAsync();

        service.LastCreateRequest.Should().BeNull();
    }

    [Fact]
    public async Task CreateReserveBucketAsync_ServiceThrows_SurfacesActionError()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.OnShowReserveBucketFormDialog = vm =>
        {
            vm.Name = "Investimento";
            vm.SplitPercentage = "20";
        };
        service.ThrowOnCreate = new InvalidOperationException("A reserve bucket named \"Investimento\" already exists.");

        await viewModel.CreateReserveBucketAsync();

        viewModel.ActionError.Should().Be("A reserve bucket named \"Investimento\" already exists.");
    }

    [Fact]
    public async Task EditReserveBucketAsync_PreFillsDialogWithCurrentValuesAndCallsUpdate()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        var id = Guid.NewGuid();
        var bucket = Bucket(id, "Investimento", 33.33m);
        dialog.OnShowReserveBucketFormDialog = vm => vm.SplitPercentage = "50";

        await viewModel.EditReserveBucketAsync(bucket);

        dialog.LastReserveBucketFormDialog!.Name.Should().Be("Investimento");
        service.LastUpdateRequest!.Value.Id.Should().Be(id);
        service.LastUpdateRequest.Value.Request.SplitPercentage.Should().Be(50m);
    }

    [Fact]
    public async Task DeactivateReserveBucketAsync_ConfirmsThenUpdatesWithIsActiveFalse()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        var id = Guid.NewGuid();
        var bucket = Bucket(id, "Investimento", 33.33m);

        await viewModel.DeactivateReserveBucketAsync(bucket);

        dialog.LastConfirmMessage.Should().Contain("deactivated, not removed");
        service.LastUpdateRequest!.Value.Id.Should().Be(id);
        service.LastUpdateRequest.Value.Request.IsActive.Should().BeFalse();
        service.LastUpdateRequest.Value.Request.Name.Should().Be("Investimento");
        service.LastUpdateRequest.Value.Request.SplitPercentage.Should().Be(33.33m);
    }

    [Fact]
    public async Task DeactivateReserveBucketAsync_ConfirmDeclined_DoesNotCallService()
    {
        var (viewModel, service, dialog) = CreateViewModel();
        dialog.ConfirmResult = false;
        var bucket = Bucket(Guid.NewGuid(), "Investimento", 33.33m);

        await viewModel.DeactivateReserveBucketAsync(bucket);

        service.LastUpdateRequest.Should().BeNull();
    }
}
