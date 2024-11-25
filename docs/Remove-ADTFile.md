---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Remove-ADTFile

## SYNOPSIS
Removes one or more items from a given path on the filesystem.

## SYNTAX

### Path
```
Remove-ADTFile -Path <String[]> [-Recurse] [<CommonParameters>]
```

### LiteralPath
```
Remove-ADTFile -LiteralPath <String[]> [-Recurse] [<CommonParameters>]
```

## DESCRIPTION
This function removes one or more items from a given path on the filesystem.
It can handle both wildcard paths and literal paths.
If the specified path does not exist, it logs a warning instead of throwing an error.
The function can also delete items recursively if the Recurse parameter is specified.

## EXAMPLES

### EXAMPLE 1
```
Remove-ADTFile -Path 'C:\Windows\Downloaded Program Files\Temp.inf'
```

Removes the specified file.

### EXAMPLE 2
```
Remove-ADTFile -LiteralPath 'C:\Windows\Downloaded Program Files' -Recurse
```

Removes the specified folder and all its contents recursively.

## PARAMETERS

### -Path
Specifies the path on the filesystem to be resolved.
The value of Path will accept wildcards.
Will accept an array of values.

```yaml
Type: String[]
Parameter Sets: Path
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -LiteralPath
Specifies the path on the filesystem to be resolved.
The value of LiteralPath is used exactly as it is typed; no characters are interpreted as wildcards.
Will accept an array of values.

```yaml
Type: String[]
Parameter Sets: LiteralPath
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Recurse
Deletes the files in the specified location(s) and in all child items of the location(s).

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

This function continues on received errors by default.
To have the function stop on an error, please provide `-ErrorAction Stop` on the end of your call.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
