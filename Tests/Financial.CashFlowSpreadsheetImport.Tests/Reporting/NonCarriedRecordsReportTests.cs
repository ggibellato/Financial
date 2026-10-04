using System.Text.Json;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Reporting;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.Reporting;

public class NonCarriedRecordsReportTests
{
    private static readonly string[] SeededNames =
        ["Ariana", "Carro", "Casa", "Estudo", "Extras", "Familia", "Gleison", "Mercado", "Samuel", "Saude", "Viagem", "Dizimo", "Investimento", "Reserva"];

    [Fact]
    public void Render_WithEveryTypePresent_ListsCountsInFixedOrder()
    {
        var json = BuildJson(12, 3, 1, null, [.. SeededNames, "Pets", "Gifts", "Hobby", "Garden"]);

        var line = NonCarriedRecordsReport.FromJson(json).Render();

        line.Should().Be("Not carried over: Transfers 12, BalanceAdjustments 3, Tithe carry-forwards 1, Categories 4");
    }

    [Fact]
    public void Render_WithSeededCategoryInDifferentCase_DoesNotCountIt()
    {
        var json = BuildJson(0, 0, 0, null, ["mercado", "DIZIMO", "Pets"]);

        var line = NonCarriedRecordsReport.FromJson(json).Render();

        line.Should().Be("Not carried over: Transfers 0, BalanceAdjustments 0, Tithe carry-forwards 0, Categories 1");
    }

    [Fact]
    public void Render_WithTitheEffectiveFromDate_IncludesItOnTheTitheSegment()
    {
        var json = BuildJson(0, 0, 2, "2026-04-01", []);

        var line = NonCarriedRecordsReport.FromJson(json).Render();

        line.Should().Be("Not carried over: Transfers 0, BalanceAdjustments 0, Tithe carry-forwards 2 (effective from 2026-04-01), Categories 0");
    }

    [Fact]
    public void FromJson_WithOldFileMissingTheCollections_CountsZero()
    {
        var line = NonCarriedRecordsReport.FromJson("""{ "Expenses": [], "Banks": [] }""").Render();

        line.Should().Be("Not carried over: Transfers 0, BalanceAdjustments 0, Tithe carry-forwards 0, Categories 0");
    }

    [Fact]
    public void FromJson_WithCorruptJson_Throws()
    {
        var act = () => NonCarriedRecordsReport.FromJson("{ not json");

        act.Should().Throw<JsonException>();
    }

    private static string BuildJson(int transfers, int adjustments, int carryForwards, string? effectiveFrom, string[] categories)
    {
        var document = new Dictionary<string, object?>
        {
            ["Transfers"] = Enumerable.Range(0, transfers).Select(_ => new { }).ToArray(),
            ["BalanceAdjustments"] = Enumerable.Range(0, adjustments).Select(_ => new { }).ToArray(),
            ["TitheCarryForwards"] = Enumerable.Range(0, carryForwards).Select(_ => new { }).ToArray(),
            ["TitheCarryForwardEffectiveFrom"] = effectiveFrom,
            ["Categories"] = categories.Select(name => new { Name = name }).ToArray()
        };

        return JsonSerializer.Serialize(document);
    }
}
