using System.Net;
using System.Net.Http;
using Financial.Integrations.GoogleDrive;
using Financial.Shared.Abstractions.Resilience;
using FluentAssertions;
using Google;
using Google.Apis.Auth.OAuth2.Responses;

namespace Financial.GoogleIntegrations.Tests;

[Trait("Category", "Unit")]
public class GoogleDriveClientContractTests
{
    private const string FileListJson = """{"files":[{"id":"file-1","name":"data.json"}]}""";
    private const string UploadSessionUri = "https://www.googleapis.com/upload/session/1";

    private readonly List<TimeSpan> _requestedDelays = [];

    private (GoogleDriveClient Client, FakeDriveHandler Handler) CreateClient(Func<RecordedRequest, HttpResponseMessage> respond)
    {
        var handler = new FakeDriveHandler(respond);
        var client = new GoogleDriveClient(_ => handler.CreateService(), delay: (delay, _) =>
        {
            _requestedDelays.Add(delay);
            return Task.CompletedTask;
        });
        return (client, handler);
    }

    private static HttpResponseMessage Ok(string content) => new(HttpStatusCode.OK) { Content = new StringContent(content) };

    private static HttpResponseMessage ListThenMedia(RecordedRequest request, string listJson, string mediaContent) =>
        request.Query("alt") == "media" ? Ok(mediaContent) : FakeDriveHandler.Json(HttpStatusCode.OK, listJson);

    [Fact]
    public async Task GetFiles_ReturnsNamesAndIds()
    {
        var (client, handler) = CreateClient(_ => FakeDriveHandler.Json(
            HttpStatusCode.OK, """{"files":[{"id":"a","name":"one.json"},{"id":"b","name":"two.json"}]}"""));

        var files = await client.GetFilesAsync();

        files.Select(f => (f.Id, f.Name)).Should().Equal(("a", "one.json"), ("b", "two.json"));
        handler.FileLists.Single().Query("fields").Should().Be("nextPageToken, files(webViewLink, name, id)");
    }

    [Fact]
    public void Download_ResolvesByNameThenReadsContent()
    {
        var (client, handler) = CreateClient(r => ListThenMedia(r, FileListJson, "{\"balance\":12.5}"));

        var content = client.DownloadFileContent("data.json");

        content.Should().Be("{\"balance\":12.5}");
        handler.FileLists.Single().Query("q").Should().Be("name = 'data.json' and trashed = false");
        handler.MediaDownloads.Single().Uri.AbsolutePath.Should().EndWith("/files/file-1");
    }

    [Fact]
    public void Download_NameWithApostrophe_EscapesTheQuery()
    {
        var (client, handler) = CreateClient(r => ListThenMedia(r, FileListJson, "x"));

        client.DownloadFileContent("Bob's data.json");

        handler.FileLists.Single().Query("q").Should().Be("name = 'Bob\\'s data.json' and trashed = false");
    }

    [Theory]
    [InlineData("folder/sub/data.json")]
    [InlineData("folder\\sub\\data.json")]
    public void Download_UsesTheLastPathSegment(string path)
    {
        var (client, handler) = CreateClient(r => ListThenMedia(r, FileListJson, "x"));

        client.DownloadFileContent(path);

        handler.FileLists.Single().Query("q").Should().Be("name = 'data.json' and trashed = false");
    }

    [Fact]
    public void Download_ShortcutResolvesToItsTarget()
    {
        const string shortcut = """
            {"files":[{"id":"shortcut-id","name":"data.json","mimeType":"application/vnd.google-apps.shortcut","shortcutDetails":{"targetId":"target-id"}}]}
            """;
        var (client, handler) = CreateClient(r => ListThenMedia(r, shortcut, "x"));

        client.DownloadFileContent("data.json");

        handler.MediaDownloads.Single().Uri.AbsolutePath.Should().EndWith("/files/target-id");
    }

    [Fact]
    public void Download_CachesTheResolvedFileId()
    {
        var (client, handler) = CreateClient(r => ListThenMedia(r, FileListJson, "x"));

        client.DownloadFileContent("data.json");
        client.DownloadFileContent("data.json");

        handler.FileLists.Should().ContainSingle();
        handler.MediaDownloads.Should().HaveCount(2);
    }

    [Fact]
    public void Download_NoMatch_ThrowsFileNotFound()
    {
        var (client, _) = CreateClient(_ => FakeDriveHandler.Json(HttpStatusCode.OK, """{"files":[]}"""));

        var act = () => client.DownloadFileContent("missing.json");

        act.Should().Throw<FileNotFoundException>().WithMessage("*missing.json*");
    }

    [Fact]
    public void Download_MultipleMatches_ThrowsInvalidOperation()
    {
        var (client, _) = CreateClient(_ => FakeDriveHandler.Json(
            HttpStatusCode.OK, """{"files":[{"id":"1","name":"data.json"},{"id":"2","name":"data.json"}]}"""));

        var act = () => client.DownloadFileContent("data.json");

        act.Should().Throw<InvalidOperationException>().WithMessage("*data.json*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void DownloadAndUpload_BlankPath_ThrowArgumentException(string path)
    {
        var (client, handler) = CreateClient(_ => FakeDriveHandler.Error(HttpStatusCode.InternalServerError));

        var download = () => client.DownloadFileContent(path);
        var upload = () => client.UploadFileContent(path, "x");

        download.Should().Throw<ArgumentException>();
        upload.Should().Throw<ArgumentException>();
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public void Upload_SendsTheContentToTheResolvedFile()
    {
        var (client, handler) = CreateClient(UploadResponder(HttpStatusCode.OK));

        client.UploadFileContent("data.json", "{\"balance\":12.5}");

        handler.Requests.Should().Contain(r => r.Uri.AbsolutePath.StartsWith("/upload/drive/v3/files/file-1"));
        var upload = handler.Requests.Single(r => r.Method == HttpMethod.Put);
        upload.Uri.ToString().Should().Be(UploadSessionUri);
        upload.Body.Should().Be("{\"balance\":12.5}");
    }

    [Fact]
    public void Upload_RejectedByDrive_ThrowsInvalidOperationNamingThePath()
    {
        var (client, _) = CreateClient(UploadResponder(HttpStatusCode.BadRequest));

        var act = () => client.UploadFileContent("data.json", "x");

        act.Should().Throw<InvalidOperationException>().WithMessage("*data.json*");
    }

    [Fact]
    public async Task Resolve_RateLimitedOnce_RetriesAfterTheFirstBackoff()
    {
        var lists = 0;
        var (client, handler) = CreateClient(_ => ++lists == 1
            ? FakeDriveHandler.Error(HttpStatusCode.TooManyRequests)
            : FakeDriveHandler.Json(HttpStatusCode.OK, FileListJson));

        var files = await client.GetFilesAsync();

        files.Should().ContainSingle();
        handler.FileLists.Should().HaveCount(2);
        _requestedDelays.Should().Equal(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Resolve_RateLimitedEveryTime_ThrowsTheRateLimitMessageWithoutWaiting()
    {
        var (client, handler) = CreateClient(_ => FakeDriveHandler.Error(HttpStatusCode.TooManyRequests));

        var act = () => client.DownloadFileContent("data.json");

        act.Should().Throw<HttpRequestException>().WithMessage("*rate limit*");
        _requestedDelays.Should().HaveCount(handler.Requests.Count - 1).And.OnlyContain(delay => delay > TimeSpan.Zero);
    }

    [Fact]
    public void FileClient_NotFoundOnMedia_PropagatesTheGoogleApiException()
    {
        var (client, _) = CreateClient(r => r.Query("alt") == "media"
            ? FakeDriveHandler.Error(HttpStatusCode.NotFound)
            : FakeDriveHandler.Json(HttpStatusCode.OK, FileListJson));
        var fileClient = new GoogleDriveFileClient(client);

        var act = () => fileClient.DownloadFileContent("data.json");

        act.Should().Throw<GoogleApiException>().Which.HttpStatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public void FileClient_ServiceUnavailableOnMedia_ThrowsTransientStorageExceptionWithoutRetrying()
    {
        var (client, handler) = CreateClient(r => r.Query("alt") == "media"
            ? FakeDriveHandler.Error(HttpStatusCode.ServiceUnavailable)
            : FakeDriveHandler.Json(HttpStatusCode.OK, FileListJson));
        var fileClient = new GoogleDriveFileClient(client);

        var act = () => fileClient.DownloadFileContent("data.json");

        act.Should().Throw<TransientStorageException>().WithInnerException<GoogleApiException>();
        handler.MediaDownloads.Should().ContainSingle();
        _requestedDelays.Should().BeEmpty();
    }

    [Fact]
    public void FileClient_CredentialRejected_PropagatesInvalidGrantUnchangedAndUnretried()
    {
        var requests = 0;
        var (client, _) = CreateClient(_ =>
        {
            requests++;
            throw new TokenResponseException(new TokenErrorResponse { Error = "invalid_grant" });
        });
        var fileClient = new GoogleDriveFileClient(client);

        var act = () => fileClient.DownloadFileContent("data.json");

        act.Should().Throw<TokenResponseException>().Which.Error.Error.Should().Be("invalid_grant");
        requests.Should().Be(1);
        _requestedDelays.Should().BeEmpty();
    }

    private static Func<RecordedRequest, HttpResponseMessage> UploadResponder(HttpStatusCode uploadStatus) => request =>
    {
        if (request.Method == HttpMethod.Put)
        {
            return uploadStatus == HttpStatusCode.OK
                ? FakeDriveHandler.Json(HttpStatusCode.OK, """{"id":"file-1"}""")
                : FakeDriveHandler.Error(uploadStatus);
        }

        if (request.Uri.AbsolutePath.StartsWith("/upload/"))
        {
            var session = new HttpResponseMessage(HttpStatusCode.OK);
            session.Headers.Location = new Uri(UploadSessionUri);
            return session;
        }

        return FakeDriveHandler.Json(HttpStatusCode.OK, FileListJson);
    };
}
