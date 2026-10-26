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
    Captures live PowerShell module windows for the documentation.
.DESCRIPTION
    Opens each example in a separate Windows PowerShell STA process, waits for its visible WPF
    window, and copies the screen pixels around that window to a PNG. The margin can include the
    real DWM shadow when Windows draws one. A desktop session with an unlocked display is required.
    Run this script while no other capture task is using the desktop.
.PARAMETER Scene
    Internal single-scene mode used by child processes. Omit to capture every scene.
.PARAMETER OutputDirectory
    Directory for PNG files. Defaults to docs/powershell/images.
.PARAMETER ShadowMargin
    Screen pixels to include outside the window rectangle. Defaults to 28.
.PARAMETER CaptureMode
    Desktop uses actual screen pixels and can include DWM shadow. WpfRender captures the WPF visual
    when no interactive screen DC is available; this mode has no DWM shadow.
.PARAMETER Include
    Optional scene names to capture. Omit for the complete set.
.PARAMETER KeepDesktop
    Skip the default reversible minimization of other desktop windows during capture.
.PARAMETER ParentDesktopIsolated
    Internal child-process flag: the parent already owns desktop minimization and restoration.
.EXAMPLE
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File build/Capture-Documentation.ps1
.NOTES
    Captures the rendered desktop with CopyFromScreen. It does not synthesize a shadow or replace
    the window background. Existing output PNGs are replaced only after each capture succeeds.
#>
[CmdletBinding()]
[OutputType([void])]
param
(
    [Parameter()]
    [ValidateSet('All', 'message-light', 'message-dark', 'message-warning-light',
        'dialog-form-light', 'dialog-form-dark', 'dialog-image-light', 'dialog-image-dark',
        'list-selection-light', 'list-selection-dark', 'list-selection-multiple-dark',
        'progress-light', 'progress-dark', 'progress-indeterminate-light',
        'restart-prompt-light', 'restart-prompt-dark', 'xaml-window-light',
        'xaml-window-dark', 'accent-purple-light', 'accent-green-dark')]
    [string]$Scene = 'All',

    [Parameter()]
    [string]$OutputDirectory,

    [Parameter()]
    [ValidateRange(0, 100)]
    [int]$ShadowMargin = 28,

    [Parameter()]
    [ValidateSet('Desktop', 'WpfRender')]
    [string]$CaptureMode = 'WpfRender',

    [Parameter()]
    [string[]]$Include,

    [Parameter()]
    [switch]$KeepDesktop,

    [Parameter()]
    [switch]$ParentDesktopIsolated
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$modulePath = Join-Path $repo 'Fluence.Wpf.PowerShell.Module\src\Fluence.Wpf.PowerShell\Fluence.Wpf.PowerShell.psd1'
$iconPath = Join-Path $repo 'assets\Fluence_Icon.ico'
$imageName = if ($Scene.EndsWith('-dark', [System.StringComparison]::Ordinal))
{
    'Fluence_Lockup_Stacked_Dark.png'
}
else
{
    'Fluence_Lockup_Stacked_Light.png'
}
$imagePath = Join-Path $repo (Join-Path 'assets' $imageName)
$xamlPath = Join-Path $repo 'Fluence.Wpf.PowerShell.Module\examples\MainWindow.xaml'

if ($Scene -ne 'All')
{
    Import-Module $modulePath -Force
    Set-FluenceAccent -System
    if ($CaptureMode -eq 'WpfRender')
    {
        Add-Type -AssemblyName PresentationFramework
        if (-not $OutputDirectory) { throw 'WpfRender requires -OutputDirectory.' }
        $capturePath = Join-Path $OutputDirectory "$Scene.png"
        $captureHandler = [System.Windows.RoutedEventHandler]{
            param($sourceWindow, $routedEventArgs)
            $null = $routedEventArgs
            if ($sourceWindow -isnot [System.Windows.Window]) { return }
            $window = [System.Windows.Window]$sourceWindow
            $timer = [System.Windows.Threading.DispatcherTimer]::new()
            $timer.Interval = [TimeSpan]::FromMilliseconds(600)
            $timer.add_Tick({
                $timer.Stop()
                try
                {
                    $width = [Math]::Max(1, [int][Math]::Ceiling($window.ActualWidth))
                    $height = [Math]::Max(1, [int][Math]::Ceiling($window.ActualHeight))
                    $surface = [System.Windows.Media.DrawingVisual]::new()
                    $drawing = $surface.RenderOpen()
                    $background = $window.TryFindResource('ApplicationBackgroundBrush')
                    if ($background -isnot [System.Windows.Media.Brush])
                    {
                        throw 'The application background brush was not published.'
                    }
                    $drawing.DrawRectangle($background, $null, [System.Windows.Rect]::new(0, 0, $width, $height))
                    $drawing.DrawRectangle([System.Windows.Media.VisualBrush]::new($window), $null,
                        [System.Windows.Rect]::new(0, 0, $width, $height))
                    $drawing.Close()
                    $bitmap = [System.Windows.Media.Imaging.RenderTargetBitmap]::new(
                        $width, $height, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
                    $bitmap.Render($surface)
                    $encoder = [System.Windows.Media.Imaging.PngBitmapEncoder]::new()
                    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
                    $stream = [System.IO.File]::Create($capturePath)
                    try { $encoder.Save($stream) } finally { $stream.Dispose() }
                }
                finally { [System.Environment]::Exit(0) }
            }.GetNewClosure())
            $timer.Start()
        }.GetNewClosure()
        [System.Windows.EventManager]::RegisterClassHandler(
            [System.Windows.Window], [System.Windows.FrameworkElement]::LoadedEvent, $captureHandler)
    }
    $theme = if ($Scene.EndsWith('-dark', [System.StringComparison]::Ordinal)) { 'Dark' } else { 'Light' }
    $common = @{ Theme = $theme; TitleBarIcon = $iconPath; Backdrop = 'Mica' }
    $isolateScene = $CaptureMode -eq 'Desktop' -and -not $KeepDesktop -and -not $ParentDesktopIsolated
    $sceneShell = $null
    $sceneMinimizeAttempted = $false
    if ($isolateScene)
    {
        $sceneStatePath = Join-Path ([System.IO.Path]::GetTempPath()) ("fluence-documentation-$Scene-$PID.json")
        $sceneState = [ordered]@{
            Scene = $Scene
            StartedUtc = [DateTime]::UtcNow.ToString('o')
            MinimizeAttempted = $false
            MinimizeSucceeded = $false
            RestoreAttempted = $false
            RestoreSucceeded = $false
            CaptureError = $null
            RestoreError = $null
            CompletedUtc = $null
        }
        $sceneState | ConvertTo-Json | Set-Content -LiteralPath $sceneStatePath -Encoding UTF8
    }

    try
    {
        if ($isolateScene)
        {
            $sceneShell = New-Object -ComObject Shell.Application
            $sceneMinimizeAttempted = $true
            $sceneState.MinimizeAttempted = $true
            $sceneShell.MinimizeAll()
            $sceneState.MinimizeSucceeded = $true
        }
    switch ($Scene)
    {
        { $_ -in @('message-light', 'message-dark') }
        {
            $null = Show-FluenceMessage @common -TitleBarText 'Setup confirmation' -Message 'Ready to install Fluence.Wpf?' -Icon Question -Buttons YesNo
            break
        }
        'message-warning-light'
        {
            $null = Show-FluenceMessage @common -TitleBarText 'Unsaved changes' -Message 'Save your changes before closing?' -Icon Warning -Buttons YesNoCancel
            break
        }
        { $_ -in @('dialog-form-light', 'dialog-form-dark') }
        {
            $prompts = @(
                New-FluencePrompt -Name FullName -Message 'Full name' -InputType Text -DefaultValue 'Alex Morgan' -ValidateNotEmpty
                New-FluencePrompt -Name Age -Message 'Age' -InputType Number -DefaultValue 30
                New-FluencePrompt -Name Country -Message 'Country' -InputType Choice -ValidateSet 'Australia', 'Canada', 'United Kingdom', 'United States' -DefaultValue 'Canada'
                New-FluencePrompt -Name StartDate -Message 'Start date' -InputType Date
                New-FluencePrompt -Name AcceptTerms -Message 'I accept the terms and conditions' -InputType Checkbox -DefaultValue $true
            )
            $null = Show-FluenceDialog @common -TitleBarText 'Registration' -Prompts $prompts -Buttons OK, Cancel
            break
        }
        { $_ -in @('dialog-image-light', 'dialog-image-dark') }
        {
            $null = Show-FluenceMessage @common -TitleBarText 'Fluence.Wpf' -Message 'Fluent controls for WPF and PowerShell.' -Icon None -Image $imagePath -MessageAlignment Center -Buttons OK
            break
        }
        { $_ -in @('list-selection-light', 'list-selection-dark') }
        {
            $null = Show-FluenceListSelection @common -TitleBarText 'Choose a region' -Message 'Deployment region' -Items 'Americas', 'Asia Pacific', 'Europe' -DefaultValue 'Americas'
            break
        }
        'list-selection-multiple-dark'
        {
            $null = Show-FluenceListSelection @common -TitleBarText 'Select features' -Message 'Features to install' -Items 'Core library', 'Documentation', 'Samples' -MultiSelect -DefaultValue 'Core library', 'Samples'
            break
        }
        { $_ -in @('progress-light', 'progress-dark') }
        {
            $handle = Show-FluenceProgress @common -TitleBarText 'Installation progress' -Message 'Installing components' -Detail 'Step 3 of 5' -PercentComplete 60 -NotTopmost
            try
            {
                if ($CaptureMode -eq 'WpfRender') { [System.Windows.Threading.Dispatcher]::Run() }
                else { Start-Sleep -Seconds 30 }
            }
            finally { Close-FluenceProgress -Handle $handle }
            break
        }
        'progress-indeterminate-light'
        {
            $handle = Show-FluenceProgress @common -TitleBarText 'Preparing installation' -Message 'Checking prerequisites' -Detail 'This takes a moment.' -NotTopmost
            try
            {
                if ($CaptureMode -eq 'WpfRender') { [System.Windows.Threading.Dispatcher]::Run() }
                else { Start-Sleep -Seconds 30 }
            }
            finally { Close-FluenceProgress -Handle $handle }
            break
        }
        { $_ -in @('restart-prompt-light', 'restart-prompt-dark') }
        {
            $null = Show-FluenceRestartPrompt @common -TitleBarText 'Restart required' -Message 'The installation requires a restart to finish.' -NoCountdown -NotTopmost
            break
        }
        { $_ -in @('xaml-window-light', 'xaml-window-dark') }
        {
            $null = Show-FluenceWindow @common -XamlPath $xamlPath -TitleBarText 'PowerShell XAML gallery'
            break
        }
        'accent-purple-light'
        {
            $buttons = @(New-FluenceButton -Text 'Continue' -IsDefault)
            $null = Show-FluenceDialog @common -TitleBarText 'Purple accent' -Message 'Accent color changes the controls and window.' -Icon Info -Buttons $buttons -Accent ([System.Windows.Media.Color]::FromRgb(0x74, 0x37, 0xC9))
            break
        }
        'accent-green-dark'
        {
            $buttons = @(New-FluenceButton -Text 'Continue' -IsDefault)
            $null = Show-FluenceDialog @common -TitleBarText 'Green accent' -Message 'Accent color changes the controls and window.' -Icon Success -Buttons $buttons -Accent ([System.Windows.Media.Color]::FromRgb(0x10, 0x89, 0x3E))
            break
        }
    }
    }
    catch
    {
        if ($isolateScene) { $sceneState.CaptureError = $_.Exception.Message }
        throw
    }
    finally
    {
        if ($isolateScene)
        {
            try
            {
                if ($sceneMinimizeAttempted)
                {
                    $sceneState.RestoreAttempted = $true
                    try
                    {
                        $sceneShell.UndoMinimizeALL()
                        $sceneState.RestoreSucceeded = $true
                    }
                    catch
                    {
                        $sceneState.RestoreError = $_.Exception.Message
                        throw
                    }
                }
            }
            finally
            {
                $sceneState.CompletedUtc = [DateTime]::UtcNow.ToString('o')
                $sceneState | ConvertTo-Json | Set-Content -LiteralPath $sceneStatePath -Encoding UTF8
                Write-Output "Desktop capture state: $sceneStatePath"
            }
        }
    }
    return
}

if (-not $OutputDirectory)
{
    $OutputDirectory = Join-Path $repo 'docs\powershell\images'
}
$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
[System.IO.Directory]::CreateDirectory($OutputDirectory) | Out-Null

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class FluenceCaptureNative
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
}
'@

$scenes = @(
    'message-light', 'message-dark', 'message-warning-light',
    'dialog-form-light', 'dialog-form-dark', 'dialog-image-light', 'dialog-image-dark',
    'list-selection-light', 'list-selection-dark', 'list-selection-multiple-dark',
    'progress-light', 'progress-dark', 'progress-indeterminate-light',
    'restart-prompt-light', 'restart-prompt-dark', 'xaml-window-light',
    'xaml-window-dark', 'accent-purple-light', 'accent-green-dark'
)
if ($Include)
{
    $Include = @($Include | ForEach-Object { $_ -split ',' })
    foreach ($name in $Include)
    {
        if ($scenes -notcontains $name) { throw "Unknown capture scene: $name" }
    }
    $scenes = @($scenes | Where-Object { $Include -contains $_ })
}

$desktopStatePath = Join-Path ([System.IO.Path]::GetTempPath()) ("fluence-documentation-batch-$PID.json")
$desktopState = [ordered]@{
    CaptureMode = $CaptureMode
    Scenes = $scenes
    StartedUtc = [DateTime]::UtcNow.ToString('o')
    MinimizeRequested = -not [bool]$KeepDesktop
    MinimizeAttempted = $false
    MinimizeSucceeded = $false
    RestoreAttempted = $false
    RestoreSucceeded = $false
    CaptureError = $null
    RestoreError = $null
    CompletedUtc = $null
}
$desktopState | ConvertTo-Json | Set-Content -LiteralPath $desktopStatePath -Encoding UTF8
$desktopShell = $null
try
{
    if (-not $KeepDesktop)
    {
        $desktopShell = New-Object -ComObject Shell.Application
        $desktopState.MinimizeAttempted = $true
        $desktopShell.MinimizeAll()
        $desktopState.MinimizeSucceeded = $true
    }
foreach ($item in $scenes)
{
    $output = Join-Path $OutputDirectory "$item.png"
    $started = [DateTime]::UtcNow
    $captureStage = 'starting child process'
    $captureFailure = $null
    $childKillRequested = $false
    $windowsPowerShell = Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe'
    $stdoutPath = [System.IO.Path]::GetTempFileName()
    $stderrPath = [System.IO.Path]::GetTempFileName()
    $childArguments = @(
        '-NoProfile', '-STA', '-ExecutionPolicy', 'Bypass', '-File', ('"' + $PSCommandPath + '"'),
        '-Scene', $item, '-CaptureMode', $CaptureMode, '-OutputDirectory', ('"' + $OutputDirectory + '"')
    )
    if ($KeepDesktop) { $childArguments += '-KeepDesktop' }
    else { $childArguments += '-ParentDesktopIsolated' }
    $child = Start-Process -FilePath $windowsPowerShell -ArgumentList $childArguments -PassThru -WindowStyle Hidden -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
    $handle = [IntPtr]::Zero
    try
    {
        if ($CaptureMode -eq 'WpfRender')
        {
            $captureStage = 'waiting for WPF render output'
            $rendered = $false
            for ($attempt = 0; $attempt -lt 360; $attempt++)
            {
                Start-Sleep -Milliseconds 250
                if (Test-Path -LiteralPath $output)
                {
                    $file = Get-Item -LiteralPath $output
                    if ($file.LastWriteTimeUtc -ge $started -and $file.Length -gt 1000)
                    {
                        $rendered = $true
                        break
                    }
                }
                $child.Refresh()
                if ($child.HasExited) { break }
            }
            if (-not $rendered)
            {
                $detail = Get-Content -LiteralPath $stderrPath -Raw
                throw "Scene '$item' failed to render within 90 seconds. $detail"
            }
            Stop-Process -Id $child.Id -Force -ErrorAction SilentlyContinue
            Write-Output $output
            continue
        }
        $captureStage = 'waiting for child window'
        for ($attempt = 0; $attempt -lt 80; $attempt++)
        {
            Start-Sleep -Milliseconds 250
            $child.Refresh()
            if ($child.HasExited)
            {
                throw "Scene '$item' exited before a window appeared (exit code $($child.ExitCode))."
            }
            $handle = $child.MainWindowHandle
            if ($handle -ne [IntPtr]::Zero) { break }
        }
        if ($handle -eq [IntPtr]::Zero) { throw "Scene '$item' did not display a window within 20 seconds." }
        $null = [FluenceCaptureNative]::SetForegroundWindow($handle)
        Start-Sleep -Milliseconds 700
        $captureStage = 'reading child window bounds'
        $bounds = [FluenceCaptureNative+RECT]::new()
        if (-not [FluenceCaptureNative]::GetWindowRect($handle, [ref]$bounds))
        {
            throw "Could not read the window bounds for '$item'."
        }
        $screen = [System.Windows.Forms.SystemInformation]::VirtualScreen
        $left = [Math]::Max($screen.Left, $bounds.Left - $ShadowMargin)
        $top = [Math]::Max($screen.Top, $bounds.Top - $ShadowMargin)
        $right = [Math]::Min($screen.Right, $bounds.Right + $ShadowMargin)
        $bottom = [Math]::Min($screen.Bottom, $bounds.Bottom + $ShadowMargin)
        $bitmap = [System.Drawing.Bitmap]::new($right - $left, $bottom - $top)
        try
        {
            $captureStage = 'copying desktop pixels'
            $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
            try
            {
                $graphics.CopyFromScreen($left, $top, 0, 0, $bitmap.Size)
            }
            finally { $graphics.Dispose() }
            $bitmap.Save($output, [System.Drawing.Imaging.ImageFormat]::Png)
            Write-Output $output
        }
        finally { $bitmap.Dispose() }
    }
    catch
    {
        $captureFailure = $_
        throw
    }
    finally
    {
        if ($null -ne $child -and -not $child.HasExited)
        {
            if ($handle -ne [IntPtr]::Zero)
            {
                $null = [FluenceCaptureNative]::PostMessage($handle, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero)
            }
            if (-not $child.WaitForExit(3000))
            {
                $childKillRequested = $true
                $child.Kill()
                $null = $child.WaitForExit(3000)
            }
        }
        if ($null -ne $captureFailure)
        {
            try
            {
                $diagnosticDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ('fluence-documentation-failure-{0}-{1}' -f $item, [guid]::NewGuid().ToString('N'))
                $null = [System.IO.Directory]::CreateDirectory($diagnosticDirectory)
                $stdoutCopy = Join-Path $diagnosticDirectory 'child-stdout.log'
                $stderrCopy = Join-Path $diagnosticDirectory 'child-stderr.log'
                Copy-Item -LiteralPath $stdoutPath -Destination $stdoutCopy
                Copy-Item -LiteralPath $stderrPath -Destination $stderrCopy
                $child.Refresh()
                $outputFile = if (Test-Path -LiteralPath $output) { Get-Item -LiteralPath $output } else { $null }
                $diagnostic = [ordered]@{
                    Scene = $item
                    CaptureMode = $CaptureMode
                    Stage = $captureStage
                    StartedUtc = $started.ToString('o')
                    FailedUtc = [DateTime]::UtcNow.ToString('o')
                    Error = $captureFailure.Exception.Message
                    ChildId = $child.Id
                    ChildExited = $child.HasExited
                    ChildExitCode = if ($child.HasExited) { $child.ExitCode } else { $null }
                    ChildKillRequested = $childKillRequested
                    ChildWindowHandle = $handle.ToInt64()
                    OutputPath = $output
                    OutputLastWriteUtc = if ($null -ne $outputFile) { $outputFile.LastWriteTimeUtc.ToString('o') } else { $null }
                    OutputBytes = if ($null -ne $outputFile) { $outputFile.Length } else { $null }
                    StdoutPath = $stdoutCopy
                    StderrPath = $stderrCopy
                }
                $diagnostic | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $diagnosticDirectory 'failure.json') -Encoding UTF8
                Write-Warning "Scene '$item' diagnostics preserved in $diagnosticDirectory"
            }
            catch
            {
                Write-Warning "Scene '$item' diagnostics could not be preserved: $($_.Exception.Message)"
            }
        }
        $child.Dispose()
        Remove-Item -LiteralPath $stdoutPath, $stderrPath -Force -ErrorAction SilentlyContinue
    }
}
}
catch
{
    $desktopState.CaptureError = $_.Exception.Message
    throw
}
finally
{
    try
    {
        if ($desktopState.MinimizeAttempted)
        {
            $desktopState.RestoreAttempted = $true
            try
            {
                $desktopShell.UndoMinimizeALL()
                $desktopState.RestoreSucceeded = $true
            }
            catch
            {
                $desktopState.RestoreError = $_.Exception.Message
                throw
            }
        }
    }
    finally
    {
        $desktopState.CompletedUtc = [DateTime]::UtcNow.ToString('o')
        $desktopState | ConvertTo-Json | Set-Content -LiteralPath $desktopStatePath -Encoding UTF8
        Write-Output "Desktop capture state: $desktopStatePath"
    }
}
