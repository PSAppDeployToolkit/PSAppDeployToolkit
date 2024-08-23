---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Copy-File

## SYNOPSIS
Copies files and directories from a source to a destination.

## SYNTAX

```
Copy-File [-Path] <String[]> [-Destination] <String> [-Recurse] [-Flatten] [-ContinueOnError <Boolean>]
 [-ContinueFileCopyOnError <Boolean>] [-UseRobocopy <Boolean>] [-RobocopyParams <String>]
 [-RobocopyAdditionalParams <String>] [<CommonParameters>]
```

## DESCRIPTION
Copies files and directories from a source to a destination.
This function supports recursive copying, overwriting existing files, and returning the copied items.

## EXAMPLES

### EXAMPLE 1
```
Copy-File -Path 'C:\Path\file.txt' -Destination 'D:\Destination\file.txt'
```

Copies the file 'file.txt' from 'C:\Path' to 'D:\Destination'.

### EXAMPLE 2
```
Copy-File -Path 'C:\Path\Folder' -Destination 'D:\Destination\Folder' -Recurse
```

Recursively copies the folder 'Folder' from 'C:\Path' to 'D:\Destination'.

### EXAMPLE 3
```
Copy-File -Path 'C:\Path\file.txt' -Destination 'D:\Destination\file.txt' -Force
```

Copies the file 'file.txt' from 'C:\Path' to 'D:\Destination', overwriting the destination file if it exists.

## PARAMETERS

### -Path
Path of the file to copy.
Multiple paths can be specified.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Destination
Destination Path of the file to copy.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Recurse
Copy files in subdirectories.

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

### -Flatten
Flattens the files into the root destination directory.

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
Continue if an error is encountered.
This will continue the deployment script, but will not continue copying files if an error is encountered.
Default is: $true.

```yaml
Type: Boolean
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: True
Accept pipeline input: False
Accept wildcard characters: False
```

### -ContinueFileCopyOnError
Continue copying files if an error is encountered.
This will continue the deployment script and will warn about files that failed to be copied.
Default is: $false.

```yaml
Type: Boolean
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -UseRobocopy
Use Robocopy to copy files rather than native PowerShell method.
Robocopy overcomes the 260 character limit.
Supports * in file names, but not folders, in source paths.
Default is configured in the AppDeployToolkitConfig.xml file: $true

```yaml
Type: Boolean
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: (Get-ADTConfig).Toolkit.UseRobocopy
Accept pipeline input: False
Accept wildcard characters: False
```

### -RobocopyParams
Override the default Robocopy parameters.
Default is: /NJH /NJS /NS /NC /NP /NDL /FP /IS /IT /IM /XX /MT:4 /R:1 /W:1

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: /NJH /NJS /NS /NC /NP /NDL /FP /IS /IT /IM /XX /MT:4 /R:1 /W:1
Accept pipeline input: False
Accept wildcard characters: False
```

### -RobocopyAdditionalParams
Append to the default Robocopy parameters.
Default is: /NJH /NJS /NS /NC /NP /NDL /FP /IS /IT /IM /XX /MT:4 /R:1 /W:1

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable.
For more information, see about_CommonParameters (http://go.microsoft.com/fwlink/?LinkID=113216).

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
Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)

