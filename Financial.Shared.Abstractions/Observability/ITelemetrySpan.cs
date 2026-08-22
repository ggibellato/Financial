namespace Financial.Shared.Abstractions;

public interface ITelemetrySpan : IDisposable
{
    void SetAttribute(string key, string value);

    void RecordException(Exception exception);
}
