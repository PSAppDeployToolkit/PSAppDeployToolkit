#-----------------------------------------------------------------------------
#
# MARK: Invoke-ADTSilentRestart
#
#-----------------------------------------------------------------------------

function Private:Invoke-ADTSilentRestart
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateScript({
                if ($null -eq $_)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Delay -ProvidedValue $_ -ExceptionMessage 'The specified Delay interval was null.'))
                }
                if ($_ -le 0)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Delay -ProvidedValue $_ -ExceptionMessage 'The specified Delay interval must be greater than zero.'))
                }
                return !!$_
            })]
        [System.Nullable[System.UInt32]]$Delay
    )

    # Hand this off to the client/server process to deal with. Run it as this current user though.
    Start-ADTProcess -FilePath ([PSADT.Foundation.EnvironmentInfo]::ClientServerClientLauncherPath) -ArgumentList "/SilentRestart -Delay $Delay" -CreateNoWindow -MsiExecWaitTime 1 -NoWait -InformationAction SilentlyContinue -ErrorAction SilentlyContinue
}