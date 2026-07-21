using Financial.Investment.Infrastructure.Integrations.WebPageParser;
using FluentAssertions;

namespace Financial.Investment.Infrastructure.Tests.Integrations;

public class StatusInvestTests
{
    [Fact]
    public void DeriveSlug_Selic_ReturnsExpectedSlug()
    {
        var slug = StatusInvest.DeriveSlug("TESOURO SELIC 2029");

        slug.Should().Be("tesouro-selic-2029");
    }

    [Fact]
    public void DeriveSlug_WithPlusSign_ReturnsExpectedSlug()
    {
        var slug = StatusInvest.DeriveSlug("TESOURO IPCA+ 2029");

        slug.Should().Be("tesouro-ipca-2029");
    }

    [Fact]
    public void DeriveSlug_WithJurosSemestrais_ReturnsExpectedSlug()
    {
        var slug = StatusInvest.DeriveSlug("TESOURO IPCA+ COM JUROS SEMESTRAIS 2035");

        slug.Should().Be("tesouro-ipca-com-juros-semestrais-2035");
    }

    [Fact]
    public void ExtractSellPrice_VendaBeforeCompra_ReturnsVendaPrice()
    {
        const string pageText = "VALOR DE VENDA Valor Unitário R$ 3.775,97 0,32% VALOR DE COMPRA Valor Unitário R$ 3.789,57 0,32%";

        var price = StatusInvest.ExtractSellPrice(pageText);

        price.Should().Be(3775.97m);
    }

    [Fact]
    public void ExtractSellPrice_NoVendaSection_ReturnsNull()
    {
        const string pageText = "Some unrelated page content with no matching section.";

        var price = StatusInvest.ExtractSellPrice(pageText);

        price.Should().BeNull();
    }

    [Fact]
    public void ExtractSellPrice_NoValorUnitarioInVendaSection_ReturnsNull()
    {
        const string pageText = "VALOR DE VENDA (structure changed, no price label here) VALOR DE COMPRA Valor Unitário R$ 3.789,57";

        var price = StatusInvest.ExtractSellPrice(pageText);

        price.Should().BeNull();
    }

    [Fact]
    public void ParsePrice_BrazilianThousandsAndDecimal_ParsesCorrectly()
    {
        var price = StatusInvest.ParsePrice("19.379,93");

        price.Should().Be(19379.93m);
    }
}
