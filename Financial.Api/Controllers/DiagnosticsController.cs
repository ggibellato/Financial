using Financial.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("")]
public sealed class DiagnosticsController : ControllerBase
{
    private readonly IRepositorySettings _repositorySettings;
    private readonly IHostEnvironment _environment;

    public DiagnosticsController(IRepositorySettings repositorySettings, IHostEnvironment environment)
    {
        _repositorySettings = repositorySettings ?? throw new ArgumentNullException(nameof(repositorySettings));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHealth()
    {
        return Ok(new { status = "ok" });
    }

    [HttpGet("config/repository")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetRepositoryConfig()
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        return Ok(new
        {
            provider = _repositorySettings.Provider,
            dataJsonFile = _repositorySettings.DataJsonFile,
            googleDriveCredentialsPath = _repositorySettings.GoogleDriveCredentialsPath,
            googleDriveFilePath = _repositorySettings.GoogleDriveFilePath
        });
    }
}
