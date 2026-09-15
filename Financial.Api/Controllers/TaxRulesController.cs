using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("tax-rules")]
public sealed class TaxRulesController : ControllerBase
{
    private readonly ITaxRuleService _taxRuleService;

    public TaxRulesController(ITaxRuleService taxRuleService)
    {
        _taxRuleService = taxRuleService ?? throw new ArgumentNullException(nameof(taxRuleService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TaxRuleDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<TaxRuleDTO>> GetTaxRules()
    {
        return Ok(_taxRuleService.GetTaxRules());
    }

    [HttpPost]
    [ProducesResponseType(typeof(TaxRuleDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TaxRuleDTO>> CreateTaxRule([FromBody] TaxRuleCreateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var rule = await _taxRuleService.CreateTaxRuleAsync(request);
        return Ok(rule);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TaxRuleDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TaxRuleDTO>> UpdateTaxRule(Guid id, [FromBody] TaxRuleUpdateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var rule = await _taxRuleService.UpdateTaxRuleAsync(id, request);
        return Ok(rule);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTaxRule(Guid id)
    {
        await _taxRuleService.DeleteTaxRuleAsync(id);
        return NoContent();
    }
}
