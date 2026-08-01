using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Financial.Presentation.App.ViewModels.CashFlow;

namespace Financial.Presentation.App.Views.CashFlow;

public partial class AnnualSummaryView : UserControl
{
    private readonly AnnualSummaryViewModel _viewModel;

    public AnnualSummaryView(AnnualSummaryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = _viewModel;

        _viewModel.AvailableYears.CollectionChanged += OnAvailableYearsChanged;
        RebuildHistoricSummaryYearColumns();
    }

    private void OnAvailableYearsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildHistoricSummaryYearColumns();
    }

    /// <summary>
    /// The Historic Summary Average grid's per-year columns aren't known until data loads, so
    /// they're built here rather than in XAML — no other grid in this app needs a dynamic
    /// column count, but this one genuinely does (see spec.md's Technical Decisions).
    /// </summary>
    private void RebuildHistoricSummaryYearColumns()
    {
        while (historicSummaryGrid.Columns.Count > 1)
        {
            historicSummaryGrid.Columns.RemoveAt(historicSummaryGrid.Columns.Count - 1);
        }

        foreach (var year in _viewModel.AvailableYears)
        {
            var column = new DataGridTextColumn
            {
                Header = year.ToString(),
                Width = 90,
                ElementStyle = (Style)FindResource("NumericColumnTextStyle"),
                Binding = new Binding($"ValuesByYear[{year}]") { StringFormat = "N2" },
            };
            historicSummaryGrid.Columns.Add(column);
        }
    }
}
