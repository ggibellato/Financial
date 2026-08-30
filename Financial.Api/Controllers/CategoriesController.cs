using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Manages transaction categories.
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

    /// <summary>Creates a new category.</summary>
    /// <param name="request">The category's name and Active/IsInvestment/IsTithe flags.</param>
    /// <returns>200 OK with the created category, or 400 Bad Request if the request is invalid.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CategoryDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CategoryDTO>> CreateCategory([FromBody] CategoryCreateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var category = await _categoryService.CreateCategoryAsync(request);
        return Ok(category);
    }

    /// <summary>Updates a category's name and Active/IsInvestment/IsTithe flags.</summary>
    /// <param name="id">The category's identifier.</param>
    /// <param name="request">The new field values.</param>
    /// <returns>200 OK with the updated category, 400 Bad Request if the request is invalid, or 404 Not Found if no such category exists.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CategoryDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDTO>> UpdateCategory(Guid id, [FromBody] CategoryUpdateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var category = await _categoryService.UpdateCategoryAsync(id, request);
        return Ok(category);
    }

    /// <summary>Deletes a category, when no transaction still references it.</summary>
    /// <param name="id">The category's identifier.</param>
    /// <returns>200 OK if deleted, 404 Not Found if no such category exists, or 409 Conflict if it is still referenced.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        await _categoryService.DeleteCategoryAsync(id);
        return Ok();
    }
}
