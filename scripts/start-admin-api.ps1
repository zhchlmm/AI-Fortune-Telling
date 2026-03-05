param(
    [string]$Url = "http://localhost:5230",
    [string]$CliUrl = "localhost:52001",
    [string]$Model = "gpt-5-mini",
    [switch]$NoBuild = $true,
    [switch]$EnableCopilotSdk = $true,
    [switch]$KillExisting = $true
)

$ErrorActionPreference = "Stop"

$projectPath = (Resolve-Path (Join-Path $PSScriptRoot "..\services\admin-api\AdminApi.Host")).Path

if ($KillExisting) {
    Get-CimInstance Win32_Process |
        Where-Object { $_.Name -match 'AdminApi.Host(.exe)?|dotnet(.exe)?' -and $_.CommandLine -match 'AdminApi.Host' } |
        ForEach-Object { Stop-Process -Id $_.ProcessId -Force }
}

Set-Location $projectPath

if ($EnableCopilotSdk) {
    $env:CopilotSdk__Enabled = 'true'
    $env:CopilotSdk__CliUrl = $CliUrl
    $env:CopilotSdk__Model = $Model
} else {
    Remove-Item Env:CopilotSdk__Enabled -ErrorAction SilentlyContinue
    Remove-Item Env:CopilotSdk__CliUrl -ErrorAction SilentlyContinue
    Remove-Item Env:CopilotSdk__Model -ErrorAction SilentlyContinue
}

$args = @('run')
if ($NoBuild) {
    $args += '--no-build'
}
$args += @('--urls', $Url)

Write-Host "[INFO] Project: $projectPath" -ForegroundColor Yellow
Write-Host "[INFO] URL: $Url" -ForegroundColor Yellow
Write-Host "[INFO] Copilot SDK: $EnableCopilotSdk (CliUrl=$CliUrl, Model=$Model)" -ForegroundColor Yellow
Write-Host "[INFO] Command: dotnet $($args -join ' ')" -ForegroundColor Yellow

& dotnet @args
