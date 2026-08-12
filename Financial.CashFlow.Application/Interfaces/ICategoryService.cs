using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface ICategoryService
{
    IReadOnlyList<CategoryDTO> GetCategories();
}
