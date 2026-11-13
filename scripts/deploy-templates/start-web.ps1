# Launches the Financial web server (API + SPA) from this deploy folder.
$ErrorActionPreference = 'Stop'

$webDir = Join-Path $PSScriptRoot 'Financial.Web'
$exePath = Join-Path $webDir 'Financial.Api.exe'
$jaegerContainer = 'financial-jaeger'
$jaegerImage = 'jaegertracing/jaeger:latest'

if (-not (Test-Path $exePath)) {
    Write-Error "Financial.Api.exe not found at '$exePath'. Run scripts/deploy.ps1 first."
    exit 1
}

function Start-Jaeger {
    try {
        $existing = docker ps -a --filter "name=^$jaegerContainer$" --format '{{.Names}}' 2>$null
        if ($LASTEXITCODE -ne 0) { throw 'Docker is not running.' }

        if ($existing) {
            docker start $jaegerContainer 2>$null | Out-Null
        }
        else {
            docker run -d --name $jaegerContainer --restart unless-stopped `
                -p 16686:16686 -p 127.0.0.1:4317:4317 $jaegerImage 2>$null | Out-Null
        }
        if ($LASTEXITCODE -ne 0) { throw "Could not start the '$jaegerContainer' container." }

        Write-Host "Jaeger tracing UI at http://localhost:16686"
    }
    catch {
        Write-Warning "Tracing unavailable ($($_.Exception.Message)); the app runs normally without it."
    }
}

Start-Jaeger

$env:ASPNETCORE_URLS = 'http://0.0.0.0:8080'
Start-Process -FilePath $exePath -WorkingDirectory $webDir
Write-Host "Financial web app starting at http://localhost:8080 (also reachable on your LAN IP)"
Write-Host "API docs (Swagger) at http://localhost:8080/swagger"
