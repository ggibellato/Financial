using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface ICategoryService
{
    IReadOnlyList<CategoryDTO> GetCategories();

    Task<CategoryDTO> CreateCategoryAsync(CategoryCreateDTO request);

    Task<CategoryDTO> UpdateCategoryAsync(Guid id, CategoryUpdateDTO request);

    Task DeleteCategoryAsync(Guid id);
}
