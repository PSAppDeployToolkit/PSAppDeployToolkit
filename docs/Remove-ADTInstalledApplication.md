---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Remove-ADTInstalledApplication

## SYNOPSIS
Removes all MSI applications matching the specified application name.

## SYNTAX

```
Remove-ADTInstalledApplication [-FilterScript] <ScriptBlock> [-ApplicationType <String>] [-Parameters <String>]
 [-AddParameters <String>] [-IncludeUpdatesAndHotfixes] [-LoggingOptions <String>] [-LogName <String>]
 [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
Removes all MSI applications matching the specified application name.
Enumerates the registry for installed applications matching the specified application name and uninstalls that application using the product code.

## EXAMPLES

### EXAMPLE 1
```
Remove-ADTInstalledApplication -FilterScript {$_.DisplayName -match 'Java'}
```

Removes all MSI applications that contain the name 'Java' in the DisplayName.

### EXAMPLE 2
```
Remove-ADTInstalledApplication -FilterScript {$_.DisplayName -match 'Java' -and $_.Publisher -eq 'Oracle Corporation' -and $_.Is64BitApplication -eq $true -and $_.DisplayVersion -notlike '8.*'}
```

Removes all MSI applications that contain the name 'Java' in the DisplayName, with Publisher as 'Oracle Corporation', 64-bit, and not version 8.x.

### EXAMPLE 3
```
Remove-ADTInstalledApplication -FilterScript {$_.DisplayName -match '^Vim\s'} -Verbose -ApplicationType EXE -Parameters '/S'
```

Remove all EXE applications starting with the name 'Vim' followed by a space, using the '/S' parameter.

## PARAMETERS

### -FilterScript
Specifies a script block to filter the applications to be removed.
The script block is evaluated for each application, and if it returns $true, the application is selected for removal.

```yaml
Type: ScriptBlock
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ApplicationType
Specifies the type of application to remove.
Valid values are 'Any', 'MSI', and 'EXE'.
The default value is 'MSI'.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: MSI
Accept pipeline input: False
Accept wildcard characters: False
```

### -Parameters
Overrides the default MSI parameters specified in the configuration file, or the parameters found in QuietUninstallString/UninstallString for EXE applications.

```yaml
Type: String
Parameter Sets: (All)
Aliases: Arguments

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -AddParameters
Adds to the default parameters specified in the configuration file, or the parameters found in QuietUninstallString/UninstallString for EXE applications.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
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

### -LoggingOptions
Overrides the default logging options specified in the configuration file.
Default options are: "/L*v".

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -LogName
Overrides the default log file name for MSI applications.
The default log file name is generated from the MSI file name.
If LogName does not end in .log, it will be automatically appended.
For uninstallations, by default the product code is resolved to the DisplayName and version of the application.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
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

