using Financial.Api.DTOs;
using Financial.CashFlow.Application.Configuration;
using Financial.CashFlow.Application.Interfaces;
using Financial.Investment.Application.Configuration;
using Financial.Investment.Application.Interfaces;
using Financial.Shared.Abstractions.Sync;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Financial.Api.Controllers;

/// <summary>
/// Reports API liveness and each bounded context's storage state.
/// <para>
/// There is deliberately no endpoint returning repository paths. One existed, gated on
/// ASPNETCORE_ENVIRONMENT: a guard one environment variable away from being off, and the repo's own
/// docker-compose.yml runs Development, so starting the app that way published the real data-file
/// path on a port with no authentication in front of it. The only protection that does not depend
/// on a runtime setting being right is not to serve the paths at all. Where an install stores its
/// data is answerable from its own compose file and environment.
/// </para>
/// </summary>
[ApiController]
[Route("")]
public sealed class DiagnosticsController : ControllerBase
{
    private readonly InvestmentRepositorySettingsOptions _investmentSettings;
    private readonly CashFlowRepositorySettingsOptions _cashFlowSettings;
    private readonly IInvestmentRepository _investmentRepository;
    private readonly ICashFlowRepository _cashFlowRepository;

    public DiagnosticsController(
        IOptions<InvestmentRepositorySettingsOptions> investmentSettings,
        IOptions<CashFlowRepositorySettingsOptions> cashFlowSettings,
        IInvestmentRepository investmentRepository,
        ICashFlowRepository cashFlowRepository)
    {
        ArgumentNullException.ThrowIfNull(investmentSettings);
        ArgumentNullException.ThrowIfNull(cashFlowSettings);
        _investmentSettings = investmentSettings.Value;
        _cashFlowSettings = cashFlowSettings.Value;
        _investmentRepository = investmentRepository ?? throw new ArgumentNullException(nameof(investmentRepository));
        _cashFlowRepository = cashFlowRepository ?? throw new ArgumentNullException(nameof(cashFlowRepository));
    }

    /// <summary>Reports whether the API is up, and each context's storage provider and sync state.</summary>
    /// <returns>
    /// Always 200 OK when the API is reachable, including when a context's storage is failing - see
    /// <see cref="HealthStatusDTO"/> for why this does not answer 503.
    /// </returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(HealthStatusDTO), StatusCodes.Status200OK)]
    public ActionResult<HealthStatusDTO> GetHealth()
    {
        return Ok(new HealthStatusDTO
        {
            Status = "ok",
            Contexts = new HealthContextsDTO
            {
                Investment = BuildContextHealth(_investmentSettings.Provider, _investmentRepository),
                CashFlow = BuildContextHealth(_cashFlowSettings.Provider, _cashFlowRepository)
            }
        });
    }

    /// <summary>
    /// A repository whose storage writes straight through, rather than through the debounced path
    /// that tracks status, reports Idle instead of failing the whole health response.
    /// </summary>
    private static HealthContextDTO BuildContextHealth(string? provider, object repository)
    {
        var status = repository is ISyncStatusProvider syncStatusProvider
            ? syncStatusProvider.GetStatus()
            : new SyncStatus(SyncState.Idle, null, null);

        return new HealthContextDTO
        {
            Provider = provider,
            Sync = status.State.ToString(),
            LastError = status.LastError,
            LastSuccessfulSaveUtc = status.LastSuccessfulSaveUtc
        };
    }
}
