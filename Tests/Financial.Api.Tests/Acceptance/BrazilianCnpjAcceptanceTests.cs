using System.Net;
using System.Net.Http.Json;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Api.Tests.Acceptance;

[Trait("Category", "Integration")]
public class BrazilianCnpjAcceptanceTests : ApiEndpointTests
{
    private const string AssetsRoute = "/api/v1/financial/assets";
    private const string Broker = "XPI";
    private const string Portfolio = "Default";
    private const string FundName = "CNPJFUND";
    private const string FundCnpj = "11222333000181";
    private const string AdministratorCnpj = "11444777000161";

    [Fact]
    [Trait("AC", "P69-F06-brazilian-cnpj-identifiers-03")]
    public async Task CreateAsset_WithMaskedCnpjs_StoresTheFourteenDigits()
    {
        await CreateFundAsync("11.222.333/0001-81", "11.444.777/0001-61");

        var stored = await GetStoredAsync(FundName);

        using (new AssertionScope())
        {
            stored.Cnpj.Should().Be(FundCnpj);
            stored.AdministratorCnpj.Should().Be(AdministratorCnpj);
        }
    }

    [Fact]
    [Trait("AC", "P69-F06-brazilian-cnpj-identifiers-05")]
    public async Task CreateAsset_BrazilianAssetWithoutCnpjs_Succeeds()
    {
        var response = await Client.PostAsJsonAsync(AssetsRoute, FundRequest(cnpj: null, administratorCnpj: null));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await GetStoredAsync(FundName);
        stored.Cnpj.Should().BeNull();
        stored.AdministratorCnpj.Should().BeNull();
    }

    [Theory]
    [Trait("AC", "P69-F06-brazilian-cnpj-identifiers-04")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateAsset_WithAnInvalidCnpj_Returns400AndKeepsTheStoredValue(bool administrator)
    {
        await CreateFundAsync(FundCnpj, AdministratorCnpj);
        var request = UpdateRequest(
            administrator ? FundCnpj : "11222333000182",
            administrator ? "11444777000162" : AdministratorCnpj);

        var response = await Client.PutAsJsonAsync($"{AssetsRoute}/{Broker}/{Portfolio}/{FundName}", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Enter a valid 14-digit CNPJ.");
        var stored = await GetStoredAsync(FundName);
        stored.Cnpj.Should().Be(FundCnpj);
        stored.AdministratorCnpj.Should().Be(AdministratorCnpj);
    }

    [Theory]
    [Trait("AC", "P69-F06-brazilian-cnpj-identifiers-06")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateAsset_WithABlankCnpjField_RemovesIt(bool administrator)
    {
        await CreateFundAsync(FundCnpj, AdministratorCnpj);
        var request = UpdateRequest(administrator ? FundCnpj : "  ", administrator ? "" : AdministratorCnpj);

        var response = await Client.PutAsJsonAsync($"{AssetsRoute}/{Broker}/{Portfolio}/{FundName}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await GetStoredAsync(FundName);
        using (new AssertionScope())
        {
            stored.Cnpj.Should().Be(administrator ? FundCnpj : null);
            stored.AdministratorCnpj.Should().Be(administrator ? null : AdministratorCnpj);
        }
    }

    [Theory]
    [Trait("AC", "P69-F06-brazilian-cnpj-identifiers-07")]
    [InlineData(CountryCode.UK, GlobalAssetClass.Fund, null, null)]
    [InlineData(CountryCode.BR, GlobalAssetClass.Equity, FundCnpj, null)]
    public async Task UpdateAsset_Reclassified_ClearsInapplicableCnpjs(
        CountryCode country, GlobalAssetClass assetClass, string? expectedCnpj, string? expectedAdministratorCnpj)
    {
        await CreateFundAsync(FundCnpj, AdministratorCnpj);
        var request = UpdateRequest(FundCnpj, AdministratorCnpj);
        request.Country = country;
        request.Class = assetClass;

        var response = await Client.PutAsJsonAsync($"{AssetsRoute}/{Broker}/{Portfolio}/{FundName}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await GetStoredAsync(FundName);
        using (new AssertionScope())
        {
            stored.Cnpj.Should().Be(expectedCnpj);
            stored.AdministratorCnpj.Should().Be(expectedAdministratorCnpj);
        }
    }

    [Fact]
    [Trait("AC", "P69-F06-brazilian-cnpj-identifiers-07")]
    public async Task UpdateAsset_EtfReclassifiedAsEquity_ClearsOnlyTheAdministratorCnpj()
    {
        var create = FundRequest(FundCnpj, AdministratorCnpj);
        create.Class = GlobalAssetClass.ETF;
        (await Client.PostAsJsonAsync(AssetsRoute, create)).StatusCode.Should().Be(HttpStatusCode.OK);
        var request = UpdateRequest(FundCnpj, AdministratorCnpj);
        request.Class = GlobalAssetClass.Equity;

        var response = await Client.PutAsJsonAsync($"{AssetsRoute}/{Broker}/{Portfolio}/{FundName}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await GetStoredAsync(FundName);
        stored.Cnpj.Should().Be(FundCnpj);
        stored.AdministratorCnpj.Should().BeNull();
    }

    [Fact]
    public async Task GetAssetDetails_ReturnsBothCnpjs()
    {
        await CreateFundAsync(FundCnpj, AdministratorCnpj);

        var details = await Client.GetFromJsonAsync<AssetDetailsDTO>($"{AssetsRoute}/{Broker}/{Portfolio}/{FundName}");

        details!.Cnpj.Should().Be(FundCnpj);
        details.AdministratorCnpj.Should().Be(AdministratorCnpj);
    }

    private async Task CreateFundAsync(string? cnpj, string? administratorCnpj)
    {
        var response = await Client.PostAsJsonAsync(AssetsRoute, FundRequest(cnpj, administratorCnpj));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static AssetAdminCreateDTO FundRequest(string? cnpj, string? administratorCnpj) => new()
    {
        BrokerName = Broker,
        PortfolioName = Portfolio,
        Name = FundName,
        Country = CountryCode.BR,
        Class = GlobalAssetClass.Fund,
        Cnpj = cnpj,
        AdministratorCnpj = administratorCnpj
    };

    private static AssetAdminUpdateDTO UpdateRequest(string? cnpj, string? administratorCnpj) => new()
    {
        Name = FundName,
        Country = CountryCode.BR,
        Class = GlobalAssetClass.Fund,
        Cnpj = cnpj,
        AdministratorCnpj = administratorCnpj
    };

    private async Task<AssetAdminDTO> GetStoredAsync(string name)
    {
        var assets = await Client.GetFromJsonAsync<List<AssetAdminDTO>>(AssetsRoute);
        return assets!.Single(a => a.Name == name);
    }
}
