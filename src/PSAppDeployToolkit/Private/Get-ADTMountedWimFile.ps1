#-----------------------------------------------------------------------------
#
# MARK: Get-ADTMountedWimFile
#
#-----------------------------------------------------------------------------

function Private:Get-ADTMountedWimFile
{
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'ImagePath', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Path', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [CmdletBinding()]
    [OutputType([Microsoft.Dism.Commands.MountedImageInfoObject])]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'ImagePath')]
        [ValidateNotNullOrEmpty()]
        [System.IO.FileInfo[]]$ImagePath,

        [Parameter(Mandatory = $true, ParameterSetName = 'Path')]
        [ValidateNotNullOrEmpty()]
        [System.IO.DirectoryInfo[]]$Path
    )

    # Get the caller's provided input via the ParameterSetName so we can filter on its name and value.
    $parameter = Get-Variable -Name $PSCmdlet.ParameterSetName
    return (Get-WindowsImage -Mounted | & { process { if ($parameter.Value.FullName.Contains($_.($parameter.Name))) { return $_ } } })
}
