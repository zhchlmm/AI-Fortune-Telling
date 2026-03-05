param(
    [string]$BaseUrl = "",
    [string]$Username = "admin",
    [string]$CorrectPassword = "admin123",
    [string]$WrongPassword = "wrong-password",
    [int]$RecentMinutes = 10
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

function Write-Fail {
    param([string]$Message)
    Write-Host "[FAIL] $Message" -ForegroundColor Red
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

function Invoke-Login {
    param(
        [string]$ApiBase,
        [string]$User,
        [string]$Password
    )

    $body = @{ username = $User; password = $Password } | ConvertTo-Json

    try {
        $response = Invoke-RestMethod -Method Post -Uri "$ApiBase/api/v1/auth/login" -ContentType "application/json" -Body $body
        return [PSCustomObject]@{
            Success = $true
            StatusCode = 200
            Data = $response
        }
    }
    catch {
        $statusCode = 0
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }

        return [PSCustomObject]@{
            Success = $false
            StatusCode = $statusCode
            Data = $null
        }
    }
}

$BaseUrl = Resolve-BaseUrl -ProvidedBaseUrl $BaseUrl
Write-Host "[INFO] Using BaseUrl: $BaseUrl" -ForegroundColor Yellow

$startUtc = [DateTime]::UtcNow

try {
    Write-Step "Trigger one failed login"
    $failed = Invoke-Login -ApiBase $BaseUrl -User $Username -Password $WrongPassword
    if ($failed.Success) {
        Write-Fail "Expected failed login but received success"
        exit 1
    }

    if ($failed.StatusCode -ne 401 -and $failed.StatusCode -ne 423) {
        Write-Fail "Failed login status is unexpected: $($failed.StatusCode)"
        exit 1
    }

    Write-Pass "Failed login triggered (status=$($failed.StatusCode))"

    Write-Step "Trigger one successful login"
    $success = Invoke-Login -ApiBase $BaseUrl -User $Username -Password $CorrectPassword
    if (-not $success.Success -or -not $success.Data.token) {
        Write-Fail "Successful login failed. status=$($success.StatusCode). 请确认账号未被锁定且密码正确"
        exit 1
    }

    Write-Pass "Successful login triggered"

    Write-Step "Query audit logs and validate both success/failed records"
    $token = $success.Data.token
    $headers = @{ Authorization = "Bearer $token" }
    $audits = Invoke-RestMethod -Method Get -Uri "$BaseUrl/api/v1/admin/auth-audits?page=1&pageSize=100&username=$Username" -Headers $headers

    if ($null -eq $audits.items) {
        Write-Fail "Audit API response is invalid (items missing)"
        exit 1
    }

    $windowStart = $startUtc.AddMinutes(-1 * [Math]::Abs($RecentMinutes))
    $parseStyles = [System.Globalization.DateTimeStyles]::AssumeUniversal -bor [System.Globalization.DateTimeStyles]::AdjustToUniversal
    $recentItems = @($audits.items | Where-Object {
        $created = [DateTime]::Parse($_.createdAt, [System.Globalization.CultureInfo]::InvariantCulture, $parseStyles)
        $created -ge $windowStart
    })

    $hasFailed = $recentItems | Where-Object { $_.isSuccess -eq $false } | Select-Object -First 1
    $hasSuccess = $recentItems | Where-Object { $_.isSuccess -eq $true } | Select-Object -First 1

    if (-not $hasFailed) {
        Write-Fail "未在最近 $RecentMinutes 分钟审计日志中找到失败登录记录"
        exit 1
    }

    if (-not $hasSuccess) {
        Write-Fail "未在最近 $RecentMinutes 分钟审计日志中找到成功登录记录"
        exit 1
    }

    Write-Pass "Audit logs verified: both failed and successful login records exist"
    Write-Host ""
    Write-Host "Auth audit verification passed" -ForegroundColor Green
    exit 0
}
catch {
    Write-Host ""
    Write-Fail "Auth audit verification failed"
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
