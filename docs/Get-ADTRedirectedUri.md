---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Get-ADTRedirectedUri

## SYNOPSIS
Returns the resolved URI from the provided permalink.

## SYNTAX

```
Get-ADTRedirectedUri [-Uri] <Uri> [[-Headers] <IDictionary>] [<CommonParameters>]
```

## DESCRIPTION
This function gets the resolved/redirected URI from the provided input and returns it to the caller.

## EXAMPLES

### EXAMPLE 1
```
Get-ADTRedirectedUri -Uri https://aka.ms/getwinget
```

Returns the absolute URI for the specified short link, e.g.
https://github.com/microsoft/winget-cli/releases/download/v1.8.1911/Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle

## PARAMETERS

### -Uri
The URL that requires redirection resolution.

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

### -Headers
Any headers that need to be provided for URI redirection resolution.

```yaml
Type: IDictionary
Parameter Sets: (All)
Aliases:

Required: False
Position: 2
Default value: @{ Accept = 'text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7' }
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

### System.Uri
### Get-ADTRedirectedUri returns a Uri of the resolved/redirected URI.
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
