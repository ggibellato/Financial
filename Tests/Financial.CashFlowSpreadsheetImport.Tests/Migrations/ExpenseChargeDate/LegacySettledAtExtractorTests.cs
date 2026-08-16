using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.ExpenseChargeDate;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.Migrations.ExpenseChargeDate;

public class LegacySettledAtExtractorTests
{
    [Fact]
    public void Extract_RealisticLegacyJson_ReturnsSettledAtByExpenseId()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var json = $$"""
            {
              "Expenses": [
                { "Id": "{{id1}}", "Date": "2026-07-10", "CardTag": "BaAmex", "PaymentSource": "Barclays", "SettledAt": "2026-08-03" },
                { "Id": "{{id2}}", "Date": "2026-07-12", "CardTag": null, "PaymentSource": "Chase", "SettledAt": null }
              ]
            }
            """;

        var result = LegacySettledAtExtractor.Extract(json);

        result.Should().ContainKey(id1).WhoseValue.Should().Be(new DateOnly(2026, 8, 3));
        result.Should().NotContainKey(id2);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Extract_NullOrBlankInput_ReturnsEmpty(string? rawJson)
    {
        var result = LegacySettledAtExtractor.Extract(rawJson);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Extract_EntryMissingSettledAt_IsSkipped()
    {
        var id = Guid.NewGuid();
        var json = $$"""
            {
              "Expenses": [
                { "Id": "{{id}}", "Date": "2026-07-10", "CardTag": "BaAmex" }
              ]
            }
            """;

        var result = LegacySettledAtExtractor.Extract(json);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Extract_NoExpensesProperty_ReturnsEmpty()
    {
        var result = LegacySettledAtExtractor.Extract("{}");

        result.Should().BeEmpty();
    }
}
