---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Initialize-ADTModule

## SYNOPSIS
Initializes the ADT module by setting up necessary configurations and environment.

## SYNTAX

```
Initialize-ADTModule [[-ScriptDirectory] <String>] [<CommonParameters>]
```

## DESCRIPTION
The Initialize-ADTModule function sets up the environment for the ADT module by initializing necessary variables, configurations, and string tables.
It ensures that the module is not initialized while there is an active ADT session in progress.
This function prepares the module for use by clearing callbacks, sessions, and setting up the environment table.

## EXAMPLES

### EXAMPLE 1
```
Initialize-ADTModule
```

Initializes the ADT module with the default settings and configurations.

## PARAMETERS

### -ScriptDirectory
An override directory to use for config and string loading.

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
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
### You cannot pipe objects to this function.
## OUTPUTS

### None
### This function does not return any output.
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
