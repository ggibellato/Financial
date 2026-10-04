namespace Financial.Api.Tests;

internal static class SnapshotUpdateFlag
{
    public const string Name = "UPDATE_OPENAPI_SNAPSHOT";

    public static bool IsRequested(Func<string, string?> readVariable)
    {
        if (string.IsNullOrWhiteSpace(readVariable(Name)))
        {
            return false;
        }

        if (readVariable("CI") == "true")
        {
            throw new InvalidOperationException($"{Name} must not be set in CI");
        }

        return true;
    }
}
