using Financial.Integrations.WebPageParser;
using FluentAssertions;

namespace Financial.WebPageParser.Tests;

[Trait("Category", "Unit")]
public class TesouroDiretoSlugTests
{
    [Fact]
    public void Derive_Selic_ReturnsExpectedSlug()
    {
        var slug = TesouroDiretoSlug.Derive("TESOURO SELIC 2029");

        slug.Should().Be("tesouro-selic-2029");
    }

    [Fact]
    public void Derive_WithPlusSign_ReturnsExpectedSlug()
    {
        var slug = TesouroDiretoSlug.Derive("TESOURO IPCA+ 2029");

        slug.Should().Be("tesouro-ipca-2029");
    }

    [Fact]
    public void Derive_WithJurosSemestrais_ReturnsExpectedSlug()
    {
        var slug = TesouroDiretoSlug.Derive("TESOURO IPCA+ COM JUROS SEMESTRAIS 2035");

        slug.Should().Be("tesouro-ipca-com-juros-semestrais-2035");
    }
}
