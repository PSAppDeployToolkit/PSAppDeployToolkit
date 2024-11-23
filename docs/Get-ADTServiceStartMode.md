---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Get-ADTServiceStartMode

## SYNOPSIS
Retrieves the startup mode of a specified service.

## SYNTAX

```
Get-ADTServiceStartMode [-Service] <ServiceController> [<CommonParameters>]
```

## DESCRIPTION
Retrieves the startup mode of a specified service.
This function checks the service's start type and adjusts the result if the service is set to 'Automatic (Delayed Start)'.

## EXAMPLES

### EXAMPLE 1
```
Get-ADTServiceStartMode -Service (Get-Service -Name 'wuauserv')
```

Retrieves the startup mode of the 'wuauserv' service.

## PARAMETERS

### -Service
Specify the service object to retrieve the startup mode for.

```yaml
Type: ServiceController
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
### You cannot pipe objects to this function.
## OUTPUTS

### System.String
### Returns the startup mode of the specified service.
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
