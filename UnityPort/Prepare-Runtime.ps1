param()
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$target = Join-Path $PSScriptRoot 'Runtime/obj/PortSources'
[void](New-Item -ItemType Directory -Force -Path $target)
$utf8 = [System.Text.UTF8Encoding]::new($false)
function Write-PortSource($name, $source) {
    $source = $source.Replace('using System.Drawing.Drawing2D;', '').Replace('using System.Drawing.Text;', '')
    $source = $source.Replace('System.Drawing.Drawing2D.', 'DragonGlareUnity.Drawing.')
    $output = Join-Path $target $name
    if (-not [IO.File]::Exists($output) -or [IO.File]::ReadAllText($output) -ne $source) {
        [System.IO.File]::WriteAllText($output, $source, $utf8)
    }
}
function Replace-Method($source, $name, $replacement) {
    $pattern = '(?ms)^    private (?:static )?[^\r\n]+ ' + [regex]::Escape($name) + '\([^\r\n]*\)\r?\n    \{.*?^    \}'
    $matches = [regex]::Matches($source, $pattern)
    if ($matches.Count -ne 1) { throw "Expected exactly one method $name, got $($matches.Count). Update the port adapter for the new baseline." }
    return $source.Substring(0, $matches[0].Index) + $replacement + $source.Substring($matches[0].Index + $matches[0].Length)
}
$files = @('UI/State','UI/Input','UI/Persistence','UI/Layout','UI/Screens')
foreach ($directory in $files) {
    foreach ($file in Get-ChildItem -LiteralPath (Join-Path $repo $directory) -Filter '*.cs' -Recurse) {
        $source = [System.IO.File]::ReadAllText($file.FullName)
        $source = $source.Replace('        prologueBgmCompleted = false;', '')
        if ($file.Name -eq 'DragonGlare.FieldRendering.cs') {
            $source = Replace-Method $source 'DrawFieldScene' @'
    private void DrawFieldScene(Graphics g)
    {
        if (WorldTexture is not null) g.DrawTexture(WorldTexture, new Rectangle(0, 0, 640, 480));
        else DrawMenuBackdrop(g);
    }
'@
        }
        if ($file.Name -eq 'DragonGlare.BattleRendering.cs') {
            $source = Replace-Method $source 'DrawBattle' @'
    private void DrawBattle(Graphics g)
    {
        if (WorldTexture is not null) g.DrawTexture(WorldTexture, new Rectangle(0, 0, 640, 480));
        else DrawBattleBackdrop(g);
        DrawBattleTopUi(g);
        DrawBattleMessageWindow(g);
    }
'@
        }
        if ($file.Name -eq 'DragonGlare.MenuRendering.cs') {
            $source = Replace-Method $source 'DrawLanguageOpeningNarration' @'
    private void DrawLanguageOpeningNarration(Graphics g)
    {
        var text = GetCurrentLanguageOpeningText();
        var alpha = GetLanguageOpeningNarrationAlpha();
        if (string.IsNullOrWhiteSpace(text) || alpha <= 0f) return;
        var lines = NormalizeTextLines(text);
        var destination = GetOpeningViewportDestination(OpeningSourceViewportWidth, OpeningSourceViewportHeight);
        var y = destination.Y + 220 + Math.Max(0, (48 - lines.Count * UiTypography.LineHeight) / 2);
        using var shadow = new SolidBrush(Color.FromArgb((int)(alpha * 255), Color.Black));
        using var ink = new SolidBrush(Color.FromArgb((int)(alpha * 255), Color.White));
        foreach (var line in lines)
        {
            var x = destination.X + (destination.Width - MeasureTextWidth(g, line, uiFont)) / 2;
            foreach (var off in new[] { new Point(0,-1), new Point(-1,0), new Point(1,0), new Point(0,1) })
                DrawTextLine(g, line, uiFont, shadow, x + off.X, y + off.Y);
            DrawTextLine(g, line, uiFont, ink, x, y); y += UiTypography.LineHeight;
        }
    }
'@
        }
        if ($file.Name -eq 'DragonGlare.BattleResolution.cs') {
            $source = [regex]::Replace($source, 'private void ApplyBattleStepVisualEffect\(BattleSequenceStep step\)\r?\n    \{',
                'private void ApplyBattleStepVisualEffect(BattleSequenceStep step)' + "`n" + '    {' + "`n" + '        VisualCue?.Invoke(step.VisualCue);')
            if (-not $source.Contains('VisualCue?.Invoke(step.VisualCue);')) { throw 'Battle visual cue hook no longer matches baseline.' }
        }
        Write-PortSource $file.Name $source
    }
}
Write-PortSource 'DragonGlare.TextRendering.cs' ([System.IO.File]::ReadAllText((Join-Path $repo 'UI/Rendering/DragonGlare.TextRendering.cs')))
Write-PortSource 'AppMetadata.cs' ([System.IO.File]::ReadAllText((Join-Path $repo 'UI/Startup/AppMetadata.cs')))
$root = [System.IO.File]::ReadAllText((Join-Path $repo 'DragonGlare.cs'))
$start = $root.IndexOf('    private const int TileSize')
$end = $root.IndexOf('    public DragonGlareAlpha(')
if ($start -lt 0 -or $end -le $start) { throw 'Root session fields no longer match baseline.' }
$fields = $root.Substring($start, $end-$start)
$fields = [regex]::Replace($fields, '(?m)^    private (?:readonly )?(?:GraphicsDeviceManager|WindowChromeService|SpriteBatch\?|Bitmap\?|Texture2D\?|byte\[\]|XnaColor\[\]|PrivateFontCollection)[^\r\n]*\r?\n', '')
$fields = $fields.Replace('private readonly SaveService saveService = new();', 'private readonly SaveService saveService;')
$fields = $fields.Replace('private readonly LaunchSettingsService launchSettingsService = new();', 'private readonly LaunchSettingsService launchSettingsService;')
$fields = $fields.Replace('UiTypography.FontPixelSize, GraphicsUnit.Pixel)', 'UiTypography.FontPixelSize)')
$fields = [regex]::Replace($fields, '(?m)^    private (?:Image\? (?:mapTileSheet|fieldMapImage)|bool prologueBgmCompleted|int windowChromeRefreshFramesRemaining)[^\r\n]*\r?\n', '')
$fields = [regex]::Replace($fields, '(?m)^    private string LegacySaveFilePath[^\r\n]*\r?\n', '')
$header = @'
using DragonGlareAlpha.Domain;
using DragonGlareAlpha.Domain.Battle;
using DragonGlareAlpha.Domain.Commerce;
using DragonGlareAlpha.Domain.Field;
using DragonGlareAlpha.Domain.Items;
using DragonGlareAlpha.Domain.Player;
using DragonGlareAlpha.Domain.Startup;
using DragonGlareAlpha.Persistence;
using DragonGlareAlpha.Security;
using DragonGlareAlpha.Services;
namespace DragonGlareAlpha;
public partial class DragonGlareAlpha
{
'@
$methods = ''
foreach ($method in @('RenderVirtualFrame','UpdateStartupOptions','SetStartupDisplayMode','UpdateLaunchSettings','SaveLaunchSettings','ToggleFullscreen','GetWindowedClientSize')) {
    $pattern = '(?ms)^    private (?:static )?[^\r\n]+ ' + $method + '\([^\r\n]*\)\r?\n    \{.*?^    \}'
    $match = [regex]::Match($root, $pattern)
    if (-not $match.Success) { throw "Root method $method no longer matches baseline." }
    $methods += $match.Value + "`n"
}
Write-PortSource 'DragonGlare.SessionFields.cs' ($header + "`n" + $fields + $methods + "`n}")
