using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows.Media;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Presentation.App.Services;
using Microsoft.Extensions.Logging;
using Wpf.Ui.Controls;
using static Financial.Presentation.App.Helpers.ObservableCollectionHelper;

namespace Financial.Presentation.App.ViewModels.Investment;

public class TaxWorkbookViewModel : ViewModelBase
{
    private static readonly string[] CsvColumns =
    [
        "Date", "Jurisdiction", "TaxYear", "EventCategory", "Proceeds", "CostBasis", "GainLoss",
        "GrossAmount", "WithheldAmount", "NetAmount", "Currency", "CalculationStatus", "EvidenceReference",
    ];

    private readonly ITaxWorkbookService _taxWorkbookService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<TaxWorkbookViewModel> _logger;
    private List<TaxWorkbookOptionDTO> _options = [];
    private TaxWorkbookDTO? _workbook;

    private bool _isLoadingOptions = true;
    private string? _optionsError;
    private string? _selectedJurisdiction;
    private string? _selectedTaxYear;
    private bool _isLoadingWorkbook;
    private string? _workbookError;
    private bool _hasAggregateStatus;
    private SolidColorBrush _aggregateStatusBrush = Brushes.Transparent;
    private SolidColorBrush _aggregateStatusForeground = Brushes.Transparent;
    private SymbolRegular _aggregateStatusSymbol;
    private bool _aggregateStatusSymbolFilled;
    private string _aggregateStatusLabel = string.Empty;

    public bool IsLoadingOptions
    {
        get => _isLoadingOptions;
        private set
        {
            if (SetProperty(ref _isLoadingOptions, value))
            {
                OnPropertyChanged(nameof(ShowContent));
            }
        }
    }

    public string? OptionsError
    {
        get => _optionsError;
        private set
        {
            if (SetProperty(ref _optionsError, value))
            {
                OnPropertyChanged(nameof(HasOptionsError));
                OnPropertyChanged(nameof(ShowContent));
            }
        }
    }

    public bool HasOptionsError => OptionsError != null;

    public bool ShowContent => !IsLoadingOptions && !HasOptionsError;

    public bool IsLoadingWorkbook
    {
        get => _isLoadingWorkbook;
        private set
        {
            if (SetProperty(ref _isLoadingWorkbook, value))
            {
                OnPropertyChanged(nameof(ShowWorkbookContent));
            }
        }
    }

    public string? WorkbookError
    {
        get => _workbookError;
        private set
        {
            if (SetProperty(ref _workbookError, value))
            {
                OnPropertyChanged(nameof(HasWorkbookError));
                OnPropertyChanged(nameof(ShowWorkbookContent));
            }
        }
    }

    public bool HasWorkbookError => WorkbookError != null;

    public bool ShowWorkbookContent => !IsLoadingWorkbook && !HasWorkbookError;

    public ObservableCollection<string> Jurisdictions { get; } = [];

    public ObservableCollection<string> TaxYearsForJurisdiction { get; } = [];

    public ObservableCollection<TaxWorkbookEntryRowViewModel> Entries { get; } = [];

    public ObservableCollection<TaxCategoryTotalDTO> CategoryTotals { get; } = [];

    public string? SelectedJurisdiction
    {
        get => _selectedJurisdiction;
        set
        {
            if (!SetProperty(ref _selectedJurisdiction, value))
            {
                return;
            }

            ReplaceAll(TaxYearsForJurisdiction, _options.Where(o => o.Jurisdiction.ToString() == value).Select(o => o.TaxYear));
            _selectedTaxYear = _options.FirstOrDefault(o => o.Jurisdiction.ToString() == value)?.TaxYear;
            OnPropertyChanged(nameof(SelectedTaxYear));
            _ = RefreshWorkbookAsync();
        }
    }

    public string? SelectedTaxYear
    {
        get => _selectedTaxYear;
        set
        {
            if (SetProperty(ref _selectedTaxYear, value))
            {
                _ = RefreshWorkbookAsync();
            }
        }
    }

    public bool HasAggregateStatus
    {
        get => _hasAggregateStatus;
        private set => SetProperty(ref _hasAggregateStatus, value);
    }

    public SolidColorBrush AggregateStatusBrush
    {
        get => _aggregateStatusBrush;
        private set => SetProperty(ref _aggregateStatusBrush, value);
    }

    public SolidColorBrush AggregateStatusForeground
    {
        get => _aggregateStatusForeground;
        private set => SetProperty(ref _aggregateStatusForeground, value);
    }

    public SymbolRegular AggregateStatusSymbol
    {
        get => _aggregateStatusSymbol;
        private set => SetProperty(ref _aggregateStatusSymbol, value);
    }

    public bool AggregateStatusSymbolFilled
    {
        get => _aggregateStatusSymbolFilled;
        private set => SetProperty(ref _aggregateStatusSymbolFilled, value);
    }

    public string AggregateStatusLabel
    {
        get => _aggregateStatusLabel;
        private set => SetProperty(ref _aggregateStatusLabel, value);
    }

    public bool CanExportCsv => Entries.Count > 0;

    public RelayCommand RetryOptionsCommand { get; }

    public RelayCommand RetryWorkbookCommand { get; }

    public RelayCommand ExportCsvCommand { get; }

    public TaxWorkbookViewModel(ITaxWorkbookService taxWorkbookService, IDialogService dialogService, ILogger<TaxWorkbookViewModel> logger)
    {
        _taxWorkbookService = taxWorkbookService ?? throw new ArgumentNullException(nameof(taxWorkbookService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        RetryOptionsCommand = new RelayCommand(async () => await RefreshOptionsAsync());
        RetryWorkbookCommand = new RelayCommand(async () => await RefreshWorkbookAsync());
        ExportCsvCommand = new RelayCommand(ExportCsv, () => CanExportCsv);

        _ = RefreshOptionsAsync();
    }

    private int _optionsRequestId;

    internal Task RefreshOptionsAsync() => ExecuteRefreshAsync(
        () => ++_optionsRequestId,
        id => id == _optionsRequestId,
        loading => IsLoadingOptions = loading,
        error => OptionsError = error,
        async isCurrent =>
        {
            var options = await Task.Run(() => _taxWorkbookService.GetWorkbookOptions());

            if (!isCurrent())
            {
                return;
            }

            _options = options.ToList();
            ReplaceAll(Jurisdictions, _options.Select(o => o.Jurisdiction.ToString()).Distinct());

            var first = _options.FirstOrDefault();
            _selectedJurisdiction = first?.Jurisdiction.ToString();
            OnPropertyChanged(nameof(SelectedJurisdiction));
            ReplaceAll(TaxYearsForJurisdiction, _options.Where(o => o.Jurisdiction == first?.Jurisdiction).Select(o => o.TaxYear));
            SelectedTaxYear = first?.TaxYear;
        },
        ex => _logger.LogError("Tax workbook options refresh failed with {ErrorType}", ex.GetType().Name));

    private int _workbookRequestId;

    internal Task RefreshWorkbookAsync() => ExecuteRefreshAsync(
        () => ++_workbookRequestId,
        id => id == _workbookRequestId,
        loading => IsLoadingWorkbook = loading,
        error => WorkbookError = error,
        async isCurrent =>
        {
            if (SelectedJurisdiction is null || SelectedTaxYear is null)
            {
                ReplaceAll(Entries, []);
                ReplaceAll(CategoryTotals, []);
                SetAggregateStatus(null);
                return;
            }

            var workbook = await Task.Run(() => _taxWorkbookService.GetWorkbook(SelectedJurisdiction, SelectedTaxYear));

            if (!isCurrent())
            {
                return;
            }

            _workbook = workbook;
            ReplaceAll(Entries, workbook.Entries.Select(e => new TaxWorkbookEntryRowViewModel(e)));
            ReplaceAll(CategoryTotals, workbook.CategoryTotals);
            SetAggregateStatus(workbook.CalculationStatus);
            OnPropertyChanged(nameof(CanExportCsv));
            ExportCsvCommand.RaiseCanExecuteChanged();
        },
        ex => _logger.LogError("Tax workbook refresh failed with {ErrorType}", ex.GetType().Name));

    private void SetAggregateStatus(CalculationStatus? status)
    {
        HasAggregateStatus = status.HasValue;
        if (!status.HasValue)
        {
            return;
        }

        var (background, foreground, symbol, filled, label) = TaxWorkbookEntryRowViewModel.ResolveStatus(status.Value);
        AggregateStatusBrush = background;
        AggregateStatusForeground = foreground;
        AggregateStatusSymbol = symbol;
        AggregateStatusSymbolFilled = filled;
        AggregateStatusLabel = label;
    }

    private void ExportCsv()
    {
        if (_workbook is null || _workbook.Entries.Count == 0)
        {
            return;
        }

        var fileName = $"tax-workbook-{_workbook.Jurisdiction}-{_workbook.TaxYear.Replace("/", "-")}.csv";
        var path = _dialogService.ShowSaveFileDialog(fileName, "CSV files (*.csv)|*.csv");
        if (path is null)
        {
            return;
        }

        try
        {
            File.WriteAllText(path, BuildCsv(_workbook));
        }
        catch (Exception ex)
        {
            _logger.LogError("Tax workbook CSV export failed with {ErrorType}", ex.GetType().Name);
            WorkbookError = ex.Message;
        }
    }

    internal static string BuildCsv(TaxWorkbookDTO workbook)
    {
        var currency = workbook.Jurisdiction == Jurisdiction.BR ? "BRL" : "GBP";
        var rows = workbook.Entries.Select(entry => string.Join(",", new[]
        {
            CsvField(entry.Date.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)),
            CsvField(workbook.Jurisdiction.ToString()),
            CsvField(workbook.TaxYear),
            CsvField(entry.EventCategory.ToString()),
            CsvField(entry.Proceeds),
            CsvField(entry.CostBasis),
            CsvField(entry.GainLoss),
            CsvField(entry.GrossAmount),
            CsvField(entry.WithheldAmount),
            CsvField(entry.NetAmount),
            CsvField(currency),
            CsvField(entry.CalculationStatus.ToString()),
            CsvField(entry.EvidenceReference.ToString()),
        }));

        return string.Join("\r\n", new[] { string.Join(",", CsvColumns) }.Concat(rows));
    }

    private static string CsvField(decimal? value) => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    private static string CsvField(string value) =>
        value.IndexOfAny(['"', ',', '\n']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
}
