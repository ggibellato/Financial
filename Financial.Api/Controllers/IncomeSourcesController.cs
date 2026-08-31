using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Manages income sources.
/// </summary>
[ApiController]
[Route("income-sources")]
public sealed class IncomeSourcesController : ControllerBase
{
    private readonly IIncomeSourceService _incomeSourceService;

    public IncomeSourcesController(IIncomeSourceService incomeSourceService)
    {
        _incomeSourceService = incomeSourceService ?? throw new ArgumentNullException(nameof(incomeSourceService));
    }

    /// <summary>Lists all income sources.</summary>
    /// <returns>200 OK with the full, unfiltered list of income sources.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<IncomeSourceDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<IncomeSourceDTO>> GetIncomeSources()
    {
        var result = _incomeSourceService.GetIncomeSources();
        return Ok(result);
    }

    /// <summary>Creates a new income source.</summary>
    /// <param name="request">The income source's name, group, active flag, and auto-split-to-reserve setting.</param>
    /// <returns>200 OK with the created income source, or 400 Bad Request if the request is invalid.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(IncomeSourceDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IncomeSourceDTO>> CreateIncomeSource([FromBody] IncomeSourceCreateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var incomeSource = await _incomeSourceService.CreateIncomeSourceAsync(request);
        return Ok(incomeSource);
    }

    /// <summary>Updates an income source's name, group, active flag, and auto-split-to-reserve setting.</summary>
    /// <param name="id">The income source's identifier.</param>
    /// <param name="request">The new field values.</param>
    /// <returns>200 OK with the updated income source, 400 Bad Request if the request is invalid, or 404 Not Found if no such income source exists.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(IncomeSourceDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncomeSourceDTO>> UpdateIncomeSource(Guid id, [FromBody] IncomeSourceUpdateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var incomeSource = await _incomeSourceService.UpdateIncomeSourceAsync(id, request);
        return Ok(incomeSource);
    }

    /// <summary>Deletes an income source, when no income entry still references it.</summary>
    /// <param name="id">The income source's identifier.</param>
    /// <returns>200 OK if deleted, 404 Not Found if no such income source exists, or 409 Conflict if it is still referenced.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteIncomeSource(Guid id)
    {
        await _incomeSourceService.DeleteIncomeSourceAsync(id);
        return Ok();
    }
}
