---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Install-ADTSCCMSoftwareUpdates

## SYNOPSIS
Scans for outstanding SCCM updates to be installed and installs the pending updates.

## SYNTAX

```
Install-ADTSCCMSoftwareUpdates [[-SoftwareUpdatesScanWaitInSeconds] <Int32>]
 [[-WaitForPendingUpdatesTimeout] <TimeSpan>] [<CommonParameters>]
```

## DESCRIPTION
Scans for outstanding SCCM updates to be installed and installs the pending updates.
Only compatible with SCCM 2012 Client or higher.
This function can take several minutes to run.

## EXAMPLES

### EXAMPLE 1
```
Install-ADTSCCMSoftwareUpdates
```

Scans for outstanding SCCM updates and installs the pending updates with default wait times.

## PARAMETERS

### -SoftwareUpdatesScanWaitInSeconds
The amount of time to wait in seconds for the software updates scan to complete.
Default is: 180 seconds.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: 180
Accept pipeline input: False
Accept wildcard characters: False
```

### -WaitForPendingUpdatesTimeout
The amount of time to wait for missing and pending updates to install before exiting the function.
Default is: 45 minutes.

```yaml
Type: TimeSpan
Parameter Sets: (All)
Aliases:

Required: False
Position: 2
Default value: [System.TimeSpan]::FromMinutes(45)
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable.
For more information, see about_CommonParameters (http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
### You cannot pipe objects to this function.
## OUTPUTS

### None
### This function does not return any objects.
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
