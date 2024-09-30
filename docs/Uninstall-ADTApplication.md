---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Uninstall-ADTApplication

## SYNOPSIS
Removes all MSI applications matching the specified application name.

## SYNTAX

### ByInstalledApplication
```
Uninstall-ADTApplication [-InstalledApplication] <InstalledApplication> [-ApplicationType <String>]
 [-IncludeUpdatesAndHotfixes] [-Parameters <String>] [-AddParameters <String>] [-LoggingOptions <String>]
 [-LogFileName <String>] [-PassThru] [<CommonParameters>]
```

### ByFilterScript
```
Uninstall-ADTApplication [-FilterScript] <ScriptBlock> [-ApplicationType <String>] [-IncludeUpdatesAndHotfixes]
 [-Parameters <String>] [-AddParameters <String>] [-LoggingOptions <String>] [-LogFileName <String>]
 [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
Removes all MSI applications matching the specified application name.

Enumerates the registry for installed applications matching the specified application name and uninstalls that application using the product code.

## EXAMPLES

### EXAMPLE 1
```
Uninstall-ADTApplication -FilterScript {$_.DisplayName -match 'Java'}
```

Removes all MSI applications that contain the name 'Java' in the DisplayName.

### EXAMPLE 2
```
Uninstall-ADTApplication -FilterScript {$_.DisplayName -match 'Java' -and $_.Publisher -eq 'Oracle Corporation' -and $_.Is64BitApplication -eq $true -and $_.DisplayVersion -notlike '8.*'}
```

Removes all MSI applications that contain the name 'Java' in the DisplayName, with Publisher as 'Oracle Corporation', 64-bit, and not version 8.x.

### EXAMPLE 3
```
Uninstall-ADTApplication -FilterScript {$_.DisplayName -match '^Vim\s'} -Verbose -ApplicationType EXE -Parameters '/S'
```

Remove all EXE applications starting with the name 'Vim' followed by a space, using the '/S' parameter.

## PARAMETERS

### -InstalledApplication
Specifies the installed application to remove.
This parameter is used to pass the output of Get-ADTInstalledApplication to Uninstall-ADTApplication via the pipeline.

```yaml
Type: InstalledApplication
Parameter Sets: ByInstalledApplication
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -FilterScript
Specifies a script block to filter the applications to be removed.
The script block is evaluated for each application, and if it returns $true, the application is selected for removal.

```yaml
Type: ScriptBlock
Parameter Sets: ByFilterScript
Aliases:

Required: True
Position: 1
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

### -LoggingOptions
Overrides the default MSI logging options specified in the configuration file.
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

### -LogFileName
Overrides the default log file name for MSI applications.
The default log file name is generated from the MSI file name.
If LogFileName does not end in .log, it will be automatically appended.

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

### PSADT.Types.ProcessResult
### Returns an object with the results of the installation if -PassThru is specified.
### - ExitCode
### - StdOut
### - StdErr
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)

