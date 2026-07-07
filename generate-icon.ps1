<#
.SYNOPSIS
    Generates the DacPac Viewer app icon (database + magnifying glass).

.DESCRIPTION
    Draws the icon with System.Drawing at multiple resolutions and writes:
      - source\DacPac.UI\Assets\app-icon.png  (256x256)
      - source\DacPac.UI\Assets\app-icon.ico  (16..256, PNG-compressed entries)
#>
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$assets = Join-Path $PSScriptRoot "source\DacPac.UI\Assets"

function New-IconBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    # Design at 256, scale down so every size is rendered natively (crisper than resizing)
    $scale = $size / 256.0
    $g.ScaleTransform($scale, $scale)

    # Rounded-square background with vertical blue gradient
    $r = 52
    $bg = New-Object System.Drawing.Drawing2D.GraphicsPath
    $bg.AddArc(0, 0, 2 * $r, 2 * $r, 180, 90)
    $bg.AddArc(256 - 2 * $r, 0, 2 * $r, 2 * $r, 270, 90)
    $bg.AddArc(256 - 2 * $r, 256 - 2 * $r, 2 * $r, 2 * $r, 0, 90)
    $bg.AddArc(0, 256 - 2 * $r, 2 * $r, 2 * $r, 90, 90)
    $bg.CloseFigure()
    $top = [System.Drawing.Color]::FromArgb(255, 59, 130, 246)   # blue-500
    $bottom = [System.Drawing.Color]::FromArgb(255, 30, 58, 138) # blue-900
    $bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.Point(0, 0)),
        (New-Object System.Drawing.Point(0, 256)),
        $top, $bottom)
    $g.FillPath($bgBrush, $bg)

    $white = [System.Drawing.Color]::White
    $stroke = New-Object System.Drawing.Pen($white, 14)
    $stroke.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $stroke.EndCap = [System.Drawing.Drawing2D.LineCap]::Round

    # Database cylinder (outline style)
    $x = 52; $w = 116; $eh = 40
    $topY = 44; $botY = 152
    $g.DrawEllipse($stroke, $x, $topY, $w, $eh)                       # top rim
    $g.DrawLine($stroke, $x, $topY + $eh / 2, $x, $botY + $eh / 2)    # left side
    $g.DrawLine($stroke, $x + $w, $topY + $eh / 2, $x + $w, $botY + $eh / 2) # right side
    $g.DrawArc($stroke, $x, $botY, $w, $eh, 0, 180)                   # bottom rim
    $g.DrawArc($stroke, $x, 96, $w, $eh, 0, 180)                      # middle band

    # Magnifying glass, bottom right, overlapping the cylinder
    $lensCx = 176; $lensCy = 172; $lensR = 40
    $lensRect = New-Object System.Drawing.RectangleF(($lensCx - $lensR), ($lensCy - $lensR), (2 * $lensR), (2 * $lensR))
    # Punch out the area behind the lens so it reads as a separate object
    $cut = New-Object System.Drawing.SolidBrush($bottom)
    $g.FillEllipse($cut, $lensRect)
    $g.DrawEllipse($stroke, $lensRect)
    $handle = New-Object System.Drawing.Pen($white, 20)
    $handle.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $handle.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $d = [math]::Sqrt(2) / 2 * $lensR
    $g.DrawLine($handle, $lensCx + $d + 4, $lensCy + $d + 4, 232, 228)

    $g.Dispose()
    return $bmp
}

# PNG (256)
$png256 = New-IconBitmap 256
$pngPath = Join-Path $assets "app-icon.png"
$png256.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Host "Wrote $pngPath"

# ICO with PNG-compressed entries
$sizes = 16, 24, 32, 48, 64, 128, 256
$entries = @()
foreach ($s in $sizes) {
    $bmp = New-IconBitmap $s
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $entries += , @{ Size = $s; Bytes = $ms.ToArray() }
    $bmp.Dispose()
}

$out = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($out)
$bw.Write([uint16]0)                 # reserved
$bw.Write([uint16]1)                 # type: icon
$bw.Write([uint16]$entries.Count)
$offset = 6 + 16 * $entries.Count
foreach ($e in $entries) {
    $dim = if ($e.Size -ge 256) { 0 } else { $e.Size }  # 0 means 256
    $bw.Write([byte]$dim); $bw.Write([byte]$dim)
    $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([uint16]1)             # color planes
    $bw.Write([uint16]32)            # bits per pixel
    $bw.Write([uint32]$e.Bytes.Length)
    $bw.Write([uint32]$offset)
    $offset += $e.Bytes.Length
}
foreach ($e in $entries) { $bw.Write($e.Bytes) }
$icoPath = Join-Path $assets "app-icon.ico"
[System.IO.File]::WriteAllBytes($icoPath, $out.ToArray())
Write-Host "Wrote $icoPath ($($entries.Count) sizes: $($sizes -join ', '))"
