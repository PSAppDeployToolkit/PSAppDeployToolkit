function Import-ADTLocalizedStrings
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$UICulture
    )

    # Store the chosen language within this session.
    Import-LocalizedData -BaseDirectory "$((Get-ADTModuleInfo).ModuleBase)\Strings" -FileName strings.psd1 @PSBoundParameters
}
