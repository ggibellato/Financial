using Financial.Presentation.App.Helpers;

namespace Financial.Presentation.App.ViewModels.Investment;

internal readonly record struct CreditsViewState(PeriodFilter Filter, CreditsTypeChartMode Mode, CreditsChartType ChartType);
