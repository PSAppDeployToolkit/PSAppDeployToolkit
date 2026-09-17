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
        [PSAppDeployToolkit.Attributes.ValidateUnique()]
        [ValidateNotNullOrEmpty()]
        [System.IO.FileInfo[]]$ImagePath,

        [Parameter(Mandatory = $true, ParameterSetName = 'Path')]
        [PSAppDeployToolkit.Attributes.ValidateUnique()]
        [ValidateNotNullOrEmpty()]
        [System.IO.DirectoryInfo[]]$Path
    )

    # The array cast keeps one value and several behaving alike. Member enumeration over a single-element
    # array unwraps to the value, which made this String.Contains and matched on any substring.
    $parameter = Get-Variable -Name $PSCmdlet.ParameterSetName
    $separators = [System.Char[]]([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $wanted = [System.String[]]$parameter.Value.FullName | & { process { $_.TrimEnd($separators) } }
    return (Get-WindowsImage -Mounted | & { process { if ($wanted -contains $_.($parameter.Name).TrimEnd($separators)) { return $_ } } })
}
