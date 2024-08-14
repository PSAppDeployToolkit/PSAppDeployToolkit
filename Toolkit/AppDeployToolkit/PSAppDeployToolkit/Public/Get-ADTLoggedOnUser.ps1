#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Get-ADTLoggedOnUser
{
    <#

    .SYNOPSIS
    Get session details for all local and RDP logged on users.

    .DESCRIPTION
    Get session details for all local and RDP logged on users using Win32 APIs. Get the following session details:
        NTAccount, SID, UserName, DomainName, SessionId, SessionName, ConnectState, IsCurrentSession, IsConsoleSession, IsUserSession, IsActiveUserSession
        IsRdpSession, IsLocalAdmin, LogonTime, IdleTime, DisconnectTime, ClientName, ClientProtocolType, ClientDirectory, ClientBuildNumber

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not return any objects.

    .EXAMPLE
    Get-ADTLoggedOnUser

    .NOTES
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

    .NOTES
    This function can be called without an active ADT session.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
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
                return [PSADT.QueryUser]::GetUserSessionInfo()
            }
            catch
            {
                & $Script:CommandTable.'Write-Error' -ErrorRecord $_
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
