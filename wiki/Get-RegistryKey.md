# Get-RegistryKey

## SYNOPSIS

Retrieves value names and value data for a specified registry key or optionally, a specific value.

## SYNTAX

 `Get-RegistryKey [-Key] <String> [[-Value] <String>] [[-SID] <String>] [-ReturnEmptyKeyIfExists] [-DoNotExpandEnvironmentNames] [[-ContinueOnError] <Boolean>] [<CommonParameters>]`

## DESCRIPTION

Retrieves value names and value data for a specified registry key or optionally, a specific value.

If the registry key does not exist or contain any values, the function will return $null by default. To test for existence of a registry key path, use built-in Test-Path cmdlet.

## PARAMETERS

`-Key <String>`

Path of the registry key.

`-Value <String>`

Value to retrieve (optional).

`-SID <String>`

The security identifier (SID) for a user. Specifying this parameter will convert a HKEY_CURRENT_USER registry key to the HKEY_USERS\$SID format.

Specify this parameter from the Invoke-HKCURegistrySettingsForAllUsers function to read/edit HKCU registry settings for all users on the system.

`-ReturnEmptyKeyIfExists [<SwitchParameter>]`

Return the registry key if it exists but it has no property/value pairs underneath it. Default is: `$false`.

`-DoNotExpandEnvironmentNames [<SwitchParameter>]`

Return unexpanded REG_EXPAND_SZ values. Default is: `$false`.

`-ContinueOnError <Boolean>`

Continue if an error is encountered. Default is: `$true`.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Get-RegistryKey -Key 'HKLM:SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{1AD147D0-BE0E-3D6C-AC11-64F6DC4163F1}'`

-------------------------- EXAMPLE 2 --------------------------

`PS C:>Get-RegistryKey -Key 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\iexplore.exe'`

-------------------------- EXAMPLE 3 --------------------------

`PS C:>Get-RegistryKey -Key 'HKLM:Software\Wow6432Node\Microsoft\Microsoft SQL Server Compact Edition\v3.5' -Value 'Version'`

-------------------------- EXAMPLE 4 --------------------------

`PS C:>Get-RegistryKey -Key 'HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment' -Value 'Path' -DoNotExpandEnvironmentNames`

Returns %ProgramFiles%\Java instead of C:\Program Files\Java

-------------------------- EXAMPLE 5 --------------------------

`PS C:>Get-RegistryKey -Key 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Example' -Value '(Default)'`

## REMARKS

To see the examples, type: `Get-Help Get-RegistryKey -Examples`

For more information, type: `Get-Help Get-RegistryKey -Detailed`

For technical information, type: `Get-Help Get-RegistryKey -Full`

For online help, type: `Get-Help Get-RegistryKey -Online`
