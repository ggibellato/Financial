using System.Net;
using System.Net.Http;
using Google.Apis.Drive.v3;
using Google.Apis.Http;
using Google.Apis.Services;

namespace Financial.GoogleIntegrations.Tests;

internal sealed record RecordedRequest(HttpMethod Method, Uri Uri, string Body, string? MediaType, string? Authorization)
{
    public string Query(string name) => System.Web.HttpUtility.ParseQueryString(Uri.Query)[name] ?? string.Empty;

    public string Form(string name) => System.Web.HttpUtility.ParseQueryString(Body)[name] ?? string.Empty;
}

internal sealed class FakeGoogleApiHandler(Func<RecordedRequest, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<RecordedRequest> Requests { get; } = [];

    public IEnumerable<RecordedRequest> FileLists => Requests.Where(r => r.Method == HttpMethod.Get && r.Uri.AbsolutePath == "/drive/v3/files");

    public IEnumerable<RecordedRequest> MediaDownloads => Requests.Where(r => r.Uri.AbsolutePath.StartsWith("/drive/v3/files/") && r.Query("alt") == "media");

    public Google.Apis.Http.IHttpClientFactory SdkHttpClientFactory => new SingleHandlerFactory(this);

    public HttpClient CreateHttpClient() => new(this, disposeHandler: false);

    public DriveService CreateService() => new(new BaseClientService.Initializer
    {
        HttpClientFactory = SdkHttpClientFactory,
        ApplicationName = "contract-tests"
    });

    public static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };

    public static HttpResponseMessage Error(HttpStatusCode status) =>
        Json(status, $$$"""{"error":{"code":{{{(int)status}}},"message":"{{{status}}}"}}""");

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = await ReadBodyAsync(request.Content, cancellationToken);
        var recorded = new RecordedRequest(
            request.Method,
            request.RequestUri!,
            body,
            request.Content?.Headers.ContentType?.MediaType,
            request.Headers.Authorization?.ToString());
        Requests.Add(recorded);
        return respond(recorded);
    }

    private static async Task<string> ReadBodyAsync(HttpContent? content, CancellationToken cancellationToken)
    {
        if (content is null)
        {
            return string.Empty;
        }

        if (!content.Headers.ContentEncoding.Contains("gzip"))
        {
            return await content.ReadAsStringAsync(cancellationToken);
        }

        await using var gzip = new System.IO.Compression.GZipStream(
            await content.ReadAsStreamAsync(cancellationToken), System.IO.Compression.CompressionMode.Decompress);
        using var reader = new StreamReader(gzip);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private sealed class SingleHandlerFactory(HttpMessageHandler handler) : Google.Apis.Http.IHttpClientFactory
    {
        // Only the credential is applied: the SDK's other initializers add its own back-off on 5xx,
        // which would hide the retry behaviour the clients under test implement themselves.
        public ConfigurableHttpClient CreateHttpClient(CreateHttpClientArgs args)
        {
            var client = new ConfigurableHttpClient(new ConfigurableMessageHandler(handler));
            foreach (var credential in args.Initializers.OfType<Google.Apis.Auth.OAuth2.ICredential>())
            {
                credential.Initialize(client);
            }

            return client;
        }
    }
}
