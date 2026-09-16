using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.Investment.Application.Services;

public sealed class UpcomingIncomeService : IUpcomingIncomeService
{
    private const string EntityType = "UpcomingIncome";
    private const string OperationName = "GetUpcomingIncome";

    private readonly IInvestmentRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<UpcomingIncomeService> _logger;

    public UpcomingIncomeService(
        IInvestmentRepository repository,
        ITelemetryTracer tracer,
        ILogger<UpcomingIncomeService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<UpcomingIncomeDTO> GetUpcomingIncome()
    {
        using var span = StartSpan(OperationName);
        try
        {
            var result = Project(_repository.GetInvestments());

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", OperationName);
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private static IReadOnlyList<UpcomingIncomeDTO> Project(Investments investments)
    {
        var entries = new List<UpcomingIncomeDTO>();
        foreach (var broker in investments.ActiveBrokers)
        {
            foreach (var asset in broker.Portfolios.SelectMany(portfolio => portfolio.Assets))
            {
                var entry = UpcomingIncomeBuilder.TryBuildEntry(asset, broker.Name);
                if (entry is not null)
                {
                    entries.Add(entry);
                }
            }
        }

        return entries
            .OrderBy(entry => entry.ProjectedNextDate)
            .ThenBy(entry => entry.AssetName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("Investment", nameof(UpcomingIncomeService), operationName, EntityType);
    }
}
