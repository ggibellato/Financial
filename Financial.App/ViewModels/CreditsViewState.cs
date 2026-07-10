using Financial.Presentation.App.Helpers;

namespace Financial.Presentation.App.ViewModels;

internal readonly record struct CreditsViewState(PeriodFilter Filter, CreditsTypeChartMode Mode, CreditsChartType ChartType);
