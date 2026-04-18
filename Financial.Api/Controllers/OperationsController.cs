using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Financial.Api.Controllers;

[ApiController]
[Route("operations")]
public sealed class OperationsController : ControllerBase
{
    private readonly IOperationService _operationService;

    public OperationsController(IOperationService operationService)
    {
        _operationService = operationService ?? throw new ArgumentNullException(nameof(operationService));
    }

    [HttpPost]
    [ProducesResponseType(typeof(AssetDetailsDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<AssetDetailsDTO> AddOperation([FromBody] OperationCreateDTO request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var asset = _operationService.AddOperation(request);
        if (asset is null)
        {
            return BadRequest();
        }

        return Ok(asset);
    }
}
