# Remove-MSIApplications

## SYNOPSIS

Removes all MSI applications matching the specified application name.

## SYNTAX

 `Remove-MSIApplications [-Name] <String> [-Exact] [-WildCard] [[-Parameters] <String>] [[-AddParameters] <String>] [[-FilterApplication] <Array>] [[-ExcludeFromUninstall] <Array>]`

[-IncludeUpdatesAndHotfixes] [[-LoggingOptions] <String>] [[-private:LogName] <String>] [-PassThru] [[-ContinueOnError] <Boolean>] [<CommonParameters>]

## DESCRIPTION

Removes all MSI applications matching the specified application name.

Enumerates the registry for installed applications matching the specified application name and uninstalls that application using the product code, provided the uninstall string matches

"msiexec".

## PARAMETERS

`-Name <String>`

The name of the application to uninstall. Performs a contains match on the application display name by default.

`-Exact [<SwitchParameter>]`

Specifies that the named application must be matched using the exact name.

`-WildCard [<SwitchParameter>]`

Specifies that the named application must be matched using a wildcard search.

`-Parameters <String>`

Overrides the default parameters specified in the XML configuration file. Uninstall default is: "REBOOT=ReallySuppress /QN".

`-AddParameters <String>`

Adds to the default parameters specified in the XML configuration file. Uninstall default is: "REBOOT=ReallySuppress /QN".

`-FilterApplication <Array>`

Two-dimensional array that contains one or more (property, value, match-type) sets that should be used to filter the list of results returned by Get-InstalledApplication to only those that

should be uninstalled.

Properties that can be filtered upon: ProductCode, DisplayName, DisplayVersion, UninstallString, InstallSource, InstallLocation, InstallDate, Publisher, Is64BitApplication

`-ExcludeFromUninstall <Array>`

Two-dimensional array that contains one or more (property, value, match-type) sets that should be excluded from uninstall if found.

Properties that can be excluded: ProductCode, DisplayName, DisplayVersion, UninstallString, InstallSource, InstallLocation, InstallDate, Publisher, Is64BitApplication

`-IncludeUpdatesAndHotfixes [<SwitchParameter>]`

Include matches against updates and hotfixes in results.

`-LoggingOptions <String>`

Overrides the default logging options specified in the XML configuration file. Default options are: "/L\*v".

`-private:LogName <String>`

`-PassThru[<SwitchParameter>]`

Returns ExitCode, STDOut, and STDErr output from the process.

`-ContinueOnError <Boolean>`

Continue if an exit code is returned by msiexec that is not recognized by the App Deploy Toolkit. Default is: `$true`.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Remove-MSIApplications -Name 'Adobe Flash'`

Removes all versions of software that match the name "Adobe Flash"

-------------------------- EXAMPLE 2 --------------------------

`PS C:>Remove-MSIApplications -Name 'Adobe'`

Removes all versions of software that match the name "Adobe"

-------------------------- EXAMPLE 3 --------------------------

`PS C:>Remove-MSIApplications -Name 'Java 8 Update' -FilterApplication ('Is64BitApplication', `$false`, 'Exact'),('Publisher', 'Oracle Corporation', 'Exact')`

Removes all versions of software that match the name "Java 8 Update" where the software is 32-bits and the publisher is "Oracle Corporation".

-------------------------- EXAMPLE 4 --------------------------

`PS C:>Remove-MSIApplications -Name 'Java 8 Update' -FilterApplication (,('Publisher', 'Oracle Corporation', 'Exact')) -ExcludeFromUninstall (,('DisplayName', 'Java 8 Update 45', 'Contains'))`

Removes all versions of software that match the name "Java 8 Update" and also have "Oracle Corporation" as the Publisher; however, it does not uninstall "Java 8 Update 45" of the software.

NOTE: if only specifying a single row in the two-dimensional arrays, the array must have the extra parentheses and leading comma as in this example.

-------------------------- EXAMPLE 5 --------------------------

`PS C:>Remove-MSIApplications -Name 'Java 8 Update' -ExcludeFromUninstall (,('DisplayName', 'Java 8 Update 45', 'Contains'))`

Removes all versions of software that match the name "Java 8 Update"; however, it does not uninstall "Java 8 Update 45" of the software.

NOTE: if only specifying a single row in the two-dimensional array, the array must have the extra parentheses and leading comma as in this example.

-------------------------- EXAMPLE 6 --------------------------

`PS C:>Remove-MSIApplications -Name 'Java 8 Update' -ExcludeFromUninstall`

('Is64BitApplication', `$true`, 'Exact'),

('DisplayName', 'Java 8 Update 45', 'Exact'),

('DisplayName', 'Java 8 Update 4\*', 'WildCard'),

('DisplayName', 'Java \d Update \d{3}', 'RegEx'),

('DisplayName', 'Java 8 Update', 'Contains')

Removes all versions of software that match the name "Java 8 Update"; however, it does not uninstall 64-bit versions of the software, Update 45 of the software, or any Update that starts with

4\.

## REMARKS

To see the examples, type: `Get-Help Remove-MSIApplications -Examples`

For more information, type: `Get-Help Remove-MSIApplications -Detailed`

For technical information, type: `Get-Help Remove-MSIApplications -Full`

For online help, type: `Get-Help Remove-MSIApplications -Online`
