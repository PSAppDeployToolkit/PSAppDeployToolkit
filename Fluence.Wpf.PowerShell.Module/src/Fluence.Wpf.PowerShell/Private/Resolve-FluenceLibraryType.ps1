function Resolve-FluenceLibraryType
{
    <#
    .SYNOPSIS
        Resolves a Fluence.Wpf type by trying a list of candidate short names in order.
    .DESCRIPTION
        The library renamed a handful of public types in 0.9.0-pre (BackdropType became
        WindowBackdropType, CornerPreference became WindowCornerPreference). The module binds to
        whichever Fluence.Wpf assembly is loaded, which may be the module's own staged build or a
        host's older copy, so type names are resolved at call time rather than written as literal
        casts. Candidates are tried in the order given; the first that resolves wins.
    .PARAMETER Name
        One or more short type names inside the Fluence.Wpf namespace, newest name first.
    .OUTPUTS
        System.Type
    .NOTES
        Does not require a host application, but the Fluence.Wpf assembly must be loaded.
    #>
    [CmdletBinding()]
    [OutputType([System.Type])]
    param
    (
        [Parameter(Mandatory = $true)]
        [string[]]$Name
    )

    foreach ($candidate in $Name)
    {
        $type = ('Fluence.Wpf.' + $candidate) -as [System.Type]
        if ($null -ne $type)
        {
            return $type
        }
    }

    throw "None of the Fluence.Wpf types '$($Name -join "', '")' is available in the loaded Fluence.Wpf assembly."
}
