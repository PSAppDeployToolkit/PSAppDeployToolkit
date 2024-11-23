---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Remove-ADTSessionClosingCallback

## SYNOPSIS
Removes a callback function from the ADT session closing event.

## SYNTAX

```
Remove-ADTSessionClosingCallback [-Callback] <CommandInfo[]> [<CommonParameters>]
```

## DESCRIPTION
This function removes a specified callback function from the ADT session closing event.
The callback function must be provided as a parameter.
If the operation fails, it throws a terminating error.

## EXAMPLES

### EXAMPLE 1
```
Remove-ADTSessionClosingCallback -Callback (Get-Command -Name 'MyCallbackFunction')
```

Removes the specified callback function from the ADT session closing event.

## PARAMETERS

### -Callback
The callback function to remove from the ADT session closing event.

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
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

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
