---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Invoke-ADTWebDownload

## SYNOPSIS
Wraps around Invoke-WebRequest to provide logging and retry support.

## SYNTAX

```
Invoke-ADTWebDownload [-Uri] <Uri> [-OutFile] <String> [[-Headers] <IDictionary>] [[-Sha256Hash] <String>]
 [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
This function allows callers to download files as part of a deployment with logging and retry support.

## EXAMPLES

### EXAMPLE 1
```
Invoke-ADTWebDownload -Uri https://aka.ms/getwinget -OutFile "$($adtSession.DirSupportFiles)\Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle"
```

Downloads the latest WinGet installer to the SupportFiles directory.

## PARAMETERS

### -Uri
The URL that to retrieve the file from.

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

### -OutFile
The path of where to save the file to.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Headers
Any headers that need to be provided for file transfer.

```yaml
Type: IDictionary
Parameter Sets: (All)
Aliases:

Required: False
Position: 3
Default value: @{ Accept = 'text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7' }
Accept pipeline input: False
Accept wildcard characters: False
```

### -Sha256Hash
An optional SHA256 reference file hash for download verification.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 4
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PassThru
Returns the WebResponseObject object from Invoke-WebRequest.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
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

### Microsoft.PowerShell.Commands.WebResponseObject
### Invoke-ADTWebDownload returns the results from Invoke-WebRequest if PassThru is specified.
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
