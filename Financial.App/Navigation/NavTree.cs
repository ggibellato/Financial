namespace Financial.Presentation.App.Navigation;

public sealed record NavChild(string Id, string Label, string ViewKey);

public sealed record NavCategory(string Id, string Label, string IconGlyph, IReadOnlyList<NavChild> Children);

public static class NavTree
{
    public static IReadOnlyList<NavCategory> Categories { get; } =
    [
        new NavCategory(
            "investments",
            "Investments",
            "",
            [
                new NavChild("active-investments", "Active Investments", "active-investments"),
                new NavChild("historic-investments", "Historic Investments", "historic-investments"),
                new NavChild("dividend-check", "Shares Dividend check", "dividend-check"),
                new NavChild("current-values", "Read Assets current values", "current-values"),
            ]),
        new NavCategory(
            "cashflow",
            "CashFlow",
            "",
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
