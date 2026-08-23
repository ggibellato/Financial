#nullable enable
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using System.IO;

namespace Financial.Integrations.GoogleCore;

public sealed class GoogleCredentialFactory
{
    private const string GoogleApplicationName = "Financial";

    private readonly string _credentialsFilePath;
    private GoogleCredential? _unscopedCredential;

    public GoogleCredentialFactory(string credentialsFilePath)
    {
        _credentialsFilePath = credentialsFilePath;
    }

    public GoogleCredential Create(string[] scopes) =>
        GetUnscopedCredential().CreateScoped(scopes);

    private GoogleCredential GetUnscopedCredential()
    {
        if (_unscopedCredential is not null)
        {
            return _unscopedCredential;
        }

        using var stream = new FileStream(_credentialsFilePath, FileMode.Open, FileAccess.Read);
        _unscopedCredential = GoogleCredential.FromStream(stream);
        return _unscopedCredential;
    }

    public static BaseClientService.Initializer CreateInitializer(GoogleCredential credential)
    {
        return new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = GoogleApplicationName,
        };
    }
}
