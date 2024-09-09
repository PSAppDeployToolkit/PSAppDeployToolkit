---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Execute-ProcessAsUser

## SYNOPSIS
Execute a process with a logged in user account, by using a scheduled task, to provide interaction with user in the SYSTEM context.

## SYNTAX

```
Execute-ProcessAsUser [[-UserName] <String>] [-Path] <String> [[-TempPath] <String>] [[-Parameters] <String>]
 [-SecureParameters] [[-RunLevel] <String>] [-Wait] [-PassThru] [[-WorkingDirectory] <String>]
 [[-ContinueOnError] <Boolean>] [<CommonParameters>]
```

## DESCRIPTION
Execute a process with a logged in user account, by using a scheduled task, to provide interaction with user in the SYSTEM context.

## EXAMPLES

### EXAMPLE 1
```
Execute-ProcessAsUser -UserName 'CONTOSO\User' -Path $envPSProcessPath -Parameters "-Command `"& { & 'C:\Test\Script.ps1'; Exit `$LastExitCode }`"" -Wait
```

Execute process under a user account by specifying a username under which to execute it.

### EXAMPLE 2
```
Execute-ProcessAsUser -Path $envPSProcessPath -Parameters "-Command `"& { & 'C:\Test\Script.ps1'; Exit `$LastExitCode }`"" -Wait
```

Execute process under a user account by using the default active logged in user that was detected when the toolkit was launched.

### EXAMPLE 3
```
Execute-ProcessAsUser -Path $envPSProcessPath -Parameters "-Command `"& { & 'C:\Test\Script.ps1'; Exit `$LastExitCode }`"" -RunLevel 'LeastPrivilege'
```

Execute process using 'LeastPrivilege' under a user account by using the default active logged in user that was detected when the toolkit was launched.

## PARAMETERS

### -UserName
Logged in Username under which to run the process from.
Default is: The active console user.
If no console user exists but users are logged in, such as on terminal servers, then the first logged-in non-console user.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: (& $Script:CommandTable.'Get-ADTRunAsActiveUser').NTAccount
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
Path to the file being executed.

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

### -TempPath
Path to the temporary directory used to store the script to be executed as user.
If using a user writable directory, ensure you select -RunLevel 'LeastPrivilege'.

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

### -Parameters
Arguments to be passed to the file being executed.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 4
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -SecureParameters
Hides all parameters passed to the executable from the Toolkit log file.

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

### -RunLevel
Specifies the level of user rights that Task Scheduler uses to run the task.
The acceptable values for this parameter are:

- HighestAvailable: Tasks run by using the highest available privileges (Admin privileges for Administrators).
Default Value.

- LeastPrivilege: Tasks run by using the least-privileged user account (LUA) privileges.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 5
Default value: HighestAvailable
Accept pipeline input: False
Accept wildcard characters: False
```

### -Wait
Wait for the process, launched by the scheduled task, to complete execution before accepting more input.
Default is $false.

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
Returns the exit code from this function or the process launched by the scheduled task.

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

### -WorkingDirectory
Set working directory for the process.

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

### -ContinueOnError
Continue if an error is encountered.
Default is $true.

```yaml
Type: Boolean
Parameter Sets: (All)
Aliases:

Required: False
Position: 7
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

### System.Int32.
### Returns the exit code from this function or the process launched by the scheduled task.
## NOTES

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)

