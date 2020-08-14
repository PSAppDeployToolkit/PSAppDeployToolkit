# Get-PendingReboot

## SYNOPSIS

Get the pending reboot status on a local computer.

## SYNTAX

 `Get-PendingReboot [<CommonParameters>]`

## DESCRIPTION

Check WMI and the registry to determine if the system has a pending reboot operation from any of the following:

a) Component Based Servicing (Vista, Windows 2008)

b) Windows Update / Auto Update (XP, Windows 2003 / 2008)

c) SCCM 2012 Clients (DetermineIfRebootPending WMI method)

d) App-V Pending Tasks (global based Appv 5.0 SP2)

e) Pending File Rename Operations (XP, Windows 2003 / 2008)

## PARAMETERS

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Get-PendingReboot`

Returns custom object with following properties:

ComputerName, LastBootUpTime, IsSystemRebootPending, IsCBServicingRebootPending, IsWindowsUpdateRebootPending, IsSCCMClientRebootPending, IsFileRenameRebootPending,

PendingFileRenameOperations, ErrorMsg

\*Notes: ErrorMsg only contains something if an error occurred

-------------------------- EXAMPLE 2 --------------------------

`PS C:>(Get-PendingReboot).IsSystemRebootPending`

Returns boolean value determining whether or not there is a pending reboot operation.

## REMARKS

To see the examples, type: `Get-Help Get-PendingReboot -Examples`

For more information, type: `Get-Help Get-PendingReboot -Detailed`

For technical information, type: `Get-Help Get-PendingReboot -Full`

For online help, type: `Get-Help Get-PendingReboot -Online`
