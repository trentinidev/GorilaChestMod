# Draws packaging/icon.png, the 256x256 icon Thunderstore and r2modman require.
# Tweak the palette below and re-run:  powershell -ExecutionPolicy Bypass -File packaging\make-icon.ps1
Add-Type -AssemblyName System.Drawing

$out = Join-Path $PSScriptRoot 'icon.png'
$size = 256

$bg       = [System.Drawing.Color]::FromArgb(255, 26, 32, 43)
$bgEdge   = [System.Drawing.Color]::FromArgb(255, 16, 20, 28)
$woodDark = [System.Drawing.Color]::FromArgb(255, 106, 66, 33)
$wood     = [System.Drawing.Color]::FromArgb(255, 140, 90, 44)
$woodLid  = [System.Drawing.Color]::FromArgb(255, 163, 106, 55)
$iron     = [System.Drawing.Color]::FromArgb(255, 58, 63, 75)
$ironLite = [System.Drawing.Color]::FromArgb(255, 96, 104, 120)
$gold     = [System.Drawing.Color]::FromArgb(255, 217, 164, 65)

$bmp = New-Object System.Drawing.Bitmap($size, $size)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias

# background with a soft vignette
$g.Clear($bg)
$vig = New-Object System.Drawing.Drawing2D.GraphicsPath
$vig.AddEllipse(-60, -60, $size + 120, $size + 120)
$brushVig = New-Object System.Drawing.Drawing2D.PathGradientBrush($vig)
$brushVig.CenterColor = $bg
$brushVig.SurroundColors = @($bgEdge)
$g.FillRectangle($brushVig, 0, 0, $size, $size)

function New-Brush($c) { New-Object System.Drawing.SolidBrush($c) }
function New-Pen($c, $w) { New-Object System.Drawing.Pen($c, $w) }

# ---- crafting hammer, tilted, sitting above the chest
$state = $g.Save()
$g.TranslateTransform(150, 74)
$g.RotateTransform(32)
$g.FillRectangle((New-Brush $woodDark), -7, -6, 14, 74)      # handle
$g.FillRectangle((New-Brush $wood), -7, -6, 5, 74)           # handle highlight
$g.FillRectangle((New-Brush $iron), -40, -30, 80, 30)        # head
$g.FillRectangle((New-Brush $ironLite), -40, -30, 80, 8)     # head highlight
$g.FillRectangle((New-Brush $bgEdge), -44, -26, 5, 22)       # head edge shadow
$g.Restore($state)

# ---- chest
$bodyX = 40; $bodyY = 148; $bodyW = 176; $bodyH = 68
$lidX  = 40; $lidY  = 108; $lidW  = 176; $lidH  = 80

$g.FillPie((New-Brush $woodLid), $lidX, $lidY, $lidW, $lidH, 180, 180)   # arched lid
$g.FillRectangle((New-Brush $wood), $bodyX, $bodyY, $bodyW, $bodyH)      # body

# plank seams
$penSeam = New-Pen $woodDark 3
foreach ($x in @(78, 116, 154, 192)) { $g.DrawLine($penSeam, $x, ($bodyY + 6), $x, ($bodyY + $bodyH - 6)) }

# iron bands
$g.FillRectangle((New-Brush $iron), $bodyX, ($bodyY - 8), $bodyW, 16)    # seam band
$g.FillRectangle((New-Brush $iron), 60, $bodyY, 14, $bodyH)
$g.FillRectangle((New-Brush $iron), 182, $bodyY, 14, $bodyH)
$g.DrawArc((New-Pen $iron 13), ($lidX + 8), ($lidY + 6), ($lidW - 16), ($lidH - 4), 180, 180)

# gold latch
$g.FillRectangle((New-Brush $gold), 116, ($bodyY - 14), 24, 30)
$g.FillEllipse((New-Brush $bgEdge), 123, ($bodyY - 2), 10, 10)

# ---- sparks flowing from the chest up into the hammer
$g.FillEllipse((New-Brush $gold), 92, 92, 12, 12)
$g.FillEllipse((New-Brush $gold), 74, 116, 8, 8)
$g.FillEllipse((New-Brush $gold), 108, 66, 7, 7)

$g.Dispose()
$bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

$check = [System.Drawing.Image]::FromFile($out)
"wrote $out  $($check.Width)x$($check.Height)"
$check.Dispose()
