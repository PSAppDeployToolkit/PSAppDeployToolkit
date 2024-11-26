---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# New-ADTTemplate

## SYNOPSIS
Creates a new folder containing a template front end and module folder, ready to customise.

## SYNTAX

```
New-ADTTemplate [[-Destination] <String>] [[-Name] <String>] [[-ModulePath] <String>] [[-Version] <Int32>]
 [-Force] [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
Specify a destination path where a new folder will be created.
You also have the option of creating a template for v3 compatibility mode.

## EXAMPLES

### EXAMPLE 1
```
New-ADTTemplate -Path 'C:\Temp' -Name 'PSAppDeployToolkitv4'
```

Creates a new v4 template named PSAppDeployToolkitv4 under C:\Temp.

### EXAMPLE 2
```
New-ADTTemplate -Path 'C:\Temp' -Name 'PSAppDeployToolkitv3' -Version 3
```

Creates a new v3 compatibility mode template named PSAppDeployToolkitv3 under C:\Temp.

## PARAMETERS

### -Destination
Path where the new folder should be created.
Default is the current working directory.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: $PWD
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
Name of the newly created folder.
Default is PSAppDeployToolkit.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 2
Default value: $MyInvocation.MyCommand.Module.Name
Accept pipeline input: False
Accept wildcard characters: False
```

### -ModulePath
Override the default module path to include with the template.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 3
Default value: $MyInvocation.MyCommand.Module.ModuleBase
Accept pipeline input: False
Accept wildcard characters: False
```

### -Version
Defaults to 4 for the standard v4 template.
Use 3 for the v3 compatibility mode template.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: 4
Default value: 4
Accept pipeline input: False
Accept wildcard characters: False
```

### -Force
If the destination folder already exists, this switch will force the creation of the new folder.

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

### -PassThru
Returns the newly created folder object.

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
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
### You cannot pipe objects to this function.
## OUTPUTS

### None
### This function does not generate any output.
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
