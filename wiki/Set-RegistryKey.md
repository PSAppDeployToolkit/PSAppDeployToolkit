# Set-RegistryKey

## SYNOPSIS

Creates a registry key name, value, and value data; it sets the same if it already exists.

## SYNTAX

 `Set-RegistryKey [-Key] <String> [[-Name] <String>] [[-Value] <Object>] [[-Type] {Unknown | String | ExpandString | Binary | DWord | MultiString | QWord | None}] [[-SID] <String>] [[-ContinueOnError] <Boolean>] [<CommonParameters>]`

## DESCRIPTION

Creates a registry key name, value, and value data; it sets the same if it already exists.

## PARAMETERS

`-Key <String>`

The registry key path.

`-Name <String>`

The value name.

`-Value <Object>`

The value data.

`-Type`

The type of registry value to create or set. Options: 'Binary','DWord','ExpandString','MultiString','None','QWord','String','Unknown'. Default: String.

Dword should be specified as a decimal.

`-SID <String>`

The security identifier (SID) for a user. Specifying this parameter will convert a HKEY_CURRENT_USER registry key to the HKEY_USERS\$SID format.

Specify this parameter from the Invoke-HKCURegistrySettingsForAllUsers function to read/edit HKCU registry settings for all users on the system.

`-ContinueOnError <Boolean>`

Continue if an error is encountered. Default is: `$true`.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Set-RegistryKey -Key $blockedAppPath -Name 'Debugger' -Value $blockedAppDebuggerValue`

-------------------------- EXAMPLE 2 --------------------------

`PS C:>Set-RegistryKey -Key 'HKEY_LOCAL_MACHINE\SOFTWARE' -Name 'Application' -Type 'Dword' -Value '1'`

-------------------------- EXAMPLE 3 --------------------------

`PS C:>Set-RegistryKey -Key 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce' -Name 'Debugger' -Value $blockedAppDebuggerValue -Type String`

-------------------------- EXAMPLE 4 --------------------------

`PS C:>Set-RegistryKey -Key 'HKCU\Software\Microsoft\Example' -Name 'Data' -Value`

(0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x02,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x02,0x01,0x01,0x01,0x01,0x01,0x01,0x01,0x00,0x01,0x01,0x01,0x02,0x02,0x02) -Type 'Binary'

-------------------------- EXAMPLE 5 --------------------------

`PS C:>Set-RegistryKey -Key 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Example' -Value '(Default)'`

## REMARKS

To see the examples, type: `Get-Help Set-RegistryKey -Examples`

For more information, type: `Get-Help Set-RegistryKey -Detailed`

For technical information, type: `Get-Help Set-RegistryKey -Full`

For online help, type: `Get-Help Set-RegistryKey -Online`
