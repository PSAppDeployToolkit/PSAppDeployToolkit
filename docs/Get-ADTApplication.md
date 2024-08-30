---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Get-ADTApplication

## SYNOPSIS
Retrieves information about installed applications.

## SYNTAX

```
Get-ADTApplication [-Name <String[]>] [-NameMatch <String>] [-ProductCode <String[]>]
 [-ApplicationType <String>] [-IncludeUpdatesAndHotfixes] [[-FilterScript] <ScriptBlock>] [<CommonParameters>]
```

## DESCRIPTION
Retrieves information about installed applications by querying the registry.
You can specify an application name, a product code, or both.
Returns information about application publisher, name & version, product code, uninstall string, install source, location, date, and application architecture.

## EXAMPLES

### EXAMPLE 1
```
Get-ADTApplication
```

This example retrieves information about all installed applications.

### EXAMPLE 2
```
Get-ADTApplication -Name 'Acrobat'
```

Returns all applications that contain the name 'Acrobat' in the DisplayName.

### EXAMPLE 3
```
Get-ADTApplication -Name 'Adobe Acrobat Reader' -NameMatch 'Exact'
```

Returns all applications that match the name 'Adobe Acrobat Reader' exactly.

### EXAMPLE 4
```
Get-ADTApplication -ProductCode '{AC76BA86-7AD7-1033-7B44-AC0F074E4100}'
```

Returns the application with the specified ProductCode.

### EXAMPLE 5
```
Get-ADTApplication -Name 'Acrobat' -ApplicationType 'MSI' -FilterScript { $_.Publisher -match 'Adobe' }
```

Returns all MSI applications that contain the name 'Acrobat' in the DisplayName and 'Adobe' in the Publisher name.

## PARAMETERS

### -Name
The name of the application to retrieve information for.
Performs a contains match on the application display name by default.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NameMatch
Specifies the type of match to perform on the application name.
Valid values are 'Contains', 'Exact', 'Wildcard', and 'Regex'.
The default value is 'Contains'.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: Contains
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
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ApplicationType
Specifies the type of application to remove.
Valid values are 'All', 'MSI', and 'EXE'.
The default value is 'All'.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: All
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
