using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
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

    public IReadOnlyList<PortfolioDTO> GetPortfolios()
    {
        using var span = StartSpan("GetPortfolios");
        try
        {
            var investments = _repository.GetInvestments();
            var result = investments.ActiveBrokers.SelectMany(b => b.Portfolios.Select(p => ToDto(p, b, "Active")))
                .Concat(investments.HistoricBrokers.SelectMany(b => b.Portfolios.Select(p => ToDto(p, b, "Historic"))))
                .ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetPortfolios");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<PortfolioDTO> CreatePortfolioAsync(PortfolioCreateDTO request)
    {
        using var span = StartSpan("CreatePortfolio");
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            var brokerName = Required(request.BrokerName, nameof(request.BrokerName));
            var name = Required(request.Name, nameof(request.Name));

            Broker? broker = null;
            Portfolio? created = null;
            await _repository.ApplyAndSaveAsync(() =>
            {
                broker = _repository.GetBrokerList(InvestmentScope.Active).FirstOrDefault(candidate => candidate.Name == brokerName)
                    ?? throw new KeyNotFoundException($"Active broker \"{brokerName}\" was not found.");
                created = broker.CreatePortfolio(name);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "CreatePortfolio");
            return ToDto(created!, broker!, "Active");
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<PortfolioDTO> UpdatePortfolioAsync(string brokerName, string currentName, PortfolioUpdateDTO request)
    {
        using var span = StartSpan("UpdatePortfolio");
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            var requiredBrokerName = Required(brokerName, nameof(brokerName));
            Required(currentName, nameof(currentName));
            var newName = Required(request.Name, nameof(request.Name));

            Broker? broker = null;
            Portfolio? updated = null;
            await _repository.ApplyAndSaveAsync(() =>
            {
                var investments = _repository.GetInvestments();
                broker = investments.FindActiveBroker(requiredBrokerName) ?? investments.FindHistoricBroker(requiredBrokerName)
                    ?? throw new KeyNotFoundException($"Broker \"{requiredBrokerName}\" was not found.");
                updated = broker.RenamePortfolio(currentName, newName);
                return true;
            }).ConfigureAwait(false);

            var status = _repository.GetBrokerList(InvestmentScope.Active).Any(b => b.Name == broker!.Name) ? "Active" : "Historic";

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdatePortfolio");
            return ToDto(updated!, broker!, status);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
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

    private static PortfolioDTO ToDto(Portfolio portfolio, Broker broker, string brokerStatus) => new()
    {
        Name = portfolio.Name,
        BrokerName = broker.Name,
        BrokerStatus = brokerStatus,
        AssetCount = portfolio.Assets.Count
    };
}
