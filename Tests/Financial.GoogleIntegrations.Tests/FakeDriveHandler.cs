using System.Net;
using System.Net.Http;
using Google.Apis.Drive.v3;
using Google.Apis.Http;
using Google.Apis.Services;

namespace Financial.GoogleIntegrations.Tests;

internal sealed record RecordedRequest(HttpMethod Method, Uri Uri, string Body, string? MediaType)
{
    public string Query(string name) => System.Web.HttpUtility.ParseQueryString(Uri.Query)[name] ?? string.Empty;
}

internal sealed class FakeDriveHandler(Func<RecordedRequest, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<RecordedRequest> Requests { get; } = [];

    public IEnumerable<RecordedRequest> FileLists => Requests.Where(r => r.Method == HttpMethod.Get && r.Uri.AbsolutePath == "/drive/v3/files");

    public IEnumerable<RecordedRequest> MediaDownloads => Requests.Where(r => r.Uri.AbsolutePath.StartsWith("/drive/v3/files/") && r.Query("alt") == "media");

    public DriveService CreateService() => new(new BaseClientService.Initializer
    {
        HttpClientFactory = new SingleHandlerFactory(this),
        ApplicationName = "contract-tests"
    });

    public static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };

    public static HttpResponseMessage Error(HttpStatusCode status) =>
        Json(status, $$$"""{"error":{"code":{{{(int)status}}},"message":"{{{status}}}"}}""");

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        var recorded = new RecordedRequest(request.Method, request.RequestUri!, body, request.Content?.Headers.ContentType?.MediaType);
        Requests.Add(recorded);
        return respond(recorded);
    }

    private sealed class SingleHandlerFactory(HttpMessageHandler handler) : Google.Apis.Http.IHttpClientFactory
    {
        public ConfigurableHttpClient CreateHttpClient(CreateHttpClientArgs args) =>
            new(new ConfigurableMessageHandler(handler));
    }
}
