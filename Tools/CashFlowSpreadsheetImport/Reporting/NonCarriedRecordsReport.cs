using System.Globalization;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.Categories;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Reporting;

public sealed record NonCarriedRecordsReport(
    int Transfers,
    int BalanceAdjustments,
    int TitheCarryForwards,
    DateOnly? TitheEffectiveFrom,
    int UserCategories)
{
    public static NonCarriedRecordsReport From(CashFlowData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return new NonCarriedRecordsReport(
            data.Transfers.Count,
            data.BalanceAdjustments.Count,
            data.TitheCarryForwards.Count,
            data.TitheCarryForwardEffectiveFrom,
            data.Categories.Count(category => !CategoryMigrator.IsSeededCategoryName(category.Name)));
    }

    public string Render()
    {
        var tithe = TitheEffectiveFrom is { } effectiveFrom
            ? $"Tithe carry-forwards {TitheCarryForwards} (effective from {effectiveFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)})"
            : $"Tithe carry-forwards {TitheCarryForwards}";

        return $"Not carried over: Transfers {Transfers}, BalanceAdjustments {BalanceAdjustments}, {tithe}, Categories {UserCategories}";
    }
}
