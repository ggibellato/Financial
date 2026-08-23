using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class InvestmentAccountService : IInvestmentAccountService
{
    private const string EntityType = "InvestmentAccount";

    private readonly ICashFlowRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<InvestmentAccountService> _logger;

    public InvestmentAccountService(ICashFlowRepository repository, ITelemetryTracer tracer, ILogger<InvestmentAccountService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<InvestmentAccountDTO> GetInvestmentAccounts()
    {
        using var span = StartSpan("GetInvestmentAccounts");
        try
        {
            var result = _repository.GetInvestmentAccounts().Select(ToDto).ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetInvestmentAccounts");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(InvestmentAccountService), operationName, EntityType);
    }

    private static InvestmentAccountDTO ToDto(InvestmentAccount account) => new()
    {
        Id = account.Id,
        Name = account.Name,
        IsActive = account.IsActive,
        IsLiability = account.IsLiability
    };
}
