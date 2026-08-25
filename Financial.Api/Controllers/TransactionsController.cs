using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Validation;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Manages investment transactions (buys, sells, and related adjustments) and their query views.
/// </summary>
[ApiController]
[Route("transactions")]
public sealed class TransactionsController : ApiControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly ITransactionQueryService _transactionQueryService;

    public TransactionsController(ITransactionService transactionService, ITransactionQueryService transactionQueryService)
    {
        _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
        _transactionQueryService = transactionQueryService ?? throw new ArgumentNullException(nameof(transactionQueryService));
    }

    /// <summary>Records a new investment transaction.</summary>
    /// <param name="request">The transaction to create.</param>
    /// <returns>200 OK with the updated asset details, or 400 Bad Request if the request is invalid.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(AssetDetailsDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssetDetailsDTO>> AddTransaction([FromBody] TransactionCreateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var asset = await _transactionService.AddTransactionAsync(request);
        return OkOrBadRequest(asset);
    }

    /// <summary>Updates an existing investment transaction.</summary>
    /// <param name="request">The transaction fields to update.</param>
    /// <returns>200 OK with the updated asset details, or 400 Bad Request if the request is invalid.</returns>
    [HttpPut]
    [ProducesResponseType(typeof(AssetDetailsDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssetDetailsDTO>> UpdateTransaction([FromBody] TransactionUpdateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var asset = await _transactionService.UpdateTransactionAsync(request);
        return OkOrBadRequest(asset);
    }

    /// <summary>Deletes an investment transaction.</summary>
    /// <param name="request">Identifies the transaction to delete.</param>
    /// <returns>200 OK with the updated asset details, or 400 Bad Request if the request is invalid.</returns>
    [HttpDelete]
    [ProducesResponseType(typeof(AssetDetailsDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssetDetailsDTO>> DeleteTransaction([FromBody] TransactionDeleteDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var asset = await _transactionService.DeleteTransactionAsync(request);
        return OkOrBadRequest(asset);
    }

    /// <summary>Lists transactions for all portfolios under a broker.</summary>
    /// <param name="brokerName">The broker's name.</param>
    /// <param name="scope">Optional investment scope filter (e.g. "all", "active-only").</param>
    /// <returns>200 OK with the matching transactions, or 400 Bad Request if <paramref name="brokerName"/> is missing.</returns>
    [HttpGet("broker/{brokerName}")]
    [ProducesResponseType(typeof(IReadOnlyList<TransactionSummaryItemDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<IReadOnlyList<TransactionSummaryItemDTO>> GetTransactionsByBroker(string brokerName, [FromQuery] string? scope)
    {
        if (string.IsNullOrWhiteSpace(brokerName))
            return BadRequest();

        var result = _transactionQueryService.GetTransactionsByBroker(brokerName, InvestmentScopeParser.ParseOrDefault(scope));
        return Ok(result);
    }

    /// <summary>Lists transactions for a specific portfolio.</summary>
    /// <param name="brokerName">The broker's name.</param>
    /// <param name="portfolioName">The portfolio's name.</param>
    /// <param name="scope">Optional investment scope filter (e.g. "all", "active-only").</param>
    /// <returns>200 OK with the matching transactions, or 400 Bad Request if the broker or portfolio name is missing.</returns>
    [HttpGet("portfolio/{brokerName}/{portfolioName}")]
    [ProducesResponseType(typeof(IReadOnlyList<TransactionSummaryItemDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<IReadOnlyList<TransactionSummaryItemDTO>> GetTransactionsByPortfolio(
        string brokerName,
        string portfolioName,
        [FromQuery] string? scope)
    {
        if (string.IsNullOrWhiteSpace(brokerName) || string.IsNullOrWhiteSpace(portfolioName))
            return BadRequest();

        var result = _transactionQueryService.GetTransactionsByPortfolio(brokerName, portfolioName, InvestmentScopeParser.ParseOrDefault(scope));
        return Ok(result);
    }
}
