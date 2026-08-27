using System.Collections.ObjectModel;
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
        private set => SetProperty(ref _editSaveError, value);
    }

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
                await _investmentSnapshotService.UpdateSnapshotValueAsync(id, new UpdateInvestmentSnapshotValueDTO
                {
                    Value = decimal.Parse(EditValue),
                });

                CloseEditForm();
                await RefreshAsync();
            });
    }

    #endregion
}
