using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("allocation-breakdown")]
public sealed class AllocationBreakdownController : ControllerBase
{
    private readonly IAllocationBreakdownService _allocationBreakdownService;

    public AllocationBreakdownController(IAllocationBreakdownService allocationBreakdownService)
    {
        _allocationBreakdownService = allocationBreakdownService ?? throw new ArgumentNullException(nameof(allocationBreakdownService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(AllocationBreakdownDTO), StatusCodes.Status200OK)]
    public ActionResult<AllocationBreakdownDTO> GetAllocationBreakdown()
    {
        return Ok(_allocationBreakdownService.GetAllocationBreakdown());
    }
}
