function ConvertTo-FluenceCornerPreference
{
    <#
    .SYNOPSIS
        Converts a corner style name (Default, DoNotRound, Round, RoundSmall) to the library's corner enum value.
    .DESCRIPTION
        Resolves the enum type at call time through Resolve-FluenceLibraryType so the module works
        against both the 0.9 name (WindowCornerPreference) and the 0.8 name (CornerPreference).
    .PARAMETER CornerStyle
        The corner style name. Matching is case-insensitive.
    .OUTPUTS
        System.Enum
    .NOTES
        Does not require a host application, but the Fluence.Wpf assembly must be loaded.
    #>
    [CmdletBinding()]
    [OutputType([System.Enum])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateSet('Default', 'DoNotRound', 'Round', 'RoundSmall')]
        [string]$CornerStyle
    )

    $type = Resolve-FluenceLibraryType -Name 'WindowCornerPreference', 'CornerPreference'
    return [System.Enum]::Parse($type, $CornerStyle, $true)
}
