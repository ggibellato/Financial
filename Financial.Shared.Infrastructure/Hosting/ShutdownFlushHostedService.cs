using Financial.Shared.Infrastructure.Sync;
using Microsoft.Extensions.Hosting;

namespace Financial.Shared.Infrastructure.Hosting;

public sealed class ShutdownFlushHostedService<TRepository> : IHostedService where TRepository : class
{
    private readonly TRepository _repository;

    public ShutdownFlushHostedService(TRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) =>
        _repository is ISyncStatusProvider syncStatusProvider
            ? syncStatusProvider.FlushAsync()
            : Task.CompletedTask;
}
