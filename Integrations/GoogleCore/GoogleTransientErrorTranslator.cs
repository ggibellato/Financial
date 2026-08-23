using System.Net;
using Financial.Shared.Abstractions.Resilience;
using Google;

namespace Financial.Integrations.GoogleCore;

public static class GoogleTransientErrorTranslator
{
    public static void ThrowIfTransient(GoogleApiException ex)
    {
        if (!IsTransientStatusCode(ex.HttpStatusCode))
        {
            return;
        }

        throw new TransientStorageException(
            $"Drive request failed with a transient status ({(int)ex.HttpStatusCode} {ex.HttpStatusCode}).",
            ex);
    }

    private static bool IsTransientStatusCode(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.TooManyRequests || (int)statusCode >= 500;
}
