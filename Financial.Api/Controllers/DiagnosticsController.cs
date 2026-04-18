using Financial.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Financial.Api.Controllers;

[ApiController]
[Route("")]
public sealed class DiagnosticsController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private const string RepositoryProviderConfigurationKey = "Repository:Provider";

    public DiagnosticsController(IConfiguration configuration, IHostEnvironment environment)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
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
            provider = _configuration[RepositoryProviderConfigurationKey],
            dataJsonFile = _configuration[LocalJsonStorage.DataJsonFileConfigurationKey],
            googleDriveCredentialsPath = _configuration[GoogleDriveJsonStorage.CredentialsPathConfigurationKey],
            googleDriveFilePath = _configuration[GoogleDriveJsonStorage.FilePathConfigurationKey]
        });
    }
}
