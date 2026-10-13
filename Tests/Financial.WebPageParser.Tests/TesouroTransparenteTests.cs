using Financial.Integrations.WebPageParser;
using FluentAssertions;

namespace Financial.WebPageParser.Tests;

[Trait("Category", "Unit")]
public class TesouroTransparenteTests
{
    private const string Header = "Tipo Titulo;Data Vencimento;Data Base;Taxa Compra Manha;Taxa Venda Manha;PU Compra Manha;PU Venda Manha;PU Base Manha";

    private static IReadOnlyList<TesouroTransparente.PriceRow> Parse(params string[] lines) =>
        TesouroTransparente.ParseLatestPrices(new StringReader(string.Join('\n', new[] { Header }.Concat(lines))));

    [Fact]
    public void FindSellValue_BondListed_ReturnsPuVendaManha()
    {
        var rows = Parse(
            "Tesouro IPCA+;15/05/2029;09/10/2026;7,50;7,60;4042,91;4025,85;4025,85",
            "Tesouro Selic;01/03/2031;09/10/2026;0,09;0,10;19991,91;19972,93;19972,93");

        TesouroTransparente.FindSellValue(rows, "TESOURO IPCA+ 2029").Should().Be(4025.85m);
        TesouroTransparente.FindSellValue(rows, "TESOURO SELIC 2031").Should().Be(19972.93m);
    }

    [Fact]
    public void FindSellValue_SeveralDaysForOneBond_UsesTheLatestBaseDate()
    {
        var rows = Parse(
            "Tesouro IPCA+;15/05/2029;08/10/2026;7,50;7,60;4000,00;3990,00;3990,00",
            "Tesouro IPCA+;15/05/2029;09/10/2026;7,50;7,60;4042,91;4025,85;4025,85",
            "Tesouro IPCA+;15/05/2029;07/10/2026;7,50;7,60;3900,00;3890,00;3890,00");

        TesouroTransparente.FindSellValue(rows, "TESOURO IPCA+ 2029").Should().Be(4025.85m);
    }

    [Fact]
    public void FindSellValue_MaturedBond_ReturnsItsLastPublishedPrice()
    {
        var rows = Parse(
            "Tesouro IPCA+;15/08/2026;13/08/2026;5,00;5,10;4700,00;4690,00;4690,00",
            "Tesouro IPCA+;15/08/2026;14/08/2026;5,00;5,10;4739,77;4740,25;4740,25");

        TesouroTransparente.FindSellValue(rows, "TESOURO IPCA+ 2026").Should().Be(4740.25m);
    }

    [Fact]
    public void FindSellValue_SameYearDifferentTypes_KeepsThemApart()
    {
        var rows = Parse(
            "Tesouro IPCA+;15/05/2035;09/10/2026;7,10;7,20;2748,88;2718,98;2718,98",
            "Tesouro IPCA+ com Juros Semestrais;15/05/2035;09/10/2026;7,00;7,10;4100,00;4090,00;4090,00");

        TesouroTransparente.FindSellValue(rows, "Tesouro IPCA+ 2035").Should().Be(2718.98m);
        TesouroTransparente.FindSellValue(rows, "TESOURO IPCA+ COM JUROS SEMESTRAIS 2035").Should().Be(4090.00m);
    }

    [Fact]
    public void FindSellValue_IgnoresCaseAccentsAndExtraSpaces()
    {
        var rows = Parse("Tesouro Renda+ Aposentadoria Extra;15/12/2065;09/10/2026;6,50;6,60;1000,00;990,00;990,00");

        TesouroTransparente.FindSellValue(rows, "  tésouro   RENDA+ aposentadoria extra 2065 ").Should().Be(990.00m);
    }

    [Fact]
    public void FindSellValue_BondNotInTheList_Throws()
    {
        var rows = Parse("Tesouro Selic;01/03/2031;09/10/2026;0,09;0,10;19991,91;19972,93;19972,93");

        var act = () => TesouroTransparente.FindSellValue(rows, "TESOURO IPCA+ 2040");

        act.Should().Throw<InvalidOperationException>().WithMessage("*TESOURO IPCA+ 2040*not in the Tesouro Transparente*");
    }

    [Fact]
    public void FindSellValue_TwoMaturitiesInTheSameYear_RefusesToGuess()
    {
        var rows = Parse(
            "Tesouro Prefixado;01/01/2009;09/10/2008;10,00;10,10;800,00;790,00;790,00",
            "Tesouro Prefixado;01/07/2009;09/10/2008;10,00;10,10;700,00;690,00;690,00");

        var act = () => TesouroTransparente.FindSellValue(rows, "TESOURO PREFIXADO 2009");

        act.Should().Throw<InvalidOperationException>().WithMessage("*more than one maturity*");
    }

    [Fact]
    public void FindSellValue_TitleWithoutAYear_Throws()
    {
        var rows = Parse("Tesouro Selic;01/03/2031;09/10/2026;0,09;0,10;19991,91;19972,93;19972,93");

        var act = () => TesouroTransparente.FindSellValue(rows, "TESOURO SELIC");

        act.Should().Throw<InvalidOperationException>().WithMessage("*maturity year*");
    }

    [Fact]
    public void ParseLatestPrices_MalformedRows_AreSkipped()
    {
        var rows = Parse(
            "Tesouro Selic;not-a-date;09/10/2026;0,09;0,10;19991,91;19972,93;19972,93",
            "Tesouro Selic;01/03/2031;09/10/2026;0,09;0,10;19991,91;sem preco;19972,93",
            "Tesouro Selic;01/03/2031",
            "Tesouro Selic;01/03/2027;09/10/2026;0,01;0,02;20067,61;20056,61;20056,61");

        rows.Should().ContainSingle().Which.SellPrice.Should().Be(20056.61m);
    }

    [Fact]
    public void ParseLatestPrices_MissingPriceColumn_NamesTheColumn()
    {
        var csv = new StringReader("Tipo Titulo;Data Vencimento;Data Base\nTesouro Selic;01/03/2031;09/10/2026");

        var act = () => TesouroTransparente.ParseLatestPrices(csv);

        act.Should().Throw<InvalidOperationException>().WithMessage("*'PU Venda Manha' column*");
    }

    [Fact]
    public void ParseLatestPrices_EmptyFile_Throws()
    {
        var act = () => TesouroTransparente.ParseLatestPrices(new StringReader(string.Empty));

        act.Should().Throw<InvalidOperationException>().WithMessage("*price list is empty*");
    }
}
