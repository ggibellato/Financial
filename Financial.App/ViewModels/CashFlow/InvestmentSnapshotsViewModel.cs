using System.Collections.ObjectModel;
using System.ComponentModel;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using static Financial.Presentation.App.Helpers.ObservableCollectionHelper;

namespace Financial.Presentation.App.ViewModels.CashFlow;

public class InvestmentSnapshotsViewModel : ViewModelBase
{
    private readonly IInvestmentSnapshotService _investmentSnapshotService;

    private int _year;
    private int _month;
    private bool _isLoading = true;
    private string? _error;

    public int Year
    {
        get => _year;
        set
        {
            if (SetProperty(ref _year, value))
            {
                _ = RefreshAsync();
            }
        }
    }

    public int Month
    {
        get => _month;
        set
        {
            if (SetProperty(ref _month, value))
            {
                _ = RefreshAsync();
            }
        }
    }

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

    public ObservableCollection<SnapshotRow> Snapshots { get; } = [];

    public decimal NetTotal => Snapshots.Sum(s => s.IsLiability ? -s.Value : s.Value);

    public RelayCommand RetryCommand { get; }

    public InvestmentSnapshotsViewModel(IInvestmentSnapshotService investmentSnapshotService)
    {
        _investmentSnapshotService = investmentSnapshotService ?? throw new ArgumentNullException(nameof(investmentSnapshotService));

        var today = DateTime.Today;
        _year = today.Year;
        _month = today.Month;

        RetryCommand = new RelayCommand(async () => await RefreshAsync());
        InitializeEditCommands();
        InitializeSuggestValuesCommands();

        _ = RefreshAsync();
    }

    private int _refreshRequestId;

    /// <summary>
    /// Reloads the snapshots for the selected Year/Month. Guards against overlapping calls
    /// (e.g. the constructor's initial load racing a rapid Year/Month change) by discarding a
    /// completion whose request has been superseded.
    /// </summary>
    internal Task RefreshAsync() => ExecuteRefreshAsync(
        () => ++_refreshRequestId,
        id => id == _refreshRequestId,
        loading => IsLoading = loading,
        error => Error = error,
        async isCurrent =>
        {
            var year = Year;
            var month = Month;
            var snapshots = await _investmentSnapshotService.GetSnapshotsForMonthAsync(year, month);

            if (!isCurrent())
            {
                return;
            }

            ReplaceAll(Snapshots, snapshots.Select(SnapshotRow.FromDto));
            OnPropertyChanged(nameof(NetTotal));
        });

    #region Edit Value

    private bool _isEditFormOpen;
    private Guid? _editingSnapshotId;
    private string _editValue = string.Empty;
    private bool _isSaving;
    private string? _editSaveError;

    public bool IsEditFormOpen
    {
        get => _isEditFormOpen;
        private set => SetProperty(ref _isEditFormOpen, value);
    }

    public string EditValue
    {
        get => _editValue;
        set => SetProperty(ref _editValue, value);
    }

    public bool IsSaving
    {
        get => _isSaving;
        private set => SetProperty(ref _isSaving, value);
    }

    public string? EditSaveError
    {
        get => _editSaveError;
        private set
        {
            if (SetProperty(ref _editSaveError, value))
            {
                OnPropertyChanged(nameof(EditValueFieldError));
                OnPropertyChanged(nameof(EditGeneralSaveError));
            }
        }
    }

    public string? EditValueFieldError => MatchEditFieldError("Value must be a non-negative number.");

    private string? MatchEditFieldError(params string[] fragments) =>
        EditSaveError?.Split(Environment.NewLine)
            .FirstOrDefault(line => fragments.Any(f => line.Contains(f, StringComparison.OrdinalIgnoreCase)));

    /// <summary>Bottom-of-form message — shown only when the error isn't already attributed to a field above.</summary>
    public string? EditGeneralSaveError => EditValueFieldError is null ? EditSaveError : null;

    public RelayCommand<SnapshotRow> EditSnapshotCommand { get; private set; } = null!;
    public RelayCommand CancelEditFormCommand { get; private set; } = null!;
    public RelayCommand SaveEditCommand { get; private set; } = null!;

    private void InitializeEditCommands()
    {
        EditSnapshotCommand = new RelayCommand<SnapshotRow>(ShowEditForm);
        CancelEditFormCommand = new RelayCommand(CloseEditForm);
        SaveEditCommand = new RelayCommand(async () => await SaveEditAsync());
    }

    private void ShowEditForm(SnapshotRow? row)
    {
        if (row is null)
        {
            return;
        }

        CloseSuggestPanel();
        _editingSnapshotId = row.Id;
        EditValue = row.Value.ToString();
        EditSaveError = null;
        IsEditFormOpen = true;
    }

    private void CloseEditForm()
    {
        IsEditFormOpen = false;
        EditSaveError = null;
        _editingSnapshotId = null;
    }

    internal Task SaveEditAsync()
    {
        if (_editingSnapshotId is not { } id)
        {
            return Task.CompletedTask;
        }

        return ExecuteSaveAsync(
            () => EditSnapshotValueFormValidation.BuildValidationMessage(EditValue),
            error => EditSaveError = error,
            saving => IsSaving = saving,
            async () =>
            {
                await _investmentSnapshotService.UpdateSnapshotValueAsync(id, new InvestmentSnapshotValueUpdateDTO
                {
                    Value = decimal.Parse(EditValue),
                });

                CloseEditForm();
                await RefreshAsync();
            });
    }

    #endregion

    #region Suggest Values

    private bool _isSuggestPanelOpen;
    private bool _isSuggestionsLoading;
    private string? _suggestionsError;
    private bool _isApplyingSuggestions;
    private bool _hasCompletedApply;
    private int _checkedSuggestionsCount;
    private int _applyProgressCurrent;
    private int _applyProgressTotal;
    private string _applyProgressAccountName = string.Empty;
    private int _succeededCount;

    public bool IsSuggestPanelOpen
    {
        get => _isSuggestPanelOpen;
        private set => SetProperty(ref _isSuggestPanelOpen, value);
    }

    public bool IsSuggestionsLoading
    {
        get => _isSuggestionsLoading;
        private set
        {
            if (SetProperty(ref _isSuggestionsLoading, value))
            {
                OnPropertyChanged(nameof(ShowSuggestionsContent));
            }
        }
    }

    public string? SuggestionsError
    {
        get => _suggestionsError;
        private set
        {
            if (SetProperty(ref _suggestionsError, value))
            {
                OnPropertyChanged(nameof(HasSuggestionsError));
                OnPropertyChanged(nameof(ShowSuggestionsContent));
            }
        }
    }

    public bool HasSuggestionsError => SuggestionsError != null;

    public bool ShowSuggestionsContent => !IsSuggestionsLoading && !HasSuggestionsError;

    public bool IsApplyingSuggestions
    {
        get => _isApplyingSuggestions;
        private set
        {
            if (SetProperty(ref _isApplyingSuggestions, value))
            {
                OnPropertyChanged(nameof(CanEditSuggestions));
            }
        }
    }

    public bool CanEditSuggestions => !IsApplyingSuggestions;

    public bool HasCompletedApply
    {
        get => _hasCompletedApply;
        private set => SetProperty(ref _hasCompletedApply, value);
    }

    public int CheckedSuggestionsCount
    {
        get => _checkedSuggestionsCount;
        private set => SetProperty(ref _checkedSuggestionsCount, value);
    }

    public int ApplyProgressCurrent
    {
        get => _applyProgressCurrent;
        private set
        {
            if (SetProperty(ref _applyProgressCurrent, value))
            {
                OnPropertyChanged(nameof(ApplyProgressText));
                OnPropertyChanged(nameof(ApplyProgressPercent));
            }
        }
    }

    public int ApplyProgressTotal
    {
        get => _applyProgressTotal;
        private set
        {
            if (SetProperty(ref _applyProgressTotal, value))
            {
                OnPropertyChanged(nameof(ApplyProgressText));
                OnPropertyChanged(nameof(ApplyProgressPercent));
            }
        }
    }

    public string ApplyProgressAccountName
    {
        get => _applyProgressAccountName;
        private set
        {
            if (SetProperty(ref _applyProgressAccountName, value))
            {
                OnPropertyChanged(nameof(ApplyProgressText));
            }
        }
    }

    public string ApplyProgressText =>
        $"Applying {ApplyProgressCurrent} of {ApplyProgressTotal}: {ApplyProgressAccountName}...";

    public double ApplyProgressPercent => ApplyProgressTotal == 0 ? 0 : (double)ApplyProgressCurrent / ApplyProgressTotal * 100;

    public int SucceededCount
    {
        get => _succeededCount;
        private set => SetProperty(ref _succeededCount, value);
    }

    public ObservableCollection<SuggestionRow> SuggestionRows { get; } = [];

    public ObservableCollection<SuggestionSkippedRow> NotUpdatedSuggestionRows { get; } = [];

    public bool HasSuggestionRows => SuggestionRows.Count > 0;

    public bool ShowEmptySuggestionsMessage => !HasSuggestionRows;

    public bool HasNotUpdatedSuggestions => NotUpdatedSuggestionRows.Count > 0;

    public IReadOnlyList<SuggestionRow> FailedSuggestionRows =>
        SuggestionRows.Where(r => r.Status == SuggestionRowStatus.Error).ToList();

    public bool HasFailedSuggestions => FailedSuggestionRows.Count > 0;

    public int AttemptedSuggestionsCount => SuggestionRows.Count(r => r.Status != SuggestionRowStatus.Pending);

    public string CompletionSummaryText
    {
        get
        {
            var failed = FailedSuggestionRows;
            var summary = $"Applied {SucceededCount} of {AttemptedSuggestionsCount}.";
            return failed.Count > 0
                ? $"{summary} {failed.Count} failed: {string.Join(", ", failed.Select(r => r.AccountName))} — try again."
                : summary;
        }
    }

    public RelayCommand SuggestValuesCommand { get; private set; } = null!;
    public RelayCommand CancelSuggestValuesCommand { get; private set; } = null!;
    public RelayCommand RetrySuggestionsFetchCommand { get; private set; } = null!;
    public RelayCommand ApplySuggestionsCommand { get; private set; } = null!;
    public RelayCommand RetryFailedSuggestionsCommand { get; private set; } = null!;

    private void InitializeSuggestValuesCommands()
    {
        SuggestValuesCommand = new RelayCommand(OpenSuggestPanel);
        CancelSuggestValuesCommand = new RelayCommand(CloseSuggestPanel);
        RetrySuggestionsFetchCommand = new RelayCommand(async () => await FetchSuggestionsAsync());
        ApplySuggestionsCommand = new RelayCommand(
            async () => await ApplySuggestionRowsAsync(SuggestionRows.Where(r => r.Included).ToList()),
            () => CheckedSuggestionsCount > 0);
        RetryFailedSuggestionsCommand = new RelayCommand(
            async () => await ApplySuggestionRowsAsync(FailedSuggestionRows));
    }

    private void OpenSuggestPanel()
    {
        CloseEditForm();
        IsSuggestPanelOpen = true;
        _ = FetchSuggestionsAsync();
    }

    private void CloseSuggestPanel()
    {
        DetachSuggestionRowHandlers();
        SuggestionRows.Clear();
        NotUpdatedSuggestionRows.Clear();
        IsSuggestPanelOpen = false;
        IsSuggestionsLoading = false;
        SuggestionsError = null;
        IsApplyingSuggestions = false;
        HasCompletedApply = false;
        CheckedSuggestionsCount = 0;
        SucceededCount = 0;
    }

    private async Task FetchSuggestionsAsync()
    {
        DetachSuggestionRowHandlers();
        IsSuggestionsLoading = true;
        SuggestionsError = null;
        HasCompletedApply = false;

        try
        {
            var result = await _investmentSnapshotService.GetSuggestionsForMonthAsync(Year, Month);

            ReplaceAll(SuggestionRows, result.Suggestions.Select(dto => new SuggestionRow(dto)));
            ReplaceAll(NotUpdatedSuggestionRows, result.NotUpdated.Select(SuggestionSkippedRow.FromDto));
            AttachSuggestionRowHandlers();
            RecomputeCheckedSuggestionsCount();
            OnPropertyChanged(nameof(HasSuggestionRows));
            OnPropertyChanged(nameof(ShowEmptySuggestionsMessage));
            OnPropertyChanged(nameof(HasNotUpdatedSuggestions));
        }
        catch (Exception ex)
        {
            SuggestionsError = ex.Message;
        }
        finally
        {
            IsSuggestionsLoading = false;
        }
    }

    private async Task ApplySuggestionRowsAsync(IReadOnlyList<SuggestionRow> rowsToApply)
    {
        IsApplyingSuggestions = true;
        HasCompletedApply = false;
        var anySucceeded = false;
        var anyFailed = false;

        for (var i = 0; i < rowsToApply.Count; i++)
        {
            var row = rowsToApply[i];
            ApplyProgressCurrent = i + 1;
            ApplyProgressTotal = rowsToApply.Count;
            ApplyProgressAccountName = row.AccountName;

            if (!decimal.TryParse(row.SuggestedValue, out var value) || value < 0)
            {
                row.Status = SuggestionRowStatus.Error;
                anyFailed = true;
                continue;
            }

            try
            {
                await _investmentSnapshotService.UpdateSnapshotValueAsync(
                    row.SnapshotId, new InvestmentSnapshotValueUpdateDTO { Value = value });
                row.Status = SuggestionRowStatus.Success;
                anySucceeded = true;
            }
            catch
            {
                row.Status = SuggestionRowStatus.Error;
                anyFailed = true;
            }
        }

        SucceededCount = SuggestionRows.Count(r => r.Status == SuggestionRowStatus.Success);
        IsApplyingSuggestions = false;
        RecomputeCheckedSuggestionsCount();
        OnPropertyChanged(nameof(FailedSuggestionRows));
        OnPropertyChanged(nameof(HasFailedSuggestions));
        OnPropertyChanged(nameof(AttemptedSuggestionsCount));
        OnPropertyChanged(nameof(CompletionSummaryText));

        if (anySucceeded)
        {
            await RefreshAsync();
        }

        if (anyFailed)
        {
            HasCompletedApply = true;
        }
        else
        {
            CloseSuggestPanel();
        }
    }

    private void AttachSuggestionRowHandlers()
    {
        foreach (var row in SuggestionRows)
        {
            row.PropertyChanged += OnSuggestionRowPropertyChanged;
        }
    }

    private void DetachSuggestionRowHandlers()
    {
        foreach (var row in SuggestionRows)
        {
            row.PropertyChanged -= OnSuggestionRowPropertyChanged;
        }
    }

    private void OnSuggestionRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SuggestionRow.Included))
        {
            RecomputeCheckedSuggestionsCount();
        }
    }

    private void RecomputeCheckedSuggestionsCount()
    {
        CheckedSuggestionsCount = SuggestionRows.Count(r => r.Included);
        ApplySuggestionsCommand.RaiseCanExecuteChanged();
    }

    #endregion
}
