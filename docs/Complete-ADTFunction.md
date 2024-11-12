---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Complete-ADTFunction

## SYNOPSIS
Completes the execution of an ADT function.

## SYNTAX

```
Complete-ADTFunction [-Cmdlet] <PSCmdlet> [<CommonParameters>]
```

## DESCRIPTION
The Complete-ADTFunction function finalizes the execution of an ADT function by writing a debug log message and restoring the original global verbosity if it was archived off.

## EXAMPLES

### EXAMPLE 1
```
Complete-ADTFunction -Cmdlet $PSCmdlet
```

This example completes the execution of the current ADT function.

## PARAMETERS

### -Cmdlet
The PSCmdlet object representing the cmdlet being completed.

```yaml
Type: PSCmdlet
Parameter Sets: (All)
Aliases:

Required: True
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

### None
### This function does not generate any output.
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
