param(
    [string]$BaseUrl = "http://localhost:5230",
    [string]$Username = "admin",
    [string]$Password = "admin123",
    [string]$UserId = "ai-audit-user",
    [string]$FortuneType = "Tarot",
    [string]$Question = "我最近工作运势如何？",
    [string]$ApiKey = "",
    [string]$Model = "gpt-4o-mini",
    [string]$ProviderBaseUrl = "https://api.openai.com/v1",
    [int]$WindowMinutes = 30
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

if ([string]::IsNullOrWhiteSpace($ApiKey)) {
    $ApiKey = $env:OpenAiCompatible__ApiKey
}
if ([string]::IsNullOrWhiteSpace($ApiKey)) {
    $ApiKey = $env:OPENAI_API_KEY
}

if ([string]::IsNullOrWhiteSpace($ApiKey)) {
    Write-Fail "未检测到AI密钥。请通过 -ApiKey 传入，或设置环境变量 OpenAiCompatible__ApiKey / OPENAI_API_KEY"
    exit 1
}

$BaseUrl = $BaseUrl.TrimEnd('/')
$windowStartUtc = [DateTime]::UtcNow.AddMinutes(-1 * [Math]::Abs($WindowMinutes)).ToString("o")

try {
    Write-Step "Validate server-side OpenAI config via health trigger"
    Write-Host "[INFO] 请确保启动 API 前已设置 OpenAiCompatible__Enabled=true 和 OpenAiCompatible__ApiKey" -ForegroundColor Yellow

    Write-Step "Login to get admin token"
    $loginBody = @{ username = $Username; password = $Password } | ConvertTo-Json
    $login = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/v1/auth/login" -ContentType "application/json" -Body $loginBody
    if (-not $login.token) {
        throw "Login succeeded but token is empty"
    }
    $headers = @{ Authorization = "Bearer $($login.token)" }
    Write-Pass "Login succeeded"

    Write-Step "Create fortune session"
    $fortuneBody = @{
        userId = $UserId
        fortuneType = $FortuneType
        parameters = @{ question = $Question }
    } | ConvertTo-Json -Depth 5

    $session = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/v1/fortune-sessions" -ContentType "application/json" -Body $fortuneBody
    if (-not $session.id) {
        throw "Fortune session created but id is empty"
    }
    Write-Pass "Fortune session created: $($session.id)"

    Write-Step "Query AI audits and verify non-degraded record"
    $auditUri = "$BaseUrl/api/v1/admin/ai-audits?page=1&pageSize=50&fortuneType=$FortuneType&fromUtc=$([uri]::EscapeDataString($windowStartUtc))"
    $audits = Invoke-RestMethod -Method Get -Uri $auditUri -Headers $headers

    if ($null -eq $audits.items) {
        throw "AI audit API response is invalid (items missing)"
    }

    $nonDegraded = @($audits.items | Where-Object { $_.degraded -eq $false } | Select-Object -First 1)
    if ($nonDegraded.Count -eq 0) {
        Write-Fail "未找到 degraded=false 的AI审计记录，请检查ProviderBaseUrl/ApiKey/Model是否可用"
        exit 1
    }

    $record = $nonDegraded[0]
    Write-Pass "Found non-degraded AI audit record, reason=$($record.reason), model=$($record.model), elapsedMs=$($record.elapsedMs)"

    Write-Host ""
    Write-Host "AI audit success-path verification passed" -ForegroundColor Green
    exit 0
}
catch {
    Write-Host ""
    Write-Fail "AI audit success-path verification failed"
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
