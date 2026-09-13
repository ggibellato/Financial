using System.Net.Http.Json;
using Financial.Investment.Application.DTOs;
using FluentAssertions;

namespace Financial.Api.Tests;

public class ReportingCurrencyEndpointsTests : ApiEndpointTests
{
    private const string BrokerName = "XPI";

    [Fact]
    public async Task GetReportingCurrency_DefaultsToEnabled()
    {
        var response = await Client.GetAsync("/api/v1/financial/reporting-currency");
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<ReportingCurrencySettingDTO>();
        dto!.Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task SetReportingCurrencyEnabled_False_PersistsAndReturnsUpdatedSetting()
    {
        var response = await Client.PutAsJsonAsync(
            "/api/v1/financial/reporting-currency/enabled", new SetReportingCurrencyEnabledRequestDTO { Enabled = false });
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<ReportingCurrencySettingDTO>();
        dto!.Enabled.Should().BeFalse();

        var getResponse = await Client.GetAsync("/api/v1/financial/reporting-currency");
        var getDto = await getResponse.Content.ReadFromJsonAsync<ReportingCurrencySettingDTO>();
        getDto!.Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task SetReportingCurrencyEnabled_MissingBody_ReturnsBadRequest()
    {
        var response = await Client.PutAsync("/api/v1/financial/reporting-currency/enabled", null);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WhenDisabled_BrokerSummary_HasNoConvertedFieldsAndSkipsConversion()
    {
        var setResponse = await Client.PutAsJsonAsync(
            "/api/v1/financial/reporting-currency/enabled", new SetReportingCurrencyEnabledRequestDTO { Enabled = false });
        setResponse.EnsureSuccessStatusCode();

        var response = await Client.GetAsync($"/api/v1/financial/summary/broker/{BrokerName}");
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<AggregatedSummaryDTO>();
        dto!.IsReportingCurrencyEnabled.Should().BeFalse();
        dto.ConvertedMarketValue.Should().BeNull();
        dto.ConvertedInvested.Should().BeNull();
        dto.IsPartial.Should().BeFalse();
        dto.IsReportingCurrencyUnavailable.Should().BeFalse();
    }
}
