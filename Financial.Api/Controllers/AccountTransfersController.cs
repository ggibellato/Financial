using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("accounts")]
public sealed class AccountTransfersController : ControllerBase
{
    private readonly IAccountTransferService _accountTransferService;

    public AccountTransfersController(IAccountTransferService accountTransferService)
    {
        _accountTransferService = accountTransferService ?? throw new ArgumentNullException(nameof(accountTransferService));
    }

    [HttpPost("transfers")]
    [ProducesResponseType(typeof(AccountTransferDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AccountTransferDTO>> AddTransfer([FromBody] AccountTransferCreateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        return Ok(await _accountTransferService.AddAsync(request));
    }

    [HttpPut("transfers/{id:guid}")]
    [ProducesResponseType(typeof(AccountTransferDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AccountTransferDTO>> UpdateTransfer(Guid id, [FromBody] AccountTransferUpdateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        return Ok(await _accountTransferService.UpdateAsync(id, request));
    }

    [HttpDelete("transfers/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteTransfer(Guid id)
    {
        await _accountTransferService.DeleteAsync(id);
        return Ok();
    }

    [HttpGet("{id:guid}/transfer-destinations")]
    [ProducesResponseType(typeof(IReadOnlyList<TransferAccountOptionDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<IReadOnlyList<TransferAccountOptionDTO>> GetTransferDestinations(Guid id)
    {
        return Ok(_accountTransferService.GetDestinations(id));
    }

    [HttpGet("{id:guid}/transfer-sources")]
    [ProducesResponseType(typeof(IReadOnlyList<TransferAccountOptionDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<IReadOnlyList<TransferAccountOptionDTO>> GetTransferSources(Guid id)
    {
        return Ok(_accountTransferService.GetSources(id));
    }
}
