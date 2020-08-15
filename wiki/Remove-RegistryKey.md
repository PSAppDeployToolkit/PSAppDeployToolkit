# Remove-RegistryKey

## SYNOPSIS

Deletes the specified registry key or value.

## SYNTAX

 `Remove-RegistryKey [-Key] <String> [[-Name] <String>] [-Recurse] [[-SID] <String>] [[-ContinueOnError] <Boolean>] [<CommonParameters>]`

## DESCRIPTION

Deletes the specified registry key or value.

## PARAMETERS

`-Key <String>`

Path of the registry key to delete.

`-Name <String>`

Name of the registry value to delete.

`-Recurse [<SwitchParameter>]`

Delete registry key recursively.

`-SID <String>`

The security identifier (SID) for a user. Specifying this parameter will convert a HKEY_CURRENT_USER registry key to the HKEY_USERS\$SID format.

Specify this parameter from the Invoke-HKCURegistrySettingsForAllUsers function to read/edit HKCU registry settings for all users on the system.

`-ContinueOnError <Boolean>`

Continue if an error is encountered. Default is: `$true`.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Remove-RegistryKey -Key 'HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\RunOnce'`

-------------------------- EXAMPLE 2 --------------------------

`PS C:>Remove-RegistryKey -Key 'HKLM:SOFTWARE\Microsoft\Windows\CurrentVersion\Run' -Name 'RunAppInstall'`

## REMARKS

To see the examples, type: `Get-Help Remove-RegistryKey -Examples`

For more information, type: `Get-Help Remove-RegistryKey -Detailed`

For technical information, type: `Get-Help Remove-RegistryKey -Full`

For online help, type: `Get-Help Remove-RegistryKey -Online`
