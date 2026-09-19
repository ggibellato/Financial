using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Validation;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Manages Broker records: registering, renaming, and retiring a broker, as distinct from the
/// read-only navigation tree served by <see cref="NavigationController"/>.
/// </summary>
[ApiController]
[Route("brokers")]
public sealed class BrokersController : ControllerBase
{
    private readonly IBrokerService _brokerService;

    public BrokersController(IBrokerService brokerService)
    {
        _brokerService = brokerService ?? throw new ArgumentNullException(nameof(brokerService));
    }

    /// <summary>Lists every broker, Active and Historic.</summary>
    /// <returns>200 OK with the list of brokers.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BrokerDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<BrokerDTO>> GetBrokers()
    {
        return Ok(_brokerService.GetBrokers());
    }

    /// <summary>Registers a new Active broker.</summary>
    /// <param name="request">The broker's name and currency.</param>
    /// <returns>200 OK with the created broker, 400 Bad Request if invalid, or 409 Conflict if the name is already in use.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(BrokerDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BrokerDTO>> CreateBroker([FromBody] BrokerCreateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var broker = await _brokerService.CreateBrokerAsync(request);
        return Ok(broker);
    }

    /// <summary>Renames and/or re-currencies an existing broker.</summary>
    /// <param name="name">The broker's current name.</param>
    /// <param name="request">The broker's new name and currency.</param>
    /// <param name="scope">Which record (Active or Historic) to resolve <paramref name="name"/> against; defaults to Active.</param>
    /// <returns>200 OK with the updated broker, 400 Bad Request if invalid, 404 Not Found if the broker doesn't exist, or 409 Conflict if the new name is already in use.</returns>
    [HttpPut("{name}")]
    [ProducesResponseType(typeof(BrokerDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BrokerDTO>> UpdateBroker(string name, [FromBody] BrokerUpdateDTO? request, [FromQuery] string? scope = null)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var broker = await _brokerService.UpdateBrokerAsync(name, request, InvestmentScopeParser.ParseOrDefault(scope));
        return Ok(broker);
    }

    /// <summary>Changes a broker's cost-basis method, regenerating every disposal record under it.</summary>
    /// <param name="name">The broker's name.</param>
    /// <param name="request">The broker's new cost-basis method.</param>
    /// <param name="scope">Which record (Active or Historic) to resolve <paramref name="name"/> against; defaults to Active.</param>
    /// <returns>200 OK with the updated broker, 400 Bad Request if invalid, or 404 Not Found if the broker doesn't exist.</returns>
    [HttpPut("{name}/cost-basis-method")]
    [ProducesResponseType(typeof(BrokerDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BrokerDTO>> SetCostBasisMethod(string name, [FromBody] SetCostBasisMethodRequestDTO? request, [FromQuery] string? scope = null)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var broker = await _brokerService.SetCostBasisMethodAsync(name, request.Method, InvestmentScopeParser.ParseOrDefault(scope));
        return Ok(broker);
    }

    /// <summary>Deletes an empty broker: an Active one archives to Historic, a Historic one is removed permanently.</summary>
    /// <param name="name">The broker's name.</param>
    /// <returns>204 No Content when deleted, 404 Not Found if the broker doesn't exist, or 409 Conflict if it still has portfolios.</returns>
    [HttpDelete("{name}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteBroker(string name)
    {
        await _brokerService.DeleteBrokerAsync(name);
        return NoContent();
    }
}
