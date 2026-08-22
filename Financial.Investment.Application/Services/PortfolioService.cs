using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.Investment.Application.Services;

public sealed class PortfolioService : IPortfolioService
{
    private const string EntityType = "Portfolio";

    private readonly IInvestmentRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<PortfolioService> _logger;

    public PortfolioService(IInvestmentRepository repository, ITelemetryTracer tracer, ILogger<PortfolioService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task DeleteEmptyPortfolioAsync(string brokerName, string portfolioName, InvestmentScope scope)
    {
        using var span = StartSpan("DeleteEmptyPortfolio");
        try
        {
            var broker = _repository.GetBrokerList(scope).FirstOrDefault(candidate => candidate.Name == Required(brokerName, nameof(brokerName)))
                ?? throw new KeyNotFoundException($"Broker \"{brokerName}\" was not found.");

            var name = Required(portfolioName, nameof(portfolioName));

            // Inside the save, like every other mutation: the domain call validates and removes
            // together, so a refusal writes nothing.
            await _repository.ApplyAndSaveAsync(() => broker.RemoveEmptyPortfolio(name)).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "DeleteEmptyPortfolio");
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private static string Required(string? value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"{parameterName} is required.", parameterName)
            : value;

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("Investment", nameof(PortfolioService), operationName, EntityType);
    }
}
