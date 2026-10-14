param([Parameter(Mandatory = $true)][string]$PlayerPath, [string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$player = [IO.Path]::GetFullPath($PlayerPath)
if (-not (Test-Path -LiteralPath $player -PathType Leaf)) { throw "Player not found: $player" }
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repo ('.codex-build/player-smoke-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 6))
}
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { throw "Use a new smoke output directory: $output" }
[void](New-Item -ItemType Directory -Path $output)
$arguments = @('-force-d3d11', '-screen-width', '1280', '-screen-height', '960', '-dragonGlareSmoke', '-dragonGlareSmokeOutput', ('"' + $output + '"'), '-logFile', ('"' + (Join-Path $output 'player.log') + '"'))
# Batch players skip the IMGUI repaint used by the retained UI. Run a
# background-enabled normal player so PNG verification checks actual frames.
$process = Start-Process -FilePath $player -WorkingDirectory (Split-Path $player -Parent) -ArgumentList $arguments -WindowStyle Hidden -PassThru
$timer = [Diagnostics.Stopwatch]::StartNew()
try {
    while (-not $process.WaitForExit(1000)) {
        if ($timer.Elapsed.TotalSeconds -gt 120) {
            $process.Kill()
            throw "Player smoke timed out. Inspect $output/player.log"
        }
    }
    $reportPath = Join-Path $output 'smoke-report.json'
    if (-not (Test-Path -LiteralPath $reportPath)) { throw "No smoke report produced: $output" }
    $report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
    if ($process.ExitCode -ne 0 -or -not $report.passed) { throw "Player smoke failed. Inspect $reportPath" }
    [pscustomobject]@{Passed = $report.passed; Checks = $report.checks.Count; Screenshots = $report.screenshots.Count; Errors = $report.errorCount; Report = $reportPath} | ConvertTo-Json
}
finally { $process.Dispose() }
