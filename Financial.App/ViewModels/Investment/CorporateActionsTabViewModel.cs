using System.Collections.ObjectModel;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Exceptions;
using Financial.Presentation.App.Views.Investment;
using System.Windows;

namespace Financial.Presentation.App.ViewModels.Investment;

public class CorporateActionsTabViewModel : ViewModelBase
{
    private readonly ICorporateActionService? _corporateActionService;
    private readonly Func<bool> _hasContext;
    private readonly Func<string> _brokerName;
    private readonly Func<string> _portfolioName;
    private readonly Func<string> _assetName;
    private readonly Action<AssetDetailsDTO> _applyDetails;
    private readonly Action<string, string, MessageBoxImage> _showMessage;

    private readonly RelayCommand _addCommand;
    private readonly RelayCommand _updateCommand;
    private readonly RelayCommand _deleteCommand;

    private CorporateActionFormViewModel? _formViewModel;
    private bool _isFormOpen;
    private CorporateActionRowViewModel? _selectedCorporateAction;
    private string _affectedAssetName = string.Empty;

    public CorporateActionsTabViewModel(
        ICorporateActionService? corporateActionService,
        Func<bool> hasContext,
        Func<string> brokerName,
        Func<string> portfolioName,
        Func<string> assetName,
        Action<AssetDetailsDTO> applyDetails,
        Action<string, string, MessageBoxImage> showMessage)
    {
        _corporateActionService = corporateActionService;
        _hasContext = hasContext ?? throw new ArgumentNullException(nameof(hasContext));
        _brokerName = brokerName ?? throw new ArgumentNullException(nameof(brokerName));
        _portfolioName = portfolioName ?? throw new ArgumentNullException(nameof(portfolioName));
        _assetName = assetName ?? throw new ArgumentNullException(nameof(assetName));
        _applyDetails = applyDetails ?? throw new ArgumentNullException(nameof(applyDetails));
        _showMessage = showMessage ?? throw new ArgumentNullException(nameof(showMessage));

        _addCommand = new RelayCommand(AddCorporateAction, CanEditCorporateActions);
        _updateCommand = new RelayCommand(UpdateCorporateAction, CanUpdateCorporateAction);
        _deleteCommand = new RelayCommand(DeleteCorporateAction, CanDeleteCorporateAction);
    }

    public ObservableCollection<CorporateActionRowViewModel> CorporateActions { get; } = new();

    public bool HasVisibleCorporateActions => CorporateActions.Count > 0;
    public bool HasNoCorporateActions => CorporateActions.Count == 0;

    public CorporateActionRowViewModel? SelectedCorporateAction
    {
        get => _selectedCorporateAction;
        set { if (SetProperty(ref _selectedCorporateAction, value)) UpdateCommandStates(); }
    }

    public RelayCommand AddCommand => _addCommand;
    public RelayCommand UpdateCommand => _updateCommand;
    public RelayCommand DeleteCommand => _deleteCommand;

    public CorporateActionFormViewModel? FormViewModel
    {
        get => _formViewModel;
        private set => SetProperty(ref _formViewModel, value);
    }

    public bool IsFormOpen
    {
        get => _isFormOpen;
        private set => SetProperty(ref _isFormOpen, value);
    }

    public void Load(string contextKey, IReadOnlyList<CorporateActionDTO> corporateActions, string affectedAssetName)
    {
        _affectedAssetName = affectedAssetName;

        CorporateActions.Clear();
        foreach (var record in corporateActions.OrderByDescending(record => record.EffectiveDate))
            CorporateActions.Add(new CorporateActionRowViewModel(record, affectedAssetName));

        SelectedCorporateAction = null;
        OnPropertyChanged(nameof(HasVisibleCorporateActions));
        OnPropertyChanged(nameof(HasNoCorporateActions));
    }

    public void Clear()
    {
        CorporateActions.Clear();
        _affectedAssetName = string.Empty;
        SelectedCorporateAction = null;
        OnPropertyChanged(nameof(HasVisibleCorporateActions));
        OnPropertyChanged(nameof(HasNoCorporateActions));
    }

    public void UpdateCommandStates()
    {
        _addCommand.RaiseCanExecuteChanged();
        _updateCommand.RaiseCanExecuteChanged();
        _deleteCommand.RaiseCanExecuteChanged();
    }

    public async Task Add(Func<Task<CorporateActionFormData?>> showForm)
    {
        if (!_hasContext())
        {
            ShowInfo("Select an asset before adding a corporate action.");
            return;
        }

        if (_corporateActionService == null)
        {
            return;
        }

        var formData = await showForm();
        while (formData != null)
        {
            AssetDetailsDTO? updatedDetails;
            try
            {
                updatedDetails = await _corporateActionService.AddSplitAsync(new CorporateActionSplitCreateDTO
                {
                    BrokerName = _brokerName(),
                    PortfolioName = _portfolioName(),
                    AssetName = _assetName(),
                    EffectiveDate = formData.Value.EffectiveDate,
                    RatioFactor = ComputeRatioFactor(formData.Value),
                    Note = formData.Value.Note
                });
            }
            catch (Exception ex)
            {
                formData = await RetrySameFormAsync(ex is InvestmentRuleViolationException ? ex.Message : "Corporate action could not be added. Check the values and try again.");
                continue;
            }

            if (updatedDetails == null)
            {
                formData = await RetrySameFormAsync("Corporate action could not be added. Check the values and try again.");
                continue;
            }

            _applyDetails(updatedDetails);
            CloseCorporateActionForm();
            return;
        }
    }

    public async Task Update(CorporateActionRowViewModel? selectedCorporateAction, Func<Task<CorporateActionFormData?>> showForm)
    {
        if (_corporateActionService == null || selectedCorporateAction == null)
        {
            return;
        }

        if (selectedCorporateAction.Id == Guid.Empty)
        {
            ShowWarning("Select a saved corporate action to update.");
            return;
        }

        var formData = await showForm();
        while (formData != null)
        {
            AssetDetailsDTO? updatedDetails;
            try
            {
                updatedDetails = await _corporateActionService.UpdateSplitAsync(new CorporateActionSplitUpdateDTO
                {
                    BrokerName = _brokerName(),
                    PortfolioName = _portfolioName(),
                    AssetName = _assetName(),
                    Id = formData.Value.CorporateActionId,
                    EffectiveDate = formData.Value.EffectiveDate,
                    RatioFactor = ComputeRatioFactor(formData.Value),
                    Note = formData.Value.Note
                });
            }
            catch (Exception ex)
            {
                formData = await RetrySameFormAsync(ex is InvestmentRuleViolationException ? ex.Message : "Corporate action could not be updated. Check the values and try again.");
                continue;
            }

            if (updatedDetails == null)
            {
                formData = await RetrySameFormAsync("Corporate action could not be updated. Check the values and try again.");
                continue;
            }

            _applyDetails(updatedDetails);
            CloseCorporateActionForm();
            return;
        }
    }

    public async Task Delete(CorporateActionRowViewModel? selectedCorporateAction, Func<bool> confirmDialog)
    {
        if (selectedCorporateAction == null)
        {
            return;
        }

        if (_corporateActionService == null)
        {
            return;
        }

        if (selectedCorporateAction.Id == Guid.Empty)
        {
            ShowWarning("Select a saved corporate action to delete.");
            return;
        }

        if (!confirmDialog())
        {
            return;
        }

        AssetDetailsDTO? updatedDetails;
        try
        {
            updatedDetails = await _corporateActionService.DeleteCorporateActionAsync(new CorporateActionDeleteDTO
            {
                BrokerName = _brokerName(),
                PortfolioName = _portfolioName(),
                AssetName = _assetName(),
                Id = selectedCorporateAction.Id
            });
        }
        catch (Exception ex)
        {
            ShowWarning(ex is InvestmentRuleViolationException ? ex.Message : "Corporate action could not be deleted. Check the values and try again.");
            return;
        }

        if (updatedDetails == null)
        {
            ShowWarning("Corporate action could not be deleted. Check the values and try again.");
            return;
        }

        _applyDetails(updatedDetails);
    }

    private static decimal ComputeRatioFactor(CorporateActionFormData formData) =>
        formData.RatioDenominator == 0m ? 0m : formData.RatioNumerator / formData.RatioDenominator;

    private void ShowInfo(string message) => _showMessage(message, "Corporate Action", MessageBoxImage.Information);
    private void ShowWarning(string message) => _showMessage(message, "Corporate Action", MessageBoxImage.Warning);

    private bool CanEditCorporateActions() => _hasContext();

    // Merger/SpinOff rows have no Update/Delete flow yet (Stage 3/4, P53-F06 PR3/PR4) - this form
    // and CorporateActionDialog only know how to build a Split request, so a non-Split row must not
    // reach either action until its own type-specific form exists.
    private bool CanUpdateCorporateAction(object? parameter) =>
        _hasContext() && ((parameter as CorporateActionRowViewModel) ?? SelectedCorporateAction) is { IsSplit: true };
    private bool CanDeleteCorporateAction(object? parameter) =>
        _hasContext() && ((parameter as CorporateActionRowViewModel) ?? SelectedCorporateAction) is { IsSplit: true };

    private async void AddCorporateAction() => await Add(ShowAddCorporateActionFormAsync);

    private async void UpdateCorporateAction(object? parameter)
    {
        if (parameter is CorporateActionRowViewModel row) SelectedCorporateAction = row;
        await Update(SelectedCorporateAction, ShowUpdateCorporateActionFormAsync);
    }

    private async void DeleteCorporateAction(object? parameter)
    {
        if (parameter is CorporateActionRowViewModel row) SelectedCorporateAction = row;
        await Delete(SelectedCorporateAction, ShowDeleteCorporateActionDialog);
    }

    // "New X" / edit actions open an inline form on the same tab instead of a modal dialog
    // (docs/ui/forms-data-and-visualisations.md's "'New X' create actions are inline forms, not
    // popup dialogs" rule), mirroring TransactionsTabViewModel's inline-form mechanics exactly - see
    // that class for the full rationale, including why Confirm doesn't close the form here (a
    // server-side refusal must not discard what the user typed).
    private Task<CorporateActionFormData?> ShowCorporateActionFormAsync(CorporateActionFormViewModel vm)
    {
        FormViewModel = vm;
        IsFormOpen = true;

        var tcs = new TaskCompletionSource<CorporateActionFormData?>();
        void OnClosed(object? sender, bool? result)
        {
            vm.CloseRequested -= OnClosed;
            if (result != true)
            {
                IsFormOpen = false;
                FormViewModel = null;
                tcs.SetResult(null);
                return;
            }

            tcs.SetResult(new CorporateActionFormData(
                vm.CorporateActionId, vm.EffectiveDate, vm.Type, vm.RatioNumerator, vm.RatioDenominator, vm.Note));
        }

        vm.CloseRequested += OnClosed;
        return tcs.Task;
    }

    private void CloseCorporateActionForm()
    {
        IsFormOpen = false;
        FormViewModel = null;
    }

    private Task<CorporateActionFormData?> RetrySameFormAsync(string message)
    {
        var vm = FormViewModel;
        if (vm == null)
        {
            ShowWarning(message);
            return Task.FromResult<CorporateActionFormData?>(null);
        }

        vm.ReportSubmitFailed(message);
        return ShowCorporateActionFormAsync(vm);
    }

    internal Task<CorporateActionFormData?> ShowAddCorporateActionFormAsync()
    {
        var vm = CorporateActionFormViewModel.CreateForAdd(_brokerName(), _portfolioName(), _assetName());
        return ShowCorporateActionFormAsync(vm);
    }

    private Task<CorporateActionFormData?> ShowUpdateCorporateActionFormAsync()
    {
        if (SelectedCorporateAction == null) return Task.FromResult<CorporateActionFormData?>(null);
        var record = SelectedCorporateAction.Record;
        var (numerator, denominator) = RatioFactorToFraction(record.RatioFactor);
        var vm = CorporateActionFormViewModel.CreateForUpdate(
            _brokerName(), _portfolioName(), _assetName(),
            record.Id, record.EffectiveDate, record.Type.ToString(), numerator, denominator, record.Note);
        return ShowCorporateActionFormAsync(vm);
    }

    private bool ShowDeleteCorporateActionDialog()
    {
        if (SelectedCorporateAction == null) return false;
        var record = SelectedCorporateAction.Record;
        var (numerator, denominator) = RatioFactorToFraction(record.RatioFactor);
        var vm = CorporateActionFormViewModel.CreateForDelete(
            _brokerName(), _portfolioName(), _assetName(),
            record.Id, record.EffectiveDate, record.Type.ToString(), numerator, denominator, record.Note);
        var dialog = new CorporateActionDialog(vm) { Owner = System.Windows.Application.Current?.MainWindow };
        return dialog.ShowDialog() == true;
    }

    /// <summary>The API only stores the converted decimal factor, not the N/M pair the user typed -
    /// ports Financial.Web's useCorporateActions.ts ratioFactorToFraction exactly. A factor below 1
    /// (a reverse split) reopens as "1 for 1/factor" so a 1-for-10 reverse split shows the whole-number
    /// denominator the user actually typed, not "0.1 for 1".</summary>
    private static (decimal Numerator, decimal Denominator) RatioFactorToFraction(decimal? ratioFactor)
    {
        if (ratioFactor is null) return (0m, 1m);
        if (ratioFactor >= 1m) return (ratioFactor.Value, 1m);
        return (1m, Math.Round(1m / ratioFactor.Value, 4, MidpointRounding.AwayFromZero));
    }
}

public readonly record struct CorporateActionFormData(
    Guid CorporateActionId,
    DateTime EffectiveDate,
    string Type,
    decimal RatioNumerator,
    decimal RatioDenominator,
    string? Note);
