param(
    [switch]$Quiet = $false
)

$ErrorActionPreference = "Stop"

$targets = Get-CimInstance Win32_Process |
    Where-Object { $_.Name -match 'AdminApi.Host(.exe)?|dotnet(.exe)?' -and $_.CommandLine -match 'AdminApi.Host' }

if (-not $targets -or $targets.Count -eq 0) {
    if (-not $Quiet) {
        Write-Host "[INFO] No AdminApi.Host process found." -ForegroundColor Yellow
    }
    exit 0
}

$stopped = @()
foreach ($proc in $targets) {
    try {
        Stop-Process -Id $proc.ProcessId -Force
        $stopped += $proc.ProcessId
    } catch {
        if (-not $Quiet) {
            Write-Host "[WARN] Failed to stop process $($proc.ProcessId): $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
}

if (-not $Quiet) {
    if ($stopped.Count -gt 0) {
        Write-Host "[PASS] Stopped AdminApi.Host processes: $($stopped -join ', ')" -ForegroundColor Green
    } else {
        Write-Host "[INFO] No process stopped." -ForegroundColor Yellow
    }
}
