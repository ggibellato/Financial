using Financial.Presentation.App.Helpers;

namespace Financial.Presentation.App.ViewModels.Investment;

internal readonly record struct TransactionsViewState(PeriodFilter Filter, ChartTypeMode Mode);
