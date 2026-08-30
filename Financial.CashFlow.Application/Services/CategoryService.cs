using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Validation;
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

    public async Task<CategoryDTO> CreateCategoryAsync(CategoryCreateDTO request)
    {
        using var span = StartSpan("CreateCategory");
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Category name is required.", nameof(request));
            }

            EnsureNameIsUnique(request.Name, excludingId: null);

            var category = Category.Create(request.Name, request.IsInvestment, request.IsTithe, request.Active);

            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.AddCategory(category);
                return true;
            }).ConfigureAwait(false);

            span.SetAttribute(TelemetryAttributeKeys.EntityId, category.Id.ToString());
            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "CreateCategory");
            return ToDto(category);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<CategoryDTO> UpdateCategoryAsync(Guid id, CategoryUpdateDTO request)
    {
        using var span = StartSpan("UpdateCategory");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Category name is required.", nameof(request));
            }

            if (!EntityIdResolver.TryResolve(id, _repository.GetCategories(), c => c.Id, out var category))
            {
                throw new KeyNotFoundException($"Category '{id}' was not found.");
            }

            EnsureNameIsUnique(request.Name, excludingId: id);

            await _repository.ApplyAndSaveAsync(() =>
            {
                category!.Update(request.Name, request.Active, request.IsInvestment, request.IsTithe);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateCategory");
            return ToDto(category);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task DeleteCategoryAsync(Guid id)
    {
        using var span = StartSpan("DeleteCategory");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, id.ToString());
        try
        {
            if (!EntityIdResolver.TryResolve(id, _repository.GetCategories(), c => c.Id, out _))
            {
                throw new KeyNotFoundException($"Category '{id}' was not found.");
            }

            EnsureNotReferenced(id);

            await _repository.ApplyAndSaveAsync(() =>
            {
                _repository.DeleteCategory(id);
                return true;
            }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "DeleteCategory");
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private void EnsureNameIsUnique(string name, Guid? excludingId)
    {
        var collision = _repository.GetCategories().FirstOrDefault(c => c.Name == name && c.Id != excludingId);
        if (collision is not null)
        {
            throw new DuplicateNameException($"A category named \"{name}\" already exists.");
        }
    }

    private void EnsureNotReferenced(Guid categoryId)
    {
        if (IsReferenced(categoryId))
        {
            throw new EntityInUseException("Cannot delete a category that is still used by a transaction.");
        }
    }

    /// <summary>Also drives <see cref="CategoryDTO.HasReferences"/>, so the client can disable Delete
    /// before attempting it rather than only learning about the guard from a failed request.</summary>
    private bool IsReferenced(Guid categoryId) =>
        _repository.GetExpenses().Any(e => e.Category.Id == categoryId);

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(CategoryService), operationName, EntityType);
    }

    private CategoryDTO ToDto(Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Active = category.Active,
        IsInvestment = category.IsInvestment,
        IsTithe = category.IsTithe,
        HasReferences = IsReferenced(category.Id)
    };
}
