# Convert-RegistryPath

## SYNOPSIS

Converts the specified registry key path to a format that is compatible with built-in PowerShell cmdlets.

## SYNTAX

 `Convert-RegistryPath [-Key] <String> [[-SID] <String>] [<CommonParameters>]`

## DESCRIPTION

Converts the specified registry key path to a format that is compatible with built-in PowerShell cmdlets.

Converts registry key hives to their full paths. Example: HKLM is converted to "Registry::HKEY_LOCAL_MACHINE".

## PARAMETERS

`-Key <String>`

Path to the registry key to convert (can be a registry hive or fully qualified path)

`-SID <String>`

The security identifier (SID) for a user. Specifying this parameter will convert a HKEY_CURRENT_USER registry key to the HKEY_USERS\$SID format.

Specify this parameter from the Invoke-HKCURegistrySettingsForAllUsers function to read/edit HKCU registry settings for all users on the system.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

```PowerShell
PS C:>Convert-RegistryPath -Key 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{1AD147D0-BE0E-3D6C-AC11-64F6DC4163F1}'
```

-------------------------- EXAMPLE 2 --------------------------

```PowerShell
PS C:>Convert-RegistryPath -Key 'HKLM:SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{1AD147D0-BE0E-3D6C-AC11-64F6DC4163F1}'
```

## REMARKS

To see the examples, type: `Get-Help Convert-RegistryPath -Examples`

For more information, type: `Get-Help Convert-RegistryPath -Detailed`

For technical information, type: `Get-Help Convert-RegistryPath -Full`

For online help, type: `Get-Help Convert-RegistryPath -Online`
