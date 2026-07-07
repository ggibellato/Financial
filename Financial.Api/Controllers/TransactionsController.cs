using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("transactions")]
public sealed class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly ITransactionQueryService _transactionQueryService;

    public TransactionsController(ITransactionService transactionService, ITransactionQueryService transactionQueryService)
    {
        _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
        _transactionQueryService = transactionQueryService ?? throw new ArgumentNullException(nameof(transactionQueryService));
    }

    [HttpPost]
    [ProducesResponseType(typeof(AssetDetailsDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssetDetailsDTO>> AddTransaction([FromBody] TransactionCreateDTO request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var asset = await _transactionService.AddTransactionAsync(request);
        if (asset is null)
        {
            return BadRequest();
        }

        return Ok(asset);
    }

    [HttpPut]
    [ProducesResponseType(typeof(AssetDetailsDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssetDetailsDTO>> UpdateTransaction([FromBody] TransactionUpdateDTO request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var asset = await _transactionService.UpdateTransactionAsync(request);
        if (asset is null)
        {
            return BadRequest();
        }

        return Ok(asset);
    }

    [HttpDelete]
    [ProducesResponseType(typeof(AssetDetailsDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssetDetailsDTO>> DeleteTransaction([FromBody] TransactionDeleteDTO request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var asset = await _transactionService.DeleteTransactionAsync(request);
        if (asset is null)
        {
            return BadRequest();
        }

        return Ok(asset);
    }

    [HttpGet("broker/{brokerName}")]
    [ProducesResponseType(typeof(IReadOnlyList<TransactionSummaryItemDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<IReadOnlyList<TransactionSummaryItemDTO>> GetTransactionsByBroker(string brokerName)
    {
        if (string.IsNullOrWhiteSpace(brokerName))
            return BadRequest();

        var result = _transactionQueryService.GetTransactionsByBroker(brokerName);
        return Ok(result);
    }

    [HttpGet("portfolio/{brokerName}/{portfolioName}")]
    [ProducesResponseType(typeof(IReadOnlyList<TransactionSummaryItemDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<IReadOnlyList<TransactionSummaryItemDTO>> GetTransactionsByPortfolio(
        string brokerName,
        string portfolioName)
    {
        if (string.IsNullOrWhiteSpace(brokerName) || string.IsNullOrWhiteSpace(portfolioName))
            return BadRequest();

        var result = _transactionQueryService.GetTransactionsByPortfolio(brokerName, portfolioName);
        return Ok(result);
    }
}
