function Show-FluenceListSelection
{
    <#
    .SYNOPSIS
        Shows a themed dialog with a list of items to pick from and returns the selection.
    .DESCRIPTION
        A single List prompt (see New-FluencePrompt -InputType List) with OK and Cancel buttons. OK
        requires a selection. Returns the selected item, or with -MultiSelect an array of the selected
        items (an array even when one item is selected). Returns $null when the user cancels or the
        dialog times out.

        The shape mirrors the PSADT list selection dialog so a deployment toolkit can map to it
        directly.
    .PARAMETER Items
        The items to list, in display order. At least one is required.
    .PARAMETER Message
        The label shown above the list.
    .PARAMETER Title
        The window title. Defaults to 'Fluence'.
    .PARAMETER MultiSelect
        Allow more than one item to be selected; the result is then an array.
    .PARAMETER DefaultValue
        The item (or, with -MultiSelect, items) selected when the dialog opens.
    .PARAMETER Timeout
        Seconds after which the dialog closes on its own and $null is returned. Between 1 and 86400.
    .PARAMETER Countdown
        Show the remaining seconds in the OK button's caption. Requires -Timeout.
    .PARAMETER Theme
        Auto, Light, Dark, or HighContrast. Omit it to keep the theme already applied to the process;
        the first Fluence call in a process applies Auto.
    .PARAMETER Backdrop
        Mica, Acrylic, Tabbed, None, or Auto. Omit it to keep the backdrop already applied to the
        process; the first Fluence call in a process applies Mica.
    .EXAMPLE
        $region = Show-FluenceListSelection -Message 'Choose a region' -Items 'Europe', 'Americas', 'Asia Pacific'
    .EXAMPLE
        $features = Show-FluenceListSelection -Message 'Features to install' -Items 'Core', 'Docs', 'Samples' -MultiSelect -DefaultValue 'Core'
        if ($null -ne $features) { "Installing: $($features -join ', ')" }
    .OUTPUTS
        System.Object
    .NOTES
        Uses the calling STA thread or a module-owned STA runspace. An existing host application
        is supported only when the command already runs on its dispatcher thread. Blocks until the dialog closes.
    #>
    [CmdletBinding()]
    [OutputType([object])]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [string[]]$Items,

        [Parameter()]
        [string]$Message = 'Select an item',

        [Parameter()]
        [string]$Title = 'Fluence',

        [Parameter()]
        [switch]$MultiSelect,

        [Parameter()]
        [object]$DefaultValue,

        [Parameter()]
        [ValidateRange(1, 86400)]
        [int]$Timeout,

        [Parameter()]
        [switch]$Countdown,

        [Parameter()]
        [ValidateSet('Auto', 'Light', 'Dark', 'HighContrast')]
        [string]$Theme,

        [Parameter()]
        [ValidateSet('Mica', 'Acrylic', 'Tabbed', 'None', 'Auto')]
        [string]$Backdrop
    )

    $promptParams = @{
        Name             = 'Selection'
        Message          = $Message
        InputType        = 'List'
        ValidateSet      = $Items
        ValidateNotEmpty = $true
    }
    if ($MultiSelect)
    {
        $promptParams['MultiSelect'] = $true
    }
    if ($PSBoundParameters.ContainsKey('DefaultValue'))
    {
        $promptParams['DefaultValue'] = $DefaultValue
    }
    $prompt = New-FluencePrompt @promptParams

    $buttons = @(
        New-FluenceButton 'OK' -IsDefault
        New-FluenceButton 'Cancel' -IsCancel
    )

    $dialogParams = @{
        Title   = $Title
        Prompts = @($prompt)
        Buttons = $buttons
    }
    foreach ($name in @('Theme', 'Backdrop', 'Timeout', 'Countdown'))
    {
        if ($PSBoundParameters.ContainsKey($name))
        {
            $dialogParams[$name] = $PSBoundParameters[$name]
        }
    }

    $result = Show-FluenceDialog @dialogParams

    if ($result.Cancelled -or $result.TimedOut)
    {
        return $null
    }

    if ($MultiSelect)
    {
        # Return a real array even for a single selection, and keep it from unrolling on output.
        return , [object[]]@($result.Selection)
    }
    return $result.Selection
}
