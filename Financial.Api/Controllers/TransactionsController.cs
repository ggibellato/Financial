using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Financial.Api.Controllers;

[ApiController]
[Route("transactions")]
public sealed class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
    }

    [HttpPost]
    [ProducesResponseType(typeof(AssetDetailsDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<AssetDetailsDTO> AddTransaction([FromBody] TransactionCreateDTO request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var asset = _transactionService.AddTransaction(request);
        if (asset is null)
        {
            return BadRequest();
        }

        return Ok(asset);
    }

    [HttpPut]
    [ProducesResponseType(typeof(AssetDetailsDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<AssetDetailsDTO> UpdateTransaction([FromBody] TransactionUpdateDTO request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var asset = _transactionService.UpdateTransaction(request);
        if (asset is null)
        {
            return BadRequest();
        }

        return Ok(asset);
    }

    [HttpDelete]
    [ProducesResponseType(typeof(AssetDetailsDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<AssetDetailsDTO> DeleteTransaction([FromBody] TransactionDeleteDTO request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var asset = _transactionService.DeleteTransaction(request);
        if (asset is null)
        {
            return BadRequest();
        }

        return Ok(asset);
    }
}
