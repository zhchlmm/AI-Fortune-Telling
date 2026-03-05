param(
    [string]$WsUrl = "ws://localhost:5230/ws/fortune-stream",
    [string]$UserId = "demo-user",
    [string]$FortuneType = "Tarot",
    [string]$Question = "How is my career trend recently?",
    [int]$TimeoutSeconds = 120
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

$socket = $null
try {
    Write-Step "Connect WebSocket: $WsUrl"
    $socket = [System.Net.WebSockets.ClientWebSocket]::new()
    $cts = [System.Threading.CancellationTokenSource]::new([TimeSpan]::FromSeconds($TimeoutSeconds))
    $null = $socket.ConnectAsync([Uri]$WsUrl, $cts.Token).GetAwaiter().GetResult()

    if ($socket.State -ne [System.Net.WebSockets.WebSocketState]::Open) {
        throw "WebSocket not connected"
    }
    Write-Pass "WebSocket connected"

    Write-Step "Send fortune.start"
    $startPayload = @{
        type = "fortune.start"
        userId = $UserId
        fortuneType = $FortuneType
        question = $Question
    } | ConvertTo-Json -Depth 5

    $sendBytes = [System.Text.Encoding]::UTF8.GetBytes($startPayload)
    $segment = [ArraySegment[byte]]::new($sendBytes)
    $null = $socket.SendAsync($segment, [System.Net.WebSockets.WebSocketMessageType]::Text, $true, $cts.Token).GetAwaiter().GetResult()

    Write-Step "Read stream"
    $buffer = New-Object byte[] 8192
    $fullText = ""
    $done = $false

    while (-not $done -and $socket.State -eq [System.Net.WebSockets.WebSocketState]::Open) {
        $ms = New-Object System.IO.MemoryStream
        do {
            $recv = $socket.ReceiveAsync([ArraySegment[byte]]::new($buffer), $cts.Token).GetAwaiter().GetResult()
            if ($recv.MessageType -eq [System.Net.WebSockets.WebSocketMessageType]::Close) {
                $done = $true
                break
            }
            $ms.Write($buffer, 0, $recv.Count)
        } while (-not $recv.EndOfMessage)

        if ($done) {
            break
        }

        $text = [System.Text.Encoding]::UTF8.GetString($ms.ToArray())
        $msg = $text | ConvertFrom-Json

        if ($msg.type -eq "delta") {
            $fullText += [string]$msg.text
            Write-Host -NoNewline ([string]$msg.text)
            continue
        }

        if ($msg.type -eq "done") {
            Write-Host ""
            Write-Pass "done received, sessionId=$($msg.id), model=$($msg.model)"
            if ([string]::IsNullOrWhiteSpace($fullText) -and -not [string]::IsNullOrWhiteSpace([string]$msg.result)) {
                $fullText = [string]$msg.result
            }
            $done = $true
            continue
        }

        if ($msg.type -eq "error") {
            throw "Server error: $($msg.message)"
        }
    }

    if ([string]::IsNullOrWhiteSpace($fullText)) {
        Write-Host ""
        Write-Host "[INFO] No delta text received. Server may have returned done.result only." -ForegroundColor Yellow
    }

    Write-Host ""
    Write-Host "Copilot WebSocket verification passed" -ForegroundColor Green
    exit 0
}
catch {
    Write-Host ""
    Write-Fail "Copilot WebSocket verification failed"
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
finally {
    if ($socket -and $socket.State -eq [System.Net.WebSockets.WebSocketState]::Open) {
        try {
            $closeToken = [System.Threading.CancellationToken]::None
            $null = $socket.CloseAsync([System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure, "done", $closeToken).GetAwaiter().GetResult()
        } catch {
            # ignore
        }
    }

    if ($socket) {
        $socket.Dispose()
    }
}
