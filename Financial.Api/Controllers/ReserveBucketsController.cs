using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Manages reserve buckets.
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

    /// <summary>Creates a new reserve bucket.</summary>
    /// <param name="request">The bucket's name, split percentage, and active flag.</param>
    /// <returns>200 OK with the created bucket (whose Warning is non-null when active buckets don't sum to ~100%), or 400 Bad Request if the request is invalid.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ReserveBucketDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReserveBucketDTO>> CreateReserveBucket([FromBody] ReserveBucketCreateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var bucket = await _reserveBucketService.CreateReserveBucketAsync(request);
        return Ok(bucket);
    }

    /// <summary>Updates a reserve bucket's name, split percentage, and active flag. "Deleting" a
    /// bucket is a call to this endpoint with isActive set to false - no hard delete exists, since
    /// existing reserve movements hold a permanent reference to their bucket.</summary>
    /// <param name="id">The bucket's identifier.</param>
    /// <param name="request">The new field values.</param>
    /// <returns>200 OK with the updated bucket (whose Warning is non-null when active buckets don't sum to ~100%), 400 Bad Request if the request is invalid, or 404 Not Found if no such bucket exists.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ReserveBucketDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReserveBucketDTO>> UpdateReserveBucket(Guid id, [FromBody] ReserveBucketUpdateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var bucket = await _reserveBucketService.UpdateReserveBucketAsync(id, request);
        return Ok(bucket);
    }
}
