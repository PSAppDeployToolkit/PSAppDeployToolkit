#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function Test-ADTMountedWimPath
{
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Path', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'ImagePath')]
        [ValidateNotNullOrEmpty()]
        [System.String]$ImagePath,

        [Parameter(Mandatory = $true, ParameterSetName = 'Path')]
        [ValidateNotNullOrEmpty()]
        [System.String]$Path
    )

    # Get the caller's provided input via the ParameterSetName so we can filter on its name and value.
    $parameter = & $Script:CommandTable.'Get-Variable' -Name $PSCmdlet.ParameterSetName
    return !!(& $Script:CommandTable.'Get-WindowsImage' -Mounted | & { process { if ($_.($parameter.Name) -eq $parameter.Value) { return $_ } } })
}
