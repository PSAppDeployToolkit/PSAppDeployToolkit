---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Remove-ADTFolder

## SYNOPSIS
Remove folder and files if they exist.

## SYNTAX

```
Remove-ADTFolder [-Path] <DirectoryInfo> [-DisableRecursion] [<CommonParameters>]
```

## DESCRIPTION
This function removes a folder and all files within it, with or without recursion, in a given path.
If the specified folder does not exist, it logs a warning instead of throwing an error.
The function can also delete items recursively if the DisableRecursion parameter is not specified.

## EXAMPLES

### EXAMPLE 1
```
Remove-ADTFolder -Path "$envWinDir\Downloaded Program Files"
```

Deletes all files and subfolders in the Windows\Downloads Program Files folder.

### EXAMPLE 2
```
Remove-ADTFolder -Path "$envTemp\MyAppCache" -DisableRecursion
```

Deletes all files in the Temp\MyAppCache folder but does not delete any subfolders.

## PARAMETERS

### -Path
Path to the folder to remove.

```yaml
Type: DirectoryInfo
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -DisableRecursion
Disables recursion while deleting.

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
