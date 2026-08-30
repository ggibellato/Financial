using System.Collections.ObjectModel;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Presentation.App.Services;
using Microsoft.Extensions.Logging;
using static Financial.Presentation.App.Helpers.ObservableCollectionHelper;

namespace Financial.Presentation.App.ViewModels.Admin;

public class AssetsViewModel : ViewModelBase
{
    private readonly IAssetAdminService _assetAdminService;
    private readonly IAssetMoveService _assetMoveService;
    private readonly IPortfolioService _portfolioService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<AssetsViewModel> _logger;

    private bool _isLoading = true;
    private string? _error;
    private string? _actionError;
    private IReadOnlyList<AssetAdminDTO> _allAssets = [];

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

    public ObservableCollection<AssetAdminDTO> Assets { get; } = [];

    private string _brokerFilter = AllFilterOption;

    public string BrokerFilter
    {
        get => _brokerFilter;
        set
        {
            if (SetProperty(ref _brokerFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    private string _portfolioFilter = AllFilterOption;

    public string PortfolioFilter
    {
        get => _portfolioFilter;
        set
        {
            if (SetProperty(ref _portfolioFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    private string _classFilter = AllFilterOption;

    public string ClassFilter
    {
        get => _classFilter;
        set
        {
            if (SetProperty(ref _classFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    private const string AllFilterOption = "(All)";

    /// <summary>The bound ComboBox's blank/"All" selection maps to this sentinel, translated back to
    /// an empty filter string in the setters above.</summary>
    public IReadOnlyList<string> BrokerFilterOptions =>
        new[] { AllFilterOption }.Concat(_allAssets.Select(a => a.BrokerName).Distinct().OrderBy(n => n)).ToList();

    public IReadOnlyList<string> PortfolioFilterOptions =>
        new[] { AllFilterOption }.Concat(_allAssets.Select(a => a.PortfolioName).Distinct().OrderBy(n => n)).ToList();

    public IReadOnlyList<string> ClassFilterOptions =>
        new[] { AllFilterOption }.Concat(_allAssets.Select(a => a.Class.ToString()).Distinct().OrderBy(n => n)).ToList();

    public RelayCommand RetryCommand { get; }

    public RelayCommand CreateAssetCommand { get; }

    public RelayCommand<AssetAdminDTO> EditAssetCommand { get; }

    public RelayCommand<AssetAdminDTO> DeleteAssetCommand { get; }

    public AssetsViewModel(
        IAssetAdminService assetAdminService,
        IAssetMoveService assetMoveService,
        IPortfolioService portfolioService,
        IDialogService dialogService,
        ILogger<AssetsViewModel> logger)
    {
        _assetAdminService = assetAdminService ?? throw new ArgumentNullException(nameof(assetAdminService));
        _assetMoveService = assetMoveService ?? throw new ArgumentNullException(nameof(assetMoveService));
        _portfolioService = portfolioService ?? throw new ArgumentNullException(nameof(portfolioService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        RetryCommand = new RelayCommand(async () => await RefreshAsync());
        CreateAssetCommand = new RelayCommand(async () => await CreateAssetAsync());
        EditAssetCommand = new RelayCommand<AssetAdminDTO>(async asset => await EditAssetAsync(asset));
        DeleteAssetCommand = new RelayCommand<AssetAdminDTO>(async asset => await DeleteAssetAsync(asset));

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
            var assets = await Task.Run(() => _assetAdminService.GetAssets());

            if (!isCurrent())
            {
                return;
            }

            _allAssets = assets;
            OnPropertyChanged(nameof(BrokerFilterOptions));
            OnPropertyChanged(nameof(PortfolioFilterOptions));
            OnPropertyChanged(nameof(ClassFilterOptions));
            ApplyFilters();
        },
        ex => _logger.LogError("Assets refresh failed with {ErrorType}", ex.GetType().Name));

    private void ApplyFilters()
    {
        var filtered = _allAssets
            .Where(a => BrokerFilter == AllFilterOption || a.BrokerName == BrokerFilter)
            .Where(a => PortfolioFilter == AllFilterOption || a.PortfolioName == PortfolioFilter)
            .Where(a => ClassFilter == AllFilterOption || a.Class.ToString() == ClassFilter)
            .ToList();
        ReplaceAll(Assets, filtered);
    }

    /// <summary>Every Active broker's portfolio names, for the create dialog's cascading picker.</summary>
    private Dictionary<string, IReadOnlyList<string>> GetActivePortfolioNamesByBroker() =>
        _portfolioService.GetPortfolios()
            .Where(p => p.BrokerStatus == "Active")
            .GroupBy(p => p.BrokerName)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(p => p.Name).ToList());

    internal async Task CreateAssetAsync()
    {
        var portfolioNamesByBroker = await Task.Run(GetActivePortfolioNamesByBroker);

        var dialog = new AssetFormDialogViewModel(portfolioNamesByBroker);
        if (!_dialogService.ShowAssetFormDialog(dialog))
        {
            return;
        }

        ActionError = null;
        try
        {
            await _assetAdminService.CreateAssetAsync(new AssetAdminCreateDTO
            {
                BrokerName = dialog.BrokerName,
                PortfolioName = dialog.PortfolioName,
                Name = dialog.Name,
                ISIN = dialog.ISIN,
                Exchange = dialog.Exchange,
                Ticker = dialog.Ticker,
                Country = dialog.Country,
                LocalTypeCode = dialog.LocalTypeCode,
                Class = dialog.Class == GlobalAssetClass.Unknown ? null : dialog.Class,
            });
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Asset create failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }

    internal async Task EditAssetAsync(AssetAdminDTO? asset)
    {
        if (asset is null)
        {
            return;
        }

        var dialog = new AssetFormDialogViewModel(new Dictionary<string, IReadOnlyList<string>>(), asset);
        if (!_dialogService.ShowAssetFormDialog(dialog))
        {
            return;
        }

        ActionError = null;
        try
        {
            await _assetAdminService.UpdateAssetAsync(asset.BrokerName, asset.PortfolioName, asset.Name, new AssetAdminUpdateDTO
            {
                Name = dialog.Name,
                ISIN = dialog.ISIN,
                Exchange = dialog.Exchange,
                Ticker = dialog.Ticker,
                Country = dialog.Country,
                LocalTypeCode = dialog.LocalTypeCode,
                Class = dialog.Class,
            });
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Asset update failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }

    internal async Task DeleteAssetAsync(AssetAdminDTO? asset)
    {
        if (asset is null)
        {
            return;
        }

        if (asset.Quantity != 0)
        {
            ActionError = $"\"{asset.Name}\" still holds a position of {asset.Quantity} and cannot be deleted.";
            return;
        }

        if (!_dialogService.Confirm($"\"{asset.Name}\" holds zero quantity and will be archived into Historic Investments. Continue?", "Delete Asset"))
        {
            return;
        }

        ActionError = null;
        try
        {
            // Archives in place: same portfolio name in Historic Investments, mirroring
            // Financial.Web's AssetsPage delete flow.
            await _assetMoveService.ArchiveAssetAsync(new ArchiveAssetRequestDTO
            {
                BrokerName = asset.BrokerName,
                SourcePortfolioName = asset.PortfolioName,
                AssetName = asset.Name,
                DestinationPortfolioName = asset.PortfolioName,
            });
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Asset delete failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }
}
