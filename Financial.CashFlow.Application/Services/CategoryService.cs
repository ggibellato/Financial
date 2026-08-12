using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Application.Services;

public sealed class CategoryService : ICategoryService
{
    private readonly ICashFlowRepository _repository;

    public CategoryService(ICashFlowRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public IReadOnlyList<CategoryDTO> GetCategories() =>
        _repository.GetCategories().Select(ToDto).ToList();

    private static CategoryDTO ToDto(Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Active = category.Active,
        IsInvestment = category.IsInvestment,
        IsTithe = category.IsTithe
    };
}
