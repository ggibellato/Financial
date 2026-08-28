using System.Collections.ObjectModel;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using static Financial.Presentation.App.Helpers.ObservableCollectionHelper;

namespace Financial.Presentation.App.ViewModels.CashFlow;

public class AnnualSummaryViewModel : ViewModelBase
{
    private static readonly string[] MonthLabels =
        ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

    private readonly ICategorySummaryService _categorySummaryService;
    private readonly IInvestmentAnnualResultService _investmentAnnualResultService;
    private readonly IHistoricAverageService _historicAverageService;

    private int _year;
    private bool _isLoading = true;
    private string? _error;
    private decimal _yearProgress;
    private decimal _averageMonthResult;
    private decimal _sumOfMonthResults;

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

    public ObservableCollection<AnnualSummaryRow> CategoryTotalRows { get; } = [];
    public ObservableCollection<AnnualSummaryRow> FilteredCategoryTotalRows { get; } = [];
    public ObservableCollection<InvestmentAnnualRow> InvestmentRows { get; } = [];
    public ObservableCollection<HistoricSummaryRow> HistoricSummaryRows { get; } = [];
    public ObservableCollection<HistoricSummaryRow> FilteredHistoricSummaryRows { get; } = [];
    public ObservableCollection<int> AvailableYears { get; } = [];

    public ColumnFilterViewModel<AnnualSummaryRow> CategoryTotalsFilter { get; }
    public ColumnFilterViewModel<HistoricSummaryRow> HistoricSummaryFilter { get; }

    public decimal YearProgress
    {
        get => _yearProgress;
        private set => SetProperty(ref _yearProgress, value);
    }

    public decimal AverageMonthResult
    {
        get => _averageMonthResult;
        private set => SetProperty(ref _averageMonthResult, value);
    }

    public decimal SumOfMonthResults
    {
        get => _sumOfMonthResults;
        private set => SetProperty(ref _sumOfMonthResults, value);
    }

    public RelayCommand RetryCommand { get; }

    public AnnualSummaryViewModel(
        ICategorySummaryService categorySummaryService,
        IInvestmentAnnualResultService investmentAnnualResultService,
        IHistoricAverageService historicAverageService)
    {
        _categorySummaryService = categorySummaryService ?? throw new ArgumentNullException(nameof(categorySummaryService));
        _investmentAnnualResultService = investmentAnnualResultService ?? throw new ArgumentNullException(nameof(investmentAnnualResultService));
        _historicAverageService = historicAverageService ?? throw new ArgumentNullException(nameof(historicAverageService));

        _year = DateTime.Today.Year;

        CategoryTotalsFilter = new ColumnFilterViewModel<AnnualSummaryRow>(
            "Category", row => [row.Label], ApplyCategoryTotalsFilter);
        HistoricSummaryFilter = new ColumnFilterViewModel<HistoricSummaryRow>(
            "Category", row => [row.Category], ApplyHistoricSummaryFilter);

        RetryCommand = new RelayCommand(async () => await RefreshAsync());

        _ = RefreshAsync();
    }

    private int _refreshRequestId;

    /// <summary>
    /// Reloads all 3 sub-tabs' data for the selected Year together. Guards against overlapping
    /// calls (e.g. the constructor's initial load racing a rapid Year change) by discarding a
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
            var categoryTotalsTask = Task.Run(() => _categorySummaryService.GetCategoryTotalsAnnualForYear(year));
            var investmentResultTask = Task.Run(() => _investmentAnnualResultService.GetInvestmentAnnualResultForYear(year));
            var historicSummaryTask = Task.Run(() => _historicAverageService.GetHistoricSummaryAverageFromYear(year));

            await Task.WhenAll(categoryTotalsTask, investmentResultTask, historicSummaryTask);

            if (!isCurrent())
            {
                return;
            }

            ReplaceAll(CategoryTotalRows, BuildCategoryTotalRows(categoryTotalsTask.Result));
            CategoryTotalsFilter.Refresh(CategoryTotalRows);
            ApplyCategoryTotalsFilter();

            var investmentResult = investmentResultTask.Result;
            ReplaceAll(InvestmentRows, BuildInvestmentRows(investmentResult));
            YearProgress = investmentResult.NetPosition.FullYearNetChange;
            AverageMonthResult = investmentResult.NetPosition.AverageMonthResult;
            SumOfMonthResults = investmentResult.NetPosition.SumOfMonthResults;

            var historicSummary = historicSummaryTask.Result;
            // HistoricSummaryRows/FilteredHistoricSummaryRows must be fully in place BEFORE AvailableYears
            // changes: the view rebuilds the grid's per-year columns off AvailableYears, and a row missing
            // a just-added year's key throws a binding KeyNotFoundException if the columns arrive first.
            ReplaceAll(HistoricSummaryRows, BuildHistoricSummaryRows(historicSummary));
            HistoricSummaryFilter.Refresh(HistoricSummaryRows);
            ApplyHistoricSummaryFilter();
            ReplaceAll(AvailableYears, historicSummary.Select(y => y.Year));
        });

    private void ApplyCategoryTotalsFilter()
    {
        ReplaceAll(FilteredCategoryTotalRows, CategoryTotalRows.Where(r => r.IsSpacer || CategoryTotalsFilter.Matches(r)));
    }

    private void ApplyHistoricSummaryFilter()
    {
        ReplaceAll(FilteredHistoricSummaryRows, HistoricSummaryRows.Where(r => r.IsSpacer || HistoricSummaryFilter.Matches(r)));
    }

    private static List<AnnualSummaryRow> BuildCategoryTotalRows(CategoryTotalsAnnualDTO dto)
    {
        var rows = new List<AnnualSummaryRow>();
        var income = dto.IncomeSummary;

        rows.Add(new AnnualSummaryRow
        {
            Label = "Salary", MonthlyValues = income.SalaryMonthly,
            Average = income.SalaryAverage, AnnualTotal = income.SalaryAnnualTotal,
        });
        rows.Add(new AnnualSummaryRow
        {
            Label = "Salary after taxes", MonthlyValues = income.SalaryAfterTaxesMonthly,
            Average = income.SalaryAfterTaxesAverage, AnnualTotal = income.SalaryAfterTaxesAnnualTotal,
        });
        rows.Add(new AnnualSummaryRow
        {
            Label = "Tax difference", MonthlyValues = income.TaxDifferenceMonthly,
            Average = income.TaxDifferenceAverage, AnnualTotal = income.TaxDifferenceAnnualTotal,
        });
        rows.Add(AnnualSummaryRow.Spacer());
        rows.Add(new AnnualSummaryRow
        {
            Label = "Dividendo/Juros", MonthlyValues = income.DividendoJurosMonthly,
            Average = income.DividendoJurosAverage, AnnualTotal = income.DividendoJurosAnnualTotal,
        });
        rows.Add(AnnualSummaryRow.Spacer());

        foreach (var category in dto.CategoryTotals)
        {
            rows.Add(new AnnualSummaryRow
            {
                Label = category.Category, MonthlyValues = category.MonthlyTotals,
                Average = category.Average, AnnualTotal = category.AnnualTotal,
            });
        }

        rows.Add(AnnualSummaryRow.Spacer());
        rows.Add(new AnnualSummaryRow
        {
            Label = "Resultado (R-D-Inv)", MonthlyValues = dto.ResultadoMonthly,
            Average = dto.ResultadoAverage, AnnualTotal = dto.ResultadoAnnualTotal, IsEmphasized = true,
        });
        rows.Add(new AnnualSummaryRow
        {
            Label = "Total despesas", MonthlyValues = dto.TotalDespesasMonthly,
            Average = dto.TotalDespesasAverage, AnnualTotal = dto.TotalDespesasAnnualTotal, IsEmphasized = true,
        });

        return rows;
    }

    private static List<InvestmentAnnualRow> BuildInvestmentRows(InvestmentAnnualResultDTO dto)
    {
        var rows = new List<InvestmentAnnualRow>();

        foreach (var account in dto.Accounts)
        {
            rows.Add(new InvestmentAnnualRow
            {
                Label = account.IsLiability ? $"{account.Account} (-)" : account.Account,
                MonthlyValues = account.MonthlyValues.Select(v => (decimal?)v).ToArray(),
            });
        }

        rows.Add(new InvestmentAnnualRow
        {
            Label = "Total", MonthlyValues = dto.NetPosition.MonthlyValues.Select(v => (decimal?)v).ToArray(), IsEmphasized = true,
        });
        rows.Add(new InvestmentAnnualRow
        {
            Label = "Month Result", MonthlyValues = dto.NetPosition.MonthlyDiffs, IsEmphasized = true,
        });

        return rows;
    }

    private static List<HistoricSummaryRow> BuildHistoricSummaryRows(IReadOnlyList<CategoryAnnualAverageDTO> historicSummary)
    {
        var rows = new List<HistoricSummaryRow>();
        if (historicSummary.Count == 0)
        {
            return rows;
        }

        var categories = historicSummary[0].AnnualAverages.Select(a => a.Category);

        foreach (var category in categories)
        {
            var valuesByYear = historicSummary.ToDictionary(
                y => y.Year,
                y => y.AnnualAverages.FirstOrDefault(a => a.Category == category)?.Value ?? 0m);

            rows.Add(new HistoricSummaryRow
            {
                Category = category, ValuesByYear = valuesByYear,
                IsEmphasized = HistoricSummaryRow.IsEmphasizedCategory(category),
            });

            if (HistoricSummaryRow.HasSpacerAfter(category))
            {
                rows.Add(HistoricSummaryRow.Spacer());
            }
        }

        return rows;
    }

}
