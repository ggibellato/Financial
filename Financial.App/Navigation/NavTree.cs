namespace Financial.Presentation.App.Navigation;

public sealed record NavChild(string Id, string Label, string ViewKey);

public sealed record NavGroup(string Id, string Label, IReadOnlyList<NavChild> Children);

/// <summary>
/// <paramref name="Children"/> and <paramref name="Groups"/> are mutually exclusive — a category has
/// one or the other. Only the "admin" category uses <paramref name="Groups"/> (3-level nav) today.
/// </summary>
public sealed record NavCategory(
    string Id,
    string Label,
    string IconData,
    IReadOnlyList<NavChild> Children,
    IReadOnlyList<NavGroup>? Groups = null);

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
        new NavCategory(
            "admin",
            "Admin",
            "M12,2 L14,5 L17.5,4.5 L18,8 L21,10 L19,13 L21,16 L18,18 L17.5,21.5 L14,21 L12,24 L10,21 L6.5,21.5 L6,18 L3,16 L5,13 L3,10 L6,8 L6.5,4.5 L10,5 Z",
            [],
            Groups:
            [
                new NavGroup(
                    "investment",
                    "Investment",
                    [
                        new NavChild("admin-assets", "Assets", "admin-assets"),
                        new NavChild("admin-brokers", "Brokers", "admin-brokers"),
                        new NavChild("admin-portfolios", "Portfolios", "admin-portfolios"),
                    ]),
                new NavGroup(
                    "cashflow",
                    "CashFlow",
                    [
                        new NavChild("admin-banks", "Banks", "admin-banks"),
                        new NavChild("admin-categories", "Categories", "admin-categories"),
                        new NavChild("admin-credit-cards", "Credit Cards", "admin-credit-cards"),
                        new NavChild("admin-income-sources", "Income Sources", "admin-income-sources"),
                        new NavChild("admin-investment-accounts", "Investment Accounts", "admin-investment-accounts"),
                        new NavChild("admin-recurring-bills", "Recurring Bills", "admin-recurring-bills"),
                        new NavChild("admin-reserve-buckets", "Reserve Buckets", "admin-reserve-buckets"),
                    ]),
            ]),
        new NavCategory(
            "settings",
            "Settings",
            "M15,12 A3,3 0 1 1 9,12 A3,3 0 1 1 15,12 Z M19.4,15 a1.65,1.65 0 0 0 .33,1.82 l.06,.06 a2,2 0 1 1 -2.83,2.83 l-.06,-.06 a1.65,1.65 0 0 0 -1.82,-.33 1.65,1.65 0 0 0 -1,1.51 V21 a2,2 0 0 1 -4,0 v-.09 A1.65,1.65 0 0 0 9,19.4 a1.65,1.65 0 0 0 -1.82,.33 l-.06,.06 a2,2 0 1 1 -2.83,-2.83 l.06,-.06 a1.65,1.65 0 0 0 .33,-1.82 1.65,1.65 0 0 0 -1.51,-1 H3 a2,2 0 0 1 0,-4 h.09 A1.65,1.65 0 0 0 4.6,9 a1.65,1.65 0 0 0 -.33,-1.82 l-.06,-.06 a2,2 0 1 1 2.83,-2.83 l.06,.06 a1.65,1.65 0 0 0 1.82,.33 H9 a1.65,1.65 0 0 0 1,-1.51 V3 a2,2 0 0 1 4,0 v.09 a1.65,1.65 0 0 0 1,1.51 1.65,1.65 0 0 0 1.82,-.33 l.06,-.06 a2,2 0 1 1 2.83,2.83 l-.06,.06 a1.65,1.65 0 0 0 -.33,1.82 V9 a1.65,1.65 0 0 0 1.51,1 H21 a2,2 0 0 1 0,4 h-.09 a1.65,1.65 0 0 0 -1.51,1 Z",
            [
                new NavChild("appearance", "Appearance", "settings-appearance"),
            ]),
    ];
}
