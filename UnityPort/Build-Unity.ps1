param([string]$UnityEditorDirectory = 'C:\Program Files\Unity\Hub\Editor\6000.7.0b2\Editor')
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
& (Join-Path $PSScriptRoot 'Build-Core.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Core build failed.' }
& (Join-Path $PSScriptRoot 'Prepare-Runtime.ps1')
dotnet build (Join-Path $PSScriptRoot 'Runtime/DragonGlare.Unity.Runtime.csproj') --configuration Release --nologo "-p:UnityEditorDirectory=$UnityEditorDirectory"
if ($LASTEXITCODE -ne 0) { throw 'Unity runtime build failed.' }
$plugin = Join-Path $repo 'Assets/DragonGlareUnity/Plugins'
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Runtime/bin/Release/netstandard2.1/DragonGlare.Unity.Runtime.dll') -Destination $plugin -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Runtime/bin/Release/netstandard2.1/DragonGlare.Unity.Runtime.pdb') -Destination $plugin -Force
$resources = Join-Path $repo 'Assets/DragonGlareUnity/Resources/DragonGlare'
function Copy-Resource($source, $destination) {
    if (Test-Path -LiteralPath $source -PathType Leaf) {
        if (-not [IO.Path]::GetFullPath($source).Equals([IO.Path]::GetFullPath($destination), [StringComparison]::OrdinalIgnoreCase)) {
            Copy-Item -LiteralPath $source -Destination $destination -Force
        }
    } elseif (-not (Test-Path -LiteralPath $destination -PathType Leaf)) {
        throw "Missing asset: neither original '$source' nor Unity resource '$destination' exists."
    }
}
function Copy-Resources($source, $folder, $extensions, $requiredNames) {
    $dest = Join-Path $resources $folder
    $original = Join-Path $repo $source
    [void](New-Item -ItemType Directory -Path $dest -Force)
    if (Test-Path -LiteralPath $original -PathType Container) {
        foreach ($file in Get-ChildItem -LiteralPath $original -File) {
            if ($extensions -contains $file.Extension.ToLowerInvariant()) {
                Copy-Resource $file.FullName (Join-Path $dest $file.Name)
            }
        }
    }
    foreach ($name in $requiredNames) {
        if (-not (Test-Path -LiteralPath (Join-Path $dest $name) -PathType Leaf)) {
            throw "Missing asset: neither original '$(Join-Path $original $name)' nor Unity resource '$(Join-Path $dest $name)' exists."
        }
    }
}
Copy-Resources 'Assets/UI' 'UI' @('.png') @(
    'SFC_battlefieldFrame1.png','SFC_battlewindowFrame1.png','SFC_glidFrame1.png',
    'SFC_opening.png','window11.png','window21.png'
)
foreach ($name in @('SFC_language.png','SFC_main.png')) {
    Copy-Resource (Join-Path $repo "Assets/$name") (Join-Path $resources "UI/$name")
}
Copy-Resources 'Assets/Portraits/NPC' 'Portraits' @('.png') @(
    'castle-3.png','castle-guard-4.png','guide-4.png','mihari-3.png','thief-1.png','young-5.png'
)
Copy-Resources 'Assets/BGM' 'BGM' @('.mp3','.wav','.ogg') @(
    'SFC_battle.mp3','SFC_castle.mp3','SFC_field.mp3','SFC_glare.mp3','SFC_glareloop.mp3',
    'SFC_main_menu.mp3','SFC_prologue02.mp3','SFC_prologue2.mp3','SFC_shop_buy.mp3','SFC_town.mp3'
)
Copy-Resources 'Assets/SE' 'SE' @('.mp3','.wav','.ogg') @(
    'SFC_atk1.mp3','SFC_cancel.mp3','SFC_cure.mp3','SFC_cursor.mp3','SFC_def.mp3',
    'SFC_equip.mp3','SFC_escape.mp3','SFC_fire.mp3','SFC_magic.mp3','SFC_poison.mp3',
    'SFC_raiden.mp3','Serif_SE.mp3','当たり判定SFC.wav'
)
[void](New-Item -ItemType Directory -Path (Join-Path $resources 'Fonts') -Force)
Copy-Resource (Join-Path $repo 'Assets/JF-Dot-ShinonomeMin14.ttf') (Join-Path $resources 'Fonts/JF-Dot-ShinonomeMin14.ttf')
Write-Host 'Unity RPG runtime and resources are ready. Open Assets/DragonGlareUnity/Scenes/DragonGlare3D.unity.'
