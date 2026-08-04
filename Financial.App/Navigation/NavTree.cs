namespace Financial.Presentation.App.Navigation;

public sealed record NavChild(string Id, string Label, string ViewKey);

public sealed record NavCategory(string Id, string Label, string IconData, IReadOnlyList<NavChild> Children);

public static class NavTree
{
    /// <summary>
    /// Path geometry mini-language equivalents of the Web app's inline SVG icons
    /// (Financial.Web/src/components/Sidebar.tsx), so both platforms show the same icon shapes.
    /// </summary>
    public static IReadOnlyList<NavCategory> Categories { get; } =
    [
        new NavCategory(
            "investments",
            "Investments",
            "M3,17 L9,11 L13,15 L21,7 M14,7 L21,7 L21,14",
            [
                new NavChild("active-investments", "Active Investments", "active-investments"),
                new NavChild("historic-investments", "Historic Investments", "historic-investments"),
                new NavChild("dividend-check", "Shares Dividend check", "dividend-check"),
                new NavChild("current-values", "Read Assets current values", "current-values"),
            ]),
        new NavCategory(
            "cashflow",
            "CashFlow",
            "M4,6 L20,6 A2,2 0 0 1 22,8 L22,16 A2,2 0 0 1 20,18 L4,18 A2,2 0 0 1 2,16 L2,8 A2,2 0 0 1 4,6 Z M2,10 L22,10",
            [
                new NavChild("monthly", "Monthly", "monthly"),
                new NavChild("reserva", "Reserva", "reserva"),
                new NavChild("mensais", "Mensais", "mensais"),
                new NavChild("controle-mae", "Controle Mae", "controle-mae"),
                new NavChild("investment-snapshots", "Investment Snapshots", "investment-snapshots"),
                new NavChild("annual-summary", "Annual Summary", "annual-summary"),
            ]),
    ];
}
