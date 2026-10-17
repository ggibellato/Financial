using System.Globalization;

namespace Financial.Investment.Infrastructure.Configuration;

public sealed class PriceQuoteOptions
{
    public const string CacheDurationKey = "Investment:PriceQuotes:CacheDuration";
    public const string ProviderFailureCooldownKey = "Investment:PriceQuotes:ProviderFailureCooldown";

    public static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan DefaultProviderFailureCooldown = TimeSpan.FromMinutes(30);

    public TimeSpan CacheDuration { get; set; } = DefaultCacheDuration;
    public TimeSpan ProviderFailureCooldown { get; set; } = DefaultProviderFailureCooldown;

    public void Load(Func<string, string?> readValue)
    {
        ArgumentNullException.ThrowIfNull(readValue);

        CacheDuration = ReadDuration(readValue, CacheDurationKey, DefaultCacheDuration);
        ProviderFailureCooldown = ReadDuration(readValue, ProviderFailureCooldownKey, DefaultProviderFailureCooldown);
    }

    private static TimeSpan ReadDuration(Func<string, string?> readValue, string key, TimeSpan defaultValue)
    {
        var value = readValue(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (!TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var duration) || duration < TimeSpan.Zero)
        {
            throw new InvalidOperationException($"Configuration value '{key}' must be a non-negative duration such as 00:05:00.");
        }

        return duration;
    }
}
