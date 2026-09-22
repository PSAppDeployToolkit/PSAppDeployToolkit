# ImageDialog.ps1 - A message dialog with a branding image above centered text, placed in the
# bottom-right corner of the work area. The image is drawn into a temporary PNG so the example has no
# external asset; pass any .png, .jpg or pack: URI of your own to -Image.
# Run: pwsh -File ImageDialog.ps1   OR   powershell.exe -File ImageDialog.ps1

Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force

Add-Type -AssemblyName System.Drawing
$png = Join-Path ([System.IO.Path]::GetTempPath()) 'fluence-example-logo.png'
$bitmap = [System.Drawing.Bitmap]::new(240, 80)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
try
{
    $graphics.Clear([System.Drawing.Color]::FromArgb(0, 120, 212))
    $font = [System.Drawing.Font]::new('Segoe UI', 24, [System.Drawing.FontStyle]::Bold)
    $graphics.DrawString('Contoso', $font, [System.Drawing.Brushes]::White, 24, 18)
    $font.Dispose()
    $bitmap.Save($png, [System.Drawing.Imaging.ImageFormat]::Png)
}
finally
{
    $graphics.Dispose()
    $bitmap.Dispose()
}

$answer = Show-FluenceMessage -Title 'Contoso Suite' -Message 'Contoso Suite is ready to install.', 'Close your documents before continuing.' -Icon None -Image $png -MessageAlignment Center -Position BottomRight -Buttons OKCancel

Write-Output "Answer: $answer"
Remove-Item -LiteralPath $png -Force -ErrorAction SilentlyContinue
