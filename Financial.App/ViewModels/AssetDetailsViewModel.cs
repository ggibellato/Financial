using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Presentation.App.ViewModels;
using Financial.Presentation.App;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Annotations;
using OxyPlot.Series;

namespace Financial.Presentation.App.ViewModels;

/// <summary>
/// ViewModel for displaying asset details with operations and credits in tabs
/// </summary>
public class AssetDetailsViewModel : ViewModelBase, IAssetDetailsViewModel
{
    private readonly IOperationService? _operationService;
    private readonly ICreditService? _creditService;
    private readonly IAssetPriceService? _assetPriceService;
    private readonly TodayInfoTracker _todayInfo;
    private readonly OperationActions _operationActions;
    private readonly CreditActions _creditActions;
    private readonly RelayCommand _addOperationCommand;
    private readonly RelayCommand _updateOperationCommand;
    private readonly RelayCommand _deleteOperationCommand;
    private readonly RelayCommand _addCreditCommand;
    private readonly RelayCommand _updateCreditCommand;
    private readonly RelayCommand _deleteCreditCommand;
    private readonly RelayCommand _refreshTodayInfoCommand;
    private readonly RelayCommand _copyAssetNameCommand;
    private readonly RelayCommand _selectCreditsFilterCommand;
    private readonly RelayCommand _selectCreditsTypeModeCommand;
    private string _assetName = string.Empty;
    private string _brokerName = string.Empty;
    private string _portfolioName = string.Empty;
    private string _ticker = string.Empty;
    private string _isin = string.Empty;
    private string _exchange = string.Empty;
    private decimal _quantity;
    private decimal _averagePrice;
    private bool _isActive;
    private decimal _totalBought;
    private decimal _totalSold;
    private decimal _totalCredits;
    private decimal _todayCurrentValue;
    private string _todayCurrentValueAsOf = string.Empty;
    private string _todayInfoMessage = string.Empty;
    private PlotModel? _creditsPlotModel;
    private CreditsFilter _selectedCreditsFilter = CreditsFilter.LastYear;
    private CreditsTypeChartMode _selectedCreditsTypeMode = CreditsTypeChartMode.Stacked;
    private double _creditsPlotWidth;
    private OperationDTO? _selectedOperation;
    private CreditDTO? _selectedCredit;
    private IReadOnlyList<CreditsMonthTypeTotals> _creditsChartMonths = Array.Empty<CreditsMonthTypeTotals>();
    private IReadOnlyList<string> _creditsChartTypes = Array.Empty<string>();
    private const string CreditsValueLabelTag = "CreditsValueLabel";

    public string AssetName
    {
        get => _assetName;
        private set => SetProperty(ref _assetName, value);
    }

    public string BrokerName
    {
        get => _brokerName;
        private set => SetProperty(ref _brokerName, value);
    }

    public string PortfolioName
    {
        get => _portfolioName;
        private set => SetProperty(ref _portfolioName, value);
    }

    public string Ticker
    {
        get => _ticker;
        private set => SetProperty(ref _ticker, value);
    }

    public string ISIN
    {
        get => _isin;
        private set => SetProperty(ref _isin, value);
    }

    public string Exchange
    {
        get => _exchange;
        private set => SetProperty(ref _exchange, value);
    }

    public decimal Quantity
    {
        get => _quantity;
        private set
        {
            if (SetProperty(ref _quantity, value))
            {
                NotifyCurrentValueChanged();
            }
        }
    }

    public decimal AveragePrice
    {
        get => _averagePrice;
        private set
        {
            if (SetProperty(ref _averagePrice, value))
            {
                NotifyCurrentValueChanged();
            }
        }
    }

    public bool IsActive
    {
        get => _isActive;
        private set => SetProperty(ref _isActive, value);
    }

    public decimal TotalBought
    {
        get => _totalBought;
        private set
        {
            if (SetProperty(ref _totalBought, value))
            {
                OnPropertyChanged(nameof(Balance));
            }
        }
    }

    public decimal TotalSold
    {
        get => _totalSold;
        private set
        {
            if (SetProperty(ref _totalSold, value))
            {
                OnPropertyChanged(nameof(Balance));
            }
        }
    }

    public decimal TotalCredits
    {
        get => _totalCredits;
        private set
        {
            if (SetProperty(ref _totalCredits, value))
            {
                NotifyCurrentValueChanged();
            }
        }
    }

    public PlotModel? CreditsPlotModel
    {
        get => _creditsPlotModel;
        private set => SetProperty(ref _creditsPlotModel, value);
    }

    public decimal Balance => TotalBought - TotalSold;

    public decimal TodayCurrentValue
    {
        get => _todayCurrentValue;
        private set
        {
            if (SetProperty(ref _todayCurrentValue, value))
            {
                NotifyCurrentValueChanged();
            }
        }
    }

    public string TodayCurrentValueAsOf
    {
        get => _todayCurrentValueAsOf;
        private set => SetProperty(ref _todayCurrentValueAsOf, value);
    }

    public string TodayInfoMessage
    {
        get => _todayInfoMessage;
        private set => SetProperty(ref _todayInfoMessage, value);
    }

    public decimal TotalCurrentValue => AssetDetailsCalculations.CalculateTotalCurrentValue(TodayCurrentValue, Quantity);

    public decimal ResultPercent =>
        AssetDetailsCalculations.CalculateResultPercent(AveragePrice, Quantity, TotalCurrentValue);

    public decimal TotalCurrentValueWithCredits => TotalCurrentValue + TotalCredits;

    public decimal ResultPercentWithCredits =>
        AssetDetailsCalculations.CalculateResultPercentWithCredits(AveragePrice, Quantity, TotalCurrentValueWithCredits);

    public bool HasAveragePrice => AssetDetailsCalculations.HasAveragePrice(AveragePrice, Quantity);

    public ObservableCollection<OperationDTO> Operations { get; } = new();
    public ObservableCollection<CreditDTO> Credits { get; } = new();
    public ObservableCollection<KeyValuePair<string, decimal>> CreditsByMonthChart { get; } = new();
    public ObservableCollection<CreditsFilterOptionViewModel> CreditsFilters { get; } = new();
    public ObservableCollection<CreditsTypeModeOptionViewModel> CreditsTypeModes { get; } = new();

    public OperationDTO? SelectedOperation
    {
        get => _selectedOperation;
        set
        {
            if (SetProperty(ref _selectedOperation, value))
            {
                UpdateCommandStates();
            }
        }
    }

    public CreditDTO? SelectedCredit
    {
        get => _selectedCredit;
        set
        {
            if (SetProperty(ref _selectedCredit, value))
            {
                UpdateCommandStates();
            }
        }
    }

    public RelayCommand AddOperationCommand => _addOperationCommand;
    public RelayCommand UpdateOperationCommand => _updateOperationCommand;
    public RelayCommand DeleteOperationCommand => _deleteOperationCommand;
    public RelayCommand AddCreditCommand => _addCreditCommand;
    public RelayCommand UpdateCreditCommand => _updateCreditCommand;
    public RelayCommand DeleteCreditCommand => _deleteCreditCommand;
    public RelayCommand RefreshTodayInfoCommand => _refreshTodayInfoCommand;
    public RelayCommand CopyAssetNameCommand => _copyAssetNameCommand;
    public RelayCommand SelectCreditsFilterCommand => _selectCreditsFilterCommand;
    public RelayCommand SelectCreditsTypeModeCommand => _selectCreditsTypeModeCommand;

    public AssetDetailsViewModel(IOperationService? operationService = null, ICreditService? creditService = null, IAssetPriceService? assetPriceService = null)
    {
        _operationService = operationService;
        _creditService = creditService;
        _assetPriceService = assetPriceService;
        _todayInfo = new TodayInfoTracker(ApplyTodayInfo, ResetTodayInfo, UpdateCommandStates);
        _operationActions = new OperationActions(
            _operationService,
            () => HasOperationContext,
            () => BrokerName,
            () => PortfolioName,
            () => AssetName,
            LoadAssetDetails,
            (message, caption, image) => MessageBox.Show(message, caption, MessageBoxButton.OK, image));
        _creditActions = new CreditActions(
            _creditService,
            () => HasCreditContext,
            () => BrokerName,
            () => PortfolioName,
            () => AssetName,
            LoadAssetDetails,
            (message, caption, image) => MessageBox.Show(message, caption, MessageBoxButton.OK, image));
        _addOperationCommand = new RelayCommand(AddOperation, CanEditOperations);
        _updateOperationCommand = new RelayCommand(UpdateOperation, CanUpdateOperation);
        _deleteOperationCommand = new RelayCommand(DeleteOperation, CanDeleteOperation);
        _addCreditCommand = new RelayCommand(AddCredit, CanEditCredits);
        _updateCreditCommand = new RelayCommand(UpdateCredit, CanUpdateCredit);
        _deleteCreditCommand = new RelayCommand(DeleteCredit, CanDeleteCredit);
        _refreshTodayInfoCommand = new RelayCommand(RefreshTodayInfo, CanRefreshTodayInfo);
        _copyAssetNameCommand = new RelayCommand(CopyAssetName, CanCopyAssetName);
        _selectCreditsFilterCommand = new RelayCommand(SelectCreditsFilter);
        _selectCreditsTypeModeCommand = new RelayCommand(SelectCreditsTypeMode);
        InitializeCreditsFilters();
        InitializeCreditsTypeModes();
    }

    /// <summary>
    /// Loads asset details from DTO
    /// </summary>
    public void LoadAssetDetails(AssetDetailsDTO details)
    {
        var assetKey = BuildAssetKey(details.BrokerName, details.PortfolioName, details.Name);
        _todayInfo.UpdateAssetKey(assetKey);

        AssetName = details.Name;
        BrokerName = details.BrokerName;
        PortfolioName = details.PortfolioName;
        Ticker = details.Ticker;
        ISIN = details.ISIN;
        Exchange = details.Exchange;
        Quantity = details.Quantity;
        AveragePrice = details.AveragePrice;
        IsActive = details.IsActive;
        TotalBought = details.TotalBought;
        TotalSold = details.TotalSold;
        TotalCredits = details.TotalCredits;

        Operations.Clear();
        foreach (var op in details.Operations)
        {
            Operations.Add(op);
        }

        Credits.Clear();
        foreach (var credit in details.Credits)
        {
            Credits.Add(credit);
        }
        ApplyCreditsFilter();

        SelectedOperation = null;
        SelectedCredit = null;
        UpdateCommandStates();
    }

    /// <summary>
    /// Clears all asset details
    /// </summary>
    public void Clear()
    {
        AssetName = string.Empty;
        BrokerName = string.Empty;
        PortfolioName = string.Empty;
        Ticker = string.Empty;
        ISIN = string.Empty;
        Exchange = string.Empty;
        Quantity = 0;
        AveragePrice = 0;
        IsActive = false;
        TotalBought = 0;
        TotalSold = 0;
        TotalCredits = 0;
        _todayInfo.Clear();
        Operations.Clear();
        Credits.Clear();
        CreditsByMonthChart.Clear();
        CreditsPlotModel = null;
        SelectedOperation = null;
        SelectedCredit = null;
        UpdateCommandStates();
    }

    private bool HasAssetContext =>
        !string.IsNullOrWhiteSpace(BrokerName) &&
        !string.IsNullOrWhiteSpace(PortfolioName) &&
        !string.IsNullOrWhiteSpace(AssetName);

    private bool HasOperationContext => _operationService != null && HasAssetContext;

    private bool HasCreditContext => _creditService != null && HasAssetContext;

    private bool CanEditOperations() => HasOperationContext;

    private bool CanUpdateOperation(object? parameter) =>
        HasOperationContext && (parameter is OperationDTO || SelectedOperation != null);

    private bool CanDeleteOperation(object? parameter) =>
        HasOperationContext && (parameter is OperationDTO || SelectedOperation != null);

    private bool CanEditCredits() => HasCreditContext;

    private bool CanUpdateCredit(object? parameter) =>
        HasCreditContext && (parameter is CreditDTO || SelectedCredit != null);

    private bool CanDeleteCredit(object? parameter) =>
        HasCreditContext && (parameter is CreditDTO || SelectedCredit != null);

    private bool CanRefreshTodayInfo() => _todayInfo.CanRefresh(HasAssetContext);

    private bool CanCopyAssetName() => HasAssetContext;

    private void CopyAssetName()
    {
        if (!HasAssetContext)
        {
            MessageBox.Show("Select an asset before copying.", "Copy Asset", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Clipboard.SetText(AssetName);
    }

    public Task EnsureTodayInfoLoadedAsync()
    {
        return RefreshTodayInfoAsync(forceRefresh: false);
    }

    private async void RefreshTodayInfo()
    {
        await RefreshTodayInfoAsync(forceRefresh: true);
    }

    private Task RefreshTodayInfoAsync(bool forceRefresh)
    {
        return _todayInfo.RefreshAsync(
            forceRefresh,
            HasAssetContext,
            _assetPriceService,
            Exchange,
            Ticker,
            message => TodayInfoMessage = message);
    }

    private void ResetTodayInfo()
    {
        TodayCurrentValue = 0;
        TodayCurrentValueAsOf = string.Empty;
        TodayInfoMessage = string.Empty;
    }

    private void ApplyTodayInfo(TodayInfoSnapshot snapshot)
    {
        TodayCurrentValue = snapshot.Price;
        TodayCurrentValueAsOf = snapshot.AsOf;
        TodayInfoMessage = string.Empty;
    }

    private void NotifyCurrentValueChanged()
    {
        OnPropertyChanged(nameof(TotalCurrentValue));
        OnPropertyChanged(nameof(ResultPercent));
        OnPropertyChanged(nameof(HasAveragePrice));
        OnPropertyChanged(nameof(TotalCurrentValueWithCredits));
        OnPropertyChanged(nameof(ResultPercentWithCredits));
    }

    public void UpdateCreditsPlotWidth(double plotWidth)
    {
        if (plotWidth <= 0)
        {
            return;
        }

        _creditsPlotWidth = plotWidth;
        ApplyCreditsPlotLabelDensity();
    }

    private void InitializeCreditsFilters()
    {
        CreditsFilters.Clear();
        CreditsFilters.Add(new CreditsFilterOptionViewModel("This month", CreditsFilter.ThisMonth));
        CreditsFilters.Add(new CreditsFilterOptionViewModel("Last 3 months", CreditsFilter.Last3Months));
        CreditsFilters.Add(new CreditsFilterOptionViewModel("Last 6 months", CreditsFilter.Last6Months));
        CreditsFilters.Add(new CreditsFilterOptionViewModel("Last year", CreditsFilter.LastYear));
        CreditsFilters.Add(new CreditsFilterOptionViewModel("All", CreditsFilter.All));
        SetCreditsFilter(CreditsFilter.LastYear, rebuild: false);
    }

    private void InitializeCreditsTypeModes()
    {
        CreditsTypeModes.Clear();
        CreditsTypeModes.Add(new CreditsTypeModeOptionViewModel("Stacked", CreditsTypeChartMode.Stacked));
        CreditsTypeModes.Add(new CreditsTypeModeOptionViewModel("Grouped", CreditsTypeChartMode.Grouped));
        SetCreditsTypeMode(CreditsTypeChartMode.Stacked, rebuild: false);
    }

    private void SelectCreditsFilter(object? parameter)
    {
        if (parameter is CreditsFilterOptionViewModel option)
        {
            SetCreditsFilter(option.Filter);
            return;
        }

        if (parameter is CreditsFilter filter)
        {
            SetCreditsFilter(filter);
        }
    }

    private void SelectCreditsTypeMode(object? parameter)
    {
        if (parameter is CreditsTypeModeOptionViewModel option)
        {
            SetCreditsTypeMode(option.Mode);
            return;
        }

        if (parameter is CreditsTypeChartMode mode)
        {
            SetCreditsTypeMode(mode);
        }
    }

    private void SetCreditsFilter(CreditsFilter filter, bool rebuild = true)
    {
        if (_selectedCreditsFilter == filter && CreditsFilters.Count > 0)
        {
            UpdateCreditsFilterSelection();
            return;
        }

        _selectedCreditsFilter = filter;
        UpdateCreditsFilterSelection();

        if (rebuild)
        {
            ApplyCreditsFilter();
        }
    }

    private void SetCreditsTypeMode(CreditsTypeChartMode mode, bool rebuild = true)
    {
        if (_selectedCreditsTypeMode == mode && CreditsTypeModes.Count > 0)
        {
            UpdateCreditsTypeModeSelection();
            return;
        }

        _selectedCreditsTypeMode = mode;
        UpdateCreditsTypeModeSelection();

        if (rebuild)
        {
            ApplyCreditsFilter();
        }
    }

    private void UpdateCreditsFilterSelection()
    {
        foreach (var option in CreditsFilters)
        {
            option.IsSelected = option.Filter == _selectedCreditsFilter;
        }
    }

    private void UpdateCreditsTypeModeSelection()
    {
        foreach (var option in CreditsTypeModes)
        {
            option.IsSelected = option.Mode == _selectedCreditsTypeMode;
        }
    }

    private void ApplyCreditsFilter()
    {
        RefreshCreditsByMonthChart(FilterCredits(Credits, _selectedCreditsFilter));
    }

    private static IEnumerable<CreditDTO> FilterCredits(IEnumerable<CreditDTO> credits, CreditsFilter filter)
    {
        if (filter == CreditsFilter.All)
        {
            return credits;
        }

        var today = DateTime.Today;
        var currentMonthStart = new DateTime(today.Year, today.Month, 1);
        var monthsBack = filter switch
        {
            CreditsFilter.ThisMonth => 0,
            CreditsFilter.Last3Months => 2,
            CreditsFilter.Last6Months => 5,
            CreditsFilter.LastYear => 11,
            _ => 0
        };

        var start = currentMonthStart.AddMonths(-monthsBack);
        var endExclusive = currentMonthStart.AddMonths(1);
        return credits.Where(credit => credit.Date >= start && credit.Date < endExclusive);
    }

    private void RefreshCreditsByMonthChart(IEnumerable<CreditDTO> credits)
    {
        CreditsByMonthChart.Clear();
        var grouped = credits
            .GroupBy(credit => new DateTime(credit.Date.Year, credit.Date.Month, 1))
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var totals = group
                    .GroupBy(credit => credit.Type, StringComparer.CurrentCultureIgnoreCase)
                    .OrderBy(typeGroup => typeGroup.Key, StringComparer.CurrentCultureIgnoreCase)
                    .ToDictionary(
                        typeGroup => typeGroup.Key,
                        typeGroup => typeGroup.Sum(credit => credit.Value),
                        StringComparer.CurrentCultureIgnoreCase);
                return new CreditsMonthTypeTotals(group.Key, totals);
            })
            .ToList();

        _creditsChartMonths = grouped;
        _creditsChartTypes = grouped
            .SelectMany(month => month.TotalsByType.Keys)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(type => type, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        foreach (var group in grouped)
        {
            CreditsByMonthChart.Add(new KeyValuePair<string, decimal>(
                group.Month.ToString("MM/yyyy"),
                group.Total));
        }

        CreditsPlotModel = BuildCreditsPlotModel(grouped, _creditsChartTypes, _selectedCreditsTypeMode);
        ApplyCreditsPlotLabelDensity();
    }

    private static PlotModel BuildCreditsPlotModel(
        IReadOnlyList<CreditsMonthTypeTotals> grouped,
        IReadOnlyList<string> creditTypes,
        CreditsTypeChartMode mode)
    {
        var model = new PlotModel { Title = "Credits by Month" };
        var categoryAxis = new CategoryAxis
        {
            Position = AxisPosition.Bottom,
            GapWidth = 0.2,
            IsPanEnabled = false,
            IsZoomEnabled = false
        };
        var valueAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot,
            IsPanEnabled = false,
            IsZoomEnabled = false,
            MaximumPadding = 0.1
        };
        model.Axes.Add(categoryAxis);
        model.Axes.Add(valueAxis);

        if (grouped.Count == 0 || creditTypes.Count == 0)
        {
            return model;
        }

        var palette = BuildBluePalette(creditTypes.Count);
        var seriesByType = creditTypes
            .Select((type, index) => new RectangleBarSeries
            {
                Title = type,
                FillColor = palette[index],
                StrokeColor = OxyColors.SlateGray,
                StrokeThickness = 1
            })
            .ToList();

        const double groupWidth = 0.8;
        var halfGroupWidth = groupWidth / 2;
        var barWidth = groupWidth / creditTypes.Count;

        for (var monthIndex = 0; monthIndex < grouped.Count; monthIndex++)
        {
            var month = grouped[monthIndex];
            categoryAxis.Labels.Add(month.Month.ToString("MM/yyyy"));

            var positiveStack = 0.0;
            var negativeStack = 0.0;

            for (var typeIndex = 0; typeIndex < creditTypes.Count; typeIndex++)
            {
                var type = creditTypes[typeIndex];
                var value = month.TotalsByType.TryGetValue(type, out var total) ? (double)total : 0d;
                double x0;
                double x1;
                double y0;
                double y1;

                if (mode == CreditsTypeChartMode.Stacked)
                {
                    x0 = monthIndex - halfGroupWidth;
                    x1 = monthIndex + halfGroupWidth;
                    if (value >= 0)
                    {
                        y0 = positiveStack;
                        y1 = positiveStack + value;
                        positiveStack = y1;
                    }
                    else
                    {
                        y1 = negativeStack;
                        y0 = negativeStack + value;
                        negativeStack = y0;
                    }
                }
                else
                {
                    x0 = monthIndex - halfGroupWidth + (barWidth * typeIndex);
                    x1 = x0 + barWidth;
                    y0 = Math.Min(0, value);
                    y1 = Math.Max(0, value);
                }

                seriesByType[typeIndex].Items.Add(new RectangleBarItem(x0, y0, x1, y1));
            }
        }

        foreach (var series in seriesByType)
        {
            model.Series.Add(series);
        }
        return model;
    }

    private static IReadOnlyList<OxyColor> BuildBluePalette(int count)
    {
        if (count <= 0)
        {
            return Array.Empty<OxyColor>();
        }

        if (count == 1)
        {
            return new[] { OxyColors.SteelBlue };
        }

        var start = OxyColor.FromRgb(173, 216, 230); // Light blue
        var end = OxyColor.FromRgb(8, 81, 156); // Dark blue
        var colors = new List<OxyColor>(count);
        for (var i = 0; i < count; i++)
        {
            var t = (double)i / (count - 1);
            colors.Add(OxyColor.FromRgb(
                LerpByte(start.R, end.R, t),
                LerpByte(start.G, end.G, t),
                LerpByte(start.B, end.B, t)));
        }

        return colors;
    }

    private static byte LerpByte(byte from, byte to, double t)
    {
        return (byte)Math.Round(from + ((to - from) * t));
    }

    private void ApplyCreditsPlotLabelDensity()
    {
        if (CreditsPlotModel == null || _creditsPlotWidth <= 0)
        {
            return;
        }

        var categoryAxis = CreditsPlotModel.Axes.OfType<CategoryAxis>().FirstOrDefault();
        if (categoryAxis == null || categoryAxis.Labels.Count == 0)
        {
            return;
        }

        const double minLabelWidth = 52;
        var maxVisibleLabels = Math.Max(1, (int)Math.Floor(_creditsPlotWidth / minLabelWidth));
        var step = Math.Max(1, (int)Math.Ceiling((double)categoryAxis.Labels.Count / maxVisibleLabels));
        categoryAxis.MajorStep = step;
        categoryAxis.MinorStep = 1;
        UpdateCreditsValueLabels(step);
        CreditsPlotModel.InvalidatePlot(false);
    }

    private void UpdateCreditsValueLabels(int step)
    {
        if (CreditsPlotModel == null)
        {
            return;
        }

        for (var index = CreditsPlotModel.Annotations.Count - 1; index >= 0; index--)
        {
            if (CreditsPlotModel.Annotations[index].Tag is string tag &&
                string.Equals(tag, CreditsValueLabelTag, StringComparison.Ordinal))
            {
                CreditsPlotModel.Annotations.RemoveAt(index);
            }
        }

        if (_creditsChartMonths.Count == 0 || _creditsChartTypes.Count == 0)
        {
            return;
        }

        const double groupWidth = 0.8;
        var halfGroupWidth = groupWidth / 2;
        var barWidth = groupWidth / _creditsChartTypes.Count;

        for (var monthIndex = 0; monthIndex < _creditsChartMonths.Count; monthIndex += step)
        {
            var month = _creditsChartMonths[monthIndex];

            if (_selectedCreditsTypeMode == CreditsTypeChartMode.Stacked)
            {
                var total = month.Total;
                AddCreditsValueLabel(monthIndex, total, total);
                continue;
            }

            for (var typeIndex = 0; typeIndex < _creditsChartTypes.Count; typeIndex++)
            {
                var type = _creditsChartTypes[typeIndex];
                var value = month.TotalsByType.TryGetValue(type, out var total) ? total : 0m;
                if (value == 0)
                {
                    continue;
                }

                var x0 = monthIndex - halfGroupWidth + (barWidth * typeIndex);
                var xCenter = x0 + (barWidth / 2);
                AddCreditsValueLabel(xCenter, value, value);
            }
        }
    }

    private void AddCreditsValueLabel(double x, decimal value, decimal labelYValue)
    {
        var y = (double)labelYValue;
        var annotation = new TextAnnotation
        {
            Text = value.ToString("N2"),
            TextPosition = new DataPoint(x, y),
            TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Center,
            TextVerticalAlignment = value >= 0 ? OxyPlot.VerticalAlignment.Bottom : OxyPlot.VerticalAlignment.Top,
            Offset = value >= 0 ? new ScreenVector(0, -6) : new ScreenVector(0, 6),
            TextColor = OxyColors.Black,
            Stroke = OxyColors.Undefined,
            Tag = CreditsValueLabelTag,
            ClipByXAxis = true,
            ClipByYAxis = false
        };
        CreditsPlotModel?.Annotations.Add(annotation);
    }

    private static string BuildAssetKey(string brokerName, string portfolioName, string assetName) =>
        $"{brokerName}|{portfolioName}|{assetName}";

    private void AddOperation()
    {
        _operationActions.Add(ShowAddOperationDialog);
    }

    private void AddCredit()
    {
        _creditActions.Add(ShowAddCreditDialog);
    }

    private void UpdateOperation(object? parameter)
    {
        if (parameter is OperationDTO operation)
        {
            SelectedOperation = operation;
        }

        _operationActions.Update(SelectedOperation, ShowUpdateOperationDialog);
    }

    private void UpdateCredit(object? parameter)
    {
        if (parameter is CreditDTO credit)
        {
            SelectedCredit = credit;
        }

        _creditActions.Update(SelectedCredit, ShowUpdateCreditDialog);
    }

    private void DeleteOperation(object? parameter)
    {
        if (parameter is OperationDTO operation)
        {
            SelectedOperation = operation;
        }

        _operationActions.Delete(SelectedOperation, ShowDeleteOperationDialog);
    }

    private void DeleteCredit(object? parameter)
    {
        if (parameter is CreditDTO credit)
        {
            SelectedCredit = credit;
        }

        _creditActions.Delete(SelectedCredit, ShowDeleteCreditDialog);
    }

    private void UpdateCommandStates()
    {
        _addOperationCommand.RaiseCanExecuteChanged();
        _updateOperationCommand.RaiseCanExecuteChanged();
        _deleteOperationCommand.RaiseCanExecuteChanged();
        _addCreditCommand.RaiseCanExecuteChanged();
        _updateCreditCommand.RaiseCanExecuteChanged();
        _deleteCreditCommand.RaiseCanExecuteChanged();
        _refreshTodayInfoCommand.RaiseCanExecuteChanged();
        _copyAssetNameCommand.RaiseCanExecuteChanged();
    }

    private bool ShowOperationDialog(OperationDialogViewModel viewModel)
    {
        var dialog = new OperationDialog(viewModel)
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };

        return dialog.ShowDialog() == true;
    }

    private OperationDialogData? ShowAddOperationDialog()
    {
        var dialogViewModel = OperationDialogViewModel.CreateForAdd(BrokerName, PortfolioName, AssetName);
        if (!ShowOperationDialog(dialogViewModel))
        {
            return null;
        }

        return new OperationDialogData(
            dialogViewModel.OperationId,
            dialogViewModel.Date,
            dialogViewModel.Type,
            dialogViewModel.Quantity,
            dialogViewModel.UnitPrice,
            dialogViewModel.Fees);
    }

    private OperationDialogData? ShowUpdateOperationDialog()
    {
        if (SelectedOperation == null)
        {
            return null;
        }

        var dialogViewModel = OperationDialogViewModel.CreateForUpdate(
            BrokerName,
            PortfolioName,
            AssetName,
            SelectedOperation.Id,
            SelectedOperation.Date,
            SelectedOperation.Type,
            SelectedOperation.Quantity,
            SelectedOperation.UnitPrice,
            SelectedOperation.Fees);

        if (!ShowOperationDialog(dialogViewModel))
        {
            return null;
        }

        return new OperationDialogData(
            dialogViewModel.OperationId,
            dialogViewModel.Date,
            dialogViewModel.Type,
            dialogViewModel.Quantity,
            dialogViewModel.UnitPrice,
            dialogViewModel.Fees);
    }

    private bool ShowDeleteOperationDialog()
    {
        if (SelectedOperation == null)
        {
            return false;
        }

        var dialogViewModel = OperationDialogViewModel.CreateForDelete(
            BrokerName,
            PortfolioName,
            AssetName,
            SelectedOperation.Id,
            SelectedOperation.Date,
            SelectedOperation.Type,
            SelectedOperation.Quantity,
            SelectedOperation.UnitPrice,
            SelectedOperation.Fees);

        return ShowOperationDialog(dialogViewModel);
    }

    private bool ShowCreditDialog(CreditDialogViewModel viewModel)
    {
        var dialog = new CreditDialog(viewModel)
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };

        return dialog.ShowDialog() == true;
    }

    private CreditDialogData? ShowAddCreditDialog()
    {
        var dialogViewModel = CreditDialogViewModel.CreateForAdd(BrokerName, PortfolioName, AssetName);
        if (!ShowCreditDialog(dialogViewModel))
        {
            return null;
        }

        return new CreditDialogData(
            dialogViewModel.CreditId,
            dialogViewModel.Date,
            dialogViewModel.Type,
            dialogViewModel.Value);
    }

    private CreditDialogData? ShowUpdateCreditDialog()
    {
        if (SelectedCredit == null)
        {
            return null;
        }

        var dialogViewModel = CreditDialogViewModel.CreateForUpdate(
            BrokerName,
            PortfolioName,
            AssetName,
            SelectedCredit.Id,
            SelectedCredit.Date,
            SelectedCredit.Type,
            SelectedCredit.Value);

        if (!ShowCreditDialog(dialogViewModel))
        {
            return null;
        }

        return new CreditDialogData(
            dialogViewModel.CreditId,
            dialogViewModel.Date,
            dialogViewModel.Type,
            dialogViewModel.Value);
    }

    private bool ShowDeleteCreditDialog()
    {
        if (SelectedCredit == null)
        {
            return false;
        }

        var dialogViewModel = CreditDialogViewModel.CreateForDelete(
            BrokerName,
            PortfolioName,
            AssetName,
            SelectedCredit.Id,
            SelectedCredit.Date,
            SelectedCredit.Type,
            SelectedCredit.Value);

        return ShowCreditDialog(dialogViewModel);
    }

    public enum CreditsFilter
    {
        ThisMonth,
        Last3Months,
        Last6Months,
        LastYear,
        All
    }

    public enum CreditsTypeChartMode
    {
        Stacked,
        Grouped
    }

    public sealed class CreditsFilterOptionViewModel : ViewModelBase
    {
        private bool _isSelected;

        public CreditsFilterOptionViewModel(string label, CreditsFilter filter)
        {
            Label = label;
            Filter = filter;
        }

        public string Label { get; }

        public CreditsFilter Filter { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }

    public sealed class CreditsTypeModeOptionViewModel : ViewModelBase
    {
        private bool _isSelected;

        public CreditsTypeModeOptionViewModel(string label, CreditsTypeChartMode mode)
        {
            Label = label;
            Mode = mode;
        }

        public string Label { get; }

        public CreditsTypeChartMode Mode { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }

    private sealed class CreditsMonthTypeTotals
    {
        public CreditsMonthTypeTotals(DateTime month, IReadOnlyDictionary<string, decimal> totalsByType)
        {
            Month = month;
            TotalsByType = totalsByType;
        }

        public DateTime Month { get; }

        public IReadOnlyDictionary<string, decimal> TotalsByType { get; }

        public decimal Total => TotalsByType.Values.Sum();
    }

}




