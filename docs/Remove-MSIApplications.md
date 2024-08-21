---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Remove-MSIApplications

## SYNOPSIS
Removes all MSI applications matching the specified application name.

## SYNTAX

```
Remove-MSIApplications [-Name] <String> [-Exact] [-WildCard] [[-Parameters] <String>]
 [[-AddParameters] <String>] [[-FilterApplication] <Array>] [[-ExcludeFromUninstall] <Array>]
 [-IncludeUpdatesAndHotfixes] [[-LoggingOptions] <String>] [[-LogName] <String>] [-PassThru]
 [[-ContinueOnError] <Boolean>] [<CommonParameters>]
```

## DESCRIPTION
Removes all MSI applications matching the specified application name.
Enumerates the registry for installed applications matching the specified application name and uninstalls that application using the product code, provided the uninstall string matches "msiexec".

## EXAMPLES

### EXAMPLE 1
```
Remove-MSIApplications -Name 'Adobe Flash'
```

Removes all versions of software that match the name "Adobe Flash"

### EXAMPLE 2
```
Remove-MSIApplications -Name 'Adobe'
```

Removes all versions of software that match the name "Adobe"

### EXAMPLE 3
```
Remove-MSIApplications -Name 'Java 8 Update' -FilterApplication @(
@('Is64BitApplication', $false, 'Exact'),
        @('Publisher', 'Oracle Corporation', 'Exact')
    )
```


Removes all versions of software that match the name "Java 8 Update" where the software is 32-bits and the publisher is "Oracle Corporation".

### EXAMPLE 4
```
Remove-MSIApplications -Name 'Java 8 Update' -FilterApplication @(, @('Publisher', 'Oracle Corporation', 'Exact')) -ExcludeFromUninstall @(, @('DisplayName', 'Java 8 Update 45', 'Contains'))
```

Removes all versions of software that match the name "Java 8 Update" and also have "Oracle Corporation" as the Publisher; however, it does not uninstall "Java 8 Update 45" of the software.
NOTE: If only specifying a single row in the two-dimensional arrays, the array must have the extra parentheses and leading comma as in this example.

### EXAMPLE 5
```
Remove-MSIApplications -Name 'Java 8 Update' -ExcludeFromUninstall @(, @('DisplayName', 'Java 8 Update 45', 'Contains'))
```

Removes all versions of software that match the name "Java 8 Update"; however, it does not uninstall "Java 8 Update 45" of the software.
NOTE: If only specifying a single row in the two-dimensional array, the array must have the extra parentheses and leading comma as in this example.

### EXAMPLE 6
```
Remove-MSIApplications -Name 'Java 8 Update' -ExcludeFromUninstall @(
@('Is64BitApplication', $true, 'Exact'),
    @('DisplayName', 'Java 8 Update 45', 'Exact'),
    @('DisplayName', 'Java 8 Update 4*', 'WildCard'),
    @('DisplayName', 'Java \d Update \d{3}', 'RegEx'),
    @('DisplayName', 'Java 8 Update', 'Contains'))
```


Removes all versions of software that match the name "Java 8 Update"; however, it does not uninstall 64-bit versions of the software, Update 45 of the software, or any Update that starts with 4.

## PARAMETERS

### -Name
The name of the application to uninstall.
Performs a contains match on the application display name by default.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
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

### -Parameters
Overrides the default parameters specified in the XML configuration file.
Uninstall default is: "REBOOT=ReallySuppress /QN".

```yaml
Type: String
Parameter Sets: (All)
Aliases: Arguments

Required: False
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -AddParameters
Adds to the default parameters specified in the XML configuration file.
Uninstall default is: "REBOOT=ReallySuppress /QN".

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 3
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -FilterApplication
Two-dimensional array that contains one or more (property, value, match-type) sets that should be used to filter the list of results returned by Get-ADTInstalledApplication to only those that should be uninstalled.
Properties that can be filtered upon: ProductCode, DisplayName, DisplayVersion, UninstallString, InstallSource, InstallLocation, InstallDate, Publisher, Is64BitApplication

```yaml
Type: Array
Parameter Sets: (All)
Aliases:

Required: False
Position: 4
Default value: @(@())
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExcludeFromUninstall
Two-dimensional array that contains one or more (property, value, match-type) sets that should be excluded from uninstall if found.
Properties that can be excluded: ProductCode, DisplayName, DisplayVersion, UninstallString, InstallSource, InstallLocation, InstallDate, Publisher, Is64BitApplication

```yaml
Type: Array
Parameter Sets: (All)
Aliases:

Required: False
Position: 5
Default value: @(@())
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

### -LoggingOptions
Overrides the default logging options specified in the XML configuration file.
Default options are: "/L*v".

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 6
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -LogName
Overrides the default log file name.
The default log file name is generated from the MSI file name.
If LogName does not end in .log, it will be automatically appended.
For uninstallations, by default the product code is resolved to the DisplayName and version of the application.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 7
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PassThru
Returns ExitCode, STDOut, and STDErr output from the process.

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

### -ContinueOnError
Continue if an error occured while trying to start the processes.
Default: $true.

```yaml
Type: Boolean
Parameter Sets: (All)
Aliases:

Required: False
Position: 8
Default value: True
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

### PSObject
### Returns an object with the following properties:
### - ExitCode
### - StdOut
### - StdErr
## NOTES
This function can be called without an active ADT session..

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)


