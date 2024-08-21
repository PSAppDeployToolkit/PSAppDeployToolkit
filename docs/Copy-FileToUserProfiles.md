---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Copy-FileToUserProfiles

## SYNOPSIS
Copy one or more items to each user profile on the system.

## SYNTAX

```
Copy-FileToUserProfiles [-Path] <String[]> [[-Destination] <String>] [-Recurse] [-Flatten]
 [-UseRobocopy <Boolean>] [-RobocopyAdditionalParams <String>] [-ExcludeNTAccount <String[]>]
 [-ExcludeSystemProfiles <Boolean>] [-ExcludeServiceProfiles <Boolean>] [-ExcludeDefaultUser]
 [-ContinueOnError <Boolean>] [-ContinueFileCopyOnError <Boolean>] [<CommonParameters>]
```

## DESCRIPTION
The Copy-FileToUserProfiles function copies one or more items to each user profile on the system.
It supports various options such as recursion, flattening files, and using Robocopy to overcome the 260 character limit.

## EXAMPLES

### EXAMPLE 1
```
Copy-FileToUserProfiles -Path "$dirSupportFiles\config.txt" -Destination "AppData\Roaming\MyApp"
```

Copy a single file to C:\Users\\\<UserName\>\AppData\Roaming\MyApp for each user.

### EXAMPLE 2
```
Copy-FileToUserProfiles -Path "$dirSupportFiles\config.txt","$dirSupportFiles\config2.txt" -Destination "AppData\Roaming\MyApp"
```

Copy two files to C:\Users\\\<UserName\>\AppData\Roaming\MyApp for each user.

### EXAMPLE 3
```
Copy-FileToUserProfiles -Path "$dirFiles\MyApp" -Destination "AppData\Local" -Recurse
```

Copy an entire folder to C:\Users\\\<UserName\>\AppData\Local for each user.

### EXAMPLE 4
```
Copy-FileToUserProfiles -Path "$dirFiles\.appConfigFolder" -Recurse
```

Copy an entire folder to C:\Users\\\<UserName\> for each user.

## PARAMETERS

### -Path
The path of the file or folder to copy.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 2
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Destination
The path of the destination folder to append to the root of the user profile.

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

### -UseRobocopy
Use Robocopy to copy files rather than native PowerShell method.
Robocopy overcomes the 260 character limit.
Only applies if $Path is specified as a folder.
Default is configured in the AppDeployToolkitConfig.xml file: $true.

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

### -RobocopyAdditionalParams
Additional parameters to pass to Robocopy.
Default is: $null.

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

### -ExcludeNTAccount
Specify NT account names in Domain\Username format to exclude from the list of user profiles.

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

### -ExcludeSystemProfiles
Exclude system profiles: SYSTEM, LOCAL SERVICE, NETWORK SERVICE.
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

### -ExcludeServiceProfiles
Exclude service profiles where NTAccount begins with NT SERVICE.
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

### -ExcludeDefaultUser
Exclude the Default User.
Default is: $false.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable.
For more information, see about_CommonParameters (http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
### You can pipe in string values for $Path.
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

