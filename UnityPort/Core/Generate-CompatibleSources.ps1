[CmdletBinding()]
param([Parameter(Mandatory = $true)][string]$OutputDirectory)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$generatedRoot = [IO.Path]::GetFullPath($OutputDirectory)
$expectedRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'obj')) + [IO.Path]::DirectorySeparatorChar
if (-not $generatedRoot.StartsWith($expectedRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Generated sources must stay inside $expectedRoot"
}
[void][IO.Directory]::CreateDirectory($generatedRoot)

function Write-AdaptedSource([string]$RelativePath, [hashtable]$Replacements) {
    $source = [IO.File]::ReadAllText((Join-Path $repositoryRoot $RelativePath))
    foreach ($entry in $Replacements.GetEnumerator() | Sort-Object { $_.Key.Length } -Descending) {
        if (-not $source.Contains([string]$entry.Key)) {
            throw "Expected compatibility adaptation no longer matches $RelativePath : $($entry.Key)"
        }
        $source = $source.Replace([string]$entry.Key, [string]$entry.Value)
    }
    $outputPath = Join-Path $generatedRoot ([IO.Path]::GetFileName($RelativePath))
    if (-not [IO.File]::Exists($outputPath) -or [IO.File]::ReadAllText($outputPath) -ne $source) {
        [IO.File]::WriteAllText($outputPath, $source, [Text.UTF8Encoding]::new($false))
    }
}

Write-AdaptedSource 'Security\ProtectedInt.cs' @{
    'BitOperations.RotateLeft((uint)value, offset)' = '(((uint)value << offset) | ((uint)value >> (32 - offset)))'
}
Write-AdaptedSource 'Persistence\SaveService.cs' @{
    'SHA256.HashData(' = 'CryptoCompatibility.Sha256('
    'HMACSHA256.HashData(' = 'CryptoCompatibility.HmacSha256('
    'ProtectedData.Protect(' = 'WindowsDataProtection.Protect('
    'ProtectedData.Unprotect(' = 'WindowsDataProtection.Unprotect('
    'DataProtectionScope.CurrentUser' = '0u'
    'File.Move(tempPath, path, overwrite: true);' = 'SaveFileOperations.Commit(tempPath, path);'
    'Environment.SpecialFolder.ApplicationData), "DragonGlareAlpha")' = 'Environment.SpecialFolder.ApplicationData), "DragonGlareUnity")'
}
Write-AdaptedSource 'Services\PlatformSupportService.cs' @{
    'OperatingSystem.IsWindows()' = 'RuntimeInformation.IsOSPlatform(OSPlatform.Windows)'
}

$launchSource = [IO.File]::ReadAllText((Join-Path $repositoryRoot 'Services\LaunchSettingsService.cs'))
$settingsFieldPattern = '(?s)    private readonly string settingsPath = Path.Combine\(.*?"launch_settings.json"\);'
if (-not [regex]::IsMatch($launchSource, $settingsFieldPattern)) {
    throw 'LaunchSettingsService settings field no longer matches the compatibility adaptation.'
}
$settingsField = @'
    private readonly string settingsPath;

    public LaunchSettingsService(string? settingsPath = null)
    {
        this.settingsPath = string.IsNullOrWhiteSpace(settingsPath)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DragonGlareUnity", "launch_settings.json")
            : Path.GetFullPath(settingsPath);
    }
'@
$launchSource = [regex]::Replace($launchSource, $settingsFieldPattern, $settingsField)
$launchOutput = Join-Path $generatedRoot 'LaunchSettingsService.cs'
if (-not [IO.File]::Exists($launchOutput) -or [IO.File]::ReadAllText($launchOutput) -ne $launchSource) {
    [IO.File]::WriteAllText($launchOutput, $launchSource, [Text.UTF8Encoding]::new($false))
}
