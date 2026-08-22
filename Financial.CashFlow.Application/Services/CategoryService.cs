using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class CategoryService : ICategoryService
{
    private const string EntityType = "Category";

    private readonly ICashFlowRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(ICashFlowRepository repository, ITelemetryTracer tracer, ILogger<CategoryService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<CategoryDTO> GetCategories()
    {
        using var span = StartSpan("GetCategories");
        try
        {
            var result = _repository.GetCategories().Select(ToDto).ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetCategories");
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
        return _tracer.StartServiceSpan("CashFlow", nameof(CategoryService), operationName, EntityType);
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
