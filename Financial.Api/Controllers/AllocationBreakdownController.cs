using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("allocation-breakdown")]
public sealed class AllocationBreakdownController : ApiControllerBase
{
    private readonly IAllocationBreakdownService _allocationBreakdownService;

    public AllocationBreakdownController(IAllocationBreakdownService allocationBreakdownService)
    {
        _allocationBreakdownService = allocationBreakdownService ?? throw new ArgumentNullException(nameof(allocationBreakdownService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(AllocationBreakdownDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AllocationBreakdownDTO>> GetAllocationBreakdown(
        [FromQuery] string? displayCurrency = null,
        [FromQuery] string? brokerCurrency = null)
    {
        if (!TryParseOptionalCurrency(displayCurrency, out var parsedDisplayCurrency) ||
            !TryParseOptionalCurrency(brokerCurrency, out var parsedBrokerCurrency))
        {
            return BadRequest();
        }

        var dto = await _allocationBreakdownService
            .GetAllocationBreakdownAsync(parsedDisplayCurrency, parsedBrokerCurrency)
            .ConfigureAwait(false);
        return Ok(dto);
    }
}
