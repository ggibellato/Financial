using System.Collections.ObjectModel;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Presentation.App.Services;
using Microsoft.Extensions.Logging;
using static Financial.Presentation.App.Helpers.ObservableCollectionHelper;

namespace Financial.Presentation.App.ViewModels.Admin;

public class PortfoliosViewModel : ViewModelBase
{
    private readonly IPortfolioService _portfolioService;
    private readonly IBrokerService _brokerService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<PortfoliosViewModel> _logger;

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

    public ObservableCollection<PortfolioDTO> Portfolios { get; } = [];

    public RelayCommand RetryCommand { get; }

    public RelayCommand CreatePortfolioCommand { get; }

    public RelayCommand<PortfolioDTO> EditPortfolioCommand { get; }

    public RelayCommand<PortfolioDTO> DeletePortfolioCommand { get; }

    public PortfoliosViewModel(
        IPortfolioService portfolioService,
        IBrokerService brokerService,
        IDialogService dialogService,
        ILogger<PortfoliosViewModel> logger)
    {
        _portfolioService = portfolioService ?? throw new ArgumentNullException(nameof(portfolioService));
        _brokerService = brokerService ?? throw new ArgumentNullException(nameof(brokerService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        RetryCommand = new RelayCommand(async () => await RefreshAsync());
        CreatePortfolioCommand = new RelayCommand(async () => await CreatePortfolioAsync());
        EditPortfolioCommand = new RelayCommand<PortfolioDTO>(async portfolio => await EditPortfolioAsync(portfolio));
        DeletePortfolioCommand = new RelayCommand<PortfolioDTO>(async portfolio => await DeletePortfolioAsync(portfolio));

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
            var portfolios = await Task.Run(() => _portfolioService.GetPortfolios());

            if (!isCurrent())
            {
                return;
            }

            ReplaceAll(Portfolios, portfolios);
        },
        ex => _logger.LogError("Portfolios refresh failed with {ErrorType}", ex.GetType().Name));

    internal async Task CreatePortfolioAsync()
    {
        var activeBrokerNames = await Task.Run(() => _brokerService.GetBrokers()
            .Where(b => b.Status == "Active")
            .Select(b => b.Name)
            .ToList());

        var dialog = new PortfolioFormDialogViewModel(activeBrokerNames);
        if (!_dialogService.ShowPortfolioFormDialog(dialog))
        {
            return;
        }

        ActionError = null;
        try
        {
            await _portfolioService.CreatePortfolioAsync(new PortfolioCreateDTO { BrokerName = dialog.BrokerName, Name = dialog.Name });
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Portfolio create failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }

    internal async Task EditPortfolioAsync(PortfolioDTO? portfolio)
    {
        if (portfolio is null)
        {
            return;
        }

        var dialog = new PortfolioFormDialogViewModel([], portfolio.BrokerName, portfolio.Name);
        if (!_dialogService.ShowPortfolioFormDialog(dialog))
        {
            return;
        }

        ActionError = null;
        try
        {
            await _portfolioService.UpdatePortfolioAsync(portfolio.BrokerName, portfolio.Name, new PortfolioUpdateDTO { Name = dialog.Name });
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Portfolio update failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }

    internal async Task DeletePortfolioAsync(PortfolioDTO? portfolio)
    {
        if (portfolio is null)
        {
            return;
        }

        if (portfolio.AssetCount > 0)
        {
            ActionError = $"\"{portfolio.Name}\" still holds {portfolio.AssetCount} asset(s) and cannot be deleted.";
            return;
        }

        if (!_dialogService.Confirm($"\"{portfolio.Name}\" holds no assets and will be permanently removed. Continue?", "Delete Portfolio"))
        {
            return;
        }

        var scope = portfolio.BrokerStatus == "Active" ? InvestmentScope.Active : InvestmentScope.Historic;

        ActionError = null;
        try
        {
            await _portfolioService.DeleteEmptyPortfolioAsync(portfolio.BrokerName, portfolio.Name, scope);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Portfolio delete failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }
}
