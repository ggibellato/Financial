using System.Collections.ObjectModel;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.Presentation.App.Services;
using Microsoft.Extensions.Logging;
using static Financial.Presentation.App.Helpers.ObservableCollectionHelper;

namespace Financial.Presentation.App.ViewModels.Admin;

public class InvestmentAccountsViewModel : ViewModelBase
{
    private readonly IInvestmentAccountService _investmentAccountService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<InvestmentAccountsViewModel> _logger;

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

    public ObservableCollection<InvestmentAccountDTO> InvestmentAccounts { get; } = [];

    public RelayCommand RetryCommand { get; }

    public RelayCommand CreateInvestmentAccountCommand { get; }

    public RelayCommand<InvestmentAccountDTO> EditInvestmentAccountCommand { get; }

    public RelayCommand<InvestmentAccountDTO> DeleteInvestmentAccountCommand { get; }

    public InvestmentAccountsViewModel(IInvestmentAccountService investmentAccountService, IDialogService dialogService, ILogger<InvestmentAccountsViewModel> logger)
    {
        _investmentAccountService = investmentAccountService ?? throw new ArgumentNullException(nameof(investmentAccountService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        RetryCommand = new RelayCommand(async () => await RefreshAsync());
        CreateInvestmentAccountCommand = new RelayCommand(async () => await CreateInvestmentAccountAsync());
        EditInvestmentAccountCommand = new RelayCommand<InvestmentAccountDTO>(async account => await EditInvestmentAccountAsync(account));
        DeleteInvestmentAccountCommand = new RelayCommand<InvestmentAccountDTO>(async account => await DeleteInvestmentAccountAsync(account));

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
            var accounts = await Task.Run(() => _investmentAccountService.GetInvestmentAccounts());

            if (!isCurrent())
            {
                return;
            }

            ReplaceAll(InvestmentAccounts, accounts);
        },
        ex => _logger.LogError("Investment accounts refresh failed with {ErrorType}", ex.GetType().Name));

    internal async Task CreateInvestmentAccountAsync()
    {
        var dialog = new InvestmentAccountFormDialogViewModel();
        if (!_dialogService.ShowInvestmentAccountFormDialog(dialog))
        {
            return;
        }

        ActionError = null;
        try
        {
            await _investmentAccountService.CreateInvestmentAccountAsync(new InvestmentAccountCreateDTO
            {
                Name = dialog.Name,
                IsActive = dialog.IsActive,
                IsLiability = dialog.IsLiability,
                Aliases = dialog.Aliases.ToList(),
            });
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Investment account create failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }

    internal async Task EditInvestmentAccountAsync(InvestmentAccountDTO? account)
    {
        if (account is null)
        {
            return;
        }

        var dialog = new InvestmentAccountFormDialogViewModel(
            account.Name,
            account.IsActive,
            account.IsLiability,
            account.Aliases);
        if (!_dialogService.ShowInvestmentAccountFormDialog(dialog))
        {
            return;
        }

        ActionError = null;
        try
        {
            await _investmentAccountService.UpdateInvestmentAccountAsync(account.Id, new InvestmentAccountUpdateDTO
            {
                Name = dialog.Name,
                IsActive = dialog.IsActive,
                IsLiability = dialog.IsLiability,
                Aliases = dialog.Aliases.ToList(),
            });
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Investment account update failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }

    internal async Task DeleteInvestmentAccountAsync(InvestmentAccountDTO? account)
    {
        if (account is null)
        {
            return;
        }

        if (account.LatestBalance != 0m)
        {
            ActionError = $"\"{account.Name}\"'s latest balance is {account.LatestBalance}, not zero, and cannot be deleted.";
            return;
        }

        if (!_dialogService.Confirm($"\"{account.Name}\" will be permanently removed. Continue?", "Delete Investment Account"))
        {
            return;
        }

        ActionError = null;
        try
        {
            await _investmentAccountService.DeleteInvestmentAccountAsync(account.Id);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Investment account delete failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }
}
