using System.Collections.ObjectModel;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.Presentation.App.Services;
using Microsoft.Extensions.Logging;
using static Financial.Presentation.App.Helpers.ObservableCollectionHelper;

namespace Financial.Presentation.App.ViewModels.Admin;

public class IncomeSourcesViewModel : ViewModelBase
{
    private readonly IIncomeSourceService _incomeSourceService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<IncomeSourcesViewModel> _logger;

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

    public ObservableCollection<IncomeSourceDTO> IncomeSources { get; } = [];

    public RelayCommand RetryCommand { get; }

    public RelayCommand CreateIncomeSourceCommand { get; }

    public RelayCommand<IncomeSourceDTO> EditIncomeSourceCommand { get; }

    public RelayCommand<IncomeSourceDTO> DeleteIncomeSourceCommand { get; }

    public IncomeSourcesViewModel(IIncomeSourceService incomeSourceService, IDialogService dialogService, ILogger<IncomeSourcesViewModel> logger)
    {
        _incomeSourceService = incomeSourceService ?? throw new ArgumentNullException(nameof(incomeSourceService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        RetryCommand = new RelayCommand(async () => await RefreshAsync());
        CreateIncomeSourceCommand = new RelayCommand(async () => await CreateIncomeSourceAsync());
        EditIncomeSourceCommand = new RelayCommand<IncomeSourceDTO>(async incomeSource => await EditIncomeSourceAsync(incomeSource));
        DeleteIncomeSourceCommand = new RelayCommand<IncomeSourceDTO>(async incomeSource => await DeleteIncomeSourceAsync(incomeSource));

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
            var incomeSources = await Task.Run(() => _incomeSourceService.GetIncomeSources());

            if (!isCurrent())
            {
                return;
            }

            ReplaceAll(IncomeSources, incomeSources);
        },
        ex => _logger.LogError("Income sources refresh failed with {ErrorType}", ex.GetType().Name));

    internal async Task CreateIncomeSourceAsync()
    {
        var dialog = new IncomeSourceFormDialogViewModel();
        if (!_dialogService.ShowIncomeSourceFormDialog(dialog))
        {
            return;
        }

        ActionError = null;
        try
        {
            await _incomeSourceService.CreateIncomeSourceAsync(new IncomeSourceCreateDTO
            {
                Name = dialog.Name,
                Group = dialog.Group,
                IsActive = dialog.IsActive,
                AutoSplitToReserve = dialog.AutoSplitToReserve,
            });
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Income source create failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }

    internal async Task EditIncomeSourceAsync(IncomeSourceDTO? incomeSource)
    {
        if (incomeSource is null)
        {
            return;
        }

        var dialog = new IncomeSourceFormDialogViewModel(
            incomeSource.Name,
            incomeSource.Group,
            incomeSource.IsActive,
            incomeSource.AutoSplitToReserve);
        if (!_dialogService.ShowIncomeSourceFormDialog(dialog))
        {
            return;
        }

        ActionError = null;
        try
        {
            await _incomeSourceService.UpdateIncomeSourceAsync(incomeSource.Id, new IncomeSourceUpdateDTO
            {
                Name = dialog.Name,
                Group = dialog.Group,
                IsActive = dialog.IsActive,
                AutoSplitToReserve = dialog.AutoSplitToReserve,
            });
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Income source update failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }

    internal async Task DeleteIncomeSourceAsync(IncomeSourceDTO? incomeSource)
    {
        if (incomeSource is null)
        {
            return;
        }

        if (incomeSource.HasReferences)
        {
            ActionError = $"\"{incomeSource.Name}\" is still used by an income entry and cannot be deleted.";
            return;
        }

        if (!_dialogService.Confirm($"\"{incomeSource.Name}\" will be permanently removed. Continue?", "Delete Income Source"))
        {
            return;
        }

        ActionError = null;
        try
        {
            await _incomeSourceService.DeleteIncomeSourceAsync(incomeSource.Id);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Income source delete failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }
}
