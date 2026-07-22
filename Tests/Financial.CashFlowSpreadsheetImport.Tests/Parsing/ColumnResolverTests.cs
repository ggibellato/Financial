using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Parsing;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.Parsing;

public class ColumnResolverTests
{
    [Fact]
    public void IsCategoryColumn_2017Era_DescriptionInB_CategoryInC_IdentifiesCCorrectly()
    {
        // 2017-era header says B="Quem", C="Motivo", but the real content is B=merchant, C=category.
        var columnB = new[] { "Lidl UK", "WY&SF LTD", "Tesco" };
        var columnC = new[] { "Mercado", "Extras", "Mercado" };

        ColumnResolver.IsCategoryColumn(columnC, columnB).Should().BeTrue();
        ColumnResolver.IsCategoryColumn(columnB, columnC).Should().BeFalse();
    }

    [Fact]
    public void IsCategoryColumn_2019PlusEra_DescriptionInB_CategoryInC_IdentifiesCCorrectly()
    {
        // 2019+-era header says B="Motivo", C="Quem", but content is still B=merchant, C=category.
        var columnB = new[] { "ICELAND", "DZ", "GREENWICH LEISURE" };
        var columnC = new[] { "Mercado", "Dizimo", "Samuel" };

        ColumnResolver.IsCategoryColumn(columnC, columnB).Should().BeTrue();
        ColumnResolver.IsCategoryColumn(columnB, columnC).Should().BeFalse();
    }

    [Fact]
    public void IsCategoryColumn_NoValuesMatchEitherColumn_DefaultsToCandidateBeingCategory()
    {
        var columnB = new[] { "Foo", "Bar" };
        var columnC = new[] { "Baz", "Qux" };

        ColumnResolver.IsCategoryColumn(columnB, columnC).Should().BeTrue();
    }
}
