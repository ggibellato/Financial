# Starts both the web server and the desktop app from this deploy folder.
$ErrorActionPreference = 'Stop'

& (Join-Path $PSScriptRoot 'start-web.ps1')
& (Join-Path $PSScriptRoot 'start-app.ps1')
