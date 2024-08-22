---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Get-ADTSchedulerTask

## SYNOPSIS
Retrieve all details for scheduled tasks on the local computer.

## SYNTAX

```
Get-ADTSchedulerTask [[-TaskName] <String>] [<CommonParameters>]
```

## DESCRIPTION
Retrieve all details for scheduled tasks on the local computer using schtasks.exe.
All property names have spaces and colons removed.
This function is deprecated.
Please migrate your scripts to use the built-in Get-ScheduledTask Cmdlet.

## EXAMPLES

### EXAMPLE 1
```
Get-ADTSchedulerTask
```

This example retrieves a list of all scheduled task properties.

### EXAMPLE 2
```
Get-ADTSchedulerTask | Out-GridView
```

This example displays a grid view of all scheduled task properties.

### EXAMPLE 3
```
Get-ADTSchedulerTask | Select-Object -Property TaskName
```

This example displays a list of all scheduled task names.

## PARAMETERS

### -TaskName
Specify the name of the scheduled task to retrieve details for.
Uses regex match to find scheduled task.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
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

### System.PSObject
### This function returns a PSObject with all scheduled task properties.
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
