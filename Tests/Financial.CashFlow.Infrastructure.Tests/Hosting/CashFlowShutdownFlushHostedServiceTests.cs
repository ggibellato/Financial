using Financial.CashFlow.Infrastructure.Hosting;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.CashFlow.Infrastructure.Tests.Hosting;

public class CashFlowShutdownFlushHostedServiceTests
{
    [Fact]
    public async Task StopAsync_WhenRepositoryIsASyncStatusProvider_CallsFlushAsync()
    {
        var repository = new SyncStatusCashFlowRepositoryStub();
        var hostedService = new CashFlowShutdownFlushHostedService(repository);

        await hostedService.StopAsync(CancellationToken.None);

        repository.FlushAsyncCallCount.Should().Be(1);
    }

    [Fact]
    public async Task StopAsync_WhenRepositoryIsNotASyncStatusProvider_CompletesWithoutError()
    {
        var repository = new StubCashFlowRepository();
        var hostedService = new CashFlowShutdownFlushHostedService(repository);

        var act = async () => await hostedService.StopAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new CashFlowShutdownFlushHostedService(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }
}
