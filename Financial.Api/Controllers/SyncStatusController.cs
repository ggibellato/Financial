using Financial.Api.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.Investment.Application.Interfaces;
using Financial.Shared.Abstractions.Sync;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Reports each bounded context's current background-persistence status.
/// </summary>
[ApiController]
[Route("sync-status")]
public sealed class SyncStatusController : ControllerBase
{
    private readonly ICashFlowRepository _cashFlowRepository;
    private readonly IInvestmentRepository _investmentRepository;

    public SyncStatusController(ICashFlowRepository cashFlowRepository, IInvestmentRepository investmentRepository)
    {
        _cashFlowRepository = cashFlowRepository ?? throw new ArgumentNullException(nameof(cashFlowRepository));
        _investmentRepository = investmentRepository ?? throw new ArgumentNullException(nameof(investmentRepository));
    }

    /// <summary>Returns both contexts' current sync status in one response.</summary>
    /// <returns>200 OK with the combined CashFlow/Investment status.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SyncStatusResponseDTO), StatusCodes.Status200OK)]
    public ActionResult<SyncStatusResponseDTO> GetSyncStatus()
    {
        return Ok(new SyncStatusResponseDTO
        {
            CashFlow = ToDto(ResolveStatus(_cashFlowRepository)),
            Investment = ToDto(ResolveStatus(_investmentRepository))
        });
    }

    private static SyncStatus ResolveStatus(object repository) =>
        repository is ISyncStatusProvider syncStatusProvider
            ? syncStatusProvider.GetStatus()
            : new SyncStatus(SyncState.Idle, null, null);

    private static SyncStatusDTO ToDto(SyncStatus status) => new()
    {
        State = status.State.ToString(),
        LastError = status.LastError,
        LastSuccessfulSaveUtc = status.LastSuccessfulSaveUtc
    };
}
