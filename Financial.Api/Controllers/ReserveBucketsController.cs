using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Reads reserve buckets.
/// </summary>
[ApiController]
[Route("reserve-buckets")]
public sealed class ReserveBucketsController : ControllerBase
{
    private readonly IReserveBucketService _reserveBucketService;

    public ReserveBucketsController(IReserveBucketService reserveBucketService)
    {
        _reserveBucketService = reserveBucketService ?? throw new ArgumentNullException(nameof(reserveBucketService));
    }

    /// <summary>Lists all reserve buckets.</summary>
    /// <returns>200 OK with the full, unfiltered list of reserve buckets.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ReserveBucketDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<ReserveBucketDTO>> GetReserveBuckets()
    {
        var result = _reserveBucketService.GetReserveBuckets();
        return Ok(result);
    }

    /// <summary>Reports whether the active buckets' split percentages sum to 100%.</summary>
    /// <returns>200 OK with the active total and a warning that is null when the total is within 0.01 of 100 or there are no buckets.</returns>
    [HttpGet("split-status")]
    [ProducesResponseType(typeof(ReserveSplitStatusDTO), StatusCodes.Status200OK)]
    public ActionResult<ReserveSplitStatusDTO> GetSplitStatus()
    {
        return Ok(_reserveBucketService.GetSplitStatus());
    }
}
