---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Add-ADTSessionClosingCallback

## SYNOPSIS
Adds a callback to be executed when the ADT session is closing.

## SYNTAX

```
Add-ADTSessionClosingCallback [-Callback] <CommandInfo[]> [<CommonParameters>]
```

## DESCRIPTION
The Add-ADTSessionClosingCallback function registers a callback command to be executed when the ADT session is closing.
This function sends the callback to the backend function for processing.

## EXAMPLES

### EXAMPLE 1
```
Add-ADTSessionClosingCallback -Callback $myCallback
```

This example adds the specified callback to be executed when the ADT session is closing.

## PARAMETERS

### -Callback
The callback command(s) to be executed when the ADT session is closing.

```yaml
Type: CommandInfo[]
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
### This function does not return any output.
## NOTES
An active ADT session is required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
