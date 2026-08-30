using System.Collections.ObjectModel;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.Presentation.App.Services;
using Microsoft.Extensions.Logging;
using static Financial.Presentation.App.Helpers.ObservableCollectionHelper;

namespace Financial.Presentation.App.ViewModels.Admin;

public class BanksViewModel : ViewModelBase
{
    private readonly IBankService _bankService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<BanksViewModel> _logger;

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

    public ObservableCollection<BankDTO> Banks { get; } = [];

    public RelayCommand RetryCommand { get; }

    public RelayCommand CreateBankCommand { get; }

    public RelayCommand<BankDTO> EditBankCommand { get; }

    public RelayCommand<BankDTO> DeleteBankCommand { get; }

    public BanksViewModel(IBankService bankService, IDialogService dialogService, ILogger<BanksViewModel> logger)
    {
        _bankService = bankService ?? throw new ArgumentNullException(nameof(bankService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        RetryCommand = new RelayCommand(async () => await RefreshAsync());
        CreateBankCommand = new RelayCommand(async () => await CreateBankAsync());
        EditBankCommand = new RelayCommand<BankDTO>(async bank => await EditBankAsync(bank));
        DeleteBankCommand = new RelayCommand<BankDTO>(async bank => await DeleteBankAsync(bank));

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
            var banks = await Task.Run(() => _bankService.GetBanks());

            if (!isCurrent())
            {
                return;
            }

            ReplaceAll(Banks, banks);
        },
        ex => _logger.LogError("Banks refresh failed with {ErrorType}", ex.GetType().Name));

    internal async Task CreateBankAsync()
    {
        var dialog = new BankFormDialogViewModel();
        if (!_dialogService.ShowBankFormDialog(dialog))
        {
            return;
        }

        ActionError = null;
        try
        {
            await _bankService.CreateBankAsync(new BankCreateDTO { Name = dialog.Name, RoundUpEnabled = dialog.RoundUpEnabled });
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Bank create failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }

    internal async Task EditBankAsync(BankDTO? bank)
    {
        if (bank is null)
        {
            return;
        }

        var dialog = new BankFormDialogViewModel(bank.Name, bank.RoundUpEnabled);
        if (!_dialogService.ShowBankFormDialog(dialog))
        {
            return;
        }

        ActionError = null;
        try
        {
            await _bankService.UpdateBankAsync(bank.Id, new BankUpdateDTO { Name = dialog.Name, RoundUpEnabled = dialog.RoundUpEnabled });
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Bank update failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }

    internal async Task DeleteBankAsync(BankDTO? bank)
    {
        if (bank is null)
        {
            return;
        }

        if (bank.HasReferences)
        {
            ActionError = $"\"{bank.Name}\" still has balance history or transactions and cannot be deleted.";
            return;
        }

        if (!_dialogService.Confirm($"\"{bank.Name}\" will be permanently removed. Continue?", "Delete Bank"))
        {
            return;
        }

        ActionError = null;
        try
        {
            await _bankService.DeleteBankAsync(bank.Id);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Bank delete failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }
}
