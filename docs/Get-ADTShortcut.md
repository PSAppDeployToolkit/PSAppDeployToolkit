---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Get-ADTShortcut

## SYNOPSIS
Get information from a .lnk or .url type shortcut.

## SYNTAX

```
Get-ADTShortcut [-Path] <String> [<CommonParameters>]
```

## DESCRIPTION
Get information from a .lnk or .url type shortcut.
Returns a hashtable with details about the shortcut such as TargetPath, Arguments, Description, and more.

## EXAMPLES

### EXAMPLE 1
```
Get-ADTShortcut -Path "$envProgramData\Microsoft\Windows\Start Menu\My Shortcut.lnk"
```

Retrieves information from the specified .lnk shortcut.

## PARAMETERS

### -Path
Path to the shortcut to get information from.

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
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable.
For more information, see about_CommonParameters (http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
### You cannot pipe objects to this function.
## OUTPUTS

### System.Collections.Hashtable
### Returns a hashtable with the following keys:
### - TargetPath
### - Arguments
### - Description
### - WorkingDirectory
### - WindowStyle
### - Hotkey
### - IconLocation
### - IconIndex
### - RunAsAdmin
## NOTES
Url shortcuts only support TargetPath, IconLocation, and IconIndex.

An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
