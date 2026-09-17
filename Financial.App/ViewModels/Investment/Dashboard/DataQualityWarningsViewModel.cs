using System.Collections.ObjectModel;
using System.Globalization;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Financial.Presentation.App.ViewModels.Investment.Dashboard;

public class DataQualityWarningsViewModel : ViewModelBase
{
    private readonly IDataQualityReportService _reportService;
    private readonly ILogger<DataQualityWarningsViewModel> _logger;

    private bool _isLoading;
    private string? _errorMessage;
    private string? _navigationError;
    private int _requestId;

    public DataQualityWarningsViewModel(
        IDataQualityReportService reportService,
        ILogger<DataQualityWarningsViewModel> logger)
    {
        _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        RefreshCommand = new RelayCommand(async () =>
        {
            NavigationError = null;
            await LoadAsync();
        });

        SelectFindingCommand = new RelayCommand<WarningHoldingRef>(holding =>
        {
            if (holding is not null)
            {
                NavigateToHoldingRequested?.Invoke(this, holding);
            }
        });
    }

    public event EventHandler<WarningHoldingRef>? NavigateToHoldingRequested;

    public event EventHandler<DataQualityCategory>? CategoryExpanded;

    public RelayCommand RefreshCommand { get; }

    public RelayCommand<WarningHoldingRef> SelectFindingCommand { get; }

    public ObservableCollection<WarningCategoryViewModel> Categories { get; } = [];

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                NotifyContentStateChanged();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
                NotifyContentStateChanged();
            }
        }
    }

    public bool HasError => ErrorMessage != null;

    public bool ShowContent => !IsLoading && !HasError;

    public bool IsAllClear => Categories.Count == 0;

    public bool ShowAllClear => ShowContent && IsAllClear;

    public bool ShowCategories => ShowContent && !IsAllClear;

    public string? NavigationError
    {
        get => _navigationError;
        set
        {
            if (SetProperty(ref _navigationError, value))
            {
                OnPropertyChanged(nameof(HasNavigationError));
            }
        }
    }

    /// <summary>
    /// Two-way target for the InfoBar's own close button, which reports a dismissal only by
    /// pushing IsOpen back to false - there is no separate "closed" command to bind.
    /// </summary>
    public bool HasNavigationError
    {
        get => NavigationError != null;
        set
        {
            if (!value)
            {
                NavigationError = null;
            }
        }
    }

    public void ExpandCategory(DataQualityCategory category)
    {
        var target = Categories.FirstOrDefault(entry => entry.Category == category);
        if (target is null || !target.HasFindings)
        {
            return;
        }

        target.IsExpanded = true;
        CategoryExpanded?.Invoke(this, category);
    }

    public Task LoadAsync() => ExecuteRefreshAsync(
        () => ++_requestId,
        id => id == _requestId,
        loading => IsLoading = loading,
        error => ErrorMessage = error,
        isCurrent =>
        {
            var report = _reportService.GenerateReport();

            if (isCurrent())
            {
                RebuildCategories(report);
            }

            return Task.CompletedTask;
        },
        ex => _logger.LogError("Data-quality report refresh failed with {ErrorType}", ex.GetType().Name));

    private void RebuildCategories(DataQualityReportDTO report)
    {
        Categories.Clear();

        foreach (var category in BuildCategories(report))
        {
            Categories.Add(category);
        }

        OnPropertyChanged(nameof(IsAllClear));
        OnPropertyChanged(nameof(ShowAllClear));
        OnPropertyChanged(nameof(ShowCategories));
    }

    private static IEnumerable<WarningCategoryViewModel> BuildCategories(DataQualityReportDTO report)
    {
        if (report.SalesExceedPurchases.Count > 0)
        {
            yield return new WarningCategoryViewModel(
                DataQualityCategory.SalesExceedPurchases,
                "Impossible cash-flow sequence",
                report.SalesExceedPurchases.Count,
                [.. report.SalesExceedPurchases.Select(finding => new WarningFindingRowViewModel(
                    new WarningHoldingRef(finding.BrokerName, finding.PortfolioName, finding.AssetName),
                    $"{Location(finding.BrokerName, finding.PortfolioName)} — sold {Amount(finding.Shortfall)} more than held on {Date(finding.OffendingSaleDate)} (held {Amount(finding.QuantityHeld)})"))]);
        }

        if (report.UnpricedOpenHoldings.Count > 0)
        {
            yield return new WarningCategoryViewModel(
                DataQualityCategory.UnpricedOpenHoldings,
                "Missing price",
                report.UnpricedOpenHoldings.Count,
                [.. report.UnpricedOpenHoldings.Select(finding => new WarningFindingRowViewModel(
                    new WarningHoldingRef(finding.BrokerName, finding.PortfolioName, finding.AssetName),
                    Location(finding.BrokerName, finding.PortfolioName)))]);
        }

        if (report.OpenHoldingsMissingCostBasis.Count > 0)
        {
            yield return new WarningCategoryViewModel(
                DataQualityCategory.OpenHoldingsMissingCostBasis,
                "Missing cost basis",
                report.OpenHoldingsMissingCostBasis.Count,
                [.. report.OpenHoldingsMissingCostBasis.Select(finding => new WarningFindingRowViewModel(
                    new WarningHoldingRef(finding.BrokerName, finding.PortfolioName, finding.AssetName),
                    Location(finding.BrokerName, finding.PortfolioName)))]);
        }

        // The backend reports this one as a bare count: it carries no per-holding identity, so it
        // renders as a plain row with nothing to expand or click through to.
        if (report.StaleValuationCount > 0)
        {
            yield return new WarningCategoryViewModel(
                DataQualityCategory.StaleValuation,
                "Stale valuation",
                report.StaleValuationCount,
                []);
        }

        if (report.UnresolvedTaxClassifications.Count > 0)
        {
            yield return new WarningCategoryViewModel(
                DataQualityCategory.UnresolvedTaxClassifications,
                "Unresolved tax classification",
                report.UnresolvedTaxClassifications.Count,
                [.. report.UnresolvedTaxClassifications.Select(finding => new WarningFindingRowViewModel(
                    new WarningHoldingRef(finding.BrokerName, finding.PortfolioName, finding.AssetName),
                    $"{Location(finding.BrokerName, finding.PortfolioName)} — {finding.EventCategory}, tax year {finding.TaxYear}"))]);
        }
    }

    private static string Location(string brokerName, string portfolioName) => $"{portfolioName} · {brokerName}";

    private static string Amount(decimal value) => value.ToString("N2", CultureInfo.CurrentCulture);

    private static string Date(DateTime value) => value.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture);

    private void NotifyContentStateChanged()
    {
        OnPropertyChanged(nameof(ShowContent));
        OnPropertyChanged(nameof(ShowAllClear));
        OnPropertyChanged(nameof(ShowCategories));
    }
}
