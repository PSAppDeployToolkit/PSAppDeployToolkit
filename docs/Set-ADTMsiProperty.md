---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Set-ADTMsiProperty

## SYNOPSIS
Set a property in the MSI property table.

## SYNTAX

```
Set-ADTMsiProperty [-Database] <__ComObject> [-PropertyName] <String> [-PropertyValue] <String>
 [<CommonParameters>]
```

## DESCRIPTION
Set a property in the MSI property table.

## EXAMPLES

### EXAMPLE 1
```
Set-ADTMsiProperty -Database $TempMsiPathDatabase -PropertyName 'ALLUSERS' -PropertyValue '1'
```

## PARAMETERS

### -Database
Specify a ComObject representing an MSI database opened in view/modify/update mode.

```yaml
Type: __ComObject
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PropertyName
The name of the property to be set/modified.

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

### -PropertyValue
The value of the property to be set/modified.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 3
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
### This function does not generate any output.
## NOTES
An active ADT session is NOT required to use this function.

Original Author: Julian DA CUNHA - dacunha.julian@gmail.com, used with permission.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
