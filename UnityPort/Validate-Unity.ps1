param([string]$UnityEditorDirectory = 'C:\Program Files\Unity\Hub\Editor\6000.7.0b2\Editor', [switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) { & (Join-Path $PSScriptRoot 'Build-Unity.ps1') -UnityEditorDirectory $UnityEditorDirectory }
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$validation = Join-Path $repo ".codex-build/Unity3D-$stamp"
if (Test-Path -LiteralPath $validation) { throw "Validation destination already exists: $validation" }
[void](New-Item -ItemType Directory -Path (Join-Path $validation 'Assets') -Force)
Copy-Item -LiteralPath (Join-Path $repo 'Assets/DragonGlareUnity') -Destination (Join-Path $validation 'Assets') -Recurse
Copy-Item -LiteralPath (Join-Path $repo 'ProjectSettings') -Destination $validation -Recurse
[void](New-Item -ItemType Directory -Path (Join-Path $validation 'Packages'))
# This isolates the RPG from unrelated editor plugins while using the same editor and settings.
# Keep the temporary Unity project isolated, but use the exact Lumis package
# version selected by the source project (no mutable main branch reference).
$mainManifestPath = Join-Path $repo 'Packages/manifest.json'
$mainManifest = Get-Content -LiteralPath $mainManifestPath -Raw | ConvertFrom-Json
$lumisSource = $mainManifest.dependencies.'jp.hotamachi.lumis.security'
if ([string]::IsNullOrWhiteSpace($lumisSource)) {
    throw "Lumis Security must be installed in $mainManifestPath before validation."
}
$manifest = @{
    dependencies = [ordered]@{
        'com.unity.modules.audio' = '1.0.0'
        'com.unity.modules.imgui' = '1.0.0'
        'com.unity.modules.imageconversion' = '1.0.0'
        'com.unity.modules.jsonserialize' = '1.0.0'
        'com.unity.modules.particlesystem' = '1.0.0'
        'com.unity.modules.physics' = '1.0.0'
        'com.unity.modules.screencapture' = '1.0.0'
        'jp.hotamachi.lumis.security' = $lumisSource
    }
} | ConvertTo-Json -Depth 5
[IO.File]::WriteAllText((Join-Path $validation 'Packages/manifest.json'), $manifest)
$exe = Join-Path $UnityEditorDirectory 'Unity.exe'
$arguments = @('-batchmode', '-projectPath', ('"' + $validation + '"'), '-executeMethod', 'DragonGlareEditor.ValidateAndBuild', '-quit', '-logFile', ('"' + (Join-Path $validation 'build.log') + '"'))
$process = Start-Process -FilePath $exe -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Unity validation failed ($($process.ExitCode)); inspect $validation/build.log" }
$report = Join-Path $validation '.codex-build/unity-validation/report.json'
if (-not (Test-Path -LiteralPath $report)) { throw "Unity validation did not produce a report: $report" }
Get-Content -LiteralPath $report
Write-Output "Validation project: $validation"
