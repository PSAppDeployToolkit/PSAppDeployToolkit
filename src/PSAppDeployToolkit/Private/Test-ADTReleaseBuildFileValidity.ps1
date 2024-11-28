#-----------------------------------------------------------------------------
#
# MARK: Test-ADTReleaseBuildInvalidFiles
#
#-----------------------------------------------------------------------------

function Test-ADTReleaseBuildFileValidity
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateScript({
                if ([System.String]::IsNullOrWhiteSpace($_))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName LiteralPath -ProvidedValue $_ -ExceptionMessage 'The specified input is null or empty.'))
                }
                if (![System.IO.Directory]::Exists($_))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName LiteralPath -ProvidedValue $_ -ExceptionMessage 'The specified directory does not exist.'))
                }
                return $_
            })]
        [System.String]$LiteralPath
    )

    # If we're running a release module, ensure the ps*1 files haven't been tampered with.
    if ((Test-ADTModuleIsReleaseBuild) -and ($badFiles = Get-ChildItem @PSBoundParameters -Filter *.ps*1 -Recurse | Get-AuthenticodeSignature | & { process { if (!$_.Status.Equals([System.Management.Automation.SignatureStatus]::Valid)) { return $_ } } }))
    {
        return $badFiles
    }
}
