using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Read-only access to the seeded reserve buckets.
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
}
