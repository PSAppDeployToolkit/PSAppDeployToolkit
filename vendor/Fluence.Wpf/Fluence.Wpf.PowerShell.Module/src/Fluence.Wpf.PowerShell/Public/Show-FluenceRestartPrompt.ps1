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

        The shape mirrors the PSADT restart dialog so a deployment toolkit can map to it directly.
    .PARAMETER Message
        One or more message lines. Defaults to a generic "restart required" sentence.
    .PARAMETER Title
        The window title. Defaults to 'Restart required'.
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
        [string]$Title = 'Restart required',

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
    foreach ($name in @('Theme', 'Backdrop'))
    {
        if ($PSBoundParameters.ContainsKey($name))
        {
            $dialogParams[$name] = $PSBoundParameters[$name]
        }
    }

    $result = Show-FluenceDialog @dialogParams
    return Resolve-FluenceRestartOutcome -Result $result
}
