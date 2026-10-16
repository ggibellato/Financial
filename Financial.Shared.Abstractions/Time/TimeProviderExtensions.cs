namespace Financial.Shared.Abstractions.Time;

public static class TimeProviderExtensions
{
    public static DateTime GetLocalDate(this TimeProvider timeProvider) => timeProvider.GetLocalNow().Date;

    public static DateOnly GetLocalToday(this TimeProvider timeProvider) =>
        DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

    public static DateOnly GetUtcToday(this TimeProvider timeProvider) =>
        DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
}
