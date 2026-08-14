using Financial.Investment.Application.Interfaces;
using Financial.Shared.Infrastructure.Sync;
using Microsoft.Extensions.Hosting;

namespace Financial.Investment.Infrastructure.Hosting;

public sealed class InvestmentShutdownFlushHostedService : IHostedService
{
    private readonly IInvestmentRepository _repository;

    public InvestmentShutdownFlushHostedService(IInvestmentRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) =>
        _repository is ISyncStatusProvider syncStatusProvider
            ? syncStatusProvider.FlushAsync()
            : Task.CompletedTask;
}
