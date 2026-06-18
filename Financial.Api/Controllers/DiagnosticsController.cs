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

        string? dataJsonFile = null;
        string? googleDriveCredentialsPath = null;
        string? googleDriveFilePath = null;

        if (_repositorySettings is ILocalJsonRepositorySettings localJson)
        {
            dataJsonFile = localJson.DataJsonFile;
        }
        else if (_repositorySettings is IGoogleDriveRepositorySettings googleDrive)
        {
            googleDriveCredentialsPath = googleDrive.GoogleDriveCredentialsPath;
            googleDriveFilePath = googleDrive.GoogleDriveFilePath;
        }

        return Ok(new
        {
            provider = _repositorySettings.Provider,
            dataJsonFile,
            googleDriveCredentialsPath,
            googleDriveFilePath
        });
    }
}
