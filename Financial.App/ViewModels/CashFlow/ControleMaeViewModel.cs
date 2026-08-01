using System.Collections.ObjectModel;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;

namespace Financial.Presentation.App.ViewModels.CashFlow;

/// <summary>
/// ViewModel for the Controle Mãe tab: a BRL/GBP ledger filtered by a "From" date, an
/// all-time totals row, Create Entry (single-currency input, backend FX conversion), Edit
/// Entry (direct BRL/GBP override), and delete. Mirrors Financial.Web's useControleMae.ts
/// hook. Standalone from the other CashFlow ViewModels — its own top-level destination.
///
/// Totals are all-time (IControleMaeService.GetTotals() has no date parameter) and are
/// fetched independently of FromDate — confirmed with the user to match the web reference's
/// actual behavior (its totals useEffect has no fromDate dependency, and the totals row is
/// literally labeled "Total (all entries)"), not the PRD's slightly imprecise wording.
/// </summary>
public class ControleMaeViewModel : ViewModelBase
{
    public static readonly string[] Currencies = ["BRL", "GBP"];

    private readonly IControleMaeService _controleMaeService;
    private readonly Func<string, bool> _confirm;

    private DateTime _fromDate;
    private bool _isLoading = true;
    private string? _error;
    private MaeLedgerTotalsDTO? _totals;

    public DateTime FromDate
    {
        get => _fromDate;
        set
        {
            if (SetProperty(ref _fromDate, value))
            {
                _ = RefreshEntriesAsync();
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

    public MaeLedgerTotalsDTO? Totals
    {
        get => _totals;
        private set => SetProperty(ref _totals, value);
    }

    public ObservableCollection<MaeLedgerEntryDTO> Entries { get; } = [];

    public RelayCommand RetryCommand { get; }

    public ControleMaeViewModel(IControleMaeService controleMaeService, Func<string, bool> confirm)
    {
        _controleMaeService = controleMaeService ?? throw new ArgumentNullException(nameof(controleMaeService));
        _confirm = confirm ?? throw new ArgumentNullException(nameof(confirm));

        _fromDate = new DateTime(DateTime.Today.Year - 1, 1, 1);

        RetryCommand = new RelayCommand(async () =>
        {
            await RefreshEntriesAsync();
            await RefreshTotalsAsync();
        });
        InitializeCreateCommands();
        InitializeEditDeleteCommands();

        _ = RefreshEntriesAsync();
        _ = RefreshTotalsAsync();
    }

    private int _refreshEntriesRequestId;

    /// <summary>
    /// Reloads the ledger entries on/after FromDate. Guards against overlapping calls (e.g.
    /// the constructor's initial load racing a rapid FromDate change) by discarding a
    /// completion whose request has been superseded.
    /// </summary>
    internal async Task RefreshEntriesAsync()
    {
        var requestId = ++_refreshEntriesRequestId;
        IsLoading = true;
        Error = null;

        try
        {
            var fromDate = DateOnly.FromDateTime(FromDate);
            var entries = await Task.Run(() => _controleMaeService.GetEntriesFromDate(fromDate));

            if (requestId != _refreshEntriesRequestId)
            {
                return;
            }

            ReplaceAll(Entries, entries);
        }
        catch (Exception ex)
        {
            if (requestId == _refreshEntriesRequestId)
            {
                Error = ex.Message;
            }
        }
        finally
        {
            if (requestId == _refreshEntriesRequestId)
            {
                IsLoading = false;
            }
        }
    }

    /// <summary>
    /// Reloads the all-time totals. Deliberately independent of FromDate — see class summary.
    /// A failure here is non-fatal (matches the web: "totals are supplementary to the ledger
    /// list; a failed refresh just keeps the last known values").
    /// </summary>
    internal async Task RefreshTotalsAsync()
    {
        try
        {
            Totals = await Task.Run(() => _controleMaeService.GetTotals());
        }
        catch
        {
            // Supplementary data; keep the last known totals on failure.
        }
    }

    private static void ReplaceAll<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    /// <summary>Closes all inline forms — only one form panel may be open at a time.</summary>
    private void CloseAllForms()
    {
        CloseCreateForm();
        CloseEditForm();
    }

    #region Create Entry

    private bool _isCreateFormOpen;
    private DateTime? _createDate;
    private string _createDescription = string.Empty;
    private string _createNote = string.Empty;
    private string _createCurrency = Currencies[0];
    private string _createValue = string.Empty;
    private bool _isCreating;
    private string? _createSaveError;

    public bool IsCreateFormOpen
    {
        get => _isCreateFormOpen;
        private set => SetProperty(ref _isCreateFormOpen, value);
    }

    public DateTime? CreateDate
    {
        get => _createDate;
        set => SetProperty(ref _createDate, value);
    }

    public string CreateDescription
    {
        get => _createDescription;
        set => SetProperty(ref _createDescription, value);
    }

    public string CreateNote
    {
        get => _createNote;
        set => SetProperty(ref _createNote, value);
    }

    public string CreateCurrency
    {
        get => _createCurrency;
        set => SetProperty(ref _createCurrency, value);
    }

    public string CreateValue
    {
        get => _createValue;
        set => SetProperty(ref _createValue, value);
    }

    public bool IsCreating
    {
        get => _isCreating;
        private set => SetProperty(ref _isCreating, value);
    }

    public string? CreateSaveError
    {
        get => _createSaveError;
        private set => SetProperty(ref _createSaveError, value);
    }

    public RelayCommand ShowCreateFormCommand { get; private set; } = null!;
    public RelayCommand CancelCreateFormCommand { get; private set; } = null!;
    public RelayCommand SubmitCreateCommand { get; private set; } = null!;

    private void InitializeCreateCommands()
    {
        ShowCreateFormCommand = new RelayCommand(ShowCreateForm);
        CancelCreateFormCommand = new RelayCommand(CloseCreateForm);
        SubmitCreateCommand = new RelayCommand(async () => await SubmitCreateAsync());
    }

    private void ShowCreateForm()
    {
        CloseAllForms();
        CreateDate = DateTime.Today;
        CreateDescription = string.Empty;
        CreateNote = string.Empty;
        CreateCurrency = Currencies[0];
        CreateValue = string.Empty;
        CreateSaveError = null;
        IsCreateFormOpen = true;
    }

    private void CloseCreateForm()
    {
        IsCreateFormOpen = false;
        CreateSaveError = null;
    }

    internal async Task SubmitCreateAsync()
    {
        var validationMessage = CreateEntryFormValidation.BuildValidationMessage(CreateDate, CreateDescription, CreateValue);
        if (!string.IsNullOrEmpty(validationMessage))
        {
            CreateSaveError = validationMessage;
            return;
        }

        IsCreating = true;
        CreateSaveError = null;

        try
        {
            await _controleMaeService.CreateEntryAsync(new CreateMaeLedgerEntryDTO
            {
                Date = DateOnly.FromDateTime(CreateDate!.Value),
                Description = CreateDescription,
                Note = string.IsNullOrWhiteSpace(CreateNote) ? string.Empty : CreateNote,
                SourceCurrency = CreateCurrency,
                SourceValue = decimal.Parse(CreateValue),
            });

            CloseCreateForm();
            await RefreshEntriesAsync();
            await RefreshTotalsAsync();
        }
        catch (Exception ex)
        {
            CreateSaveError = ex.Message;
        }
        finally
        {
            IsCreating = false;
        }
    }

    #endregion

    #region Edit and Delete Entry

    private bool _isEditFormOpen;
    private Guid? _editingEntryId;
    private string _editBrlValue = string.Empty;
    private string _editGbpValue = string.Empty;
    private bool _isSaving;
    private string? _editSaveError;
    private string? _deleteError;

    public bool IsEditFormOpen
    {
        get => _isEditFormOpen;
        private set => SetProperty(ref _isEditFormOpen, value);
    }

    public string EditBrlValue
    {
        get => _editBrlValue;
        set => SetProperty(ref _editBrlValue, value);
    }

    public string EditGbpValue
    {
        get => _editGbpValue;
        set => SetProperty(ref _editGbpValue, value);
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

    public string? DeleteError
    {
        get => _deleteError;
        private set => SetProperty(ref _deleteError, value);
    }

    public RelayCommand<MaeLedgerEntryDTO> EditEntryCommand { get; private set; } = null!;
    public RelayCommand CancelEditFormCommand { get; private set; } = null!;
    public RelayCommand SaveEditCommand { get; private set; } = null!;
    public RelayCommand<MaeLedgerEntryDTO> DeleteEntryCommand { get; private set; } = null!;

    private void InitializeEditDeleteCommands()
    {
        EditEntryCommand = new RelayCommand<MaeLedgerEntryDTO>(ShowEditForm);
        CancelEditFormCommand = new RelayCommand(CloseEditForm);
        SaveEditCommand = new RelayCommand(async () => await SaveEditAsync());
        DeleteEntryCommand = new RelayCommand<MaeLedgerEntryDTO>(async entry => await DeleteEntryAsync(entry));
    }

    private void ShowEditForm(MaeLedgerEntryDTO? entry)
    {
        if (entry is null)
        {
            return;
        }

        CloseAllForms();
        _editingEntryId = entry.Id;
        EditBrlValue = entry.BrlValue?.ToString() ?? string.Empty;
        EditGbpValue = entry.GbpValue?.ToString() ?? string.Empty;
        EditSaveError = null;
        IsEditFormOpen = true;
    }

    private void CloseEditForm()
    {
        IsEditFormOpen = false;
        EditSaveError = null;
        _editingEntryId = null;
    }

    internal async Task SaveEditAsync()
    {
        if (_editingEntryId is not { } id)
        {
            return;
        }

        var validationMessage = EditEntryFormValidation.BuildValidationMessage(EditBrlValue, EditGbpValue);
        if (!string.IsNullOrEmpty(validationMessage))
        {
            EditSaveError = validationMessage;
            return;
        }

        IsSaving = true;
        EditSaveError = null;

        try
        {
            await _controleMaeService.UpdateEntryValuesAsync(id, new UpdateMaeLedgerEntryValuesDTO
            {
                BrlValue = string.IsNullOrWhiteSpace(EditBrlValue) ? null : decimal.Parse(EditBrlValue),
                GbpValue = string.IsNullOrWhiteSpace(EditGbpValue) ? null : decimal.Parse(EditGbpValue),
            });

            CloseEditForm();
            await RefreshEntriesAsync();
            await RefreshTotalsAsync();
        }
        catch (Exception ex)
        {
            EditSaveError = ex.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }

    internal async Task DeleteEntryAsync(MaeLedgerEntryDTO? entry)
    {
        if (entry is null)
        {
            return;
        }

        if (!_confirm($"Delete \"{entry.Description}\"? This removes it for good."))
        {
            return;
        }

        DeleteError = null;

        try
        {
            await _controleMaeService.DeleteEntryAsync(entry.Id);
            await RefreshEntriesAsync();
            await RefreshTotalsAsync();
        }
        catch (Exception ex)
        {
            DeleteError = ex.Message;
        }
    }

    #endregion
}
