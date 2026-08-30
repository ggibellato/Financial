using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.Investment.Application.Services;

public sealed class BrokerService : IBrokerService
{
    private const string EntityType = "Broker";

    private readonly IInvestmentRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<BrokerService> _logger;

    public BrokerService(IInvestmentRepository repository, ITelemetryTracer tracer, ILogger<BrokerService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<BrokerDTO> GetBrokers()
    {
        using var span = StartSpan("GetBrokers");
        try
        {
            var investments = _repository.GetInvestments();
            var result = investments.ActiveBrokers.Select(b => ToDto(b, "Active"))
                .Concat(investments.HistoricBrokers.Select(b => ToDto(b, "Historic")))
                .ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetBrokers");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<BrokerDTO> CreateBrokerAsync(BrokerCreateDTO request)
    {
        using var span = StartSpan("CreateBroker");
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            var name = Required(request.Name, nameof(request.Name));
            var currency = Required(request.Currency, nameof(request.Currency));

            Broker? created = null;
            await _repository.ApplyAndSaveAsync(() =>
            {
                created = _repository.GetInvestments().CreateActiveBroker(name, currency);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "CreateBroker");
            return ToDto(created!, "Active");
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<BrokerDTO> UpdateBrokerAsync(string currentName, BrokerUpdateDTO request)
    {
        using var span = StartSpan("UpdateBroker");
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            Required(currentName, nameof(currentName));
            var newName = Required(request.Name, nameof(request.Name));
            var newCurrency = Required(request.Currency, nameof(request.Currency));

            Broker? updated = null;
            string? status = null;
            await _repository.ApplyAndSaveAsync(() =>
            {
                var investments = _repository.GetInvestments();
                updated = investments.RenameBroker(currentName, newName, newCurrency);
                status = investments.FindActiveBroker(updated.Name) is not null ? "Active" : "Historic";
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateBroker");
            return ToDto(updated!, status!);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task DeleteBrokerAsync(string name)
    {
        using var span = StartSpan("DeleteBroker");
        try
        {
            var required = Required(name, nameof(name));

            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.GetInvestments().DeleteBroker(required);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "DeleteBroker");
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
        return _tracer.StartServiceSpan("Investment", nameof(BrokerService), operationName, EntityType);
    }

    private static BrokerDTO ToDto(Broker broker, string status) => new()
    {
        Name = broker.Name,
        Currency = broker.Currency,
        Status = status,
        PortfolioCount = broker.Portfolios.Count
    };
}
