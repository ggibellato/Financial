using Financial.Presentation.App.Helpers;

namespace Financial.Presentation.App.ViewModels;

internal readonly record struct TransactionsViewState(PeriodFilter Filter, ChartTypeMode Mode);
