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
        The `Get-ADTLoggedOnUser` function retrieves session details for all local and RDP logged on users using Win32 APIs. It provides basic information about the logged on user's account and logon session.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        PSADT.TerminalServices.SessionInfo

        Returns a SessionInfo object with information about user sessions:
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

        Valid ConnectState enum values are:
        - `WTSActive`: A user is logged on to the session. This state occurs when a user is signed in and actively connected to the device.
        - `WTSConnected`: The session is connected to the client.
        - `WTSConnectQuery`: The session is in the process of connecting to the client.
        - `WTSShadow`: The session is shadowing another session.
        - `WTSDisconnected`: The session is active but the client is disconnected. This state occurs when a user is signed in but not actively connected to the device, such as when the user has chosen to exit to the lock screen.
        - `WTSIdle`: The session is waiting for a client to connect.
        - `WTSListen`: The session is listening for a connection. A listener session waits for requests for new client connections. No user is logged on a listener session. A listener session cannot be reset, shadowed, or changed to a regular client session.
        - `WTSRest`: The session is being reset.
        - `WTSDown`: The session is down due to an error.
        - `WTSInit`: The session is initializing.

        Description of IsActiveUserSession property:
        - If a console user session exists, the active user is the user with the console session.
        - If no console user exists but users are logged in, such as on terminal servers, the first logged-in, non-console user, that has a ConnectState of `WTSActive` or `WTSConnected` is the active user.

        Description of IsRdpSession property:
        - Gets a value indicating whether the user is associated with an RDP client session.

        Description of IsLocalAdmin property:
        - Checks whether the user is a member of the Administrators group

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTLoggedOnUser
    #>

    [CmdletBinding()]
    [OutputType([PSADT.TerminalServices.SessionInfo])]
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
                if (($sessionInfo = [PSADT.TerminalServices.SessionInfo]::GetAsync().GetAwaiter().GetResult()))
                {
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
