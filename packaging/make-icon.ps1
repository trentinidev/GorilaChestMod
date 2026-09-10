# Composes packaging/icon.png, the 256x256 icon Thunderstore and r2modman require:
# the artwork in packaging/icon-source.png with the mod name across the top band
# and the author across the bottom one.
# Re-run after changing either:  powershell -ExecutionPolicy Bypass -File packaging\make-icon.ps1
Add-Type -AssemblyName System.Drawing

$source = Join-Path $PSScriptRoot 'icon-source.png'
$out    = Join-Path $PSScriptRoot 'icon.png'
$size   = 256

$title  = 'GorilaChestMod'
$author = 'by trentinidev'

# The two bands the text sits in, as top and height in pixels.
$topBand    = @{ Y = 0;   H = 36 }
$bottomBand = @{ Y = 214; H = 42 }

$ink   = [System.Drawing.Color]::FromArgb(255, 20, 20, 20)
$halo  = [System.Drawing.Color]::FromArgb(210, 255, 255, 255)
$band  = [System.Drawing.Color]::FromArgb(165, 255, 255, 255)

$bmp = New-Object System.Drawing.Bitmap($size, $size)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode     = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

$art = [System.Drawing.Image]::FromFile($source)
$g.DrawImage($art, 0, 0, $size, $size)
$art.Dispose()

# Lighten the bands so the lettering reads at gallery size.
$brushBand = New-Object System.Drawing.SolidBrush($band)
$g.FillRectangle($brushBand, 0, $topBand.Y, $size, $topBand.H)
$g.FillRectangle($brushBand, 0, $bottomBand.Y, $size, $bottomBand.H)

function Get-Family {
    foreach ($name in @('Arial Black', 'Segoe UI Black', 'Segoe UI', 'Tahoma', 'Arial')) {
        try { return New-Object System.Drawing.FontFamily($name) } catch { }
    }
    return [System.Drawing.FontFamily]::GenericSansSerif
}

$family = Get-Family

# Largest size that still fits the band with a margin on each side.
function New-FittedFont($text, $startSize, $maxWidth, $maxHeight) {
    for ($s = $startSize; $s -ge 6; $s--) {
        $font = New-Object System.Drawing.Font($family, $s, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
        $m = $g.MeasureString($text, $font)
        if ($m.Width -le $maxWidth -and $m.Height -le $maxHeight) { return $font }
        $font.Dispose()
    }
    return New-Object System.Drawing.Font($family, 6, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
}

# Drawn a few times in white underneath, so the letters hold up over busy art.
function Write-Centered($text, $font, $bandY, $bandH) {
    $m = $g.MeasureString($text, $font)
    $x = ($size - $m.Width) / 2
    $y = $bandY + ($bandH - $m.Height) / 2

    $brushHalo = New-Object System.Drawing.SolidBrush($halo)
    foreach ($dx in -1, 0, 1) {
        foreach ($dy in -1, 0, 1) {
            if ($dx -ne 0 -or $dy -ne 0) { $g.DrawString($text, $font, $brushHalo, ($x + $dx), ($y + $dy)) }
        }
    }
    $brushHalo.Dispose()

    $brushInk = New-Object System.Drawing.SolidBrush($ink)
    $g.DrawString($text, $font, $brushInk, $x, $y)
    $brushInk.Dispose()
}

$titleFont = New-FittedFont $title 26 ($size - 16) ($topBand.H - 4)
Write-Centered $title $titleFont $topBand.Y $topBand.H

$authorFont = New-FittedFont $author 20 ($size - 40) ($bottomBand.H - 6)
Write-Centered $author $authorFont $bottomBand.Y $bottomBand.H

"title font: $($titleFont.Size)px, author font: $($authorFont.Size)px"

$titleFont.Dispose()
$authorFont.Dispose()
$g.Dispose()

$bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

$check = [System.Drawing.Image]::FromFile($out)
"wrote $out  $($check.Width)x$($check.Height)"
$check.Dispose()
