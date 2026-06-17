using Financial.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace Financial.Api.Controllers;

[ApiController]
[Route("")]
public sealed class DiagnosticsController : ControllerBase
{
    private readonly IRepositoryDiagnostics _repositoryDiagnostics;
    private readonly IHostEnvironment _environment;

    public DiagnosticsController(IRepositoryDiagnostics repositoryDiagnostics, IHostEnvironment environment)
    {
        _repositoryDiagnostics = repositoryDiagnostics ?? throw new ArgumentNullException(nameof(repositoryDiagnostics));
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
            provider = _repositoryDiagnostics.Provider,
            dataJsonFile = _repositoryDiagnostics.DataJsonFile,
            googleDriveCredentialsPath = _repositoryDiagnostics.GoogleDriveCredentialsPath,
            googleDriveFilePath = _repositoryDiagnostics.GoogleDriveFilePath
        });
    }
}
