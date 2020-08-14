# Install-SCCMSoftwareUpdates

## SYNOPSIS

Scans for outstanding SCCM updates to be installed and installs the pending updates.

## SYNTAX

 `Install-SCCMSoftwareUpdates [[-SoftwareUpdatesScanWaitInSeconds] <Int32>] [[-WaitForPendingUpdatesTimeout] <TimeSpan>] [[-ContinueOnError] <Boolean>] [<CommonParameters>]`

## DESCRIPTION

Scans for outstanding SCCM updates to be installed and installs the pending updates.

Only compatible with SCCM 2012 Client or higher. This function can take several minutes to run.

## PARAMETERS

`-SoftwareUpdatesScanWaitInSeconds <Int32>`

The amount of time to wait in seconds for the software updates scan to complete. Default is: 180 seconds.

`-WaitForPendingUpdatesTimeout <TimeSpan>`

The amount of time to wait for missing and pending updates to install before exiting the function. Default is: 45 minutes.

`-ContinueOnError <Boolean>`

Continue if an error is encountered. Default is: `$true`.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Install-SCCMSoftwareUpdates`

## REMARKS

To see the examples, type: `Get-Help Install-SCCMSoftwareUpdates -Examples`

For more information, type: `Get-Help Install-SCCMSoftwareUpdates -Detailed`

For technical information, type: `Get-Help Install-SCCMSoftwareUpdates -Full`

For online help, type: `Get-Help Install-SCCMSoftwareUpdates -Online`
