using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Reporting;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.Reporting;

[Trait("Category", "Unit")]
public class NonCarriedRecordsReportTests
{
    private static readonly string[] SeededNames =
        ["Ariana", "Carro", "Casa", "Estudo", "Extras", "Familia", "Gleison", "Mercado", "Samuel", "Saude", "Viagem", "Dizimo", "Investimento", "Reserva"];

    [Fact]
    public void Render_WithEveryTypePresent_ListsCountsInFixedOrder()
    {
        var data = BuildData(12, 3, 1, [.. SeededNames, "Pets", "Gifts", "Hobby", "Garden"]);

        var line = NonCarriedRecordsReport.From(data).Render();

        line.Should().Be("Not carried over: Transfers 12, BalanceAdjustments 3, Tithe carry-forwards 1, Categories 4");
    }

    [Fact]
    public void Render_WithSeededCategoryInDifferentCase_DoesNotCountIt()
    {
        var data = BuildData(0, 0, 0, ["mercado", "DIZIMO", "Pets"]);

        var line = NonCarriedRecordsReport.From(data).Render();

        line.Should().Be("Not carried over: Transfers 0, BalanceAdjustments 0, Tithe carry-forwards 0, Categories 1");
    }

    [Fact]
    public void Render_WithTitheEffectiveFromDate_IncludesItOnTheTitheSegment()
    {
        var data = BuildData(0, 0, 2, []);
        data.SetTitheCarryForwardEffectiveFrom(new DateOnly(2026, 4, 1));

        var line = NonCarriedRecordsReport.From(data).Render();

        line.Should().Be("Not carried over: Transfers 0, BalanceAdjustments 0, Tithe carry-forwards 2 (effective from 2026-04-01), Categories 0");
    }

    [Fact]
    public void From_EmptyData_CountsZero()
    {
        var line = NonCarriedRecordsReport.From(CashFlowData.Create()).Render();

        line.Should().Be("Not carried over: Transfers 0, BalanceAdjustments 0, Tithe carry-forwards 0, Categories 0");
    }

    private static CashFlowData BuildData(int transfers, int adjustments, int carryForwards, string[] categories)
    {
        var data = CashFlowData.Create();
        var barclays = TestAccounts.Bank("Barclays");
        var chase = TestAccounts.Bank("Chase");
        var date = new DateOnly(2026, 9, 10);

        for (var i = 0; i < transfers; i++)
        {
            data.AddTransfer(Transfer.Create(date, barclays, chase, 10m, destinationAmount: null, note: null, today: TestClock.Today));
        }

        for (var i = 0; i < adjustments; i++)
        {
            data.AddBalanceAdjustment(BalanceAdjustment.Create(date, barclays, 990m, -10m, note: null));
        }

        for (var month = 1; month <= carryForwards; month++)
        {
            data.AddTitheCarryForward(TitheCarryForward.Create(2026, month, included: true));
        }

        foreach (var name in categories)
        {
            data.AddCategory(Category.Create(name));
        }

        return data;
    }
}
