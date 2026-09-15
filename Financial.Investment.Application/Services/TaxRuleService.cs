using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;
using static Financial.Investment.Application.Validation.RequiredValueValidator;

namespace Financial.Investment.Application.Services;

public sealed class TaxRuleService : ITaxRuleService
{
    private const string EntityType = "TaxRule";

    private readonly IInvestmentRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<TaxRuleService> _logger;

    public TaxRuleService(IInvestmentRepository repository, ITelemetryTracer tracer, ILogger<TaxRuleService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<TaxRuleDTO> GetTaxRules()
    {
        using var span = StartSpan("GetTaxRules");
        try
        {
            var result = _repository.GetInvestments().TaxRules.Select(ToDto).ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetTaxRules");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<TaxRuleDTO> CreateTaxRuleAsync(TaxRuleCreateDTO request)
    {
        using var span = StartSpan("CreateTaxRule");
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            var label = Required(request.Label, nameof(request.Label));

            TaxRule? created = null;
            await _repository.ApplyAndSaveAsync(() =>
            {
                created = _repository.GetInvestments().CreateTaxRule(
                    request.Jurisdiction, request.EventCategory, label, request.Description,
                    request.EffectiveFrom, request.EffectiveTo);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "CreateTaxRule");
            return ToDto(created!);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<TaxRuleDTO> UpdateTaxRuleAsync(Guid id, TaxRuleUpdateDTO request)
    {
        using var span = StartSpan("UpdateTaxRule");
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            var label = Required(request.Label, nameof(request.Label));

            TaxRule? updated = null;
            await _repository.ApplyAndSaveAsync(() =>
            {
                updated = _repository.GetInvestments().UpdateTaxRule(
                    id, label, request.Description, request.EffectiveFrom, request.EffectiveTo);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateTaxRule");
            return ToDto(updated!);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task DeleteTaxRuleAsync(Guid id)
    {
        using var span = StartSpan("DeleteTaxRule");
        try
        {
            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.GetInvestments().DeleteTaxRule(id);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "DeleteTaxRule");
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
        return _tracer.StartServiceSpan("Investment", nameof(TaxRuleService), operationName, EntityType);
    }

    private static TaxRuleDTO ToDto(TaxRule rule) => new()
    {
        Id = rule.Id,
        Jurisdiction = rule.Jurisdiction,
        EventCategory = rule.EventCategory,
        Label = rule.Label,
        Description = rule.Description,
        EffectiveFrom = rule.EffectiveFrom,
        EffectiveTo = rule.EffectiveTo
    };
}
