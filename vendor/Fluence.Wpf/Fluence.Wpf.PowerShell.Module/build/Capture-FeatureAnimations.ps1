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
    Captures the Features page animations from a live FluenceWindow.
.DESCRIPTION
    ProgressDurationTime is the ProgressBar's 2.0-second repeat period. Each
    theme, accent, or backdrop stage lasts one complete period. Each backdrop scene
    moves from its initial theme's Mica to Acrylic, then switches to the opposite
    theme while keeping Acrylic in one live window. Captures the
    visible screen pixels at 30 frames per second and encodes eight looping GIFs
    plus section-specific posters with ffmpeg. Raw frames and capture timing
    remain under artifacts/feature-animation for review.
.PARAMETER Only
    Optional animation name, for example controls-light.
.PARAMETER WallpaperPath
    Path to the website backdrop wallpaper image. Required when capturing either
    backdrop variant so Acrylic samples that image from a real window behind it.
.PARAMETER WallpaperOverscan
    Multiplier applied to the wallpaper's cover scale. Defaults to 1.0; larger
    values enlarge the centered wallpaper while keeping the capture window centered.
.PARAMETER BackdropCaptureX
    Optional physical pixel X offset of the backdrop window from the wallpaper host's left edge.
    Supply with BackdropCaptureY. By default the window is centered in the host.
.PARAMETER BackdropCaptureY
    Optional physical pixel Y offset of the backdrop window from the wallpaper host's top edge.
    Supply with BackdropCaptureX. By default the window is centered in the host.
.PARAMETER KeepDesktop
    Skip the default reversible minimization of other desktop windows during capture.
.EXAMPLE
    powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File build/Capture-FeatureAnimations.ps1 -WallpaperPath C:\Images\backdrops-coronascape.webp
#>
[CmdletBinding()]
[OutputType([void])]
param
(
    [Parameter()]
    [ValidateSet('controls-light', 'controls-dark', 'themes-light', 'themes-dark',
        'accents-light', 'accents-dark', 'backdrops-light', 'backdrops-dark')]
    [string]$Only,

    [Parameter()]
    [string]$WallpaperPath,

    [Parameter()]
    [ValidateRange(1.0, 4.0)]
    [double]$WallpaperOverscan = 1.0,

    [Parameter()]
    [ValidateRange(0, 2147483647)]
    [int]$BackdropCaptureX,

    [Parameter()]
    [ValidateRange(0, 2147483647)]
    [int]$BackdropCaptureY,

    [Parameter()]
    [switch]$KeepDesktop
)

$ErrorActionPreference = 'Stop'

function Get-CaptureSha256([string]$Path)
{
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try
    {
        $stream = [System.IO.File]::OpenRead($Path)
        try { return [System.BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-', '') }
        finally { $stream.Dispose() }
    }
    finally { $sha.Dispose() }
}

if ([System.Threading.Thread]::CurrentThread.GetApartmentState() -ne [System.Threading.ApartmentState]::STA)
{
    throw 'Run Capture-FeatureAnimations.ps1 with powershell.exe -STA.'
}

$captureBackdrops = -not $Only -or $Only.StartsWith('backdrops-', [System.StringComparison]::OrdinalIgnoreCase)
$hasBackdropCaptureX = $PSBoundParameters.ContainsKey('BackdropCaptureX')
$hasBackdropCaptureY = $PSBoundParameters.ContainsKey('BackdropCaptureY')
if ($hasBackdropCaptureX -ne $hasBackdropCaptureY)
{
    throw 'Supply both -BackdropCaptureX and -BackdropCaptureY, or neither.'
}
if ($hasBackdropCaptureX -and -not $captureBackdrops)
{
    throw 'Backdrop capture coordinates are only used for backdrop variants.'
}
if ($PSBoundParameters.ContainsKey('WallpaperOverscan') -and -not $captureBackdrops)
{
    throw 'Wallpaper overscan is only used for backdrop variants.'
}
if ($captureBackdrops)
{
    if (-not ('FluenceFeatureCaptureGuard' -as [type]))
    {
        Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

public static class FluenceFeatureCaptureGuard
{
    [StructLayout(LayoutKind.Explicit)]
    private struct WtsInfoExData
    {
        [FieldOffset(0)] public long Alignment;
        [FieldOffset(0)] public uint SessionId;
        [FieldOffset(4)] public int SessionState;
        [FieldOffset(8)] public int SessionFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WtsInfoExHeader
    {
        public uint Level;
        public WtsInfoExData Data;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ProcessIdToSessionId(uint processId, out uint sessionId);

    [DllImport("wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSQuerySessionInformationW(
        IntPtr server, uint sessionId, int infoClass, out IntPtr buffer, out int bytesReturned);

    [DllImport("wtsapi32.dll")]
    private static extern void WTSFreeMemory(IntPtr buffer);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int attribute, out int value, int size);

    public static int GetSessionLockFlag()
    {
        uint sessionId;
        if (!ProcessIdToSessionId((uint)Process.GetCurrentProcess().Id, out sessionId))
            throw new Win32Exception(Marshal.GetLastWin32Error());

        IntPtr buffer;
        int bytesReturned;
        // WTSSessionInfoEx is 25; Level 1 contains SessionId, SessionState and SessionFlags.
        if (!WTSQuerySessionInformationW(IntPtr.Zero, sessionId, 25, out buffer, out bytesReturned))
            throw new Win32Exception(Marshal.GetLastWin32Error());

        try
        {
            int dataOffset = Marshal.OffsetOf(typeof(WtsInfoExHeader), "Data").ToInt32();
            if (bytesReturned < dataOffset + 12 || Marshal.ReadInt32(buffer) != 1 ||
                Marshal.ReadInt32(buffer, dataOffset) != (int)sessionId)
                throw new InvalidOperationException("Windows returned an unexpected session-state layout.");

            return Marshal.ReadInt32(buffer, dataOffset + 8);
        }
        finally { WTSFreeMemory(buffer); }
    }

    public static int GetCloakState(IntPtr hwnd)
    {
        int cloakState;
        int result = DwmGetWindowAttribute(hwnd, 14, out cloakState, sizeof(int));
        if (result < 0) Marshal.ThrowExceptionForHR(result);
        return cloakState;
    }
}
'@
    }
    $sessionLockFlag = [FluenceFeatureCaptureGuard]::GetSessionLockFlag()
    if ($sessionLockFlag -ne 1)
    {
        throw "Backdrop capture requires an unlocked Windows session (session flag $sessionLockFlag). Unlock the desktop and try again."
    }
}

$repo = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$modulePath = Join-Path $repo 'Fluence.Wpf.PowerShell.Module\src\Fluence.Wpf.PowerShell\Fluence.Wpf.PowerShell.psd1'
$libraryPath = Join-Path $repo 'Fluence.Wpf.PowerShell.Module\src\Fluence.Wpf.PowerShell\lib\net472\Fluence.Wpf.dll'
$iconPath = Join-Path $repo 'assets\Fluence_Icon.ico'
$assetDirectory = Join-Path $repo 'website\static\images\features'
$captureDirectory = Join-Path $repo 'artifacts\feature-animation'
$null = [System.IO.Directory]::CreateDirectory($assetDirectory)
$null = [System.IO.Directory]::CreateDirectory($captureDirectory)

Import-Module $modulePath -Force
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
$ffmpeg = (Get-Command ffmpeg -ErrorAction Stop).Source

$fps = 30
$progressDurationTime = 2.0
$framesPerState = [int]($fps * $progressDurationTime)
$hues = @(
    [pscustomobject]@{ Name = 'Red'; Color = [System.Windows.Media.Color]::FromRgb(0xC4, 0x2B, 0x1C) }
    [pscustomobject]@{ Name = 'Orange'; Color = [System.Windows.Media.Color]::FromRgb(0xF7, 0x63, 0x0C) }
    [pscustomobject]@{ Name = 'Yellow'; Color = [System.Windows.Media.Color]::FromRgb(0xFF, 0xB9, 0x00) }
    [pscustomobject]@{ Name = 'LightGreen'; Color = [System.Windows.Media.Color]::FromRgb(0x6B, 0xB7, 0x00) }
    [pscustomobject]@{ Name = 'DarkGreen'; Color = [System.Windows.Media.Color]::FromRgb(0x10, 0x7C, 0x10) }
    [pscustomobject]@{ Name = 'DarkBlue'; Color = [System.Windows.Media.Color]::FromRgb(0x00, 0x57, 0xA8) }
    [pscustomobject]@{ Name = 'LightBlue'; Color = [System.Windows.Media.Color]::FromRgb(0x00, 0xA6, 0xED) }
    [pscustomobject]@{ Name = 'Purple'; Color = [System.Windows.Media.Color]::FromRgb(0x74, 0x37, 0xC9) }
    [pscustomobject]@{ Name = 'Pink'; Color = [System.Windows.Media.Color]::FromRgb(0xE3, 0x00, 0x8C) }
)

$variants = @(
    [pscustomobject]@{ Name = 'controls-light'; Theme = 'Light'; Kind = 'controls'; States = @('Light') }
    [pscustomobject]@{ Name = 'controls-dark'; Theme = 'Dark'; Kind = 'controls'; States = @('Dark') }
    [pscustomobject]@{ Name = 'themes-light'; Theme = 'Light'; Kind = 'themes'; States = @('Light', 'Dark', 'HighContrast') }
    [pscustomobject]@{ Name = 'themes-dark'; Theme = 'Dark'; Kind = 'themes'; States = @('Dark', 'Light', 'HighContrast') }
    [pscustomobject]@{ Name = 'accents-light'; Theme = 'Light'; Kind = 'accents'; States = @($hues) }
    [pscustomobject]@{ Name = 'accents-dark'; Theme = 'Dark'; Kind = 'accents'; States = @($hues) }
    [pscustomobject]@{ Name = 'backdrops-light'; Theme = 'Light'; Kind = 'backdrops'; States = @('LightMica', 'LightAcrylic', 'DarkAcrylic') }
    [pscustomobject]@{ Name = 'backdrops-dark'; Theme = 'Dark'; Kind = 'backdrops'; States = @('DarkMica', 'DarkAcrylic', 'LightAcrylic') }
)

$resolvedWallpaperPath = $null
if ($captureBackdrops)
{
    if ([string]::IsNullOrWhiteSpace($WallpaperPath))
    {
        throw 'Supply -WallpaperPath with the website backdrop wallpaper when capturing backdrops.'
    }
    $resolvedWallpaperPath = (Resolve-Path -LiteralPath $WallpaperPath -ErrorAction Stop).ProviderPath
    if (-not (Test-Path -LiteralPath $resolvedWallpaperPath -PathType Leaf))
    {
        throw "Backdrop wallpaper is not a file: $resolvedWallpaperPath"
    }
}

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
    UseLayoutRounding="True">
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

foreach ($variant in $variants)
{
    if ($Only -and $variant.Name -ne $Only) { continue }

    if ($variant.Kind -eq 'backdrops' -and [FluenceFeatureCaptureGuard]::GetSessionLockFlag() -ne 1)
    {
        throw "Backdrop capture requires an unlocked Windows session before $($variant.Name)."
    }

    $frameDirectory = Join-Path $captureDirectory $variant.Name
    $null = [System.IO.Directory]::CreateDirectory($frameDirectory)
    $data = @{
        Variant = $variant
        FramesPerState = $framesPerState
        Fps = $fps
        FrameDirectory = $frameDirectory
        NextFrame = 0
        Failure = $null
        Clock = $null
        Frames = [System.Collections.Generic.List[object]]::new()
        CurrentState = $variant.States[0]
        ProgressTranslate = $null
        ProgressTranslate2 = $null
        WallpaperHost = $null
        Wallpaper = $null
        BackdropCaptureX = if ($hasBackdropCaptureX) { $BackdropCaptureX } else { $null }
        BackdropCaptureY = if ($hasBackdropCaptureY) { $BackdropCaptureY } else { $null }
        RequestedCapture = $null
    }
    if ($variant.Kind -eq 'accents') { Set-FluenceAccent -Color $hues[0].Color }
    else { Set-FluenceAccent -System }
    $initialBackdrop = if ($variant.Kind -eq 'backdrops') { 'Mica' } else { 'None' }
    $desktopStatePath = Join-Path ([System.IO.Path]::GetTempPath()) ('fluence-feature-desktop-{0}-{1}.json' -f $variant.Name, [guid]::NewGuid().ToString('N'))
    $desktopState = [ordered]@{
        capture = 'Capture-FeatureAnimations.ps1'
        variant = $variant.Name
        startedUtc = [DateTime]::UtcNow.ToString('o')
        minimizeRequested = -not $KeepDesktop
        minimizeAttempted = $false
        minimized = $false
        restoreAttempted = $false
        restored = $false
        accentReset = $false
        wallpaperHostShown = $false
        wallpaperHostClosed = $false
        wallpaperTempDeleted = $false
        captureError = $null
        restoreError = $null
        accentResetError = $null
        wallpaperCleanupError = $null
    }
    $desktopShell = $null
    $wallpaperHost = $null
    $wallpaperCanvas = $null
    $wallpaperPngPath = $null
    [System.IO.File]::WriteAllText($desktopStatePath, (($desktopState | ConvertTo-Json) + "`n"), [System.Text.UTF8Encoding]::new($false))
    try
    {
    if ($variant.Kind -eq 'backdrops')
    {
        $wallpaperPngPath = Join-Path ([System.IO.Path]::GetTempPath()) ('fluence-feature-wallpaper-{0}.png' -f [guid]::NewGuid().ToString('N'))
        & $ffmpeg -hide_banner -loglevel error -y -i $resolvedWallpaperPath -frames:v 1 $wallpaperPngPath
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $wallpaperPngPath -PathType Leaf))
        {
            throw "ffmpeg could not decode the backdrop wallpaper: $resolvedWallpaperPath"
        }
    }
    if (-not $KeepDesktop)
    {
        $desktopShell = New-Object -ComObject Shell.Application
        $desktopState.minimizeAttempted = $true
        $desktopShell.MinimizeAll()
        $desktopState.minimized = $true
    }
    if ($variant.Kind -eq 'backdrops')
    {
        $screen = [System.Windows.Forms.Screen]::PrimaryScreen
        if ($null -eq $screen) { throw 'No primary screen is available for the backdrop wallpaper host.' }
        $bounds = $screen.Bounds
        $wallpaperImage = [System.Drawing.Image]::FromFile($wallpaperPngPath)
        try
        {
            $coverScale = [Math]::Max([double]$bounds.Width / $wallpaperImage.Width,
                [double]$bounds.Height / $wallpaperImage.Height)
            $imageScale = $coverScale * $WallpaperOverscan
            $drawWidth = [int][Math]::Ceiling($wallpaperImage.Width * $imageScale)
            $drawHeight = [int][Math]::Ceiling($wallpaperImage.Height * $imageScale)
            $drawX = [int][Math]::Floor(($bounds.Width - $drawWidth) / 2.0)
            $drawY = [int][Math]::Floor(($bounds.Height - $drawHeight) / 2.0)
            $wallpaperCanvas = [System.Drawing.Bitmap]::new($bounds.Width, $bounds.Height)
            $wallpaperGraphics = [System.Drawing.Graphics]::FromImage($wallpaperCanvas)
            try
            {
                $wallpaperGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $wallpaperGraphics.DrawImage($wallpaperImage,
                    [System.Drawing.Rectangle]::new($drawX, $drawY, $drawWidth, $drawHeight))
            }
            finally { $wallpaperGraphics.Dispose() }
        }
        finally { $wallpaperImage.Dispose() }

        $wallpaperHost = [System.Windows.Forms.Form]::new()
        $wallpaperHost.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::None
        $wallpaperHost.StartPosition = [System.Windows.Forms.FormStartPosition]::Manual
        $wallpaperHost.Bounds = $bounds
        $wallpaperHost.ShowInTaskbar = $false
        $wallpaperHost.TopMost = $false
        $wallpaperHost.BackgroundImage = $wallpaperCanvas
        $wallpaperHost.BackgroundImageLayout = [System.Windows.Forms.ImageLayout]::None
        $wallpaperHost.Show()
        $wallpaperHost.Refresh()
        $desktopState.wallpaperHostShown = $wallpaperHost.Visible
        $data.WallpaperHost = $wallpaperHost
        $data.Wallpaper = [ordered]@{
            sourceSha256 = Get-CaptureSha256 $resolvedWallpaperPath
            sourcePath = $resolvedWallpaperPath
            hostX = $bounds.X
            hostY = $bounds.Y
            hostWidth = $bounds.Width
            hostHeight = $bounds.Height
            coverScale = [Math]::Round($coverScale, 6)
            overscan = $WallpaperOverscan
            imageScale = [Math]::Round($imageScale, 6)
            imageDrawX = $drawX
            imageDrawY = $drawY
            imageDrawWidth = $drawWidth
            imageDrawHeight = $drawHeight
            positioning = 'center center / cover'
        }
    }
    $null = Show-FluenceWindow -Xaml $xaml -Theme $variant.Theme -Backdrop $initialBackdrop -TitleBarIcon $iconPath -Data $data -Initialize {
        param($Window, $Data)

        if ($Data.Variant.Kind -eq 'backdrops')
        {
            $Window.WindowStartupLocation = [System.Windows.WindowStartupLocation]::Manual
            $Window.Left = 0
            $Window.Top = 0
        }

        $periodMilliseconds = 1000.0 / $Data.Fps
        $frameCount = $Data.Variant.States.Count * $Data.FramesPerState
        $captureFrame = {
            param([int]$index)

            if ($index -gt 0 -and ($index % $Data.FramesPerState) -eq 0)
            {
                $stateIndex = [int]($index / $Data.FramesPerState)
                $state = $Data.Variant.States[$stateIndex]
                switch ($Data.Variant.Kind)
                {
                    'themes' { Set-FluenceTheme -Theme $state }
                    'accents' { Set-FluenceAccent -Color $state.Color }
                    'backdrops'
                    {
                        switch ($stateIndex)
                        {
                            1 { Set-FluenceBackdrop -Backdrop Acrylic -Window $Window }
                            2
                            {
                                $oppositeTheme = if ($Data.Variant.Theme -eq 'Light') { 'Dark' } else { 'Light' }
                                Set-FluenceTheme -Theme $oppositeTheme
                            }
                            default { throw "Unknown backdrop stage index: $stateIndex" }
                        }
                    }
                }
                $Data.CurrentState = $state
            }
            $Window.UpdateLayout()
            if (-not $Window.IsActive) { $null = $Window.Activate() }

            $nativeForeground = $null
            $windowCloak = $null
            $wallpaperHostCloak = $null
            if ($Data.Variant.Kind -eq 'backdrops')
            {
                $windowHandle = [System.Windows.Interop.WindowInteropHelper]::new($Window).Handle
                $nativeForeground = [FluenceFeatureCaptureGuard]::GetForegroundWindow()
                $windowCloak = [FluenceFeatureCaptureGuard]::GetCloakState($windowHandle)
                $wallpaperHostCloak = [FluenceFeatureCaptureGuard]::GetCloakState($Data.WallpaperHost.Handle)
                if (-not $Window.IsActive -or $nativeForeground -ne $windowHandle -or
                    $windowCloak -ne 0 -or $wallpaperHostCloak -ne 0)
                {
                    $observed = $Window.PointToScreen([System.Windows.Point]::new(0, 0))
                    $requested = $Data.RequestedCapture
                    throw "Backdrop capture requires a visible foreground window and wallpaper host (foreground $($nativeForeground.ToInt64()), window $($windowHandle.ToInt64()), active $($Window.IsActive), cloak $windowCloak, host cloak $wallpaperHostCloak, observed origin $([int][Math]::Round($observed.X)),$([int][Math]::Round($observed.Y)), requested origin $($requested.x),$($requested.y))."
                }
            }

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
            }

            $origin = $Window.PointToScreen([System.Windows.Point]::new(0, 0))
            $end = $Window.PointToScreen([System.Windows.Point]::new($Window.ActualWidth, $Window.ActualHeight))
            $captureX = [int][Math]::Round($origin.X)
            $captureY = [int][Math]::Round($origin.Y)
            $width = [Math]::Max(1, [int][Math]::Round($end.X - $origin.X))
            $height = [Math]::Max(1, [int][Math]::Round($end.Y - $origin.Y))
            if ($null -ne $Data.Wallpaper)
            {
                $wallpaper = $Data.Wallpaper
                $requested = $Data.RequestedCapture
                if ($null -eq $requested -or $captureX -ne $requested.x -or $captureY -ne $requested.y -or
                    $width -ne $requested.width -or $height -ne $requested.height)
                {
                    throw "Backdrop capture geometry ($captureX, $captureY, $width, $height) differs from requested geometry ($($requested.x), $($requested.y), $($requested.width), $($requested.height))."
                }
                if ($captureX -lt $wallpaper.hostX -or $captureY -lt $wallpaper.hostY -or
                    ($captureX + $width) -gt ($wallpaper.hostX + $wallpaper.hostWidth) -or
                    ($captureY + $height) -gt ($wallpaper.hostY + $wallpaper.hostHeight))
                {
                    throw "Backdrop capture rectangle ($captureX, $captureY, $width, $height) is outside wallpaper host ($($wallpaper.hostX), $($wallpaper.hostY), $($wallpaper.hostWidth), $($wallpaper.hostHeight))."
                }
            }
            $framePath = Join-Path $Data.FrameDirectory ('frame-{0:D4}.png' -f $index)
            $bitmap = [System.Drawing.Bitmap]::new($width, $height)
            try
            {
                $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
                try
                {
                    $graphics.CopyFromScreen($captureX, $captureY, 0, 0,
                        [System.Drawing.Size]::new($width, $height))
                }
                finally { $graphics.Dispose() }
                $bitmap.Save($framePath, [System.Drawing.Imaging.ImageFormat]::Png)
            }
            catch
            {
                throw "Screen capture failed at ($($origin.X), $($origin.Y)), size ${width}x${height}, virtual screen $([System.Windows.Forms.SystemInformation]::VirtualScreen): $($_.Exception.Message)"
            }
            finally { $bitmap.Dispose() }

            $stateLabel = if ($Data.Variant.Kind -eq 'accents') { $Data.CurrentState.Name } else { [string]$Data.CurrentState }
            $wallpaperHostVisible = if ($null -ne $Data.WallpaperHost) { [bool]$Data.WallpaperHost.Visible } else { $null }
            $Data.Frames.Add([ordered]@{
                index = $index
                expectedMilliseconds = [Math]::Round($index * $periodMilliseconds, 2)
                actualMilliseconds = [Math]::Round($Data.Clock.Elapsed.TotalMilliseconds, 2)
                state = $stateLabel
                progressX = [Math]::Round($Data.ProgressTranslate.X, 2)
                progress2X = [Math]::Round($Data.ProgressTranslate2.X, 2)
                captureX = $captureX
                captureY = $captureY
                windowActive = [bool]$Window.IsActive
                nativeForeground = if ($null -ne $nativeForeground) { $nativeForeground.ToInt64() } else { $null }
                windowCloak = $windowCloak
                wallpaperHostCloak = $wallpaperHostCloak
                windowBackdrop = [string]$Window.SystemBackdropType
                applicationTheme = [string][Fluence.Wpf.ApplicationThemeManager]::CurrentTheme
                wallpaperHostVisible = $wallpaperHostVisible
                width = $width
                height = $height
            })
        }.GetNewClosure()

        $timer = [System.Windows.Threading.DispatcherTimer]::new()
        $timer.Interval = [TimeSpan]::FromMilliseconds(5)
        $timer.add_Tick({
            try
            {
                if ($Data.NextFrame -lt $frameCount -and
                    $Data.Clock.Elapsed.TotalMilliseconds -ge ($Data.NextFrame * $periodMilliseconds))
                {
                    & $captureFrame $Data.NextFrame
                    $Data.NextFrame++
                }
                if ($Data.NextFrame -ge $frameCount)
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
            if ($state.Variant.Kind -eq 'backdrops')
            {
                try
                {
                    $wallpaper = $state.Wallpaper
                    $origin = $hostWindow.PointToScreen([System.Windows.Point]::new(0, 0))
                    $end = $hostWindow.PointToScreen([System.Windows.Point]::new($hostWindow.ActualWidth, $hostWindow.ActualHeight))
                    $width = [Math]::Max(1, [int][Math]::Round($end.X - $origin.X))
                    $height = [Math]::Max(1, [int][Math]::Round($end.Y - $origin.Y))
                    $relativeX = if ($null -ne $state.BackdropCaptureX) { [int]$state.BackdropCaptureX }
                        else { [int][Math]::Floor(($wallpaper.hostWidth - $width) / 2.0) }
                    $relativeY = if ($null -ne $state.BackdropCaptureY) { [int]$state.BackdropCaptureY }
                        else { [int][Math]::Floor(($wallpaper.hostHeight - $height) / 2.0) }
                    if ($relativeX -lt 0 -or $relativeY -lt 0 -or
                        ($relativeX + $width) -gt $wallpaper.hostWidth -or
                        ($relativeY + $height) -gt $wallpaper.hostHeight)
                    {
                        throw "Requested backdrop rectangle ($relativeX, $relativeY, $width, $height) does not fit wallpaper host $($wallpaper.hostWidth)x$($wallpaper.hostHeight)."
                    }
                    $targetX = $wallpaper.hostX + $relativeX
                    $targetY = $wallpaper.hostY + $relativeY
                    $source = [System.Windows.PresentationSource]::FromVisual($hostWindow)
                    if ($null -eq $source -or $null -eq $source.CompositionTarget)
                    {
                        throw 'WPF has no presentation source for backdrop window placement.'
                    }
                    $deviceDelta = [System.Windows.Vector]::new($targetX - $origin.X, $targetY - $origin.Y)
                    $logicalDelta = $source.CompositionTarget.TransformFromDevice.Transform($deviceDelta)
                    $hostWindow.Left += $logicalDelta.X
                    $hostWindow.Top += $logicalDelta.Y
                    $hostWindow.UpdateLayout()
                    $null = $hostWindow.Activate()
                    $state.RequestedCapture = [ordered]@{
                        mode = if ($null -ne $state.BackdropCaptureX) { 'explicit host-relative physical pixels' }
                            else { 'centered in wallpaper host' }
                        hostRelativeX = $relativeX
                        hostRelativeY = $relativeY
                        x = $targetX
                        y = $targetY
                        width = $width
                        height = $height
                    }
                }
                catch
                {
                    $state.Failure = $_.ToString()
                    $hostWindow.Close()
                    return
                }
            }
            $warmup = [System.Windows.Threading.DispatcherTimer]::new()
            $warmup.Interval = [TimeSpan]::FromMilliseconds(300)
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
        if ($data.Failure) { throw "$($variant.Name): $($data.Failure)" }
        throw
    }
    finally
    {
        if ($data.Failure) { $desktopState.captureError = $data.Failure }
        if ($null -ne $wallpaperHost)
        {
            try
            {
                $wallpaperHost.BackgroundImage = $null
                $wallpaperHost.Close()
                $wallpaperHost.Dispose()
                $desktopState.wallpaperHostClosed = $true
            }
            catch { $desktopState.wallpaperCleanupError = $_.Exception.Message }
        }
        if ($null -ne $wallpaperCanvas)
        {
            try { $wallpaperCanvas.Dispose() }
            catch { $desktopState.wallpaperCleanupError = $_.Exception.Message }
        }
        if ($null -ne $wallpaperPngPath -and (Test-Path -LiteralPath $wallpaperPngPath))
        {
            try
            {
                [System.IO.File]::Delete($wallpaperPngPath)
                $desktopState.wallpaperTempDeleted = $true
            }
            catch { $desktopState.wallpaperCleanupError = $_.Exception.Message }
        }
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
        if ($desktopState.wallpaperCleanupError) { throw "Backdrop wallpaper host could not be cleaned up: $($desktopState.wallpaperCleanupError). State: $desktopStatePath" }
    }

    if ($data.Failure) { throw "$($variant.Name): $($data.Failure)" }
    if ($data.Frames.Count -ne ($variant.States.Count * $framesPerState))
    {
        throw "$($variant.Name): Captured $($data.Frames.Count) of $($variant.States.Count * $framesPerState) frames."
    }

    $metadata = [ordered]@{
        source = 'Fluence.Wpf.PowerShell.Module/build/Capture-FeatureAnimations.ps1'
        variant = $variant.Name
        progressDurationTimeSeconds = $progressDurationTime
        framesPerSecond = $fps
        framesPerState = $framesPerState
        states = @($variant.States | ForEach-Object { if ($_ -is [string]) { $_ } else { $_.Name } })
        librarySha256 = Get-CaptureSha256 $libraryPath
        capture = 'Visible screen pixels of live FluenceWindow within its WPF window bounds'
        wallpaper = $data.Wallpaper
        requestedCapture = $data.RequestedCapture
        actualCapture = if ($variant.Kind -eq 'backdrops')
        {
            [ordered]@{ x = $data.Frames[0].captureX; y = $data.Frames[0].captureY;
                width = $data.Frames[0].width; height = $data.Frames[0].height }
        } else { $null }
        frames = $data.Frames.ToArray()
    }
    [System.IO.File]::WriteAllText((Join-Path $frameDirectory 'timeline.json'),
        (($metadata | ConvertTo-Json -Depth 7) + "`n"), [System.Text.UTF8Encoding]::new($true))

    $poster = Join-Path $assetDirectory ($variant.Name + '.png')
    $posterIndex = if ($variant.Kind -eq 'backdrops') { $framesPerState + 30 } else { 0 }
    Copy-Item -LiteralPath (Join-Path $frameDirectory ('frame-{0:D4}.png' -f $posterIndex)) -Destination $poster -Force

    $gif = Join-Path $assetDirectory ($variant.Name + '.gif')
    $pattern = Join-Path $frameDirectory 'frame-%04d.png'
    $filter = '[0:v]split[a][b];[a]palettegen=stats_mode=diff[p];[b][p]paletteuse=dither=sierra2_4a:diff_mode=rectangle'
    & $ffmpeg -hide_banner -loglevel error -y -framerate $fps -i $pattern -filter_complex $filter -loop 0 $gif
    if ($LASTEXITCODE -ne 0) { throw "ffmpeg failed for $($variant.Name) with exit code $LASTEXITCODE." }
    Write-Output "Captured $($variant.Name): $($data.Frames.Count) frames, $($data.Frames[0].width)x$($data.Frames[0].height), $gif"
}
