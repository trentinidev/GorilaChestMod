# Draws packaging/icon.png, the 256x256 icon Thunderstore and r2modman require.
# The badge is derived from the Gorila Holdings mark: black roundel, gorilla bust
# with a light face mask, and the brand orange carried by the chest.
# Tweak and re-run:  powershell -ExecutionPolicy Bypass -File packaging\make-icon.ps1
Add-Type -AssemblyName System.Drawing

$out = Join-Path $PSScriptRoot 'icon.png'
$size = 256

$paper  = [System.Drawing.Color]::FromArgb(255, 246, 246, 244)
$ink    = [System.Drawing.Color]::FromArgb(255, 24, 24, 24)
$face   = [System.Drawing.Color]::FromArgb(255, 246, 246, 244)
$orange = [System.Drawing.Color]::FromArgb(255, 247, 124, 20)
$orangeDark = [System.Drawing.Color]::FromArgb(255, 198, 92, 8)

$bmp = New-Object System.Drawing.Bitmap($size, $size)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear($paper)

function New-Brush($c) { New-Object System.Drawing.SolidBrush($c) }
function New-Pen($c, $w) { New-Object System.Drawing.Pen($c, $w) }

# ---- roundel
$ringBox = New-Object System.Drawing.Rectangle(34, 8, 188, 188)
$ringWidth = 14
$g.DrawEllipse((New-Pen $ink $ringWidth), $ringBox)

# everything inside the roundel is clipped to it, like the source mark
$inner = New-Object System.Drawing.Drawing2D.GraphicsPath
$inner.AddEllipse($ringBox.X + $ringWidth, $ringBox.Y + $ringWidth, $ringBox.Width - 2 * $ringWidth, $ringBox.Height - 2 * $ringWidth)
$state = $g.Save()
$g.SetClip($inner)

# ---- gorilla bust, built from overlapping shapes filled in one colour
$brushInk = New-Brush $ink
$g.FillEllipse($brushInk, 68, 28, 120, 118)     # skull
$g.FillEllipse($brushInk, 38, 62, 68, 116)       # left mane
$g.FillEllipse($brushInk, 150, 62, 68, 116)      # right mane
$g.FillEllipse($brushInk, 26, 110, 204, 150)    # shoulders

# ---- face mask
$brushFace = New-Brush $face
$g.FillEllipse($brushFace, 96, 68, 64, 56)       # brow and eye area
$g.FillEllipse($brushFace, 100, 96, 56, 62)      # muzzle

# ---- brow ridge
$g.FillEllipse($brushInk, 100, 82, 26, 13)
$g.FillEllipse($brushInk, 130, 82, 26, 13)
$g.FillRectangle($brushInk, 106, 82, 44, 13)

# ---- eyes
$g.FillEllipse($brushInk, 106, 97, 17, 12)
$g.FillEllipse($brushInk, 133, 97, 17, 12)

# ---- nose
$g.FillEllipse($brushInk, 114, 118, 11, 9)
$g.FillEllipse($brushInk, 131, 118, 11, 9)
$g.FillRectangle($brushInk, 120, 121, 16, 5)

# ---- downturned mouth
$penMouth = New-Pen $ink 6
$penMouth.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$penMouth.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$g.DrawArc($penMouth, 112, 132, 32, 22, 200, 140)

$g.Restore($state)

# ---- chest across the bottom, carrying the brand orange
$bodyX = 58; $bodyY = 212; $bodyW = 140; $bodyH = 34
$g.FillPie((New-Brush $orange), $bodyX, 192, $bodyW, 42, 180, 180)
$g.FillRectangle((New-Brush $orange), $bodyX, $bodyY, $bodyW, $bodyH)

$g.FillRectangle((New-Brush $orangeDark), $bodyX, ($bodyY - 6), $bodyW, 12)
$g.FillRectangle((New-Brush $ink), 74, $bodyY, 9, $bodyH)
$g.FillRectangle((New-Brush $ink), 173, $bodyY, 9, $bodyH)
$g.FillRectangle((New-Brush $ink), 120, ($bodyY - 9), 16, 20)
$g.FillEllipse((New-Brush $orange), 124, ($bodyY - 1), 8, 8)

$g.Dispose()
$bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

$check = [System.Drawing.Image]::FromFile($out)
"wrote $out  $($check.Width)x$($check.Height)"
$check.Dispose()
