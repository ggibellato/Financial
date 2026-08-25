using System.Collections.ObjectModel;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using static Financial.Presentation.App.Helpers.ObservableCollectionHelper;

namespace Financial.Presentation.App.ViewModels.CashFlow;

public class IncomeWorkflowViewModel : ViewModelBase
{
    public const string NoBankOptionLabel = "(None)";

    private const int IncomeSplitConfirmationHideDelayMs = 4000;

    private static readonly HashSet<string> IncomeSourcesWithGrossValue = ["Gleison", "Ariana"];

    /// <summary>Matches the picklist's historical display order. A source name outside this list
    /// (unexpected but not invalid) sorts last rather than being dropped or erroring.</summary>
    private static readonly string[] IncomeSourceDisplayOrder = ["Gleison", "Ariana", "Lottery", "DividendoJuros"];

    private readonly IIncomeService _incomeService;
    private readonly Func<string, bool> _confirm;
    private readonly Func<Task> _refresh;

    private bool _isIncomeFormOpen;
    private Guid? _editingIncomeId;
    private DateTime? _incomeFormDate;
    private Guid? _incomeFormSource;
    private string _incomeFormGrossValue = string.Empty;
    private string _incomeFormNetValue = string.Empty;
    private Guid? _incomeFormBank;
    private string _incomeFormDescription = string.Empty;
    private bool _incomeFormSplitToReserve;
    private bool _isSavingIncome;
    private string? _incomeSaveError;
    private string? _deletingIncomeError;
    private string? _incomeSplitConfirmationMessage;
    private IReadOnlyList<IncomeBankOptionViewModel> _incomeBankOptions = BuildIncomeBankOptions([]);

    public ObservableCollection<IncomeDTO> Incomes { get; } = [];
    public ObservableCollection<IncomeSourceDTO> IncomeSources { get; } = [];

    public IReadOnlyList<IncomeSourceDTO> IncomeSourceOptions =>
        IncomeSources
            .Where(s => s.IsActive)
            .OrderBy(s => IncomeSourceRank(s.Name))
            .ToList();

    public bool IsIncomeFormOpen
    {
        get => _isIncomeFormOpen;
        private set => SetProperty(ref _isIncomeFormOpen, value);
    }

    public bool IsEditingIncome => _editingIncomeId != null;

    public DateTime? IncomeFormDate
    {
        get => _incomeFormDate;
        set => SetProperty(ref _incomeFormDate, value);
    }

    public Guid? IncomeFormSource
    {
        get => _incomeFormSource;
        set
        {
            if (SetProperty(ref _incomeFormSource, value))
            {
                OnPropertyChanged(nameof(ShowIncomeGrossValueField));
                OnPropertyChanged(nameof(ShowIncomeSplitField));
                IncomeFormSplitToReserve = ShowIncomeSplitField;
            }
        }
    }

    public bool ShowIncomeGrossValueField =>
        IncomeSourcesWithGrossValue.Contains(IncomeSources.FirstOrDefault(s => s.Id == IncomeFormSource)?.Name ?? string.Empty);

    public bool ShowIncomeSplitField =>
        IncomeSources.FirstOrDefault(s => s.Id == IncomeFormSource)?.AutoSplitToReserve == true;

    public string IncomeFormGrossValue
    {
        get => _incomeFormGrossValue;
        set => SetProperty(ref _incomeFormGrossValue, value);
    }

    public string IncomeFormNetValue
    {
        get => _incomeFormNetValue;
        set => SetProperty(ref _incomeFormNetValue, value);
    }

    public Guid? IncomeFormBank
    {
        get => _incomeFormBank;
        set => SetProperty(ref _incomeFormBank, value);
    }

    public string IncomeFormDescription
    {
        get => _incomeFormDescription;
        set => SetProperty(ref _incomeFormDescription, value);
    }

    public bool IncomeFormSplitToReserve
    {
        get => _incomeFormSplitToReserve;
        set => SetProperty(ref _incomeFormSplitToReserve, value);
    }

    public IReadOnlyList<IncomeBankOptionViewModel> IncomeBankOptions
    {
        get => _incomeBankOptions;
        private set => SetProperty(ref _incomeBankOptions, value);
    }

    public bool IsSavingIncome
    {
        get => _isSavingIncome;
        private set => SetProperty(ref _isSavingIncome, value);
    }

    public string? IncomeSaveError
    {
        get => _incomeSaveError;
        private set => SetProperty(ref _incomeSaveError, value);
    }

    public string? DeletingIncomeError
    {
        get => _deletingIncomeError;
        private set => SetProperty(ref _deletingIncomeError, value);
    }

    public string? IncomeSplitConfirmationMessage
    {
        get => _incomeSplitConfirmationMessage;
        private set => SetProperty(ref _incomeSplitConfirmationMessage, value);
    }

    public RelayCommand ShowCreateIncomeFormCommand { get; }
    public RelayCommand CancelIncomeFormCommand { get; }
    public RelayCommand SaveIncomeCommand { get; }
    public RelayCommand<IncomeDTO> EditIncomeCommand { get; }
    public RelayCommand<IncomeDTO> DeleteIncomeCommand { get; }

    public IncomeWorkflowViewModel(IIncomeService incomeService, Func<string, bool> confirm, Func<Task> refresh)
    {
        _incomeService = incomeService ?? throw new ArgumentNullException(nameof(incomeService));
        _confirm = confirm ?? throw new ArgumentNullException(nameof(confirm));
        _refresh = refresh ?? throw new ArgumentNullException(nameof(refresh));

        ShowCreateIncomeFormCommand = new RelayCommand(ShowCreateIncomeForm);
        CancelIncomeFormCommand = new RelayCommand(CloseIncomeForm);
        SaveIncomeCommand = new RelayCommand(async () => await SaveIncomeAsync(), () => !IsSavingIncome);
        EditIncomeCommand = new RelayCommand<IncomeDTO>(ShowEditIncomeForm);
        DeleteIncomeCommand = new RelayCommand<IncomeDTO>(async income => await DeleteIncomeAsync(income));
    }

    /// <summary>Applies data the coordinator's own refresh already fetched — this workflow never fetches on its own.</summary>
    public void ApplyRefresh(IReadOnlyList<IncomeDTO> incomes, IReadOnlyList<IncomeSourceDTO> incomeSources, IReadOnlyList<BankDTO> banks)
    {
        ReplaceAll(Incomes, incomes);
        ReplaceAll(IncomeSources, incomeSources);
        OnPropertyChanged(nameof(IncomeSourceOptions));
        IncomeBankOptions = BuildIncomeBankOptions(banks);
    }

    private static int IncomeSourceRank(string name)
    {
        var index = Array.IndexOf(IncomeSourceDisplayOrder, name);
        return index == -1 ? IncomeSourceDisplayOrder.Length : index;
    }

    /// <summary>Options for the Income form's Bank dropdown: a "(None)" placeholder plus each configured bank.</summary>
    private static IReadOnlyList<IncomeBankOptionViewModel> BuildIncomeBankOptions(IReadOnlyList<BankDTO> banks) =>
        new[] { new IncomeBankOptionViewModel(null, NoBankOptionLabel) }
            .Concat(banks.Select(b => new IncomeBankOptionViewModel(b.Id, b.Name)))
            .ToList();

    private void ShowCreateIncomeForm()
    {
        _editingIncomeId = null;
        IncomeFormDate = DateTime.Today;
        IncomeFormSource = IncomeSourceOptions.Count > 0 ? IncomeSourceOptions[0].Id : null;
        IncomeFormGrossValue = string.Empty;
        IncomeFormNetValue = string.Empty;
        IncomeFormBank = null;
        IncomeFormDescription = string.Empty;
        IncomeFormSplitToReserve = ShowIncomeSplitField;
        IncomeSaveError = null;
        OnPropertyChanged(nameof(IsEditingIncome));
        IsIncomeFormOpen = true;
    }

    private void ShowEditIncomeForm(IncomeDTO? income)
    {
        if (income is null)
        {
            return;
        }

        _editingIncomeId = income.Id;
        IncomeFormDate = income.Date.ToDateTime(TimeOnly.MinValue);
        IncomeFormSource = income.IncomeSourceId;
        IncomeFormGrossValue = income.GrossValue?.ToString("0.##") ?? string.Empty;
        IncomeFormNetValue = income.NetValue.ToString("0.##");
        IncomeFormBank = income.BankId;
        IncomeFormDescription = income.Description ?? string.Empty;
        IncomeFormSplitToReserve = income.SplitToReserve;
        IncomeSaveError = null;
        OnPropertyChanged(nameof(IsEditingIncome));
        IsIncomeFormOpen = true;
    }

    private void CloseIncomeForm()
    {
        IsIncomeFormOpen = false;
        _editingIncomeId = null;
        IncomeSaveError = null;
    }

    internal Task SaveIncomeAsync() => ExecuteSaveAsync(
        () => IncomeFormValidation.BuildValidationMessage(IncomeFormDate, IncomeFormSource, IncomeFormNetValue),
        error => IncomeSaveError = error,
        saving => IsSavingIncome = saving,
        async () =>
        {
            var date = DateOnly.FromDateTime(IncomeFormDate!.Value);
            var netValue = decimal.Parse(IncomeFormNetValue);
            decimal? grossValue = ShowIncomeGrossValueField && decimal.TryParse(IncomeFormGrossValue, out var parsedGross)
                ? parsedGross
                : null;

            var description = string.IsNullOrWhiteSpace(IncomeFormDescription) ? null : IncomeFormDescription;
            var splitToReserve = ShowIncomeSplitField && IncomeFormSplitToReserve;

            IncomeDTO savedIncome;
            if (_editingIncomeId is { } id)
            {
                savedIncome = await _incomeService.UpdateIncomeAsync(id, new IncomeUpdateDTO
                {
                    Date = date,
                    IncomeSourceId = IncomeFormSource!.Value,
                    GrossValue = grossValue,
                    NetValue = netValue,
                    BankId = IncomeFormBank,
                    Description = description,
                    SplitToReserve = splitToReserve,
                });
            }
            else
            {
                savedIncome = await _incomeService.AddIncomeAsync(new IncomeCreateDTO
                {
                    Date = date,
                    IncomeSourceId = IncomeFormSource!.Value,
                    GrossValue = grossValue,
                    NetValue = netValue,
                    BankId = IncomeFormBank,
                    Description = description,
                    SplitToReserve = splitToReserve,
                });
            }

            CloseIncomeForm();
            await _refresh();

            if (savedIncome.SplitToReserve)
            {
                IncomeSplitConfirmationMessage = "Income saved and split to reserve";
                await Task.Delay(IncomeSplitConfirmationHideDelayMs);
                IncomeSplitConfirmationMessage = null;
            }
        },
        SaveIncomeCommand.RaiseCanExecuteChanged);

    internal async Task DeleteIncomeAsync(IncomeDTO? income)
    {
        if (income is null)
        {
            return;
        }

        if (!_confirm($"Delete this income entry from {income.IncomeSourceName}? This removes it for good."))
        {
            return;
        }

        DeletingIncomeError = null;

        try
        {
            await _incomeService.DeleteIncomeAsync(income.Id);
            await _refresh();
        }
        catch (Exception ex)
        {
            DeletingIncomeError = ex.Message;
        }
    }
}
