---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Register-ADTDll

## SYNOPSIS
Register a DLL file.

## SYNTAX

```
Register-ADTDll [-FilePath] <String> [<CommonParameters>]
```

## DESCRIPTION
This function registers a DLL file using regsvr32.exe.
It ensures that the specified DLL file exists before attempting to register it.
If the file does not exist, it throws an error.

## EXAMPLES

### EXAMPLE 1
```
Register-ADTDll -FilePath "C:\Test\DcTLSFileToDMSComp.dll"
```

Registers the specified DLL file.

## PARAMETERS

### -FilePath
Path to the DLL file.

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

### None
### This function does not return objects.
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
