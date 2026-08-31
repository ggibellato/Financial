using System.Collections.ObjectModel;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.Presentation.App.Services;
using Microsoft.Extensions.Logging;
using static Financial.Presentation.App.Helpers.ObservableCollectionHelper;

namespace Financial.Presentation.App.ViewModels.Admin;

public class CreditCardsViewModel : ViewModelBase
{
    private readonly ICreditCardService _creditCardService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<CreditCardsViewModel> _logger;

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

    public ObservableCollection<CreditCardDTO> CreditCards { get; } = [];

    public RelayCommand RetryCommand { get; }

    public RelayCommand CreateCreditCardCommand { get; }

    public RelayCommand<CreditCardDTO> EditCreditCardCommand { get; }

    public RelayCommand<CreditCardDTO> DeleteCreditCardCommand { get; }

    public CreditCardsViewModel(ICreditCardService creditCardService, IDialogService dialogService, ILogger<CreditCardsViewModel> logger)
    {
        _creditCardService = creditCardService ?? throw new ArgumentNullException(nameof(creditCardService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        RetryCommand = new RelayCommand(async () => await RefreshAsync());
        CreateCreditCardCommand = new RelayCommand(async () => await CreateCreditCardAsync());
        EditCreditCardCommand = new RelayCommand<CreditCardDTO>(async card => await EditCreditCardAsync(card));
        DeleteCreditCardCommand = new RelayCommand<CreditCardDTO>(async card => await DeleteCreditCardAsync(card));

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
            var creditCards = await Task.Run(() => _creditCardService.GetCreditCards());

            if (!isCurrent())
            {
                return;
            }

            ReplaceAll(CreditCards, creditCards);
        },
        ex => _logger.LogError("CreditCards refresh failed with {ErrorType}", ex.GetType().Name));

    internal async Task CreateCreditCardAsync()
    {
        var dialog = new CreditCardFormDialogViewModel();
        if (!_dialogService.ShowCreditCardFormDialog(dialog))
        {
            return;
        }

        ActionError = null;
        try
        {
            await _creditCardService.CreateCreditCardAsync(new CreditCardCreateDTO { Name = dialog.Name, IsActive = dialog.IsActive });
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("CreditCard create failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }

    internal async Task EditCreditCardAsync(CreditCardDTO? creditCard)
    {
        if (creditCard is null)
        {
            return;
        }

        var currentDueDate = creditCard.NextInvoiceDueDate?.ToDateTime(TimeOnly.MinValue);
        var dialog = new CreditCardFormDialogViewModel(creditCard.Name, creditCard.IsActive, currentDueDate);
        if (!_dialogService.ShowCreditCardFormDialog(dialog))
        {
            return;
        }

        ActionError = null;
        try
        {
            await _creditCardService.UpdateCreditCardAsync(creditCard.Id, new CreditCardUpdateDTO
            {
                Name = dialog.Name,
                IsActive = dialog.IsActive,
                NextInvoiceDueDate = dialog.NextInvoiceDueDate.HasValue ? DateOnly.FromDateTime(dialog.NextInvoiceDueDate.Value) : null,
            });
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("CreditCard update failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }

    internal async Task DeleteCreditCardAsync(CreditCardDTO? creditCard)
    {
        if (creditCard is null)
        {
            return;
        }

        if (creditCard.HasReferences)
        {
            ActionError = $"\"{creditCard.Name}\" is still referenced by a statement or expense and cannot be deleted.";
            return;
        }

        if (!_dialogService.Confirm($"\"{creditCard.Name}\" will be permanently removed. Continue?", "Delete Credit Card"))
        {
            return;
        }

        ActionError = null;
        try
        {
            await _creditCardService.DeleteCreditCardAsync(creditCard.Id);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("CreditCard delete failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }
}
