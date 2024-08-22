---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Test-ADTRegistryValue

## SYNOPSIS
Test if a registry value exists.

## SYNTAX

```
Test-ADTRegistryValue [-Key] <String> [-Value] <Object> [[-SID] <String>] [-Wow6432Node] [<CommonParameters>]
```

## DESCRIPTION
Checks a registry key path to see if it has a value with a given name.
Can correctly handle cases where a value simply has an empty or null value.

## EXAMPLES

### EXAMPLE 1
```
Test-ADTRegistryValue -Key 'HKLM:SYSTEM\CurrentControlSet\Control\Session Manager' -Value 'PendingFileRenameOperations'
```

Checks if the registry value 'PendingFileRenameOperations' exists under the specified key.

## PARAMETERS

### -Key
Path of the registry key.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName, ByValue)
Accept wildcard characters: False
```

### -Value
Specify the registry key value to check the existence of.

```yaml
Type: Object
Parameter Sets: (All)
Aliases:

Required: True
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -SID
The security identifier (SID) for a user.
Specifying this parameter will convert a HKEY_CURRENT_USER registry key to the HKEY_USERS\$SID format.

Specify this parameter from the Invoke-ADTAllUsersRegistryChange function to read/edit HKCU registry settings for all users on the system.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 3
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Wow6432Node
Specify this switch to check the 32-bit registry (Wow6432Node) on 64-bit systems.

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

### System.String
### Accepts a string value for the registry key path.
## OUTPUTS

### System.Boolean
### Returns $true if the registry value exists, $false if it does not.
## NOTES
An active ADT session is NOT required to use this function.

To test if a registry key exists, use the Test-Path function like so: Test-Path -LiteralPath $Key -PathType 'Container'

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
