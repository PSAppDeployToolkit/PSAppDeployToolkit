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


function Show-FluenceRestartPrompt
{
    <#
    .SYNOPSIS
        Shows a themed restart prompt with Restart now and Restart later buttons and an optional countdown.
    .DESCRIPTION
        A deployment-style prompt built on Show-FluenceDialog: a warning icon, a message, a Restart now
        default button and a Restart later cancel button. By default it counts down sixty seconds on
        the Restart now button and returns 'TimedOut' when nobody answers; -Countdown changes the
        seconds and -NoCountdown removes the timer. The window is topmost unless -NotTopmost is given.
        The prompt never restarts the machine itself: act on the returned value.

        The prompt reports a decision; it never restarts the computer.
    .PARAMETER Message
        One or more message lines. Defaults to a generic "restart required" sentence.
    .PARAMETER Title
        The window title. -TitleBarText is an alias.
    .PARAMETER TitleBarIcon
        Title bar icon from a local file path, file: URI, or pack: URI. Other schemes and missing
        local files are rejected before UI dispatch. This does not change a message severity icon.
    .PARAMETER Countdown
        Seconds until the prompt times out, shown on the Restart now button. Defaults to 60.
    .PARAMETER NoCountdown
        Show the prompt without a timer; it stays open until the user answers.
    .PARAMETER Icon
        The severity icon beside the message. Warning (default), Info, Success, Error, Question, or None.
    .PARAMETER NotTopmost
        Do not keep the prompt above other windows.
    .PARAMETER Theme
        Auto, Light, Dark, or HighContrast. Omit it to keep the theme already applied to the process;
        the first Fluence call in a process applies Auto.
    .PARAMETER Backdrop
        Mica, Acrylic, Tabbed, None, or Auto. Omit it to keep the backdrop already applied to the
        process; the first Fluence call in a process applies Mica.
    .EXAMPLE
        switch (Show-FluenceRestartPrompt) {
            'Restart'  { Restart-Computer -Force }
            'TimedOut' { Restart-Computer -Force }
            'Later'    { Write-Output 'Deferred by the user.' }
        }
    .EXAMPLE
        Show-FluenceRestartPrompt -Message 'Contoso Suite was installed.', 'Restart to finish.' -Countdown 300
    .OUTPUTS
        System.String. 'Restart', 'Later', or 'TimedOut'.
    .NOTES
        Uses the calling STA thread or a module-owned STA runspace. An existing host application
        is supported only when the command already runs on its dispatcher thread. Blocks until the prompt closes.
    #>
    [CmdletBinding(DefaultParameterSetName = 'Countdown')]
    [OutputType([string])]
    param
    (
        [Parameter(Position = 0)]
        [string[]]$Message = @('The installation requires a restart to complete. Save your work before restarting.'),

        [Parameter()]
        [Alias('TitleBarText')]
        [string]$Title = 'Restart required',

        [Parameter()]
        [string]$TitleBarIcon,

        [Parameter(ParameterSetName = 'Countdown')]
        [ValidateRange(1, 86400)]
        [int]$Countdown = 60,

        [Parameter(Mandatory = $true, ParameterSetName = 'NoCountdown')]
        [switch]$NoCountdown,

        [Parameter()]
        [ValidateSet('None', 'Info', 'Success', 'Warning', 'Error', 'Question')]
        [string]$Icon = 'Warning',

        [Parameter()]
        [switch]$NotTopmost,

        [Parameter()]
        [ValidateSet('Auto', 'Light', 'Dark', 'HighContrast')]
        [string]$Theme,

        [Parameter()]
        [ValidateSet('Mica', 'Acrylic', 'Tabbed', 'None', 'Auto')]
        [string]$Backdrop
    )

    $buttons = @(
        New-FluenceButton -Text 'Restart now' -Name 'Restart' -IsDefault
        New-FluenceButton -Text 'Restart later' -Name 'Later' -IsCancel
    )

    $dialogParams = @{
        Title   = $Title
        Message = $Message
        Icon    = $Icon
        Buttons = $buttons
        Topmost = (-not [bool]$NotTopmost)
    }
    if (-not $NoCountdown)
    {
        $dialogParams['Timeout'] = $Countdown
        $dialogParams['Countdown'] = $true
    }
    foreach ($name in @('Theme', 'Backdrop', 'TitleBarIcon'))
    {
        if ($PSBoundParameters.ContainsKey($name))
        {
            $dialogParams[$name] = $PSBoundParameters[$name]
        }
    }

    $result = Show-FluenceDialog @dialogParams
    return Resolve-FluenceRestartOutcome -Result $result
}
