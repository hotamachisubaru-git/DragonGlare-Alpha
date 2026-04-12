param(
    [string]$OutputDirectory = "Assets\Sprites\Characters"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

$palette = @{
    "." = [System.Drawing.Color]::FromArgb(0, 0, 0, 0)
    "K" = [System.Drawing.Color]::FromArgb(255, 8, 8, 12)
    "H" = [System.Drawing.Color]::FromArgb(255, 74, 38, 20)
    "R" = [System.Drawing.Color]::FromArgb(255, 125, 70, 32)
    "S" = [System.Drawing.Color]::FromArgb(255, 255, 221, 176)
    "U" = [System.Drawing.Color]::FromArgb(255, 214, 150, 104)
    "B" = [System.Drawing.Color]::FromArgb(255, 18, 60, 146)
    "C" = [System.Drawing.Color]::FromArgb(255, 46, 116, 216)
    "D" = [System.Drawing.Color]::FromArgb(255, 97, 170, 255)
    "G" = [System.Drawing.Color]::FromArgb(255, 70, 78, 94)
    "J" = [System.Drawing.Color]::FromArgb(255, 120, 132, 154)
    "T" = [System.Drawing.Color]::FromArgb(255, 123, 74, 27)
    "Y" = [System.Drawing.Color]::FromArgb(255, 219, 165, 72)
    "O" = [System.Drawing.Color]::FromArgb(255, 120, 70, 18)
    "P" = [System.Drawing.Color]::FromArgb(255, 189, 112, 38)
    "L" = [System.Drawing.Color]::FromArgb(255, 241, 232, 214)
}

$downTemplate = @(
    "....KKKK....",
    "...KRRRRK...",
    "..KRRRRRRK..",
    "..KHHHHRRK..",
    ".KHHSSSSHHK.",
    ".KHSSSSSSHK.",
    ".KHSKSSKSHK.",
    ".KHSSSSSSHK.",
    ".KHHSSSSHHK.",
    "..KBBBBBBK..",
    ".KBBDBBDBBK.",
    ".KBBLTTLBBK.",
    ".KBBTTTTBBK.",
    ".KBBCJJCBBK.",
    ".KBCJGGJCBK.",
    "..KOPPPOK.."
)

$upTemplate = @(
    "....KKKK....",
    "...KRRRRK...",
    "..KRRRRRRK..",
    "..KHHHHHHK..",
    ".KHHBBBBHHK.",
    ".KHBBBBBBHK.",
    ".KHBBBBBBHK.",
    ".KBBBBBBBBK.",
    ".KBBDBBDBBK.",
    ".KBBTTTTBBK.",
    ".KBBCTTCBBK.",
    ".KBBCJJCBBK.",
    ".KBCJGGJCBK.",
    ".KBBJGGJBBK.",
    "..KOPPPOK..",
    "..KOOOOKK..."
)

$leftTemplate = @(
    "....KKK.....",
    "...KRRRK....",
    "..KRRRRRK...",
    "..KHHRRRKK..",
    ".KHHSSSHHK..",
    ".KHSKSSHRK..",
    ".KHSSSHBBBK.",
    "..KHHBBBBBK.",
    ".KBBDBBBBBK.",
    ".KBBLTTTBBK.",
    ".KBBTTTTBBK.",
    ".KBBCJJGBK..",
    ".KBCJGGOOK..",
    "..KOPPPOK...",
    "..KOOOOK....",
    "...KKKK....."
)

function Get-MirroredTemplate {
    param([string[]]$Template)

    return $Template | ForEach-Object {
        $chars = $_.ToCharArray()
        [array]::Reverse($chars)
        -join $chars
    }
}

function Save-SpritePng {
    param(
        [string[]]$Template,
        [string]$Path
    )

    $scale = 3
    $width = ($Template | Measure-Object -Maximum Length).Maximum
    $height = $Template.Count
    $bitmap = New-Object System.Drawing.Bitmap ($width * $scale), ($height * $scale), ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None

    $brushes = @{}
    try {
        foreach ($key in $palette.Keys) {
            if ($key -eq ".") {
                continue
            }

            $brushes[$key] = New-Object System.Drawing.SolidBrush($palette[$key])
        }

        for ($y = 0; $y -lt $height; $y++) {
            $line = $Template[$y]
            for ($x = 0; $x -lt $width; $x++) {
                $token = if ($x -lt $line.Length) { [string]$line[$x] } else { "." }
                if ($token -eq ".") {
                    continue
                }

                $graphics.FillRectangle($brushes[$token], $x * $scale, $y * $scale, $scale, $scale)
            }
        }

        $directory = Split-Path -Parent $Path
        if (![string]::IsNullOrWhiteSpace($directory)) {
            [System.IO.Directory]::CreateDirectory($directory) | Out-Null
        }

        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        foreach ($brush in $brushes.Values) {
            $brush.Dispose()
        }

        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

$resolvedOutputDirectory = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $OutputDirectory))
Save-SpritePng -Template $leftTemplate -Path (Join-Path $resolvedOutputDirectory "hero_left.png")
Save-SpritePng -Template (Get-MirroredTemplate $leftTemplate) -Path (Join-Path $resolvedOutputDirectory "hero_right.png")
Save-SpritePng -Template $upTemplate -Path (Join-Path $resolvedOutputDirectory "hero_up.png")
Save-SpritePng -Template $downTemplate -Path (Join-Path $resolvedOutputDirectory "hero_down.png")

Write-Output "Generated hero_left.png, hero_right.png, hero_up.png, hero_down.png"
