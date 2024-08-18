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
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Path
    )

    return !!(& $Script:CommandTable.'Get-WindowsImage' -Mounted | & { process { if ($_.Path -eq $Path) { return $_ } } })
}
