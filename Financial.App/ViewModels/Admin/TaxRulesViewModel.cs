using System.Collections.ObjectModel;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Presentation.App.Services;
using Microsoft.Extensions.Logging;
using static Financial.Presentation.App.Helpers.ObservableCollectionHelper;

namespace Financial.Presentation.App.ViewModels.Admin;

public class TaxRulesViewModel : ViewModelBase
{
    private readonly ITaxRuleService _taxRuleService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<TaxRulesViewModel> _logger;

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

    public ObservableCollection<TaxRuleDTO> TaxRules { get; } = [];

    public bool HasNoTaxRules => TaxRules.Count == 0;

    public bool ShowTaxRules => TaxRules.Count > 0;

    public RelayCommand RetryCommand { get; }

    public RelayCommand CreateTaxRuleCommand { get; }

    public RelayCommand<TaxRuleDTO> EditTaxRuleCommand { get; }

    public RelayCommand<TaxRuleDTO> DeleteTaxRuleCommand { get; }

    public TaxRulesViewModel(ITaxRuleService taxRuleService, IDialogService dialogService, ILogger<TaxRulesViewModel> logger)
    {
        _taxRuleService = taxRuleService ?? throw new ArgumentNullException(nameof(taxRuleService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        RetryCommand = new RelayCommand(async () => await RefreshAsync());
        CreateTaxRuleCommand = new RelayCommand(async () => await CreateTaxRuleAsync());
        EditTaxRuleCommand = new RelayCommand<TaxRuleDTO>(async rule => await EditTaxRuleAsync(rule));
        DeleteTaxRuleCommand = new RelayCommand<TaxRuleDTO>(async rule => await DeleteTaxRuleAsync(rule));

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
            var rules = await Task.Run(() => _taxRuleService.GetTaxRules());

            if (!isCurrent())
            {
                return;
            }

            ReplaceAll(TaxRules, rules);
            OnPropertyChanged(nameof(HasNoTaxRules));
            OnPropertyChanged(nameof(ShowTaxRules));
        },
        ex => _logger.LogError("Tax rules refresh failed with {ErrorType}", ex.GetType().Name));

    internal async Task CreateTaxRuleAsync()
    {
        var dialog = new TaxRuleFormDialogViewModel();
        if (!_dialogService.ShowTaxRuleFormDialog(dialog))
        {
            return;
        }

        ActionError = null;
        try
        {
            await _taxRuleService.CreateTaxRuleAsync(new TaxRuleCreateDTO
            {
                Jurisdiction = dialog.Jurisdiction,
                EventCategory = dialog.EventCategory,
                Label = dialog.Label,
                Description = dialog.Description,
                EffectiveFrom = dialog.EffectiveFrom!.Value,
                EffectiveTo = dialog.EffectiveTo,
            });
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Tax rule create failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }

    internal async Task EditTaxRuleAsync(TaxRuleDTO? rule)
    {
        if (rule is null)
        {
            return;
        }

        var dialog = new TaxRuleFormDialogViewModel(rule);
        if (!_dialogService.ShowTaxRuleFormDialog(dialog))
        {
            return;
        }

        ActionError = null;
        try
        {
            await _taxRuleService.UpdateTaxRuleAsync(rule.Id, new TaxRuleUpdateDTO
            {
                Label = dialog.Label,
                Description = dialog.Description,
                EffectiveFrom = dialog.EffectiveFrom!.Value,
                EffectiveTo = dialog.EffectiveTo,
            });
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Tax rule update failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }

    /// <summary>Unlike Reserve Buckets, a Tax Rule has no soft-delete state - F01's DeleteTaxRuleAsync
    /// hard-removes it, rejecting the call outright when a Final classification still depends on
    /// it. That rejection surfaces here as ActionError, without removing the row.</summary>
    internal async Task DeleteTaxRuleAsync(TaxRuleDTO? rule)
    {
        if (rule is null)
        {
            return;
        }

        if (!_dialogService.Confirm($"\"{rule.Label}\" will be permanently deleted. Continue?", "Delete Tax Rule"))
        {
            return;
        }

        ActionError = null;
        try
        {
            await _taxRuleService.DeleteTaxRuleAsync(rule.Id);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Tax rule delete failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }
}
