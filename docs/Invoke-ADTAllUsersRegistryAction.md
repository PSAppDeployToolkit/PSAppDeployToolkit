---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Invoke-ADTAllUsersRegistryAction

## SYNOPSIS
Set current user registry settings for all current users and any new users in the future.

## SYNTAX

```
Invoke-ADTAllUsersRegistryAction [-ScriptBlock] <ScriptBlock[]> [-UserProfiles <UserProfile[]>]
 [<CommonParameters>]
```

## DESCRIPTION
Set HKCU registry settings for all current and future users by loading their NTUSER.dat registry hive file, and making the modifications.
This function will modify HKCU settings for all users even when executed under the SYSTEM account.
To ensure new users in the future get the registry edits, the Default User registry hive used to provision the registry for new users is modified.
This function can be used as an alternative to using ActiveSetup for registry settings.
The advantage of using this function over ActiveSetup is that a user does not have to log off and log back on before the changes take effect.

## EXAMPLES

### EXAMPLE 1
```
Invoke-ADTAllUsersRegistryAction -ScriptBlock {
```

Set-ADTRegistryKey -Key 'HKCU\Software\Microsoft\Office\14.0\Common' -Name 'qmenable' -Value 0 -Type DWord -SID $_.SID
    Set-ADTRegistryKey -Key 'HKCU\Software\Microsoft\Office\14.0\Common' -Name 'updatereliabilitydata' -Value 1 -Type DWord -SID $_.SID
}

### EXAMPLE 2
```
Invoke-ADTAllUsersRegistryAction {
Set-ADTRegistryKey -Key 'HKCU\Software\Microsoft\Office\14.0\Common' -Name 'qmenable' -Value 0 -Type DWord -SID $_.SID
    Set-ADTRegistryKey -Key 'HKCU\Software\Microsoft\Office\14.0\Common' -Name 'updatereliabilitydata' -Value 1 -Type DWord -SID $_.SID
}
```


As the previous example, but showing how to use ScriptBlock as a positional parameter with no name specified.

### EXAMPLE 3
```
Invoke-ADTAllUsersRegistryAction -UserProfiles (Get-ADTUserProfiles -ExcludeDefaultUser) -ScriptBlock {
Set-ADTRegistryKey -Key 'HKCU\Software\Microsoft\Office\14.0\Common' -Name 'qmenable' -Value 0 -Type DWord -SID $_.SID
    Set-ADTRegistryKey -Key 'HKCU\Software\Microsoft\Office\14.0\Common' -Name 'updatereliabilitydata' -Value 1 -Type DWord -SID $_.SID
}
```


As the previous example, but sending specific user profiles through to exclude the Default profile.

## PARAMETERS

### -ScriptBlock
Script block which contains HKCU registry actions to be run for all users on the system.

```yaml
Type: ScriptBlock[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -UserProfiles
Specify the user profiles to modify HKCU registry settings for.
Default is all user profiles except for system profiles.

```yaml
Type: UserProfile[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: (Get-ADTUserProfiles)
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

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)


