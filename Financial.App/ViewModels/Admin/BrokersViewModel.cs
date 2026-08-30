using System.Collections.ObjectModel;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Presentation.App.Services;
using Microsoft.Extensions.Logging;
using static Financial.Presentation.App.Helpers.ObservableCollectionHelper;

namespace Financial.Presentation.App.ViewModels.Admin;

public class BrokersViewModel : ViewModelBase
{
    private readonly IBrokerService _brokerService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<BrokersViewModel> _logger;

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

    public ObservableCollection<BrokerDTO> Brokers { get; } = [];

    public RelayCommand RetryCommand { get; }

    public RelayCommand CreateBrokerCommand { get; }

    public RelayCommand<BrokerDTO> EditBrokerCommand { get; }

    public RelayCommand<BrokerDTO> DeleteBrokerCommand { get; }

    public BrokersViewModel(IBrokerService brokerService, IDialogService dialogService, ILogger<BrokersViewModel> logger)
    {
        _brokerService = brokerService ?? throw new ArgumentNullException(nameof(brokerService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        RetryCommand = new RelayCommand(async () => await RefreshAsync());
        CreateBrokerCommand = new RelayCommand(async () => await CreateBrokerAsync());
        EditBrokerCommand = new RelayCommand<BrokerDTO>(async broker => await EditBrokerAsync(broker));
        DeleteBrokerCommand = new RelayCommand<BrokerDTO>(async broker => await DeleteBrokerAsync(broker));

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
            var brokers = await Task.Run(() => _brokerService.GetBrokers());

            if (!isCurrent())
            {
                return;
            }

            ReplaceAll(Brokers, brokers);
        },
        ex => _logger.LogError("Brokers refresh failed with {ErrorType}", ex.GetType().Name));

    internal async Task CreateBrokerAsync()
    {
        var dialog = new BrokerFormDialogViewModel();
        if (!_dialogService.ShowBrokerFormDialog(dialog))
        {
            return;
        }

        ActionError = null;
        try
        {
            await _brokerService.CreateBrokerAsync(new BrokerCreateDTO { Name = dialog.Name, Currency = dialog.Currency });
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Broker create failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }

    internal async Task EditBrokerAsync(BrokerDTO? broker)
    {
        if (broker is null)
        {
            return;
        }

        var dialog = new BrokerFormDialogViewModel(broker.Name, broker.Currency);
        if (!_dialogService.ShowBrokerFormDialog(dialog))
        {
            return;
        }

        ActionError = null;
        try
        {
            await _brokerService.UpdateBrokerAsync(broker.Name, new BrokerUpdateDTO { Name = dialog.Name, Currency = dialog.Currency });
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Broker update failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }

    internal async Task DeleteBrokerAsync(BrokerDTO? broker)
    {
        if (broker is null)
        {
            return;
        }

        if (broker.PortfolioCount > 0)
        {
            ActionError = $"\"{broker.Name}\" still has {broker.PortfolioCount} portfolio(s) and cannot be deleted.";
            return;
        }

        var message = broker.Status == "Active"
            ? $"\"{broker.Name}\" has no portfolios. It will move to the Historic list rather than be removed. Continue?"
            : $"\"{broker.Name}\" has no portfolios. It will be permanently removed. Continue?";

        if (!_dialogService.Confirm(message, "Delete Broker"))
        {
            return;
        }

        ActionError = null;
        try
        {
            await _brokerService.DeleteBrokerAsync(broker.Name);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Broker delete failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }
}
