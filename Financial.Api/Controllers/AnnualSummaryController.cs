using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("annual-summary")]
public sealed class AnnualSummaryController : ControllerBase
{
    private readonly IAnnualSummaryService _annualSummaryService;

    public AnnualSummaryController(IAnnualSummaryService annualSummaryService)
    {
        _annualSummaryService = annualSummaryService ?? throw new ArgumentNullException(nameof(annualSummaryService));
    }

    [HttpGet("{year:int}/expense-categories")]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryAnnualTotalDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<CategoryAnnualTotalDTO>> GetExpenseCategoryTotals(int year)
    {
        return Ok(_annualSummaryService.GetCategoryTotalsForYear(year));
    }

    [HttpGet("{year:int}/investment-diffs")]
    [ProducesResponseType(typeof(InvestmentDiffsAnnualDTO), StatusCodes.Status200OK)]
    public ActionResult<InvestmentDiffsAnnualDTO> GetInvestmentDiffs(int year)
    {
        return Ok(_annualSummaryService.GetInvestmentDiffsForYear(year));
    }

    [HttpGet("{year:int}/income-summary")]
    [ProducesResponseType(typeof(IncomeAnnualSummaryDTO), StatusCodes.Status200OK)]
    public ActionResult<IncomeAnnualSummaryDTO> GetIncomeSummary(int year)
    {
        return Ok(_annualSummaryService.GetIncomeSummaryForYear(year));
    }

    [HttpGet("{year:int}/historic-summary-averages")]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryAnnualGroupValueDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<CategoryAnnualGroupValueDTO>> GetHistoricSummaryAverages(int year)
    {
        return Ok(_annualSummaryService.GetHistoricSummaryAverageFromYear(year));
    }

    [HttpGet("{year:int}/category-totals")]
    [ProducesResponseType(typeof(CategoryTotalsAnnualDTO), StatusCodes.Status200OK)]
    public ActionResult<CategoryTotalsAnnualDTO> GetCategoryTotals(int year)
    {
        return Ok(_annualSummaryService.GetCategoryTotalsAnnualForYear(year));
    }
}
