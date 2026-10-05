namespace Financial.Shared.Abstractions.Time;

public static class TimeProviderExtensions
{
    public static DateOnly GetLocalToday(this TimeProvider timeProvider) =>
        DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
}
