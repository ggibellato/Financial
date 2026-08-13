using Financial.CashFlow.Application.Interfaces;
using Financial.Shared.Infrastructure.Sync;
using Microsoft.Extensions.Hosting;

namespace Financial.CashFlow.Infrastructure.Hosting;

public sealed class CashFlowShutdownFlushHostedService : IHostedService
{
    private readonly ICashFlowRepository _repository;

    public CashFlowShutdownFlushHostedService(ICashFlowRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) =>
        _repository is ISyncStatusProvider syncStatusProvider
            ? syncStatusProvider.FlushAsync()
            : Task.CompletedTask;
}
