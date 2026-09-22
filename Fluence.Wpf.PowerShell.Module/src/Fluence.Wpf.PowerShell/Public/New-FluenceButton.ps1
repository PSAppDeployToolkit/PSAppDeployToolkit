function New-FluenceButton
{
    <#
    .SYNOPSIS
        Builds a single button specification for Show-FluenceDialog.
    .DESCRIPTION
        Returns a Fluence.Button object carrying the caption, the result key, and the default and
        cancel flags. Pass one or more to Show-FluenceDialog -Buttons; a plain string there is
        shorthand for a button with no flags, so this cmdlet is what you use to mark one.
    .PARAMETER Text
        The button caption (and the default result key).
    .PARAMETER Name
        The result key; defaults to Text.
    .PARAMETER IsDefault
        Mark as the default button (activated by Enter).
    .PARAMETER IsCancel
        Mark as the cancel button (activated by Esc; skips input validation).
    .EXAMPLE
        New-FluenceButton -Text 'Sign in' -Name Login -IsDefault
    .EXAMPLE
        Show-FluenceDialog -Message 'Delete the folder?' -Buttons (New-FluenceButton 'Delete' -IsDefault), (New-FluenceButton 'Keep' -IsCancel)
    .OUTPUTS
        Fluence.Button
    .NOTES
        Does not require a host application.
    #>
    [CmdletBinding()]
    [OutputType('Fluence.Button')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Builds an in-memory specification object; changes no system state.')]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [string]$Text,

        [Parameter()]
        [string]$Name,

        [Parameter()]
        [switch]$IsDefault,

        [Parameter()]
        [switch]$IsCancel
    )

    if ([string]::IsNullOrWhiteSpace($Name))
    {
        $Name = $Text
    }

    return [pscustomobject]@{
        PSTypeName = 'Fluence.Button'
        Name       = $Name
        Text       = $Text
        IsDefault  = [bool]$IsDefault
        IsCancel   = [bool]$IsCancel
    }
}
