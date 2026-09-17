#-----------------------------------------------------------------------------
#
# MARK: Get-ADTDefaultStringTable
#
#-----------------------------------------------------------------------------

function Private:Get-ADTDefaultStringTable
{
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Globalization.CultureInfo]$UICulture,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SessionState]$SessionState
    )

    # Resolve the language against the module's defaults, as the config is what specifies any override.
    $config = Get-ADTDefaultConfig
    if (!$PSBoundParameters.ContainsKey('UICulture'))
    {
        $UICulture = Get-ADTStringLanguage -Config $config
    }

    # Import without a base directory so only the module's defaults are used. This substitutes the config's
    # values in via [Expand-ADTConfigValuesInStringTable], leaving only the caller's variables to expand.
    $strings = Import-ADTStringTable -BaseDirectory $null -UICulture $UICulture -Config $config
    if ($PSBoundParameters.ContainsKey('SessionState'))
    {
        Expand-ADTVariablesInHashtable -Hashtable $strings -SessionState $SessionState
    }
    return $strings
}
