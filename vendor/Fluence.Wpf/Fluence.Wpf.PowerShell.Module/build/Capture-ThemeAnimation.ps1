<#
Copyright (c) 2026, Dan Cunningham. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice,
   this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#>


<#
.SYNOPSIS
    Captures a 31-second theme and accent sequence from a live PowerShell FluenceWindow.
.DESCRIPTION
    Opens a compact control sample and renders its WPF visual to 20 PNG frames per second.
    A Light, Dark, Light theme prelude uses the Windows system accent. Seven named accent
    hues then last 1.5 seconds each in Light and repeat in Dark. High Contrast with the
    system accent follows for 5 seconds. Frame 610 returns to Light with the system accent;
    frames 0-609 form the loop. Extra frames record the return state. Screen composition
    can lag the requested theme change, so frame 610 may differ from frame zero.
    The library supplies any visible theme transition; this script does not synthesize a fade.
    Captures the visible window's screen pixels within its WPF window bounds.
.PARAMETER OutputDirectory
    Destination for frames, posters, and timeline.json. Defaults to artifacts/theme-animation.
.PARAMETER KeepDesktop
    Skip the default reversible minimization of other desktop windows during capture.
.EXAMPLE
    powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File build/Capture-ThemeAnimation.ps1
#>
[CmdletBinding()]
[OutputType([void])]
param
(
    [Parameter()]
    [string]$OutputDirectory,

    [Parameter()]
    [switch]$KeepDesktop
)

$ErrorActionPreference = 'Stop'
if ([System.Threading.Thread]::CurrentThread.GetApartmentState() -ne [System.Threading.ApartmentState]::STA)
{
    throw 'Capture-ThemeAnimation.ps1 requires an STA host. Run with powershell.exe -STA.'
}

$repo = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$modulePath = Join-Path $repo 'Fluence.Wpf.PowerShell.Module\src\Fluence.Wpf.PowerShell\Fluence.Wpf.PowerShell.psd1'
$libraryPath = Join-Path $repo 'Fluence.Wpf.PowerShell.Module\src\Fluence.Wpf.PowerShell\lib\net472\Fluence.Wpf.dll'
$iconPath = Join-Path $repo 'assets\Fluence_Icon.ico'
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repo 'artifacts\theme-animation' }
$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
$framesDirectory = Join-Path $OutputDirectory 'frames'
$null = [System.IO.Directory]::CreateDirectory($framesDirectory)

Import-Module $modulePath -Force
Add-Type -AssemblyName System.Drawing

$hues = @(
    [pscustomobject]@{ Name = 'Red'; Color = [System.Windows.Media.Color]::FromRgb(0xC4, 0x2B, 0x1C) }
    [pscustomobject]@{ Name = 'Orange'; Color = [System.Windows.Media.Color]::FromRgb(0xF7, 0x63, 0x0C) }
    [pscustomobject]@{ Name = 'Yellow'; Color = [System.Windows.Media.Color]::FromRgb(0xFF, 0xB9, 0x00) }
    [pscustomobject]@{ Name = 'Green'; Color = [System.Windows.Media.Color]::FromRgb(0x10, 0x7C, 0x10) }
    [pscustomobject]@{ Name = 'Blue'; Color = [System.Windows.Media.Color]::FromRgb(0x00, 0x78, 0xD4) }
    [pscustomobject]@{ Name = 'Indigo'; Color = [System.Windows.Media.Color]::FromRgb(0x4F, 0x6B, 0xED) }
    [pscustomobject]@{ Name = 'Violet'; Color = [System.Windows.Media.Color]::FromRgb(0x74, 0x37, 0xC9) }
)
$steps = @(
    [pscustomobject]@{ Frame = 0; Theme = 'Light'; Accent = 'System'; Color = $null }
    [pscustomobject]@{ Frame = 30; Theme = 'Dark'; Accent = 'System'; Color = $null }
    [pscustomobject]@{ Frame = 60; Theme = 'Light'; Accent = 'System'; Color = $null }
)
foreach ($themeName in @('Light', 'Dark'))
{
    $cycle = if ($themeName -eq 'Light') { 0 } else { 1 }
    for ($hueIndex = 0; $hueIndex -lt $hues.Count; $hueIndex++)
    {
        $steps += [pscustomobject]@{
            Frame = 90 + ((($cycle * 7) + $hueIndex) * 30)
            Theme = $themeName
            Accent = $hues[$hueIndex].Name
            Color = $hues[$hueIndex].Color
        }
    }
}
$steps += [pscustomobject]@{ Frame = 510; Theme = 'HighContrast'; Accent = 'System'; Color = $null }
$steps += [pscustomobject]@{ Frame = 610; Theme = 'Light'; Accent = 'System'; Color = $null }

$xaml = @'
<fluence:FluenceWindow
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:fluence="http://schemas.fluencewpf.com"
    Title="Fluence.Wpf"
    Width="500"
    Height="340"
    ResizeMode="NoResize"
    ExtendsContentIntoTitleBar="False"
    SystemBackdropType="None"
    Topmost="True"
    SnapsToDevicePixels="True"
    UseLayoutRounding="True"
    Background="{DynamicResource ApplicationBackgroundBrush}">
    <Grid Margin="32">
        <StackPanel VerticalAlignment="Center">
            <TextBlock Text="Fluent controls adapt to your theme."
                       fluence:TextBlockExtensions.Typography="BodyStrong"
                       Foreground="{DynamicResource TextFillColorPrimaryBrush}" />
            <Grid Margin="0,18,0,18">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="12" />
                    <ColumnDefinition Width="*" />
                </Grid.ColumnDefinitions>
                <fluence:Button Grid.Column="0" Content="Primary action" Appearance="Accent" HorizontalAlignment="Stretch" FontSize="16" MinHeight="42" Padding="18,8" />
                <fluence:Button Grid.Column="2" Content="Secondary action" HorizontalAlignment="Stretch" FontSize="16" MinHeight="42" Padding="18,8" />
            </Grid>
            <Grid Margin="0,0,0,18">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="12" />
                    <ColumnDefinition Width="*" />
                </Grid.ColumnDefinitions>
                <fluence:CheckBox Grid.Column="0" Content="Remember choice" IsChecked="True" FontSize="14" />
                <fluence:ToggleSwitch Grid.Column="2" Content="Enabled" IsChecked="True" OnContent="On" OffContent="Off" FontSize="14" />
            </Grid>
            <fluence:Slider Minimum="0" Maximum="100" Value="64" AutomationProperties.Name="Accent level" Margin="0,0,0,18" />
            <fluence:ProgressBar x:Name="ProgressIndicator" IsIndeterminate="True" Height="8" AutomationProperties.Name="Progress" />
        </StackPanel>
    </Grid>
</fluence:FluenceWindow>
'@

$captureData = @{
    OutputDirectory = $OutputDirectory
    FramesDirectory = $framesDirectory
    Frames = [System.Collections.Generic.List[object]]::new()
    NextFrame = 0
    Failure = $null
    Clock = $null
    CurrentTheme = 'Light'
    CurrentAccent = 'System'
    Steps = $steps
    ProgressBounds = $null
    ProgressTranslate = $null
    ProgressTranslate2 = $null
}

Set-FluenceAccent -System
$desktopStatePath = Join-Path ([System.IO.Path]::GetTempPath()) ('fluence-theme-desktop-{0}.json' -f [guid]::NewGuid().ToString('N'))
$desktopState = [ordered]@{
    capture = 'Capture-ThemeAnimation.ps1'
    startedUtc = [DateTime]::UtcNow.ToString('o')
    minimizeRequested = -not $KeepDesktop
    minimizeAttempted = $false
    minimized = $false
    restoreAttempted = $false
    restored = $false
    accentReset = $false
    captureError = $null
    restoreError = $null
    accentResetError = $null
}
$desktopShell = $null
[System.IO.File]::WriteAllText($desktopStatePath, (($desktopState | ConvertTo-Json) + "`n"), [System.Text.UTF8Encoding]::new($false))
try
{
if (-not $KeepDesktop)
{
    $desktopShell = New-Object -ComObject Shell.Application
    $desktopState.minimizeAttempted = $true
    $desktopShell.MinimizeAll()
    $desktopState.minimized = $true
}
$null = Show-FluenceWindow -Xaml $xaml -Theme Light -Backdrop None -TitleBarIcon $iconPath -Data $captureData -Initialize {
    param($Window, $Data)

    if ($null -eq $Data) { throw 'Capture state was not provided.' }

    $periodMilliseconds = 50
    $lastFrame = 618
    $captureFrame = {
        param([int]$index)

        foreach ($step in $Data.Steps)
        {
            if ($step.Frame -ne $index) { continue }
            if ($step.Theme -ne $Data.CurrentTheme)
            {
                Set-FluenceTheme -Theme $step.Theme
                $Data.CurrentTheme = $step.Theme
            }
            if ($step.Accent -ne $Data.CurrentAccent)
            {
                if ($step.Accent -eq 'System') { Set-FluenceAccent -System }
                else { Set-FluenceAccent -Color $step.Color }
                $Data.CurrentAccent = $step.Accent
            }
            break
        }
        $Window.UpdateLayout()

        if ($index -eq 0)
        {
            $progress = $Window.FindName('ProgressIndicator')
            if ($progress -isnot [System.Windows.FrameworkElement]) { throw 'ProgressIndicator was not found.' }
            $Data.ProgressTranslate = $progress.Template.FindName('PART_IndeterminateTranslate', $progress)
            $Data.ProgressTranslate2 = $progress.Template.FindName('PART_IndeterminateTranslate2', $progress)
            if ($Data.ProgressTranslate -isnot [System.Windows.Media.TranslateTransform] -or
                $Data.ProgressTranslate2 -isnot [System.Windows.Media.TranslateTransform])
            {
                throw 'Indeterminate ProgressBar animation transforms were not found.'
            }
            $progressOrigin = $progress.TransformToAncestor($Window).Transform([System.Windows.Point]::new(0, 0))
            $Data.ProgressBounds = [ordered]@{
                x = [Math]::Round($progressOrigin.X, 2)
                y = [Math]::Round($progressOrigin.Y, 2)
                width = [Math]::Round($progress.ActualWidth, 2)
                height = [Math]::Round($progress.ActualHeight, 2)
            }
        }

        $screenOrigin = $Window.PointToScreen([System.Windows.Point]::new(0, 0))
        $screenEnd = $Window.PointToScreen([System.Windows.Point]::new($Window.ActualWidth, $Window.ActualHeight))
        $width = [Math]::Max(1, [int][Math]::Round($screenEnd.X - $screenOrigin.X))
        $height = [Math]::Max(1, [int][Math]::Round($screenEnd.Y - $screenOrigin.Y))
        $background = $Window.TryFindResource('ApplicationBackgroundBrush')
        $primaryAccent = $Window.TryFindResource('AccentFillColorDefaultBrush')
        if ($background -isnot [System.Windows.Media.Brush])
        {
            throw 'ApplicationBackgroundBrush was not published.'
        }
        if ($primaryAccent -isnot [System.Windows.Media.SolidColorBrush])
        {
            throw 'AccentFillColorDefaultBrush was not published as a solid brush.'
        }

        $name = 'frame-{0:D4}.png' -f $index
        $framePath = Join-Path $Data.FramesDirectory $name
        $bitmap = [System.Drawing.Bitmap]::new($width, $height)
        try
        {
            $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
            try
            {
                $graphics.CopyFromScreen([int][Math]::Round($screenOrigin.X),
                    [int][Math]::Round($screenOrigin.Y), 0, 0,
                    [System.Drawing.Size]::new($width, $height))
            }
            finally { $graphics.Dispose() }
            $bitmap.Save($framePath, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally { $bitmap.Dispose() }

        $Data.Frames.Add([ordered]@{
            index = $index
            file = "frames/$name"
            nominalMilliseconds = $index * $periodMilliseconds
            actualMilliseconds = [Math]::Round($Data.Clock.Elapsed.TotalMilliseconds, 2)
            theme = $Data.CurrentTheme
            accent = $Data.CurrentAccent
            publishedPrimaryAccentColor = $primaryAccent.Color.ToString()
            width = $width
            height = $height
            progressTranslateX = [Math]::Round($Data.ProgressTranslate.X, 2)
            progressTranslate2X = [Math]::Round($Data.ProgressTranslate2.X, 2)
        })
    }.GetNewClosure()

    $timer = [System.Windows.Threading.DispatcherTimer]::new()
    $timer.Interval = [TimeSpan]::FromMilliseconds(10)
    $timer.add_Tick({
        try
        {
            if ($Data.NextFrame -le $lastFrame -and
                $Data.Clock.Elapsed.TotalMilliseconds -ge ($Data.NextFrame * $periodMilliseconds))
            {
                & $captureFrame $Data.NextFrame
                $Data.NextFrame++
            }
            if ($Data.NextFrame -gt $lastFrame)
            {
                $timer.Stop()
                $Window.Close()
            }
        }
        catch
        {
            $Data.Failure = $_.ToString()
            $timer.Stop()
            $Window.Close()
        }
    }.GetNewClosure())

    $Window.add_Loaded({
        $state = $Data
        $renderFrame = $captureFrame
        $samplingTimer = $timer
        $hostWindow = $Window
        $warmup = [System.Windows.Threading.DispatcherTimer]::new()
        $warmup.Interval = [TimeSpan]::FromMilliseconds(250)
        $warmup.add_Tick({
            $warmup.Stop()
            try
            {
                $state.Clock = [System.Diagnostics.Stopwatch]::StartNew()
                & $renderFrame 0
                $state.NextFrame = 1
                $samplingTimer.Start()
            }
            catch
            {
                $state.Failure = $_.ToString()
                $hostWindow.Close()
            }
        }.GetNewClosure())
        $warmup.Start()
    }.GetNewClosure())
}
}
catch
{
    $desktopState.captureError = $_.Exception.Message
    throw
}
finally
{
    if ($captureData.Failure) { $desktopState.captureError = $captureData.Failure }
    try
    {
        Set-FluenceAccent -System
        $desktopState.accentReset = $true
    }
    catch { $desktopState.accentResetError = $_.Exception.Message }
    if ($desktopState.minimizeAttempted)
    {
        $desktopState.restoreAttempted = $true
        try
        {
            $desktopShell.UndoMinimizeALL()
            $desktopState.restored = $true
        }
        catch { $desktopState.restoreError = $_.Exception.Message }
    }
    $desktopState.finishedUtc = [DateTime]::UtcNow.ToString('o')
    [System.IO.File]::WriteAllText($desktopStatePath, (($desktopState | ConvertTo-Json) + "`n"), [System.Text.UTF8Encoding]::new($false))
    Write-Output "Desktop capture state: $desktopStatePath"
    if ($desktopState.restoreError) { throw "Desktop windows could not be restored: $($desktopState.restoreError). State: $desktopStatePath" }
    if ($desktopState.accentResetError) { throw "System accent could not be restored: $($desktopState.accentResetError). State: $desktopStatePath" }
}

if ($captureData.Failure) { throw $captureData.Failure }
if ($captureData.Frames.Count -ne 619)
{
    throw "Capture stopped after $($captureData.Frames.Count) frames; expected 619."
}

Copy-Item -LiteralPath (Join-Path $framesDirectory 'frame-0000.png') -Destination (Join-Path $OutputDirectory 'poster-light.png') -Force
Copy-Item -LiteralPath (Join-Path $framesDirectory 'frame-0045.png') -Destination (Join-Path $OutputDirectory 'poster-dark.png') -Force
Copy-Item -LiteralPath (Join-Path $framesDirectory 'frame-0550.png') -Destination (Join-Path $OutputDirectory 'poster-high-contrast.png') -Force

$frameZeroHash = (Get-FileHash -LiteralPath (Join-Path $framesDirectory 'frame-0000.png') -Algorithm SHA256).Hash
$returnLightHash = (Get-FileHash -LiteralPath (Join-Path $framesDirectory 'frame-0610.png') -Algorithm SHA256).Hash
$timeline = [ordered]@{
    source = 'Fluence.Wpf.PowerShell.Module/build/Capture-ThemeAnimation.ps1'
    libraryAssemblyVersion = [System.Reflection.AssemblyName]::GetAssemblyName($libraryPath).Version.ToString()
    librarySha256 = (Get-FileHash -LiteralPath $libraryPath -Algorithm SHA256).Hash
    progressBounds = $captureData.ProgressBounds
    capture = 'Screen pixels of the visible custom FluenceWindow within its WPF window bounds'
    themeChange = 'FluenceWindow renders library theme transitions when motion is enabled; High Contrast has no appearance fade, though screen composition can lag the request'
    framesPerSecond = 20
    totalFrames = $captureData.Frames.Count
    loopStartFrame = 0
    loopEndExclusiveFrame = 610
    transitions = @($steps | ForEach-Object {
        [ordered]@{ second = $_.Frame / 20; frame = $_.Frame; theme = $_.Theme; accent = $_.Accent;
            color = if ($null -eq $_.Color) { $null } else { $_.Color.ToString() } }
    })
    firstAndReturnLightFramesByteIdentical = $frameZeroHash -eq $returnLightHash
    firstLightSha256 = $frameZeroHash
    returnLightSha256 = $returnLightHash
    frames = $captureData.Frames.ToArray()
}
$json = $timeline | ConvertTo-Json -Depth 8
[System.IO.File]::WriteAllText((Join-Path $OutputDirectory 'timeline.json'), $json + "`n", [System.Text.UTF8Encoding]::new($false))
Write-Output "Captured $($captureData.Frames.Count) frames in $OutputDirectory"
