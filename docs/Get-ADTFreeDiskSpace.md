---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Get-ADTFreeDiskSpace

## SYNOPSIS
Retrieves the free disk space in MB on a particular drive (defaults to system drive).

## SYNTAX

```
Get-ADTFreeDiskSpace [[-Drive] <DriveInfo>] [<CommonParameters>]
```

## DESCRIPTION
The Get-ADTFreeDiskSpace function retrieves the free disk space in MB on a specified drive.
If no drive is specified, it defaults to the system drive.
This function is useful for monitoring disk space availability.

## EXAMPLES

### EXAMPLE 1
```
Get-ADTFreeDiskSpace -Drive 'C:'
```

This example retrieves the free disk space on the C: drive.

## PARAMETERS

### -Drive
The drive to check free disk space on.

```yaml
Type: DriveInfo
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: [System.IO.Path]::GetPathRoot([System.Environment]::SystemDirectory)
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

### System.Double
### Returns the free disk space in MB.
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
