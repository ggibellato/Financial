$ErrorActionPreference = 'Stop'

$iconXaml = @'
<DrawingImage xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
  <DrawingImage.Drawing>
    <DrawingGroup>
      <GeometryDrawing Brush="#1D6FB7">
        <GeometryDrawing.Pen>
          <Pen Brush="#0F4E87" Thickness="1" />
        </GeometryDrawing.Pen>
        <GeometryDrawing.Geometry>
          <RectangleGeometry Rect="2,6,20,14" RadiusX="2" RadiusY="2" />
        </GeometryDrawing.Geometry>
      </GeometryDrawing>
      <GeometryDrawing Brush="#2A82C6">
        <GeometryDrawing.Geometry>
          <RectangleGeometry Rect="2,4,20,4" RadiusX="2" RadiusY="2" />
        </GeometryDrawing.Geometry>
      </GeometryDrawing>
      <GeometryDrawing Brush="#F2C94C">
        <GeometryDrawing.Pen>
          <Pen Brush="#C9971E" Thickness="1" />
        </GeometryDrawing.Pen>
        <GeometryDrawing.Geometry>
          <EllipseGeometry Center="18,13" RadiusX="3" RadiusY="3" />
        </GeometryDrawing.Geometry>
      </GeometryDrawing>
      <GeometryDrawing>
        <GeometryDrawing.Pen>
          <Pen Brush="#0C2F4F" Thickness="2" />
        </GeometryDrawing.Pen>
        <GeometryDrawing.Geometry>
          <PathGeometry Figures="M6,18 L9,14 L12,16 L16,11" />
        </GeometryDrawing.Geometry>
      </GeometryDrawing>
    </DrawingGroup>
  </DrawingImage.Drawing>
</DrawingImage>
'@

Add-Type -AssemblyName PresentationCore,PresentationFramework,WindowsBase,System.Drawing

$reader = New-Object System.Xml.XmlNodeReader ([xml]$iconXaml)
$drawingImage = [System.Windows.Markup.XamlReader]::Load($reader)

$sizes = @(16, 32, 48, 64, 128, 256)
$pngFrames = New-Object System.Collections.Generic.List[byte[]]

foreach ($size in $sizes) {
    $dv = New-Object System.Windows.Media.DrawingVisual
    $dc = $dv.RenderOpen()
    $dc.DrawDrawing($drawingImage.Drawing)
    $dc.Close()

    $rtb = New-Object System.Windows.Media.Imaging.RenderTargetBitmap($size, $size, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
    $rtb.Render($dv)

    $pngStream = New-Object System.IO.MemoryStream
    $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($rtb))
    $encoder.Save($pngStream)
    $pngFrames.Add($pngStream.ToArray())
}

$projectRoot = Split-Path -Parent $PSScriptRoot
$icoPath = Join-Path $projectRoot "app.ico"
$fs = [System.IO.File]::Open($icoPath, [System.IO.FileMode]::Create)

$bw = New-Object System.IO.BinaryWriter($fs)
$bw.Write([UInt16]0)
$bw.Write([UInt16]1)
$bw.Write([UInt16]$sizes.Count)

$dirEntrySize = 16
$dataOffset = 6 + ($sizes.Count * $dirEntrySize)

for ($i = 0; $i -lt $sizes.Count; $i++) {
    $size = $sizes[$i]
    $png = $pngFrames[$i]

    $bw.Write([Byte]([Math]::Min($size, 256) % 256))
    $bw.Write([Byte]([Math]::Min($size, 256) % 256))
    $bw.Write([Byte]0)
    $bw.Write([Byte]0)
    $bw.Write([UInt16]1)
    $bw.Write([UInt16]32)
    $bw.Write([UInt32]$png.Length)
    $bw.Write([UInt32]$dataOffset)

    $dataOffset += $png.Length
}

foreach ($png in $pngFrames) {
    $bw.Write($png)
}

$bw.Flush()
$bw.Close()
$fs.Close()

Write-Output "Wrote $icoPath with sizes: $($sizes -join ', ')"
