#-----------------------------------------------------------------------------
#
# MARK: Get-ADTLoggedOnUser
#
#-----------------------------------------------------------------------------

function Get-ADTLoggedOnUser
{
    <#
    .SYNOPSIS
        Retrieves session details for all local and RDP logged on users.

    .DESCRIPTION
        The Get-ADTLoggedOnUser function retrieves session details for all local and RDP logged on users using Win32 APIs. It provides information such as NTAccount, SID, UserName, DomainName, SessionId, SessionName, ConnectState, IsCurrentSession, IsConsoleSession, IsUserSession, IsActiveUserSession, IsRdpSession, IsLocalAdmin, LogonTime, IdleTime, DisconnectTime, ClientName, ClientProtocolType, ClientDirectory, and ClientBuildNumber.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        PSADT.Types.UserSessionInfo

        Returns a custom type with information about user sessions:
        - NTAccount
        - SID
        - UserName
        - DomainName
        - SessionId
        - SessionName
        - ConnectState
        - IsCurrentSession
        - IsConsoleSession
        - IsUserSession
        - IsActiveUserSession
        - IsRdpSession
        - IsLocalAdmin
        - LogonTime
        - IdleTime
        - DisconnectTime
        - ClientName
        - ClientProtocolType
        - ClientDirectory
        - ClientBuildNumber

    .EXAMPLE
        Get-ADTLoggedOnUser

        This example retrieves session details for all local and RDP logged on users.

    .NOTES
        An active ADT session is NOT required to use this function.

        Description of ConnectState property:

        Value        Description
        -----        -----------
        Active       A user is logged on to the session.
        ConnectQuery The session is in the process of connecting to a client.
        Connected    A client is connected to the session.
        Disconnected The session is active, but the client has disconnected from it.
        Down         The session is down due to an error.
        Idle         The session is waiting for a client to connect.
        Initializing The session is initializing.
        Listening    The session is listening for connections.
        Reset        The session is being reset.
        Shadowing    This session is shadowing another session.

        Description of IsActiveUserSession property:
        - If a console user exists, then that will be the active user session.
        - If no console user exists but users are logged in, such as on terminal servers, then the first logged-in non-console user that has ConnectState either 'Active' or 'Connected' is the active user.

        Description of IsRdpSession property:
        - Gets a value indicating whether the user is associated with an RDP client session.

        Description of IsLocalAdmin property:
        - Checks whether the user is a member of the Administrators group

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTLoggedOnUser
    #>

    [CmdletBinding()]
    [OutputType([System.Collections.Generic.IReadOnlyList[PSADT.TerminalServices.SessionInfo]])]
    param
    (
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        Write-ADTLogEntry -Message 'Getting session information for all logged on users.'
        try
        {
            try
            {
                if (($sessionInfo = [PSADT.TerminalServices.SessionManager]::GetSessionInfo()))
                {
                    # Write out any local admin check exceptions as warnings prior to writing output.
                    foreach ($session in $sessionInfo)
                    {
                        if ($session.IsLocalAdminException)
                        {
                            try
                            {
                                $naerParams = @{
                                    Exception = [System.InvalidProgramException]::new("Failed to determine whether [$($_.TargetObject.NTAccount)] is a local administrator.", $session.IsLocalAdminException)
                                    Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                                    ErrorId = 'SessionInfoIsLocalAdminError'
                                    TargetObject = $session
                                }
                                Write-Error -ErrorRecord (New-ADTErrorRecord @naerParams)
                            }
                            catch
                            {
                                Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -ErrorAction SilentlyContinue
                            }
                        }
                    }
                    return $sessionInfo
                }
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
