# Get-LoggedOnUser

## SYNOPSIS

Get session details for all local and RDP logged on users.

## SYNTAX

 `Get-LoggedOnUser [<CommonParameters>]`

## DESCRIPTION

Get session details for all local and RDP logged on users using Win32 APIs. Get the following session details:

NTAccount, SID, UserName, DomainName, SessionId, SessionName, ConnectState, IsCurrentSession, IsConsoleSession, IsUserSession, IsActiveUserSession

IsRdpSession, IsLocalAdmin, LogonTime, IdleTime, DisconnectTime, ClientName, ClientProtocolType, ClientDirectory, ClientBuildNumber

## PARAMETERS

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Get-LoggedOnUser`

## REMARKS

To see the examples, type: `Get-Help Get-LoggedOnUser -Examples`

For more information, type: `Get-Help Get-LoggedOnUser -Detailed`

For technical information, type: `Get-Help Get-LoggedOnUser -Full`

For online help, type: `Get-Help Get-LoggedOnUser -Online`
