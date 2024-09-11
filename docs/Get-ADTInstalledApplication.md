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
Get-ADTInstalledApplication [[-FilterScript] <ScriptBlock>] [-IncludeUpdatesAndHotfixes] [<CommonParameters>]
```

## DESCRIPTION
Retrieves information about installed applications by querying the registry.
You can specify an application name, a product code, or both.
Returns information about application publisher, name & version, product code, uninstall string, install source, location, date, and application architecture.

## EXAMPLES

### EXAMPLE 1
```
Get-ADTInstalledApplication
```

This example retrieves information about all installed applications.

### EXAMPLE 2
```
Get-ADTInstalledApplication -FilterScript { $_.DisplayName -eq 'Adobe Flash' }
```

This example retrieves information about installed applications with the name 'Adobe Flash'.

## PARAMETERS

### -FilterScript
A script used to filter the results as they're processed.

```yaml
Type: ScriptBlock
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
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

