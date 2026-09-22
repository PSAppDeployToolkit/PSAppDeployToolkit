function Show-FluenceDialog
{
    <#
    .SYNOPSIS
        Shows a themed Fluent dialog built from prompts and buttons, and returns the user's input.
    .DESCRIPTION
        Renders a FluenceWindow with an optional message, a stack of input prompts, and a row of
        buttons. Returns a Fluence.DialogResult object with a property per named prompt and a boolean
        per button, plus Cancelled and TimedOut flags.
    .PARAMETER Title
        The window title.
    .PARAMETER Message
        One or more message lines shown above the prompts.
    .PARAMETER Icon
        Optional severity icon shown to the left of the message. None (default) renders the message
        as plain text. Info, Success, Warning, Error, and Question each map to a Segoe Fluent glyph
        and themed brush (Question uses the Help glyph with the Informational brush).
    .PARAMETER Prompts
        Strings or Fluence.Prompt objects (see New-FluencePrompt). A bare string becomes a Text prompt.
    .PARAMETER Buttons
        Strings or Fluence.Button objects (see New-FluenceButton). Defaults to a single OK button.
        A bare 'Cancel' string is treated as a cancel button (closes on Esc, no validation); any
        other bare string (for example 'No' or 'Close') is a plain button, so build it with
        New-FluenceButton -IsCancel if you want it to act as the Esc/cancel affordance.
    .PARAMETER Timeout
        Seconds after which the dialog closes on its own with TimedOut set to $true and no button
        flag set. Between 1 and 86400.
    .PARAMETER Countdown
        Show the remaining seconds in the caption of the default button (or the first button when
        none is default), refreshed every second. Requires -Timeout.
    .PARAMETER Image
        An image shown above the message at up to 120 device-independent pixels high: a file path,
        a file: URI, or a pack: URI (pack://application:,,,/Assembly;component/path). Other schemes
        are rejected before the dialog opens.
    .PARAMETER MessageAlignment
        Left (default) or Center. Applies to the image and the message lines.
    .PARAMETER Position
        Center (default), TopRight, or BottomRight of the primary work area. With -ParentWindow,
        Center gives way to centering over the owner, but TopRight and BottomRight still win: they
        are applied from the window's Loaded handler, which runs after WPF has placed it.
    .PARAMETER Theme
        Auto, Light, Dark, or HighContrast. Omit it to keep the theme already applied to the process;
        the first Fluence call in a process applies Auto.
    .PARAMETER Backdrop
        Mica, Acrylic, Tabbed, None, or Auto. Omit it to keep the backdrop already applied to the
        process; the first Fluence call in a process applies Mica.
    .PARAMETER Accent
        Optional accent color (System.Windows.Media.Color or a parseable string). Defaults to system accent.
    .PARAMETER MinWidth
        Minimum window width (default 360).
    .PARAMETER Topmost
        Show above other windows.
    .PARAMETER ParentWindow
        An owning System.Windows.Window for modal parenting.
    .EXAMPLE
        Show-FluenceDialog -Title 'Setup' -Prompts 'Your name?' -Buttons OK
    .EXAMPLE
        $r = Show-FluenceDialog -Message 'Installation starts in one minute.' -Buttons 'Start now', 'Defer' -Timeout 60 -Countdown
        if ($r.TimedOut -or $r.'Start now') { Start-Install }

        Counts down on the first button and proceeds when the user clicks it or the minute elapses.
    .OUTPUTS
        Fluence.DialogResult
    .NOTES
        Uses the calling STA thread or a module-owned STA runspace. An existing host application
        is supported only when the command already runs on its dispatcher thread. Blocks until the dialog closes.
    #>
    [CmdletBinding()]
    [OutputType('Fluence.DialogResult')]
    param
    (
        [Parameter()]
        [string]$Title = 'Fluence',

        [Parameter()]
        [string[]]$Message,

        [Parameter()]
        [ValidateSet('None', 'Info', 'Success', 'Warning', 'Error', 'Question')]
        [string]$Icon = 'None',

        [Parameter()]
        [object[]]$Prompts,

        [Parameter()]
        [object[]]$Buttons = @('OK'),

        [Parameter()]
        [ValidateRange(1, 86400)]
        [int]$Timeout,

        [Parameter()]
        [switch]$Countdown,

        [Parameter()]
        [string]$Image,

        [Parameter()]
        [ValidateSet('Left', 'Center')]
        [string]$MessageAlignment = 'Left',

        [Parameter()]
        [ValidateSet('Center', 'TopRight', 'BottomRight')]
        [string]$Position = 'Center',

        [Parameter()]
        [ValidateSet('Auto', 'Light', 'Dark', 'HighContrast')]
        [string]$Theme,

        [Parameter()]
        [ValidateSet('Mica', 'Acrylic', 'Tabbed', 'None', 'Auto')]
        [string]$Backdrop,

        [Parameter()]
        [System.Windows.Media.Color]$Accent,

        [Parameter()]
        [int]$MinWidth = 360,

        [Parameter()]
        [switch]$Topmost,

        [Parameter()]
        [System.Windows.Window]$ParentWindow
    )

    # Caller-thread work: normalize and pre-validate the specification (no UI here).
    if ($Countdown -and -not $PSBoundParameters.ContainsKey('Timeout'))
    {
        throw '-Countdown requires -Timeout.'
    }

    $promptList = @()
    if ($null -ne $Prompts)
    {
        $promptList = ConvertTo-FluencePromptList -InputObject $Prompts
    }
    $buttonList = ConvertTo-FluenceButtonList -InputObject $Buttons
    Test-FluenceResultName -Prompts @($promptList) -Buttons @($buttonList)

    $imageSource = $null
    if ($PSBoundParameters.ContainsKey('Image'))
    {
        $imageSource = Resolve-FluenceImageSource -Image $Image
    }

    $accentColor = $null
    if ($PSBoundParameters.ContainsKey('Accent'))
    {
        $accentColor = $Accent
    }

    $spec = @{
        Title            = $Title
        Message          = $Message
        Icon             = $Icon
        Prompts          = $promptList
        Buttons          = $buttonList
        AccentColor      = $accentColor
        MinWidth         = $MinWidth
        Topmost          = [bool]$Topmost
        ParentWindow     = $ParentWindow
        Countdown        = [bool]$Countdown
        Image            = $imageSource
        MessageAlignment = $MessageAlignment
        Position         = $Position
        CallerRunspaceId = [System.Management.Automation.Runspaces.Runspace]::DefaultRunspace.InstanceId
    }
    if ($PSBoundParameters.ContainsKey('Timeout'))
    {
        $spec['Timeout'] = $Timeout
    }

    # Theme and backdrop reach the spec only when the caller asked for one. An absent key tells
    # Initialize-FluenceApplication to leave the applied theming alone, which is what makes
    # Set-FluenceTheme and Set-FluenceBackdrop survive a later dialog.
    foreach ($name in @('Theme', 'Backdrop'))
    {
        if ($PSBoundParameters.ContainsKey($name))
        {
            $spec[$name] = $PSBoundParameters[$name]
        }
    }

    $result = Invoke-OnFluenceUi -Script {
        param($s)
        Invoke-FluenceWindow -Spec $s
    } -ArgumentList @($spec)

    # Invoke-OnFluenceUi yields the bare result hashtable. A $null result means the UI returned
    # nothing (for example a swallowed fault on an MTA host); surface a cancelled result instead of
    # passing $null to ConvertTo-FluenceResult's Mandatory [hashtable] parameter, which would raise a
    # confusing parameter-binding error far from the real cause.
    if ($null -eq $result)
    {
        return ConvertTo-FluenceResult -Result @{ Cancelled = $true; TimedOut = $false }
    }

    return ConvertTo-FluenceResult -Result $result
}
