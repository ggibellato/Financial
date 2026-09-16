using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("data-quality-report")]
public sealed class DataQualityReportController : ControllerBase
{
    private readonly IDataQualityReportService _dataQualityReportService;

    public DataQualityReportController(IDataQualityReportService dataQualityReportService)
    {
        _dataQualityReportService = dataQualityReportService ?? throw new ArgumentNullException(nameof(dataQualityReportService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(DataQualityReportDTO), StatusCodes.Status200OK)]
    public ActionResult<DataQualityReportDTO> GetDataQualityReport()
    {
        return Ok(_dataQualityReportService.GenerateReport());
    }
}
