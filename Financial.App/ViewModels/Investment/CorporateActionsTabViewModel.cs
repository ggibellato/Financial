using System.Collections.ObjectModel;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Exceptions;
using Financial.Presentation.App.Views.Investment;
using System.Windows;

namespace Financial.Presentation.App.ViewModels.Investment;

public class CorporateActionsTabViewModel : ViewModelBase
{
    private readonly ICorporateActionService? _corporateActionService;
    private readonly IAssetAdminService? _assetAdminService;
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
    private decimal _assetQuantity;
    private decimal _assetCostBasis;

    public CorporateActionsTabViewModel(
        ICorporateActionService? corporateActionService,
        Func<bool> hasContext,
        Func<string> brokerName,
        Func<string> portfolioName,
        Func<string> assetName,
        Action<AssetDetailsDTO> applyDetails,
        Action<string, string, MessageBoxImage> showMessage,
        IAssetAdminService? assetAdminService = null)
    {
        _corporateActionService = corporateActionService;
        _assetAdminService = assetAdminService;
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

    public event EventHandler<CorporateActionRowViewModel>? FocusRequested;

    public void FocusCorporateAction(Guid corporateActionId)
    {
        var target = CorporateActions.FirstOrDefault(row => row.Id == corporateActionId);
        if (target is null)
        {
            return;
        }

        SelectedCorporateAction = target;
        FocusRequested?.Invoke(this, target);
    }

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

    public void Load(
        string contextKey,
        IReadOnlyList<CorporateActionDTO> corporateActions,
        string affectedAssetName,
        decimal assetQuantity = 0m,
        decimal assetCostBasis = 0m)
    {
        _affectedAssetName = affectedAssetName;
        _assetQuantity = assetQuantity;
        _assetCostBasis = assetCostBasis;

        CorporateActions.Clear();
        foreach (var record in corporateActions)
            CorporateActions.Add(new CorporateActionRowViewModel(record, affectedAssetName));

        SelectedCorporateAction = null;
        OnPropertyChanged(nameof(HasVisibleCorporateActions));
        OnPropertyChanged(nameof(HasNoCorporateActions));
    }

    public void Clear()
    {
        CorporateActions.Clear();
        _affectedAssetName = string.Empty;
        _assetQuantity = 0m;
        _assetCostBasis = 0m;
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
                if (formData.Value.Type == CorporateActionFormValidation.MergerTypeValue)
                {
                    updatedDetails = ResolveAffectedAsset(await _corporateActionService.AddMergerAsync(new CorporateActionMergerCreateDTO
                    {
                        BrokerName = _brokerName(),
                        PortfolioName = _portfolioName(),
                        SourceAssetName = _assetName(),
                        EffectiveDate = formData.Value.EffectiveDate,
                        ExchangeRatio = formData.Value.ExchangeRatio,
                        CashInLieuAmount = formData.Value.CashInLieuAmount,
                        Note = formData.Value.Note,
                        TargetAssetName = formData.Value.TargetAssetName,
                        CreateTargetAssetInline = formData.Value.CreateTargetAssetInline,
                        TargetISIN = formData.Value.TargetISIN,
                        TargetExchange = formData.Value.TargetExchange,
                        TargetTicker = formData.Value.TargetTicker,
                        TargetCountry = formData.Value.TargetCountry,
                        TargetClass = formData.Value.TargetClass,
                    }));
                }
                else if (formData.Value.Type == CorporateActionFormValidation.SpinOffTypeValue)
                {
                    updatedDetails = ResolveAffectedAsset(await _corporateActionService.AddSpinOffAsync(new CorporateActionSpinOffCreateDTO
                    {
                        BrokerName = _brokerName(),
                        PortfolioName = _portfolioName(),
                        ParentAssetName = _assetName(),
                        EffectiveDate = formData.Value.EffectiveDate,
                        QuantityReceived = formData.Value.QuantityReceived,
                        AllocationPercentage = formData.Value.AllocationPercentage,
                        Note = formData.Value.Note,
                        NewAssetName = formData.Value.TargetAssetName,
                        CreateNewAssetInline = formData.Value.CreateTargetAssetInline,
                        NewISIN = formData.Value.TargetISIN,
                        NewExchange = formData.Value.TargetExchange,
                        NewTicker = formData.Value.TargetTicker,
                        NewCountry = formData.Value.TargetCountry,
                        NewClass = formData.Value.TargetClass,
                    }));
                }
                else
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
                if (formData.Value.Type == CorporateActionFormValidation.MergerTypeValue)
                {
                    updatedDetails = ResolveAffectedAsset(await _corporateActionService.UpdateMergerAsync(new CorporateActionMergerUpdateDTO
                    {
                        BrokerName = _brokerName(),
                        PortfolioName = _portfolioName(),
                        SourceAssetName = _assetName(),
                        Id = formData.Value.CorporateActionId,
                        EffectiveDate = formData.Value.EffectiveDate,
                        ExchangeRatio = formData.Value.ExchangeRatio,
                        CashInLieuAmount = formData.Value.CashInLieuAmount,
                        Note = formData.Value.Note
                    }));
                }
                else if (formData.Value.Type == CorporateActionFormValidation.SpinOffTypeValue)
                {
                    updatedDetails = ResolveAffectedAsset(await _corporateActionService.UpdateSpinOffAsync(new CorporateActionSpinOffUpdateDTO
                    {
                        BrokerName = _brokerName(),
                        PortfolioName = _portfolioName(),
                        ParentAssetName = _assetName(),
                        Id = formData.Value.CorporateActionId,
                        EffectiveDate = formData.Value.EffectiveDate,
                        QuantityReceived = formData.Value.QuantityReceived,
                        AllocationPercentage = formData.Value.AllocationPercentage,
                        Note = formData.Value.Note
                    }));
                }
                else
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

    private AssetDetailsDTO? ResolveAffectedAsset(CorporateActionMergerResultDTO? result) =>
        result is null ? null : ResolveAffectedAsset(result.Source, result.Target);

    private AssetDetailsDTO? ResolveAffectedAsset(CorporateActionSpinOffResultDTO? result) =>
        result is null ? null : ResolveAffectedAsset(result.Parent, result.New);

    private AssetDetailsDTO? ResolveAffectedAsset(AssetDetailsDTO? primary, AssetDetailsDTO? secondary)
    {
        var currentName = _assetName();
        if (string.Equals(primary?.Name, currentName, StringComparison.Ordinal))
        {
            return primary;
        }

        if (string.Equals(secondary?.Name, currentName, StringComparison.Ordinal))
        {
            return secondary;
        }

        return primary ?? secondary;
    }

    private static decimal ComputeRatioFactor(CorporateActionFormData formData) =>
        formData.RatioDenominator == 0m ? 0m : formData.RatioNumerator / formData.RatioDenominator;

    private void ShowInfo(string message) => _showMessage(message, "Corporate Action", MessageBoxImage.Information);
    private void ShowWarning(string message) => _showMessage(message, "Corporate Action", MessageBoxImage.Warning);

    private bool CanEditCorporateActions() => _hasContext();

    private bool CanUpdateCorporateAction(object? parameter) =>
        _hasContext() && ((parameter as CorporateActionRowViewModel) ?? SelectedCorporateAction) != null;
    private bool CanDeleteCorporateAction(object? parameter) =>
        _hasContext() && ((parameter as CorporateActionRowViewModel) ?? SelectedCorporateAction) != null;

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
                vm.CorporateActionId, vm.EffectiveDate, vm.Type, vm.RatioNumerator, vm.RatioDenominator, vm.Note,
                TargetAssetName: vm.TargetAssetPicker?.AssetName ?? vm.TargetAssetName,
                CreateTargetAssetInline: vm.TargetAssetPicker?.MatchedAsset is null,
                TargetISIN: vm.TargetAssetPicker?.ISIN,
                TargetExchange: vm.TargetAssetPicker?.Exchange,
                TargetTicker: vm.TargetAssetPicker?.Ticker,
                TargetCountry: vm.TargetAssetPicker?.Country,
                TargetClass: vm.TargetAssetPicker?.Class,
                ExchangeRatio: vm.ExchangeRatio,
                CashInLieuAmount: vm.CashInLieuAmount,
                QuantityReceived: vm.QuantityReceived,
                AllocationPercentage: vm.AllocationPercentage));
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

    private IReadOnlyList<AssetAdminDTO> BuildTargetAssetOptions() =>
        (_assetAdminService?.GetAssets() ?? Array.Empty<AssetAdminDTO>())
            .Where(a => a.BrokerName == _brokerName() && a.PortfolioName == _portfolioName() && a.Name != _assetName())
            .ToList();

    internal Task<CorporateActionFormData?> ShowAddCorporateActionFormAsync()
    {
        var picker = new TargetAssetPickerViewModel(BuildTargetAssetOptions());
        var vm = CorporateActionFormViewModel.CreateForAdd(_brokerName(), _portfolioName(), _assetName(), picker, _assetQuantity, _assetCostBasis);
        return ShowCorporateActionFormAsync(vm);
    }

    private Task<CorporateActionFormData?> ShowUpdateCorporateActionFormAsync()
    {
        if (SelectedCorporateAction == null) return Task.FromResult<CorporateActionFormData?>(null);
        var record = SelectedCorporateAction.Record;
        var (numerator, denominator) = RatioFactorToFraction(record.RatioFactor);
        var vm = CorporateActionFormViewModel.CreateForUpdate(
            _brokerName(), _portfolioName(), _assetName(),
            record.Id, record.EffectiveDate, record.Type.ToString(), numerator, denominator, record.Note,
            record.LinkedAssetName ?? string.Empty, _assetName(), record.ExchangeRatio ?? 0m, record.CashInLieu,
            _assetQuantity, _assetCostBasis, record.ConvertedQuantity ?? 0m, record.AllocationPercentage ?? 0m);
        return ShowCorporateActionFormAsync(vm);
    }

    private bool ShowDeleteCorporateActionDialog()
    {
        if (SelectedCorporateAction == null) return false;
        var record = SelectedCorporateAction.Record;
        var (numerator, denominator) = RatioFactorToFraction(record.RatioFactor);
        var vm = CorporateActionFormViewModel.CreateForDelete(
            _brokerName(), _portfolioName(), _assetName(),
            record.Id, record.EffectiveDate, record.Type.ToString(), numerator, denominator, record.Note,
            record.LinkedAssetName ?? string.Empty, _assetName(), record.ExchangeRatio ?? 0m, record.CashInLieu,
            _assetQuantity, _assetCostBasis, record.ConvertedQuantity ?? 0m, record.AllocationPercentage ?? 0m);
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
    string? Note,
    string TargetAssetName = "",
    bool CreateTargetAssetInline = false,
    string? TargetISIN = null,
    string? TargetExchange = null,
    string? TargetTicker = null,
    CountryCode? TargetCountry = null,
    GlobalAssetClass? TargetClass = null,
    decimal ExchangeRatio = 0m,
    decimal? CashInLieuAmount = null,
    decimal QuantityReceived = 0m,
    decimal AllocationPercentage = 0m);
