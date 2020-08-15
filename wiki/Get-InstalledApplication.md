# Get-InstalledApplication

## SYNOPSIS

Retrieves information about installed applications.

## SYNTAX

 `Get-InstalledApplication [[-Name] <String>] [-Exact] [-WildCard] [-RegEx] [[-ProductCode] <String>] [-IncludeUpdatesAndHotfixes] [<CommonParameters>]`

## DESCRIPTION

Retrieves information about installed applications by querying the registry. You can specify an application name, a product code, or both.

Returns information about application publisher, name & version, product code, uninstall string, install source, location, date, and application architecture.

## PARAMETERS

`-Name <String>`

The name of the application to retrieve information for. Performs a contains match on the application display name by default.

`-Exact [<SwitchParameter>]`

Specifies that the named application must be matched using the exact name.

`-WildCard [<SwitchParameter>]`

Specifies that the named application must be matched using a wildcard search.

`-RegEx [<SwitchParameter>]`

Specifies that the named application must be matched using a regular expression search.

`-ProductCode <String>`

The product code of the application to retrieve information for.

`-IncludeUpdatesAndHotfixes [<SwitchParameter>]`

Include matches against updates and hotfixes in results.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Get-InstalledApplication -Name 'Adobe Flash'`

-------------------------- EXAMPLE 2 --------------------------

`PS C:>Get-InstalledApplication -ProductCode '{1AD147D0-BE0E-3D6C-AC11-64F6DC4163F1}'`

## REMARKS

To see the examples, type: `Get-Help Get-InstalledApplication -Examples`

For more information, type: `Get-Help Get-InstalledApplication -Detailed`

For technical information, type: `Get-Help Get-InstalledApplication -Full`

For online help, type: `Get-Help Get-InstalledApplication -Online`
