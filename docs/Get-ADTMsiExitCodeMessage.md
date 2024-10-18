---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: http://msdn.microsoft.com/en-us/library/aa368542(v=vs.85).aspx
schema: 2.0.0
---

# Get-ADTMsiExitCodeMessage

## SYNOPSIS
Get message for MSI error code.

## SYNTAX

```
Get-ADTMsiExitCodeMessage [-MsiExitCode] <Int32> [<CommonParameters>]
```

## DESCRIPTION
Get message for MSI error code by reading it from msimsg.dll.

## EXAMPLES

### EXAMPLE 1
```
Get-ADTMsiExitCodeMessage -MsiErrorCode 1618
```

## PARAMETERS

### -MsiExitCode
MSI error code.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: 0
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
### Returns the message for the MSI error code.
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[http://msdn.microsoft.com/en-us/library/aa368542(v=vs.85).aspx](http://msdn.microsoft.com/en-us/library/aa368542(v=vs.85).aspx)

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)

