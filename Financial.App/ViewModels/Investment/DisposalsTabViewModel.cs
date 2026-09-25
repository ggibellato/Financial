using System.Collections.ObjectModel;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;

namespace Financial.Presentation.App.ViewModels.Investment;

/// <summary>
/// Disposals list + tax-year filter for the asset detail view (F05). Tax-year options are
/// rebuilt per Load() from the data actually present, so this manages its own
/// ObservableCollection rather than SelectableOptionGroup&lt;T&gt;, which assumes a fixed option
/// set built once in its constructor (PeriodFilter/ChartTypeMode).
/// </summary>
public class DisposalsTabViewModel : ViewModelBase
{
    private const string DefaultDisposalsContextKey = "default";
    private const string AllTaxYearsLabel = "All";

    private readonly RelayCommand _selectTaxYearCommand;
    private List<DisposalRecordDTO> _disposalRecords = new();
    private string? _selectedTaxYear;
    private string _disposalsContextKey = DefaultDisposalsContextKey;
    private readonly Dictionary<string, DisposalsViewState> _disposalsViewStateByKey = new(StringComparer.OrdinalIgnoreCase);

    public DisposalsTabViewModel()
    {
        _selectTaxYearCommand = new RelayCommand(SelectTaxYear);
    }

    public ObservableCollection<DisposalRecordRowViewModel> Disposals { get; } = new();
    public ObservableCollection<SelectableOptionViewModel<string?>> TaxYearFilters { get; } = new();

    public RelayCommand SelectTaxYearCommand => _selectTaxYearCommand;

    public bool HasNoDisposalsAtAll => !_disposalRecords.Any(record => record.Status == DisposalRecordStatus.Active);
    public bool HasAnyDisposals => !HasNoDisposalsAtAll;
    public bool HasNoDisposalsInSelectedTaxYear => HasAnyDisposals && Disposals.Count == 0;
    public bool HasVisibleDisposals => Disposals.Count > 0;

    public void Load(string contextKey, IReadOnlyList<DisposalRecordDTO> disposalRecords)
    {
        _disposalRecords = disposalRecords.ToList();
        SetDisposalsContext(contextKey, rebuild: false);
        RebuildTaxYearFilters();
        ApplyTaxYearFilter();
    }

    public void Clear()
    {
        _disposalRecords.Clear();
        Disposals.Clear();
        TaxYearFilters.Clear();
        _selectedTaxYear = null;
        _disposalsContextKey = DefaultDisposalsContextKey;

        OnPropertyChanged(nameof(HasNoDisposalsAtAll));
        OnPropertyChanged(nameof(HasAnyDisposals));
        OnPropertyChanged(nameof(HasNoDisposalsInSelectedTaxYear));
        OnPropertyChanged(nameof(HasVisibleDisposals));
    }

    private void SelectTaxYear(object? parameter)
    {
        if (parameter is SelectableOptionViewModel<string?> option) SetTaxYearFilter(option.Value);
    }

    private void SetTaxYearFilter(string? taxYear, bool rebuild = true)
    {
        if (_selectedTaxYear == taxYear && TaxYearFilters.Count > 0)
        {
            UpdateTaxYearFilterSelection();
            return;
        }
        _selectedTaxYear = taxYear;
        UpdateTaxYearFilterSelection();
        UpdateDisposalsViewState();
        if (rebuild) ApplyTaxYearFilter();
    }

    private void UpdateTaxYearFilterSelection()
    {
        foreach (var option in TaxYearFilters)
            option.IsSelected = option.Value == _selectedTaxYear;
    }

    private void RebuildTaxYearFilters()
    {
        var taxYearsPresent = _disposalRecords
            .Where(record => record.Status == DisposalRecordStatus.Active)
            .Select(record => record.TaxYear)
            .Distinct()
            .OrderByDescending(taxYear => taxYear, StringComparer.OrdinalIgnoreCase)
            .ToList();

        TaxYearFilters.Clear();
        TaxYearFilters.Add(new SelectableOptionViewModel<string?>(AllTaxYearsLabel, null));
        foreach (var taxYear in taxYearsPresent)
            TaxYearFilters.Add(new SelectableOptionViewModel<string?>(taxYear, taxYear));

        var state = GetDisposalsViewState(_disposalsContextKey);
        var restoredTaxYear = state.TaxYear != null && taxYearsPresent.Contains(state.TaxYear) ? state.TaxYear : null;
        SetTaxYearFilter(restoredTaxYear, rebuild: false);
    }

    private void ApplyTaxYearFilter()
    {
        var activeRecords = _disposalRecords.Where(record => record.Status == DisposalRecordStatus.Active);
        if (_selectedTaxYear != null)
            activeRecords = activeRecords.Where(record => record.TaxYear == _selectedTaxYear);

        var byTransactionId = _disposalRecords.ToLookup(record => record.TransactionId);

        var rows = activeRecords
            .Select(record => new DisposalRecordRowViewModel(record, BuildSupersededHistory(record, byTransactionId)))
            .ToList();

        Disposals.Clear();
        foreach (var row in rows)
            Disposals.Add(row);

        OnPropertyChanged(nameof(HasNoDisposalsAtAll));
        OnPropertyChanged(nameof(HasAnyDisposals));
        OnPropertyChanged(nameof(HasNoDisposalsInSelectedTaxYear));
        OnPropertyChanged(nameof(HasVisibleDisposals));
    }

    private static IReadOnlyList<DisposalRecordDTO> BuildSupersededHistory(
        DisposalRecordDTO activeRecord,
        ILookup<Guid, DisposalRecordDTO> byTransactionId)
    {
        var history = new List<DisposalRecordDTO>();
        var supersededById = byTransactionId[activeRecord.TransactionId]
            .Where(record => record.Status == DisposalRecordStatus.Superseded)
            .ToDictionary(record => record.Id);

        // Walk backwards from whichever Superseded record points at the current record,
        // following the chain until no predecessor remains.
        var current = FindPredecessorOf(activeRecord.Id, supersededById);
        while (current != null)
        {
            history.Insert(0, current);
            current = FindPredecessorOf(current.Id, supersededById);
        }

        return history;
    }

    private static DisposalRecordDTO? FindPredecessorOf(Guid recordId, Dictionary<Guid, DisposalRecordDTO> supersededById) =>
        supersededById.Values.FirstOrDefault(record => record.SupersededByRecordId == recordId);

    private void SetDisposalsContext(string contextKey, bool rebuild = true)
    {
        _disposalsContextKey = string.IsNullOrWhiteSpace(contextKey) ? DefaultDisposalsContextKey : contextKey;
        if (rebuild) ApplyTaxYearFilter();
    }

    private DisposalsViewState GetDisposalsViewState(string contextKey) =>
        _disposalsViewStateByKey.TryGetValue(contextKey, out var state) ? state : new DisposalsViewState(null);

    private void UpdateDisposalsViewState()
    {
        if (!string.IsNullOrWhiteSpace(_disposalsContextKey))
            _disposalsViewStateByKey[_disposalsContextKey] = new DisposalsViewState(_selectedTaxYear);
    }
}
