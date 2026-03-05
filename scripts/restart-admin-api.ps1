param(
    [string]$Url = "http://localhost:5230",
    [string]$CliUrl = "localhost:52001",
    [string]$Model = "gpt-5-mini",
    [switch]$NoBuild = $true,
    [switch]$EnableCopilotSdk = $true,
    [switch]$KillExisting = $true
)

$ErrorActionPreference = "Stop"

$stopScript = Join-Path $PSScriptRoot "stop-admin-api.ps1"
$startScript = Join-Path $PSScriptRoot "start-admin-api.ps1"

if (-not (Test-Path $stopScript)) {
    throw "Missing script: $stopScript"
}

if (-not (Test-Path $startScript)) {
    throw "Missing script: $startScript"
}

Write-Host "[STEP] Stopping AdminApi.Host..." -ForegroundColor Cyan
& $stopScript

Write-Host "[STEP] Starting AdminApi.Host..." -ForegroundColor Cyan
& $startScript `
    -Url $Url `
    -CliUrl $CliUrl `
    -Model $Model `
    -NoBuild:$NoBuild `
    -EnableCopilotSdk:$EnableCopilotSdk `
    -KillExisting:$KillExisting
