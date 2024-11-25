---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Mount-ADTWimFile

## SYNOPSIS
Mounts a WIM file to a specified directory.

## SYNTAX

### Name
```
Mount-ADTWimFile -ImagePath <FileInfo> -Path <DirectoryInfo> -Name <String> [-Force] [-PassThru]
 [<CommonParameters>]
```

### Index
```
Mount-ADTWimFile -ImagePath <FileInfo> -Path <DirectoryInfo> -Index <UInt32> [-Force] [-PassThru]
 [<CommonParameters>]
```

## DESCRIPTION
Mounts a WIM file to a specified directory.
The function supports mounting by image index or image name.
It also provides options to forcefully remove existing directories and return the mounted image details.

## EXAMPLES

### EXAMPLE 1
```
Mount-ADTWimFile -ImagePath 'C:\Images\install.wim' -Path 'C:\Mount' -Index 1
```

Mounts the first image in the 'install.wim' file to the 'C:\Mount' directory.

### EXAMPLE 2
```
Mount-ADTWimFile -ImagePath 'C:\Images\install.wim' -Path 'C:\Mount' -Name 'Windows 10 Pro'
```

Mounts the image named 'Windows 10 Pro' in the 'install.wim' file to the 'C:\Mount' directory.

### EXAMPLE 3
```
Mount-ADTWimFile -ImagePath 'C:\Images\install.wim' -Path 'C:\Mount' -Index 1 -Force
```

Mounts the first image in the 'install.wim' file to the 'C:\Mount' directory, forcefully removing the existing directory if it is not empty.

## PARAMETERS

### -ImagePath
Path to the WIM file to be mounted.

```yaml
Type: FileInfo
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
Directory where the WIM file will be mounted.
The directory must be empty and not have a pre-existing WIM mounted.

```yaml
Type: DirectoryInfo
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Index
Index of the image within the WIM file to be mounted.

```yaml
Type: UInt32
Parameter Sets: Index
Aliases:

Required: True
Position: Named
Default value: 0
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
Name of the image within the WIM file to be mounted.

```yaml
Type: String
Parameter Sets: Name
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Force
Forces the removal of the existing directory if it is not empty.

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
If specified, the function will return the results from `Mount-WindowsImage`.

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

### Microsoft.Dism.Commands.ImageObject
### Returns the mounted image details if the PassThru parameter is specified.
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
