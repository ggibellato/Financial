using System.Collections.ObjectModel;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.Presentation.App.Services;
using Microsoft.Extensions.Logging;
using static Financial.Presentation.App.Helpers.ObservableCollectionHelper;

namespace Financial.Presentation.App.ViewModels.Admin;

public class RecurringBillsViewModel : ViewModelBase
{
    private readonly IMensaisService _mensaisService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<RecurringBillsViewModel> _logger;

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

    public ObservableCollection<RecurringBillDTO> RecurringBills { get; } = [];

    public RelayCommand RetryCommand { get; }

    public RelayCommand CreateRecurringBillCommand { get; }

    public RelayCommand<RecurringBillDTO> EditRecurringBillCommand { get; }

    public RelayCommand<RecurringBillDTO> DeleteRecurringBillCommand { get; }

    public RecurringBillsViewModel(IMensaisService mensaisService, IDialogService dialogService, ILogger<RecurringBillsViewModel> logger)
    {
        _mensaisService = mensaisService ?? throw new ArgumentNullException(nameof(mensaisService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        RetryCommand = new RelayCommand(async () => await RefreshAsync());
        CreateRecurringBillCommand = new RelayCommand(async () => await CreateRecurringBillAsync());
        EditRecurringBillCommand = new RelayCommand<RecurringBillDTO>(async bill => await EditRecurringBillAsync(bill));
        DeleteRecurringBillCommand = new RelayCommand<RecurringBillDTO>(async bill => await DeleteRecurringBillAsync(bill));

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
            var bills = await Task.Run(() => _mensaisService.GetBills());

            if (!isCurrent())
            {
                return;
            }

            ReplaceAll(RecurringBills, bills);
        },
        ex => _logger.LogError("Recurring bills refresh failed with {ErrorType}", ex.GetType().Name));

    internal async Task CreateRecurringBillAsync()
    {
        var dialog = new RecurringBillFormDialogViewModel();
        if (!_dialogService.ShowRecurringBillFormDialog(dialog))
        {
            return;
        }

        ActionError = null;
        try
        {
            await _mensaisService.CreateBillAsync(new RecurringBillCreateDTO
            {
                DueDay = dialog.ParsedDueDay,
                Description = dialog.Description,
                Value = dialog.ParsedValue,
                Area = dialog.Area,
                Note = dialog.Note,
            });
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Recurring bill create failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }

    internal async Task EditRecurringBillAsync(RecurringBillDTO? bill)
    {
        if (bill is null)
        {
            return;
        }

        var dialog = new RecurringBillFormDialogViewModel(
            bill.Id,
            bill.DueDay,
            bill.Description,
            bill.Value,
            bill.Area,
            bill.Note,
            bill.NitNumber,
            bill.MinimumWageValue,
            bill.Status);
        if (!_dialogService.ShowRecurringBillFormDialog(dialog))
        {
            return;
        }

        ActionError = null;
        try
        {
            await _mensaisService.UpdateBillAsync(bill.Id, new RecurringBillUpdateDTO
            {
                DueDay = dialog.ParsedDueDay,
                Description = dialog.Description,
                Value = dialog.ParsedValue,
                Area = dialog.Area,
                Note = dialog.Note,
                NitNumber = dialog.NormalizedNitNumber,
                MinimumWageValue = dialog.ParsedMinimumWageValue,
                Status = dialog.Status,
            });
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Recurring bill update failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }

    internal async Task DeleteRecurringBillAsync(RecurringBillDTO? bill)
    {
        if (bill is null)
        {
            return;
        }

        if (!_dialogService.Confirm($"\"{bill.Description}\" will be permanently removed. Continue?", "Delete Recurring Bill"))
        {
            return;
        }

        ActionError = null;
        try
        {
            await _mensaisService.DeleteBillAsync(bill.Id);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Recurring bill delete failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }
}
