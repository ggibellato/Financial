using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Provides year-level cash flow summaries: historic category averages, annual totals, and investment results.
/// </summary>
[ApiController]
[Route("annual-summary")]
public sealed class AnnualSummaryController : ControllerBase
{
    private readonly ICategorySummaryService _categorySummaryService;
    private readonly IInvestmentAnnualResultService _investmentAnnualResultService;
    private readonly IHistoricAverageService _historicAverageService;

    public AnnualSummaryController(
        ICategorySummaryService categorySummaryService,
        IInvestmentAnnualResultService investmentAnnualResultService,
        IHistoricAverageService historicAverageService)
    {
        _categorySummaryService = categorySummaryService ?? throw new ArgumentNullException(nameof(categorySummaryService));
        _investmentAnnualResultService = investmentAnnualResultService ?? throw new ArgumentNullException(nameof(investmentAnnualResultService));
        _historicAverageService = historicAverageService ?? throw new ArgumentNullException(nameof(historicAverageService));
    }

    /// <summary>Returns the historic average category values leading up to the given year.</summary>
    /// <param name="year">The year to compute averages up to.</param>
    /// <returns>200 OK with the historic averages.</returns>
    [HttpGet("{year:int}/historic-summary-averages")]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryAnnualGroupValueDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<CategoryAnnualGroupValueDTO>> GetHistoricSummaryAverages(int year)
    {
        return Ok(_historicAverageService.GetHistoricSummaryAverageFromYear(year));
    }

    /// <summary>Returns the category totals for a given year.</summary>
    /// <param name="year">The year.</param>
    /// <returns>200 OK with the annual category totals.</returns>
    [HttpGet("{year:int}/category-totals")]
    [ProducesResponseType(typeof(CategoryTotalsAnnualDTO), StatusCodes.Status200OK)]
    public ActionResult<CategoryTotalsAnnualDTO> GetCategoryTotals(int year)
    {
        return Ok(_categorySummaryService.GetCategoryTotalsAnnualForYear(year));
    }

    /// <summary>Returns the investment result (contributions vs. growth) for a given year.</summary>
    /// <param name="year">The year.</param>
    /// <returns>200 OK with the annual investment result.</returns>
    [HttpGet("{year:int}/investment-annual-result")]
    [ProducesResponseType(typeof(InvestmentAnnualResultDTO), StatusCodes.Status200OK)]
    public ActionResult<InvestmentAnnualResultDTO> GetInvestmentAnnualResult(int year)
    {
        return Ok(_investmentAnnualResultService.GetInvestmentAnnualResultForYear(year));
    }
}
