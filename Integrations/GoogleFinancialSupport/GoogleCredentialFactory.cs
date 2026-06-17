using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using System.IO;

namespace Financial.Infrastructure.Integrations.GoogleFinancialSupport;

internal sealed class GoogleCredentialFactory
{
    private const string GoogleApplicationName = "Financial";

    private readonly string _credentialsFilePath;

    internal GoogleCredentialFactory(string credentialsFilePath)
    {
        _credentialsFilePath = credentialsFilePath;
    }

    internal GoogleCredential Create(string[] scopes)
    {
        using var stream = new FileStream(_credentialsFilePath, FileMode.Open, FileAccess.Read);
        return GoogleCredential.FromStream(stream).CreateScoped(scopes);
    }

    internal static BaseClientService.Initializer CreateInitializer(GoogleCredential credential)
    {
        return new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = GoogleApplicationName,
        };
    }
}
