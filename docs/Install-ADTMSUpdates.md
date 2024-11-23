---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Install-ADTMSUpdates

## SYNOPSIS
Install all Microsoft Updates in a given directory.

## SYNTAX

```
Install-ADTMSUpdates [-Directory] <String> [<CommonParameters>]
```

## DESCRIPTION
Install all Microsoft Updates of type ".exe", ".msu", or ".msp" in a given directory (recursively search directory).
The function will check if the update is already installed and skip it if it is.
It handles older redistributables and different types of updates appropriately.

## EXAMPLES

### EXAMPLE 1
```
Install-ADTMSUpdates -Directory "$dirFiles\MSUpdates"
```

Installs all Microsoft Updates found in the specified directory.

## PARAMETERS

### -Directory
Directory containing the updates.

```yaml
Type: String
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
### This function does not return any objects.
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
