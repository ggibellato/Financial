using Financial.Integrations.GoogleCalendar;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.GoogleIntegrations.Tests;

public class GoogleCalendarServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGoogleCalendarOAuthClient_RegistersOAuthClient()
    {
        var services = new ServiceCollection();

        services.AddGoogleCalendarOAuthClient();
        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IGoogleCalendarOAuthClient>().Should().NotBeNull();
    }
}
