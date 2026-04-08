Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

function New-RoundedRectanglePath {
    param(
        [float]$X,
        [float]$Y,
        [float]$Width,
        [float]$Height,
        [float]$Radius
    )

    $diameter = $Radius * 2
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $path.AddArc($X, $Y, $diameter, $diameter, 180, 90)
    $path.AddArc($X + $Width - $diameter, $Y, $diameter, $diameter, 270, 90)
    $path.AddArc($X + $Width - $diameter, $Y + $Height - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($X, $Y + $Height - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-DragonBitmap {
    param([int]$Size)

    $bitmap = [System.Drawing.Bitmap]::new($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.Clear([System.Drawing.Color]::Transparent)

    $scale = $Size / 256.0
    $backgroundPath = New-RoundedRectanglePath -X (16 * $scale) -Y (16 * $scale) -Width (224 * $scale) -Height (224 * $scale) -Radius (42 * $scale)
    $silhouette = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $silhouette.AddPolygon([System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new(72 * $scale, 94 * $scale),
        [System.Drawing.PointF]::new(104 * $scale, 56 * $scale),
        [System.Drawing.PointF]::new(142 * $scale, 54 * $scale),
        [System.Drawing.PointF]::new(172 * $scale, 84 * $scale),
        [System.Drawing.PointF]::new(156 * $scale, 94 * $scale),
        [System.Drawing.PointF]::new(194 * $scale, 122 * $scale),
        [System.Drawing.PointF]::new(164 * $scale, 130 * $scale),
        [System.Drawing.PointF]::new(186 * $scale, 166 * $scale),
        [System.Drawing.PointF]::new(154 * $scale, 170 * $scale),
        [System.Drawing.PointF]::new(132 * $scale, 202 * $scale),
        [System.Drawing.PointF]::new(104 * $scale, 186 * $scale),
        [System.Drawing.PointF]::new(92 * $scale, 154 * $scale),
        [System.Drawing.PointF]::new(64 * $scale, 146 * $scale),
        [System.Drawing.PointF]::new(76 * $scale, 118 * $scale)
    ))

    $eyeCutout = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $eyeCutout.AddPolygon([System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new(112 * $scale, 108 * $scale),
        [System.Drawing.PointF]::new(130 * $scale, 100 * $scale),
        [System.Drawing.PointF]::new(146 * $scale, 112 * $scale),
        [System.Drawing.PointF]::new(126 * $scale, 124 * $scale)
    ))

    $jawCutout = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $jawCutout.AddPolygon([System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new(148 * $scale, 132 * $scale),
        [System.Drawing.PointF]::new(174 * $scale, 124 * $scale),
        [System.Drawing.PointF]::new(156 * $scale, 144 * $scale)
    ))

    $shadowColor = [System.Drawing.Color]::FromArgb(36, 36, 40)
    $shapeColor = [System.Drawing.Color]::FromArgb(47, 52, 58)
    $backgroundColor = [System.Drawing.Color]::FromArgb(230, 233, 237)
    $borderColor = [System.Drawing.Color]::FromArgb(194, 199, 204)

    $graphics.FillPath([System.Drawing.SolidBrush]::new($backgroundColor), $backgroundPath)
    $graphics.DrawPath([System.Drawing.Pen]::new($borderColor, [Math]::Max(2, 5 * $scale)), $backgroundPath)

    $shadowMatrix = [System.Drawing.Drawing2D.Matrix]::new()
    $shadowMatrix.Translate(6 * $scale, 6 * $scale)
    $shadowPath = $silhouette.Clone()
    $shadowPath.Transform($shadowMatrix)
    $graphics.FillPath([System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(32, $shadowColor)), $shadowPath)

    $graphics.FillPath([System.Drawing.SolidBrush]::new($shapeColor), $silhouette)
    $graphics.FillPath([System.Drawing.SolidBrush]::new($backgroundColor), $eyeCutout)
    $graphics.FillPath([System.Drawing.SolidBrush]::new($backgroundColor), $jawCutout)

    $shadowPath.Dispose()
    $shadowMatrix.Dispose()
    $jawCutout.Dispose()
    $eyeCutout.Dispose()
    $silhouette.Dispose()
    $backgroundPath.Dispose()
    $graphics.Dispose()
    return $bitmap
}

$root = Split-Path -Parent $PSScriptRoot
$outputDirectory = Join-Path $root 'Resources\App'
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null

$previewPath = Join-Path $outputDirectory 'dragon_glare-preview.png'
$iconPath = Join-Path $outputDirectory 'dragon_glare.ico'
$sizes = 16, 24, 32, 48, 64, 128, 256
$images = [System.Collections.Generic.List[object]]::new()

foreach ($size in $sizes) {
    $bitmap = New-DragonBitmap -Size $size
    $stream = [System.IO.MemoryStream]::new()
    $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $images.Add([pscustomobject]@{
        Size = $size
        Data = $stream.ToArray()
    }) | Out-Null

    if ($size -eq 256) {
        $bitmap.Save($previewPath, [System.Drawing.Imaging.ImageFormat]::Png)
    }

    $stream.Dispose()
    $bitmap.Dispose()
}

$fileStream = [System.IO.File]::Open($iconPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
$writer = [System.IO.BinaryWriter]::new($fileStream)

$writer.Write([uint16]0)
$writer.Write([uint16]1)
$writer.Write([uint16]$images.Count)

$offset = 6 + (16 * $images.Count)
foreach ($image in $images) {
    $dimension = if ($image.Size -ge 256) { [byte]0 } else { [byte]$image.Size }
    $writer.Write($dimension)
    $writer.Write($dimension)
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]32)
    $writer.Write([uint32]$image.Data.Length)
    $writer.Write([uint32]$offset)
    $offset += $image.Data.Length
}

foreach ($image in $images) {
    $writer.Write($image.Data)
}

$writer.Dispose()
$fileStream.Dispose()

Write-Output "Created icon: $iconPath"
Write-Output "Created preview: $previewPath"
