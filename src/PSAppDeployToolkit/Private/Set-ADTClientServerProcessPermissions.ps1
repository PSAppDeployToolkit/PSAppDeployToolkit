#-----------------------------------------------------------------------------
#
# MARK: Set-ADTClientServerProcessPermissions
#
#-----------------------------------------------------------------------------

function Private:Set-ADTClientServerProcessPermissions
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [PSADT.Foundation.RunAsActiveUser]$User
    )

    # Set required permissions on this module's library files.
    try
    {
        $null = [PSADT.ClientServer.ClientPermissions]::Remediate($User, [System.IO.FileInfo[]]$(
                if (Test-ADTModuleInitialized)
                {
                    (Get-ADTConfig).Assets.Values.GetEnumerator() | & {
                        process
                        {
                            if (![System.String]::IsNullOrWhiteSpace($_) -and ($null -eq [PSADT.Utilities.MiscUtilities]::GetBase64StringBytes($_)))
                            {
                                return $_
                            }
                        }
                    }
                })).GetAwaiter().GetResult()
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}
