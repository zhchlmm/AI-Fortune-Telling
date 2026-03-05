param(
    [string]$BaseUrl = "",
    [string]$Username = "admin",
    [string]$Password = "admin123",
    [string]$UserId = "demo-user",
    [string]$FortuneType = "Tarot"
)

$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host "[STEP] $Message" -ForegroundColor Cyan
}

function Write-Pass {
    param([string]$Message)
    Write-Host "[PASS] $Message" -ForegroundColor Green
}

function Get-CountFromResult {
    param([object]$Result)

    if ($Result -is [System.Array]) {
        return $Result.Count
    }

    if ($null -ne $Result -and $null -ne $Result.value) {
        if ($Result.value -is [System.Array]) {
            return $Result.value.Count
        }
        return 1
    }

    if ($null -eq $Result) {
        return 0
    }

    return 1
}

function Resolve-BaseUrl {
    param([string]$ProvidedBaseUrl)

    if (-not [string]::IsNullOrWhiteSpace($ProvidedBaseUrl)) {
        return $ProvidedBaseUrl.TrimEnd('/')
    }

    $launchSettingsPath = Join-Path $PSScriptRoot "..\services\admin-api\AdminApi.Host\Properties\launchSettings.json"
    if (Test-Path $launchSettingsPath) {
        $launchSettings = Get-Content $launchSettingsPath -Raw | ConvertFrom-Json
        $url = $launchSettings.profiles.http.applicationUrl
        if (-not [string]::IsNullOrWhiteSpace($url)) {
            return $url.TrimEnd('/')
        }
    }

    return "http://localhost:5228"
}

$BaseUrl = Resolve-BaseUrl -ProvidedBaseUrl $BaseUrl
Write-Host "[INFO] Using BaseUrl: $BaseUrl" -ForegroundColor Yellow

try {
    Write-Step "Verify auth/login"
    $loginBody = @{ username = $Username; password = $Password } | ConvertTo-Json
    $loginResult = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/v1/auth/login" -ContentType "application/json" -Body $loginBody
    if (-not $loginResult.token) {
        throw "Login succeeded but token was empty"
    }
    Write-Pass "Login succeeded"

    $headers = @{ Authorization = "Bearer $($loginResult.token)" }

    Write-Step "Verify templates (authorized)"
    $templates = Invoke-RestMethod -Method Get -Uri "$BaseUrl/api/v1/templates" -Headers $headers
    $templateCount = Get-CountFromResult -Result $templates
    Write-Pass "Templates query succeeded, count=$templateCount"

    Write-Step "Create a fortune session"
    $fortuneBody = @{
        userId = $UserId
        fortuneType = $FortuneType
        parameters = @{ question = "healthcheck" }
    } | ConvertTo-Json -Depth 5

    $createdSession = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/v1/fortune-sessions" -ContentType "application/json" -Body $fortuneBody
    if (-not $createdSession.id) {
        throw "Session created but id was empty"
    }
    Write-Pass "Session created, id=$($createdSession.id)"

    Write-Step "Verify admin paged sessions (authorized)"
    $pagedUri = "$BaseUrl/api/v1/admin/fortune-sessions?page=1`&pageSize=10"
    $paged = Invoke-RestMethod -Method Get -Uri $pagedUri -Headers $headers
    if ($null -eq $paged.total -or $null -eq $paged.items) {
        throw "Paged response shape is invalid"
    }

    $itemCount = Get-CountFromResult -Result $paged.items
    Write-Pass "Paged query succeeded, total=$($paged.total), items=$itemCount"

    Write-Host ""
    Write-Host "API verification passed" -ForegroundColor Green
    exit 0
}
catch {
    Write-Host ""
    Write-Host "[FAIL] API verification failed" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
