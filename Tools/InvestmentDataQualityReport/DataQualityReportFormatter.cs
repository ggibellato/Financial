using System.Text;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;

namespace Financial.Investment.DataQualityReport;

public static class DataQualityReportFormatter
{
    public static string Format(DataQualityReportDTO report)
    {
        var sb = new StringBuilder();

        AppendSalesExceedPurchases(sb, report.SalesExceedPurchases);
        sb.AppendLine();
        AppendUnpricedOpenHoldings(sb, report.UnpricedOpenHoldings);
        sb.AppendLine();
        AppendUnclassifiedHoldings(sb, report.UnclassifiedHoldings);
        sb.AppendLine();
        AppendUnclassifiedAndUnpricedLink(sb, report.UnclassifiedAndUnpricedOpenHoldings);
        sb.AppendLine();
        AppendHistoricHoldingsStillOpen(sb, report.HistoricHoldingsStillOpen);

        return sb.ToString();
    }

    private static void AppendSalesExceedPurchases(StringBuilder sb, IReadOnlyList<SalesExceedPurchasesFinding> findings)
    {
        sb.AppendLine($"Holdings selling more than they hold ({findings.Count}):");
        if (findings.Count == 0)
        {
            sb.AppendLine("  (none)");
            return;
        }

        foreach (var f in findings)
        {
            sb.AppendLine(
                $"  {f.BrokerName} / {f.PortfolioName} / {f.AssetName}: sale on {f.OffendingSaleDate:yyyy-MM-dd} " +
                $"left {f.Shortfall} short of the {f.QuantityHeld} held at that point.");
        }
    }

    private static void AppendUnpricedOpenHoldings(StringBuilder sb, IReadOnlyList<UnpricedOpenHoldingFinding> findings)
    {
        sb.AppendLine($"Open holdings with no recorded price ({findings.Count}):");
        if (findings.Count == 0)
        {
            sb.AppendLine("  (none)");
            return;
        }

        foreach (var f in findings)
        {
            sb.AppendLine($"  {f.BrokerName} / {f.PortfolioName} / {f.AssetName}");
        }
    }

    private static void AppendUnclassifiedHoldings(StringBuilder sb, IReadOnlyList<UnclassifiedHoldingFinding> findings)
    {
        var active = findings.Where(f => f.Scope == InvestmentScope.Active).ToList();
        var historic = findings.Where(f => f.Scope == InvestmentScope.Historic).ToList();

        sb.AppendLine($"Unclassified holdings ({findings.Count}: {active.Count} active, {historic.Count} historic):");
        if (findings.Count == 0)
        {
            sb.AppendLine("  (none)");
            return;
        }

        sb.AppendLine("  Active:");
        foreach (var f in active)
        {
            sb.AppendLine($"    {f.BrokerName} / {f.PortfolioName} / {f.AssetName}");
        }

        sb.AppendLine("  Historic:");
        foreach (var f in historic)
        {
            sb.AppendLine($"    {f.BrokerName} / {f.PortfolioName} / {f.AssetName}");
        }
    }

    private static void AppendUnclassifiedAndUnpricedLink(StringBuilder sb, IReadOnlyList<UnclassifiedHoldingFinding> findings)
    {
        sb.AppendLine(
            "Note: which price source is used is chosen by asset class, so an unclassified open " +
            "holding may fail to price for that reason alone. Classifying it may also fix its price.");
        sb.AppendLine($"Unclassified holdings that are also unpriced ({findings.Count}):");
        if (findings.Count == 0)
        {
            sb.AppendLine("  (none)");
            return;
        }

        foreach (var f in findings)
        {
            sb.AppendLine($"  {f.BrokerName} / {f.PortfolioName} / {f.AssetName}");
        }
    }

    private static void AppendHistoricHoldingsStillOpen(StringBuilder sb, IReadOnlyList<HistoricHoldingStillOpenFinding> findings)
    {
        sb.AppendLine($"Historic holdings still carrying a quantity ({findings.Count}):");
        if (findings.Count == 0)
        {
            sb.AppendLine("  (none)");
            return;
        }

        foreach (var f in findings)
        {
            sb.AppendLine($"  {f.BrokerName} / {f.PortfolioName} / {f.AssetName}: {f.Quantity} units, {f.CostOfUnitsHeld} cost");
        }
    }
}
