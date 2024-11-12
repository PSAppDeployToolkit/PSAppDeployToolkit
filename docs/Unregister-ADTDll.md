---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Unregister-ADTDll

## SYNOPSIS
Unregister a DLL file.

## SYNTAX

```
Unregister-ADTDll [-FilePath] <String> [<CommonParameters>]
```

## DESCRIPTION
Unregister a DLL file using regsvr32.exe.
This function takes the path to the DLL file and attempts to unregister it using the regsvr32.exe utility.

## EXAMPLES

### EXAMPLE 1
```
Unregister-ADTDll -FilePath "C:\Test\DcTLSFileToDMSComp.dll"
```

Unregisters the specified DLL file.

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
Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
