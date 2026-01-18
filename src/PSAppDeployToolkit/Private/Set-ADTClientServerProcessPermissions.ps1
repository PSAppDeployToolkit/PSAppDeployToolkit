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

    # If we're running under the active user's account, return early as the user already has access.
    if ([PSADT.AccountManagement.AccountUtilities]::CallerSid.Equals($User.SID))
    {
        return
    }

    # Set required permissions on this module's library files.
    try
    {
        [PSADT.ClientServer.ClientPermissions]::Remediate($User, [System.IO.FileInfo[]]$(
                if (Test-ADTModuleInitialized)
                {
                    (Get-ADTConfig).Assets.Values.GetEnumerator() | & {
                        process
                        {
                            if ($null -eq [PSADT.Utilities.MiscUtilities]::GetBase64StringBytes($_))
                            {
                                return $_
                            }
                        }
                    }
                }))
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}
