using Financial.Api.DTOs;
using Financial.Investment.Application.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Financial.Api.Controllers;

/// <summary>
/// Diagnostic endpoints for checking API liveness and the active data repository configuration.
/// </summary>
[ApiController]
[Route("")]
public sealed class DiagnosticsController : ControllerBase
{
    private readonly InvestmentRepositorySettingsOptions _repositorySettings;
    private readonly IHostEnvironment _environment;

    public DiagnosticsController(IOptions<InvestmentRepositorySettingsOptions> repositorySettings, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(repositorySettings);
        _repositorySettings = repositorySettings.Value;
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <summary>Reports whether the API is up and responding.</summary>
    /// <returns>Always 200 OK with a static "ok" status when the API is reachable.</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(HealthStatusDTO), StatusCodes.Status200OK)]
    public ActionResult<HealthStatusDTO> GetHealth()
    {
        return Ok(new HealthStatusDTO { Status = "ok" });
    }

    /// <summary>Returns the active data repository configuration (provider and file paths).</summary>
    /// <returns>200 OK with the configuration when running in Development; 404 Not Found otherwise.</returns>
    [HttpGet("config/repository")]
    [ProducesResponseType(typeof(RepositoryConfigDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<RepositoryConfigDTO> GetRepositoryConfig()
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        return Ok(new RepositoryConfigDTO
        {
            Provider = _repositorySettings.Provider,
            DataJsonFile = _repositorySettings.DataJsonFile,
            GoogleDriveCredentialsPath = _repositorySettings.GoogleDriveCredentialsPath,
            GoogleDriveFilePath = _repositorySettings.GoogleDriveFilePath
        });
    }
}
