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


function Show-FluenceProgress
{
    <#
    .SYNOPSIS
        Shows a non-modal themed progress window and returns a handle for updating and closing it.
    .DESCRIPTION
        Opens a fixed-width FluenceWindow with a message line, an optional detail line, and a Fluence
        ProgressBar, indeterminate by default. The call returns as soon as the window is shown; the
        script keeps running. Change the text or percentage with Update-FluenceProgress and close the
        window with Close-FluenceProgress. The user cannot close it: the caption buttons are hidden.

        On the inline STA host (Windows PowerShell and pwsh by default) the window shares the caller's
        thread, so it repaints on every Show-FluenceProgress and Update-FluenceProgress call and stays
        static in between; update it at least every few seconds during long work. On an MTA host
        (pwsh -mta) the window lives on the module-owned UI runspace, stays responsive between updates,
        and other Fluence dialogs can still be shown while it is open. An existing host application is supported only when this command runs on its dispatcher
        thread; automatic dispatch to a foreign UI thread is not supported.

        Use Message as the required status line. Add Detail for a second line, and PercentComplete
        when progress is determinate.
    .PARAMETER Message
        The main status line.
    .PARAMETER Title
        The window title. -TitleBarText is an alias.
    .PARAMETER TitleBarIcon
        Title bar icon from a local file path, file: URI, or pack: URI. Other schemes and missing
        local files are rejected before UI dispatch. This does not change a message severity icon.
    .PARAMETER Detail
        An optional second line in the secondary text color, for example the current file or step.
    .PARAMETER PercentComplete
        When given, the bar is determinate at this value (clamped to 0..100). Omit for an
        indeterminate bar.
    .PARAMETER NotTopmost
        Do not keep the window above other windows. Topmost is the default.
    .PARAMETER Position
        Center (default), TopRight, or BottomRight of the primary work area.
    .PARAMETER Width
        The window width in device-independent pixels. Defaults to 450.
    .PARAMETER Theme
        Auto, Light, Dark, or HighContrast. Omit it to keep the theme already applied to the process;
        the first Fluence call in a process applies Auto.
    .PARAMETER Backdrop
        Mica, Acrylic, Tabbed, None, or Auto. Omit it to keep the backdrop already applied to the
        process; the first Fluence call in a process applies Mica.
    .PARAMETER Accent
        Optional accent color (System.Windows.Media.Color or a parseable string). Defaults to system accent.
    .EXAMPLE
        $progress = Show-FluenceProgress -Title 'Contoso Suite' -Message 'Installing...' -Detail 'Copying files'
        Update-FluenceProgress -Handle $progress -Detail 'Registering components' -PercentComplete 60
        Close-FluenceProgress -Handle $progress
    .OUTPUTS
        Fluence.ProgressHandle
    .NOTES
        Uses the calling STA thread or a module-owned STA runspace. An existing host application
        is supported only when the command already runs on its dispatcher thread. Only one progress window can be
        open at a time.
    #>
    [CmdletBinding()]
    [OutputType('Fluence.ProgressHandle')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Shows a transient in-process window; makes no persistent or destructive system change, so ShouldProcess prompting is not appropriate.')]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [string]$Message,

        [Parameter()]
        [Alias('TitleBarText')]
        [string]$Title = 'Fluence',

        [Parameter()]
        [string]$TitleBarIcon,

        [Parameter()]
        [string]$Detail,

        [Parameter()]
        [double]$PercentComplete,

        [Parameter()]
        [switch]$NotTopmost,

        [Parameter()]
        [ValidateSet('Center', 'TopRight', 'BottomRight')]
        [string]$Position = 'Center',

        [Parameter()]
        [ValidateRange(200, 1600)]
        [int]$Width = 450,

        [Parameter()]
        [ValidateSet('Auto', 'Light', 'Dark', 'HighContrast')]
        [string]$Theme,

        [Parameter()]
        [ValidateSet('Mica', 'Acrylic', 'Tabbed', 'None', 'Auto')]
        [string]$Backdrop,

        [Parameter()]
        [System.Windows.Media.Color]$Accent
    )

    if ($null -ne $script:ProgressHandle -and $script:ProgressHandle.IsOpen)
    {
        throw 'A Fluence progress window is already open. Close it with Close-FluenceProgress before showing another.'
    }

    # A completed pump still owns disposable pipeline handles until collected.
    if ($null -ne $script:ProgressHandle -and $null -ne $script:UiPump)
    {
        Close-FluenceProgress -Handle $script:ProgressHandle
    }

    $accentColor = $null
    if ($PSBoundParameters.ContainsKey('Accent'))
    {
        $accentColor = $Accent
    }

    $titleBarIconSource = $null
    if ($PSBoundParameters.ContainsKey('TitleBarIcon'))
    {
        $titleBarIconSource = Resolve-FluenceImageSource -Image $TitleBarIcon -ParameterName 'TitleBarIcon'
    }

    # The state hashtable is shared with the UI thread (and, on an MTA host, with another runspace),
    # so it is synchronized. CloseRequested is the pump's exit signal.
    $state = [hashtable]::Synchronized((Resolve-FluenceProgressState -Bound $PSBoundParameters))
    $state.CloseRequested = $false

    $spec = @{
        Title       = $Title
        TitleBarIcon = $titleBarIconSource
        Topmost     = (-not [bool]$NotTopmost)
        Position    = $Position
        Width       = $Width
        AccentColor = $accentColor
    }

    # Theme and backdrop reach the spec only when the caller asked for one, so a progress window
    # opened after Set-FluenceTheme keeps that theme; see Initialize-FluenceApplication.
    foreach ($name in @('Theme', 'Backdrop'))
    {
        if ($PSBoundParameters.ContainsKey($name))
        {
            $spec[$name] = $PSBoundParameters[$name]
        }
    }

    $mode = Get-FluenceUiHostMode
    $handle = [pscustomobject]@{
        PSTypeName = 'Fluence.ProgressHandle'
        Id         = [guid]::NewGuid()
        Mode       = $mode
        IsOpen     = $true
        State      = $state
        Spec       = $spec
        Parts      = $null
    }

    if ($mode -eq 'Runspace')
    {
        # The STA runspace must stay pumping for the window to remain live, so the window is shown by
        # an asynchronous pipeline that keeps running until Close-FluenceProgress; later UI work is
        # queued to that pump by Invoke-InFluenceStaRunspace.
        $script:OwnsApplication = $true
        $null = Initialize-FluenceStaRunspace

        $pump = [hashtable]::Synchronized(@{
                Active      = $true
                Queue       = [System.Collections.Concurrent.ConcurrentQueue[object]]::new()
                Shown       = [System.Threading.ManualResetEventSlim]::new($false)
                PowerShell  = $null
                AsyncResult = $null
            })
        $ps = [powershell]::Create()
        $ps.Runspace = $script:StaRunspace
        $null = $ps.AddScript({
                param($h, $p)
                $module = Get-Module -Name 'Fluence.Wpf.PowerShell'
                & $module { param($h2, $p2) Start-FluenceUiPump -Handle $h2 -Pump $p2 } $h $p
            })
        $null = $ps.AddArgument($handle)
        $null = $ps.AddArgument($pump)
        $pump.PowerShell = $ps
        $script:UiPump = $pump
        $script:ProgressHandle = $handle
        $pump.AsyncResult = $ps.BeginInvoke()

        $deadline = [System.DateTime]::UtcNow.AddSeconds(15)
        while (-not $pump.Shown.Wait(250))
        {
            $pipelineState = $ps.InvocationStateInfo.State
            if ($pipelineState -in @('Completed', 'Failed', 'Stopped') -or [System.DateTime]::UtcNow -gt $deadline)
            {
                $reason = "pipeline state $pipelineState"
                if ($ps.Streams.Error.Count -gt 0)
                {
                    $reason = $ps.Streams.Error[0].ToString()
                }
                $handle.State.CloseRequested = $true
                if ($pump.AsyncResult.AsyncWaitHandle.WaitOne(5000))
                {
                    $ps.Dispose()
                    $pump.Shown.Dispose()
                    $script:UiPump = $null
                    $script:ProgressHandle = $null
                }
                throw "The progress window did not open on the Fluence UI runspace: $reason"
            }
        }
    }
    else
    {
        $parts = Invoke-OnFluenceUi -Script {
            param($h)
            Initialize-FluenceApplication -Theme $h.Spec.Theme -Backdrop $h.Spec.Backdrop -Accent $h.Spec.AccentColor
            $built = New-FluenceProgressWindow -Spec $h.Spec -State $h.State
            $built.Window.Show()
            if ($h.Mode -eq 'Inline')
            {
                Invoke-FluenceDispatcherPump
            }
            return $built
        } -ArgumentList @($handle)
        $handle.Parts = $parts
    }

    $script:ProgressHandle = $handle
    return $handle
}
