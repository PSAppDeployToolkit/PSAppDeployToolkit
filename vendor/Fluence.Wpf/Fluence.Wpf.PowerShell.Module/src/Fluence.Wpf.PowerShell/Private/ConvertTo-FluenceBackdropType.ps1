function ConvertTo-FluenceBackdropType
{
    <#
    .SYNOPSIS
        Converts a backdrop name (Mica, Acrylic, Tabbed, None, Auto) to the library's backdrop enum value.
    .DESCRIPTION
        Resolves the enum type at call time through Resolve-FluenceLibraryType so the module works
        against both the 0.9 name (WindowBackdropType) and the 0.8 name (BackdropType).
    .PARAMETER Backdrop
        The backdrop name. Matching is case-insensitive.
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
        [ValidateSet('Mica', 'Acrylic', 'Tabbed', 'None', 'Auto')]
        [string]$Backdrop
    )

    $type = Resolve-FluenceLibraryType -Name 'WindowBackdropType', 'BackdropType'
    return [System.Enum]::Parse($type, $Backdrop, $true)
}
