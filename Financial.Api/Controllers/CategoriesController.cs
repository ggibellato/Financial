using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Lists the seeded categories. No field is editable via this API - see CategoryDTO for details.
/// </summary>
[ApiController]
[Route("categories")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
    }

    /// <summary>Lists all categories, active and inactive.</summary>
    /// <returns>200 OK with the full list of categories.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<CategoryDTO>> GetCategories()
    {
        var result = _categoryService.GetCategories();
        return Ok(result);
    }
}
