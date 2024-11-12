---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Get-ADTUriFileName

## SYNOPSIS
Returns the filename of the provided URI.

## SYNTAX

```
Get-ADTUriFileName [-Uri] <Uri> [<CommonParameters>]
```

## DESCRIPTION
This function gets the filename of the provided URI from the provided input and returns it to the caller.

## EXAMPLES

### EXAMPLE 1
```
Get-ADTUriFileName -Uri https://aka.ms/getwinget
```

Returns the filename for the specified URI, redirected or otherwise.
e.g.
Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle

## PARAMETERS

### -Uri
The URL that to retrieve the filename from.

```yaml
Type: Uri
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

### System.String
### Get-ADTUriFileName returns a string value of the URI's filename.
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
