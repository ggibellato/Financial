using Financial.Api.DTOs;
using Financial.CashFlow.Application.Configuration;
using Financial.CashFlow.Application.Interfaces;
using Financial.Investment.Application.Configuration;
using Financial.Investment.Application.Interfaces;
using Financial.Shared.Infrastructure.Sync;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Financial.Api.Controllers;

/// <summary>
/// Diagnostic endpoints for checking API liveness and the active data repository configuration.
/// Both cover each bounded context; reporting only Investment made a CashFlow misconfiguration
/// invisible in the one place you would look for it.
/// </summary>
[ApiController]
[Route("")]
public sealed class DiagnosticsController : ControllerBase
{
    private readonly InvestmentRepositorySettingsOptions _investmentSettings;
    private readonly CashFlowRepositorySettingsOptions _cashFlowSettings;
    private readonly IInvestmentRepository _investmentRepository;
    private readonly ICashFlowRepository _cashFlowRepository;
    private readonly IHostEnvironment _environment;

    public DiagnosticsController(
        IOptions<InvestmentRepositorySettingsOptions> investmentSettings,
        IOptions<CashFlowRepositorySettingsOptions> cashFlowSettings,
        IInvestmentRepository investmentRepository,
        ICashFlowRepository cashFlowRepository,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(investmentSettings);
        ArgumentNullException.ThrowIfNull(cashFlowSettings);
        _investmentSettings = investmentSettings.Value;
        _cashFlowSettings = cashFlowSettings.Value;
        _investmentRepository = investmentRepository ?? throw new ArgumentNullException(nameof(investmentRepository));
        _cashFlowRepository = cashFlowRepository ?? throw new ArgumentNullException(nameof(cashFlowRepository));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
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

    /// <summary>Returns both contexts' active repository configuration (provider and file paths).</summary>
    /// <returns>
    /// 200 OK with the configuration. Path values are populated only in Development; elsewhere the
    /// provider and the <c>*Configured</c> flags are returned without them.
    /// </returns>
    [HttpGet("config/repository")]
    [ProducesResponseType(typeof(RepositoryConfigDTO), StatusCodes.Status200OK)]
    public ActionResult<RepositoryConfigDTO> GetRepositoryConfig()
    {
        var includePaths = _environment.IsDevelopment();

        return Ok(new RepositoryConfigDTO
        {
            Investment = BuildContextConfig(
                _investmentSettings.Provider,
                _investmentSettings.DataJsonFile,
                _investmentSettings.GoogleDriveCredentialsPath,
                _investmentSettings.GoogleDriveFilePath,
                includePaths),
            CashFlow = BuildContextConfig(
                _cashFlowSettings.Provider,
                _cashFlowSettings.DataJsonFile,
                _cashFlowSettings.GoogleDriveCredentialsPath,
                _cashFlowSettings.GoogleDriveFilePath,
                includePaths)
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

    private static RepositoryContextConfigDTO BuildContextConfig(
        string? provider,
        string? dataJsonFile,
        string? googleDriveCredentialsPath,
        string? googleDriveFilePath,
        bool includePaths) => new()
        {
            Provider = provider,
            DataJsonFile = includePaths ? dataJsonFile : null,
            DataJsonFileConfigured = !string.IsNullOrWhiteSpace(dataJsonFile),
            GoogleDriveCredentialsPath = includePaths ? googleDriveCredentialsPath : null,
            GoogleDriveCredentialsConfigured = !string.IsNullOrWhiteSpace(googleDriveCredentialsPath),
            GoogleDriveFilePath = includePaths ? googleDriveFilePath : null,
            GoogleDriveFileConfigured = !string.IsNullOrWhiteSpace(googleDriveFilePath)
        };
}
