using Financial.Investment.Infrastructure.Hosting;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Investment.Infrastructure.Tests.Hosting;

public class InvestmentShutdownFlushHostedServiceTests
{
    [Fact]
    public async Task StopAsync_WhenRepositoryIsASyncStatusProvider_CallsFlushAsync()
    {
        var repository = new SyncStatusInvestmentRepositoryStub();
        var hostedService = new InvestmentShutdownFlushHostedService(repository);

        await hostedService.StopAsync(CancellationToken.None);

        repository.FlushAsyncCallCount.Should().Be(1);
    }

    [Fact]
    public async Task StopAsync_WhenRepositoryIsNotASyncStatusProvider_CompletesWithoutError()
    {
        var repository = new StubInvestmentRepository();
        var hostedService = new InvestmentShutdownFlushHostedService(repository);

        var act = async () => await hostedService.StopAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new InvestmentShutdownFlushHostedService(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }
}
