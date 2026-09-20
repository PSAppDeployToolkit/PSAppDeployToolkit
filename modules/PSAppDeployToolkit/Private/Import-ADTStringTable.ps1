#-----------------------------------------------------------------------------
#
# MARK: Import-ADTStringTable
#
#-----------------------------------------------------------------------------

function Private:Import-ADTStringTable
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [AllowNull()][PSAppDeployToolkit.Attributes.AllowNullButNotEmptyOrWhiteSpace()]
        [PSAppDeployToolkit.Attributes.ValidateUnique()]
        [System.String[]]$BaseDirectory,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Globalization.CultureInfo]$UICulture,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Hashtable]$Config
    )

    # Import string table, perform value substitutions, then return it to the caller.
    $null = $PSBoundParameters.Remove('Config')
    $strings = Import-ADTModuleDataFile @PSBoundParameters -FileName strings.psd1 -IgnorePolicy
    Expand-ADTConfigValuesInStringTable -Hashtable $strings -Config $Config
    return $strings
}
