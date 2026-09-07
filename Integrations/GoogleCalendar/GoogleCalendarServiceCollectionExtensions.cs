using Microsoft.Extensions.DependencyInjection;

namespace Financial.Integrations.GoogleCalendar;

public static class GoogleCalendarServiceCollectionExtensions
{
    public static IServiceCollection AddGoogleCalendarOAuthClient(this IServiceCollection services)
    {
        services.AddHttpClient<IGoogleCalendarOAuthClient, GoogleCalendarOAuthClient>();
        return services;
    }
}
