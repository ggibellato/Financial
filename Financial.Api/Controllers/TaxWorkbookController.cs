using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("tax-workbook")]
public sealed class TaxWorkbookController : ControllerBase
{
    private readonly ITaxWorkbookService _taxWorkbookService;

    public TaxWorkbookController(ITaxWorkbookService taxWorkbookService)
    {
        _taxWorkbookService = taxWorkbookService ?? throw new ArgumentNullException(nameof(taxWorkbookService));
    }

    [HttpGet("options")]
    [ProducesResponseType(typeof(IReadOnlyList<TaxWorkbookOptionDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<TaxWorkbookOptionDTO>> GetWorkbookOptions()
    {
        return Ok(_taxWorkbookService.GetWorkbookOptions());
    }

    [HttpGet]
    [ProducesResponseType(typeof(TaxWorkbookDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<TaxWorkbookDTO> GetWorkbook([FromQuery] string? jurisdiction, [FromQuery] string? taxYear)
    {
        return Ok(_taxWorkbookService.GetWorkbook(jurisdiction ?? string.Empty, taxYear ?? string.Empty));
    }
}
