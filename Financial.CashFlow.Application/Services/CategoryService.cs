using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities;
using Financial.Shared.Abstractions;

namespace Financial.CashFlow.Application.Services;

public sealed class CategoryService : ICategoryService
{
    private const string EntityType = "Category";

    private readonly ICashFlowRepository _repository;
    private readonly ITelemetryTracer _tracer;

    public CategoryService(ICashFlowRepository repository, ITelemetryTracer tracer)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
    }

    public IReadOnlyList<CategoryDTO> GetCategories()
    {
        using var span = StartSpan("GetCategories");
        try
        {
            var result = _repository.GetCategories().Select(ToDto).ToList();

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            return result;
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
            throw;
        }
    }

    private ITelemetrySpan StartSpan(string operationName)
    {
        var span = _tracer.StartSpan($"CashFlow.CategoryService.{operationName}");
        span.SetAttribute(TelemetryAttributeKeys.BoundedContext, "CashFlow");
        span.SetAttribute(TelemetryAttributeKeys.EntityType, EntityType);
        span.SetAttribute(TelemetryAttributeKeys.OperationName, operationName);
        return span;
    }

    private static CategoryDTO ToDto(Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Active = category.Active,
        IsInvestment = category.IsInvestment,
        IsTithe = category.IsTithe
    };
}
