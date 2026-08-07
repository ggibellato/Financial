using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Manages transfers of money between the user's own banks.
/// </summary>
[ApiController]
[Route("transfers")]
public sealed class TransfersController : ControllerBase
{
    private readonly ITransferService _transferService;
    private readonly IBankService _bankService;

    public TransfersController(ITransferService transferService, IBankService bankService)
    {
        _transferService = transferService ?? throw new ArgumentNullException(nameof(transferService));
        _bankService = bankService ?? throw new ArgumentNullException(nameof(bankService));
    }

    /// <summary>Records a new transfer.</summary>
    /// <param name="request">The transfer to create.</param>
    /// <returns>200 OK with the created transfer, 400 Bad Request if the request is invalid or missing.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(TransferDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TransferDTO>> AddTransfer([FromBody] TransferCreateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        try
        {
            var transfer = await _transferService.AddTransferAsync(request);
            return Ok(transfer);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>Updates an existing transfer.</summary>
    /// <param name="id">The transfer's identifier.</param>
    /// <param name="request">The new transfer fields.</param>
    /// <returns>200 OK with the updated transfer, 400 Bad Request if the request is invalid, or 404 Not Found if the transfer doesn't exist.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TransferDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransferDTO>> UpdateTransfer(Guid id, [FromBody] TransferUpdateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        try
        {
            var transfer = await _transferService.UpdateTransferAsync(id, request);
            return Ok(transfer);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    /// <summary>Deletes a transfer.</summary>
    /// <param name="id">The transfer's identifier.</param>
    /// <returns>200 OK if deleted, or 404 Not Found if the transfer doesn't exist.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTransfer(Guid id)
    {
        try
        {
            await _transferService.DeleteTransferAsync(id);
            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    /// <summary>Lists transfers recorded in a given month.</summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <returns>200 OK with the matching transfers.</returns>
    [HttpGet("month/{year:int}/{month:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<TransferDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<TransferDTO>> GetTransfersByMonth(int year, int month)
    {
        var result = _transferService.GetTransfersByMonth(year, month);
        return Ok(result);
    }

    /// <summary>Lists all transfers touching a given bank, either as source or destination.</summary>
    /// <param name="name">The bank's name.</param>
    /// <returns>200 OK with the matching transfers.</returns>
    [HttpGet("bank/{name}")]
    [ProducesResponseType(typeof(IReadOnlyList<TransferDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<TransferDTO>> GetTransfersByBank(string name)
    {
        var bank = _bankService.GetBanks().FirstOrDefault(b => b.Name == name);
        if (bank is null)
        {
            return Ok(Array.Empty<TransferDTO>());
        }

        var result = _transferService.GetTransfersByBank(bank.Id);
        return Ok(result);
    }
}
