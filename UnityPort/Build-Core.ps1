[CmdletBinding()]
param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Release')

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$project = Join-Path $PSScriptRoot 'Core\DragonGlare.Core.csproj'
$pluginRoot = Join-Path $repositoryRoot 'Assets\DragonGlareUnity\Plugins'

dotnet build $project --configuration $Configuration --nologo
if ($LASTEXITCODE -ne 0) { throw "DragonGlare core build failed ($LASTEXITCODE)." }

[void][IO.Directory]::CreateDirectory($pluginRoot)
$outputRoot = Join-Path $PSScriptRoot "Core\bin\$Configuration\netstandard2.1"
foreach ($file in Get-ChildItem -LiteralPath $outputRoot -File -Filter '*.dll') {
    Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $pluginRoot $file.Name) -Force
}
Copy-Item -LiteralPath (Join-Path $outputRoot 'DragonGlare.Core.pdb') -Destination $pluginRoot -Force
Write-Output "Unity core and JSON dependencies built: $pluginRoot"
