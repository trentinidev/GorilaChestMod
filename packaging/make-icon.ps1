# Draws packaging/icon.png, the 256x256 icon Thunderstore and r2modman require.
# Tweak the palette below and re-run:  powershell -ExecutionPolicy Bypass -File packaging\make-icon.ps1
Add-Type -AssemblyName System.Drawing

$out = Join-Path $PSScriptRoot 'icon.png'
$size = 256

$bg       = [System.Drawing.Color]::FromArgb(255, 26, 32, 43)
$bgEdge   = [System.Drawing.Color]::FromArgb(255, 15, 19, 26)
$furDark  = [System.Drawing.Color]::FromArgb(255, 44, 46, 54)
$fur      = [System.Drawing.Color]::FromArgb(255, 70, 74, 86)
$face     = [System.Drawing.Color]::FromArgb(255, 112, 100, 96)
$faceLite = [System.Drawing.Color]::FromArgb(255, 138, 124, 118)
$woodDark = [System.Drawing.Color]::FromArgb(255, 106, 66, 33)
$wood     = [System.Drawing.Color]::FromArgb(255, 140, 90, 44)
$woodLid  = [System.Drawing.Color]::FromArgb(255, 163, 106, 55)
$iron     = [System.Drawing.Color]::FromArgb(255, 58, 63, 75)
$gold     = [System.Drawing.Color]::FromArgb(255, 217, 164, 65)

$bmp = New-Object System.Drawing.Bitmap($size, $size)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias

function New-Brush($c) { New-Object System.Drawing.SolidBrush($c) }
function New-Pen($c, $w) { New-Object System.Drawing.Pen($c, $w) }

# background with a soft vignette
$g.Clear($bg)
$vig = New-Object System.Drawing.Drawing2D.GraphicsPath
$vig.AddEllipse(-60, -60, $size + 120, $size + 120)
$brushVig = New-Object System.Drawing.Drawing2D.PathGradientBrush($vig)
$brushVig.CenterColor = $bg
$brushVig.SurroundColors = @($bgEdge)
$g.FillRectangle($brushVig, 0, 0, $size, $size)

# ---- gorilla head, upper two thirds
$g.FillEllipse((New-Brush $furDark), 36, 44, 40, 44)      # left ear
$g.FillEllipse((New-Brush $furDark), 180, 44, 40, 44)     # right ear
$g.FillEllipse((New-Brush $fur), 44, 52, 24, 28)          # ear inner
$g.FillEllipse((New-Brush $fur), 188, 52, 24, 28)

$g.FillEllipse((New-Brush $fur), 56, 20, 144, 132)        # skull
$g.FillEllipse((New-Brush $furDark), 70, 26, 116, 46)     # brow ridge shadow

$g.FillEllipse((New-Brush $face), 82, 74, 92, 76)         # face
$g.FillEllipse((New-Brush $faceLite), 96, 104, 64, 44)    # muzzle

$g.FillEllipse((New-Brush $furDark), 100, 82, 20, 18)     # eyes
$g.FillEllipse((New-Brush $furDark), 136, 82, 20, 18)
$g.FillEllipse((New-Brush $bgEdge), 105, 86, 9, 9)
$g.FillEllipse((New-Brush $bgEdge), 141, 86, 9, 9)

$g.FillEllipse((New-Brush $furDark), 114, 112, 9, 7)      # nostrils
$g.FillEllipse((New-Brush $furDark), 133, 112, 9, 7)
$g.DrawArc((New-Pen $furDark 4), 112, 122, 32, 16, 20, 140)  # mouth

# ---- chest across the bottom, partly behind the head
$bodyX = 28; $bodyY = 186; $bodyW = 200; $bodyH = 54
$g.FillPie((New-Brush $woodLid), $bodyX, 154, $bodyW, 70, 180, 180)
$g.FillRectangle((New-Brush $wood), $bodyX, $bodyY, $bodyW, $bodyH)

$penSeam = New-Pen $woodDark 3
foreach ($x in @(74, 118, 162)) { $g.DrawLine($penSeam, $x, ($bodyY + 4), $x, ($bodyY + $bodyH - 4)) }

$g.FillRectangle((New-Brush $iron), $bodyX, ($bodyY - 7), $bodyW, 14)
$g.FillRectangle((New-Brush $iron), 48, $bodyY, 13, $bodyH)
$g.FillRectangle((New-Brush $iron), 195, $bodyY, 13, $bodyH)
$g.FillRectangle((New-Brush $gold), 116, ($bodyY - 13), 24, 28)
$g.FillEllipse((New-Brush $bgEdge), 123, ($bodyY - 1), 10, 10)

$g.Dispose()
$bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

$check = [System.Drawing.Image]::FromFile($out)
"wrote $out  $($check.Width)x$($check.Height)"
$check.Dispose()
