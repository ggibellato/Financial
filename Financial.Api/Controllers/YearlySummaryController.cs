using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("yearly-summary")]
public sealed class YearlySummaryController : ControllerBase
{
    private readonly IYearlySummaryService _yearlySummaryService;

    public YearlySummaryController(IYearlySummaryService yearlySummaryService)
    {
        _yearlySummaryService = yearlySummaryService ?? throw new ArgumentNullException(nameof(yearlySummaryService));
    }

    [HttpGet("{year:int}/expense-categories")]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryYearlyTotalDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<CategoryYearlyTotalDTO>> GetExpenseCategoryTotals(int year)
    {
        return Ok(_yearlySummaryService.GetCategoryTotalsForYear(year));
    }

    [HttpGet("{year:int}/investment-diffs")]
    [ProducesResponseType(typeof(InvestmentDiffsYearlyDTO), StatusCodes.Status200OK)]
    public ActionResult<InvestmentDiffsYearlyDTO> GetInvestmentDiffs(int year)
    {
        return Ok(_yearlySummaryService.GetInvestmentDiffsForYear(year));
    }
}
