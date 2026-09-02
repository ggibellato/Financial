using System.Windows.Media;
using Financial.CashFlow.Application.DTOs;
using Wpf.Ui.Controls;

namespace Financial.Presentation.App.ViewModels;

/// <summary>
/// Display-ready wrapper around one <see cref="PaymentDueDTO"/>. Urgency colors are the literal
/// values Financial.Web's Fluent Badge renders for color="danger"/"warning"/"informative"
/// (resolved from @fluentui/react-theme's webLightTheme, not a screenshot approximation), per
/// docs/ui/decisions/ADR-005's "match the rendered pixel" guidance.
/// </summary>
public sealed class PaymentDueRowViewModel
{
    private static readonly SolidColorBrush TodayBackground = Freeze(0xD1, 0x34, 0x38);
    private static readonly SolidColorBrush TodayForeground = Freeze(0xFF, 0xFF, 0xFF);
    private static readonly SolidColorBrush SoonBackground = Freeze(0xFD, 0xE3, 0x00);
    private static readonly SolidColorBrush SoonForeground = Freeze(0x24, 0x24, 0x24);
    private static readonly SolidColorBrush UpcomingBackground = Freeze(0xEB, 0xEB, 0xEB);
    private static readonly SolidColorBrush UpcomingForeground = Freeze(0x61, 0x61, 0x61);

    public PaymentDueRowViewModel(PaymentDueDTO payment)
    {
        ArgumentNullException.ThrowIfNull(payment);

        TypeLabel = payment.Type == "CreditCard" ? "Credit card" : payment.Type;
        Name = payment.Name;
        DueDate = payment.DueDate;
        DaysRemainingText = BuildDaysRemainingText(payment.DaysRemaining);

        var (background, foreground, symbol, filled, urgencyWord) = ResolveTier(payment.DaysRemaining);
        UrgencyBrush = background;
        UrgencyForeground = foreground;
        UrgencySymbol = symbol;
        UrgencySymbolFilled = filled;
        UrgencyAccessibleLabel = $"{DaysRemainingText} – {urgencyWord}";
    }

    public string TypeLabel { get; }
    public string Name { get; }
    public DateOnly DueDate { get; }
    public string DaysRemainingText { get; }
    public SolidColorBrush UrgencyBrush { get; }
    public SolidColorBrush UrgencyForeground { get; }
    public SymbolRegular UrgencySymbol { get; }
    public bool UrgencySymbolFilled { get; }
    public string UrgencyAccessibleLabel { get; }

    private static string BuildDaysRemainingText(int daysRemaining) => daysRemaining switch
    {
        0 => "Due today",
        1 => "Due in 1 day",
        _ => $"Due in {daysRemaining} days",
    };

    private static (SolidColorBrush Background, SolidColorBrush Foreground, SymbolRegular Symbol, bool Filled, string Word) ResolveTier(int daysRemaining) =>
        daysRemaining switch
        {
            0 => (TodayBackground, TodayForeground, SymbolRegular.AlertUrgent20, true, "urgent"),
            <= 2 => (SoonBackground, SoonForeground, SymbolRegular.Clock20, false, "soon"),
            _ => (UpcomingBackground, UpcomingForeground, SymbolRegular.Calendar20, false, "upcoming"),
        };

    private static SolidColorBrush Freeze(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
