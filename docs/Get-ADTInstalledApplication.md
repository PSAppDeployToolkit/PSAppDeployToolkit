---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Get-ADTInstalledApplication

## SYNOPSIS
Retrieves information about installed applications.

## SYNTAX

```
Get-ADTInstalledApplication [[-Name] <String[]>] [[-ProductCode] <String[]>] [-Exact] [-WildCard] [-RegEx]
 [-IncludeUpdatesAndHotfixes] [<CommonParameters>]
```

## DESCRIPTION
Retrieves information about installed applications by querying the registry.
You can specify an application name, a product code, or both.
Returns information about application publisher, name & version, product code, uninstall string, install source, location, date, and application architecture.

## EXAMPLES

### EXAMPLE 1
```
Get-ADTInstalledApplication -Name 'Adobe Flash'
```

This example retrieves information about installed applications with the name 'Adobe Flash'.

### EXAMPLE 2
```
Get-ADTInstalledApplication -ProductCode '{1AD147D0-BE0E-3D6C-AC11-64F6DC4163F1}'
```

This example retrieves information about the installed application with the specified product code.

## PARAMETERS

### -Name
The name of the application to retrieve information for.
Performs a contains match on the application display name by default.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProductCode
The product code of the application to retrieve information for.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Exact
Specifies that the named application must be matched using the exact name.

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

### -WildCard
Specifies that the named application must be matched using a wildcard search.

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

### -RegEx
Specifies that the named application must be matched using a regular expression search.

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

### -IncludeUpdatesAndHotfixes
Include matches against updates and hotfixes in results.

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

### None
### You cannot pipe objects to this function.
## OUTPUTS

### PSADT.Types.InstalledApplication
### Returns a custom type with information about an installed application:
### - Publisher
### - DisplayName
### - DisplayVersion
### - ProductCode
### - UninstallString
### - InstallSource
### - InstallLocation
### - InstallDate
### - Architecture
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)

