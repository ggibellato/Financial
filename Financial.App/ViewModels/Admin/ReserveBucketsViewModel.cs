using System.Collections.ObjectModel;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.Presentation.App.Services;
using Microsoft.Extensions.Logging;
using static Financial.Presentation.App.Helpers.ObservableCollectionHelper;

namespace Financial.Presentation.App.ViewModels.Admin;

public class ReserveBucketsViewModel : ViewModelBase
{
    private const decimal SplitPercentageTolerance = 0.01m;

    private readonly IReserveBucketService _reserveBucketService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<ReserveBucketsViewModel> _logger;

    private bool _isLoading = true;
    private string? _error;
    private string? _actionError;

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(ShowContent));
            }
        }
    }

    public string? Error
    {
        get => _error;
        private set
        {
            if (SetProperty(ref _error, value))
            {
                OnPropertyChanged(nameof(HasError));
                OnPropertyChanged(nameof(ShowContent));
            }
        }
    }

    public bool HasError => Error != null;

    public bool ShowContent => !IsLoading && !HasError;

    public string? ActionError
    {
        get => _actionError;
        private set => SetProperty(ref _actionError, value);
    }

    public ObservableCollection<ReserveBucketDTO> ReserveBuckets { get; } = [];

    /// <summary>Non-blocking, computed live from the fetched list - mirrors <see
    /// cref="Financial.Presentation.App.ViewModels.CashFlow.ReservaViewModel"/>'s own independent
    /// computation, per this feature's Technical Decisions.</summary>
    public string SplitPercentageWarning
    {
        get
        {
            if (ReserveBuckets.Count == 0)
            {
                return string.Empty;
            }

            var activeSum = ReserveBuckets.Where(b => b.IsActive).Sum(b => b.SplitPercentage);
            if (Math.Abs(activeSum - 100m) <= SplitPercentageTolerance)
            {
                return string.Empty;
            }

            return $"Active buckets currently sum to {activeSum:0.##}% — review your split percentages";
        }
    }

    public RelayCommand RetryCommand { get; }

    public RelayCommand CreateReserveBucketCommand { get; }

    public RelayCommand<ReserveBucketDTO> EditReserveBucketCommand { get; }

    public RelayCommand<ReserveBucketDTO> DeactivateReserveBucketCommand { get; }

    public ReserveBucketsViewModel(IReserveBucketService reserveBucketService, IDialogService dialogService, ILogger<ReserveBucketsViewModel> logger)
    {
        _reserveBucketService = reserveBucketService ?? throw new ArgumentNullException(nameof(reserveBucketService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        RetryCommand = new RelayCommand(async () => await RefreshAsync());
        CreateReserveBucketCommand = new RelayCommand(async () => await CreateReserveBucketAsync());
        EditReserveBucketCommand = new RelayCommand<ReserveBucketDTO>(async bucket => await EditReserveBucketAsync(bucket));
        DeactivateReserveBucketCommand = new RelayCommand<ReserveBucketDTO>(async bucket => await DeactivateReserveBucketAsync(bucket));

        _ = RefreshAsync();
    }

    private int _refreshRequestId;

    internal Task RefreshAsync() => ExecuteRefreshAsync(
        () => ++_refreshRequestId,
        id => id == _refreshRequestId,
        loading => IsLoading = loading,
        error => Error = error,
        async isCurrent =>
        {
            var buckets = await Task.Run(() => _reserveBucketService.GetReserveBuckets());

            if (!isCurrent())
            {
                return;
            }

            ReplaceAll(ReserveBuckets, buckets);
            OnPropertyChanged(nameof(SplitPercentageWarning));
        },
        ex => _logger.LogError("Reserve buckets refresh failed with {ErrorType}", ex.GetType().Name));

    internal async Task CreateReserveBucketAsync()
    {
        var dialog = new ReserveBucketFormDialogViewModel();
        if (!_dialogService.ShowReserveBucketFormDialog(dialog))
        {
            return;
        }

        ActionError = null;
        try
        {
            await _reserveBucketService.CreateReserveBucketAsync(new ReserveBucketCreateDTO
            {
                Name = dialog.Name,
                SplitPercentage = dialog.ParsedSplitPercentage,
                IsActive = dialog.IsActive,
            });
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Reserve bucket create failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }

    internal async Task EditReserveBucketAsync(ReserveBucketDTO? bucket)
    {
        if (bucket is null)
        {
            return;
        }

        var dialog = new ReserveBucketFormDialogViewModel(bucket.Name, bucket.SplitPercentage, bucket.IsActive);
        if (!_dialogService.ShowReserveBucketFormDialog(dialog))
        {
            return;
        }

        ActionError = null;
        try
        {
            await _reserveBucketService.UpdateReserveBucketAsync(bucket.Id, new ReserveBucketUpdateDTO
            {
                Name = dialog.Name,
                SplitPercentage = dialog.ParsedSplitPercentage,
                IsActive = dialog.IsActive,
            });
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Reserve bucket update failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }

    /// <summary>"Deleting" a Reserve Bucket deactivates it rather than removing it - this calls the
    /// same Update path as Edit, with IsActive forced to false, since no hard delete exists.</summary>
    internal async Task DeactivateReserveBucketAsync(ReserveBucketDTO? bucket)
    {
        if (bucket is null)
        {
            return;
        }

        if (!_dialogService.Confirm(
            $"\"{bucket.Name}\" will be deactivated, not removed. Existing reserve movements linked to it remain valid. Continue?",
            "Delete Reserve Bucket"))
        {
            return;
        }

        ActionError = null;
        try
        {
            await _reserveBucketService.UpdateReserveBucketAsync(bucket.Id, new ReserveBucketUpdateDTO
            {
                Name = bucket.Name,
                SplitPercentage = bucket.SplitPercentage,
                IsActive = false,
            });
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Reserve bucket deactivate failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }
}
