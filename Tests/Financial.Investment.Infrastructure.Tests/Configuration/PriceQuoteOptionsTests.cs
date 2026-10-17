using Financial.Investment.Infrastructure.Configuration;
using FluentAssertions;

namespace Financial.Investment.Infrastructure.Tests.Configuration;

[Trait("Category", "Unit")]
public class PriceQuoteOptionsTests
{
    private static PriceQuoteOptions LoadFrom(Dictionary<string, string?> values)
    {
        var options = new PriceQuoteOptions();
        options.Load(key => values.GetValueOrDefault(key));
        return options;
    }

    [Fact]
    public void Load_NothingConfigured_UsesFiveMinuteCacheAndThirtyMinuteCooldown()
    {
        var options = LoadFrom([]);

        options.CacheDuration.Should().Be(TimeSpan.FromMinutes(5));
        options.ProviderFailureCooldown.Should().Be(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void Load_DurationsConfigured_UsesConfiguredValues()
    {
        var options = LoadFrom(new Dictionary<string, string?>
        {
            [PriceQuoteOptions.CacheDurationKey] = "00:02:30",
            [PriceQuoteOptions.ProviderFailureCooldownKey] = "01:00:00"
        });

        options.CacheDuration.Should().Be(TimeSpan.FromSeconds(150));
        options.ProviderFailureCooldown.Should().Be(TimeSpan.FromHours(1));
    }

    [Theory]
    [InlineData("ten minutes")]
    [InlineData("-00:05:00")]
    public void Load_InvalidDuration_ThrowsNamingTheKey(string value)
    {
        Action act = () => LoadFrom(new Dictionary<string, string?> { [PriceQuoteOptions.CacheDurationKey] = value });

        act.Should().Throw<InvalidOperationException>().WithMessage($"*{PriceQuoteOptions.CacheDurationKey}*");
    }
}
