using Financial.Shared.Infrastructure.Hosting;
using Financial.Shared.Infrastructure.Sync;
using FluentAssertions;

namespace Financial.Shared.Infrastructure.Tests.Hosting;

public class ShutdownFlushHostedServiceTests
{
    [Fact]
    public async Task StopAsync_WhenRepositoryIsASyncStatusProvider_CallsFlushAsync()
    {
        var repository = new SyncStatusProviderStub();
        var hostedService = new ShutdownFlushHostedService<SyncStatusProviderStub>(repository);

        await hostedService.StopAsync(CancellationToken.None);

        repository.FlushAsyncCallCount.Should().Be(1);
    }

    [Fact]
    public async Task StopAsync_WhenRepositoryIsNotASyncStatusProvider_CompletesWithoutError()
    {
        var repository = new object();
        var hostedService = new ShutdownFlushHostedService<object>(repository);

        var act = async () => await hostedService.StopAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new ShutdownFlushHostedService<object>(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    private sealed class SyncStatusProviderStub : ISyncStatusProvider
    {
        public int FlushAsyncCallCount { get; private set; }

        public SyncStatus GetStatus() => new(SyncState.Idle, null, null);

        public Task FlushAsync()
        {
            FlushAsyncCallCount++;
            return Task.CompletedTask;
        }
    }
}
