# Test-RegistryValue

## SYNOPSIS

Test if a registry value exists.

## SYNTAX

 `Test-RegistryValue [-Key] <Object> [-Value] <Object> [[-SID] <String>] [<CommonParameters>]`

## DESCRIPTION

Checks a registry key path to see if it has a value with a given name. Can correctly handle cases where a value simply has an empty or null value.

## PARAMETERS

`-Key <Object>`

Path of the registry key.

`-Value <Object>`

Specify the registry key value to check the existence of.

`-SID <String>`

The security identifier (SID) for a user. Specifying this parameter will convert a HKEY_CURRENT_USER registry key to the HKEY_USERS\$SID format.

Specify this parameter from the Invoke-HKCURegistrySettingsForAllUsers function to read/edit HKCU registry settings for all users on the system.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Test-RegistryValue -Key 'HKLM:SYSTEM\CurrentControlSet\Control\Session Manager' -Value 'PendingFileRenameOperations'`

## REMARKS

To see the examples, type: `Get-Help Test-RegistryValue -Examples`

For more information, type: `Get-Help Test-RegistryValue -Detailed`

For technical information, type: `Get-Help Test-RegistryValue -Full`

For online help, type: `Get-Help Test-RegistryValue -Online`
