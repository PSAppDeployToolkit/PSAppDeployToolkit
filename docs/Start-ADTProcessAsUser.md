---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Start-ADTProcessAsUser

## SYNOPSIS
Invokes a process in another user's session.

## SYNTAX

### PrimaryActiveUserSessionWithWait
```
Start-ADTProcessAsUser -FilePath <String> [-ArgumentList <String[]>] [-WorkingDirectory <String>] [-HideWindow]
 [-ProcessCreationFlags <CREATE_PROCESS>] [-InheritEnvironmentVariables] [-Wait] [-PrimaryActiveUserSession]
 [-UseLinkedAdminToken] [-SuccessExitCodes <Int32[]>] [-ConsoleTimeoutInSeconds <UInt32>] [-IsGuiApplication]
 [-NoRedirectOutput] [-MergeStdErrAndStdOut] [-OutputDirectory <String>] [-NoTerminateOnTimeout]
 [-AdditionalEnvironmentVariables <IDictionary>] [-WaitOption <WaitType>] [-SecureParameters] [-PassThru]
 [<CommonParameters>]
```

### PrimaryActiveUserSession
```
Start-ADTProcessAsUser -FilePath <String> [-ArgumentList <String[]>] [-WorkingDirectory <String>] [-HideWindow]
 [-ProcessCreationFlags <CREATE_PROCESS>] [-InheritEnvironmentVariables] [-PrimaryActiveUserSession]
 [-UseLinkedAdminToken] [-SuccessExitCodes <Int32[]>] [-ConsoleTimeoutInSeconds <UInt32>] [-IsGuiApplication]
 [-NoRedirectOutput] [-MergeStdErrAndStdOut] [-OutputDirectory <String>] [-NoTerminateOnTimeout]
 [-AdditionalEnvironmentVariables <IDictionary>] [-SecureParameters] [-PassThru] [<CommonParameters>]
```

### AllActiveUserSessionsWithWait
```
Start-ADTProcessAsUser -FilePath <String> [-ArgumentList <String[]>] [-WorkingDirectory <String>] [-HideWindow]
 [-ProcessCreationFlags <CREATE_PROCESS>] [-InheritEnvironmentVariables] [-Wait] [-AllActiveUserSessions]
 [-UseLinkedAdminToken] [-SuccessExitCodes <Int32[]>] [-ConsoleTimeoutInSeconds <UInt32>] [-IsGuiApplication]
 [-NoRedirectOutput] [-MergeStdErrAndStdOut] [-OutputDirectory <String>] [-NoTerminateOnTimeout]
 [-AdditionalEnvironmentVariables <IDictionary>] [-WaitOption <WaitType>] [-SecureParameters] [-PassThru]
 [<CommonParameters>]
```

### AllActiveUserSessions
```
Start-ADTProcessAsUser -FilePath <String> [-ArgumentList <String[]>] [-WorkingDirectory <String>] [-HideWindow]
 [-ProcessCreationFlags <CREATE_PROCESS>] [-InheritEnvironmentVariables] [-AllActiveUserSessions]
 [-UseLinkedAdminToken] [-SuccessExitCodes <Int32[]>] [-ConsoleTimeoutInSeconds <UInt32>] [-IsGuiApplication]
 [-NoRedirectOutput] [-MergeStdErrAndStdOut] [-OutputDirectory <String>] [-NoTerminateOnTimeout]
 [-AdditionalEnvironmentVariables <IDictionary>] [-SecureParameters] [-PassThru] [<CommonParameters>]
```

### SessionIdWithWait
```
Start-ADTProcessAsUser -FilePath <String> [-ArgumentList <String[]>] [-WorkingDirectory <String>] [-HideWindow]
 [-ProcessCreationFlags <CREATE_PROCESS>] [-InheritEnvironmentVariables] [-Wait] -SessionId <UInt32>
 [-UseLinkedAdminToken] [-SuccessExitCodes <Int32[]>] [-ConsoleTimeoutInSeconds <UInt32>] [-IsGuiApplication]
 [-NoRedirectOutput] [-MergeStdErrAndStdOut] [-OutputDirectory <String>] [-NoTerminateOnTimeout]
 [-AdditionalEnvironmentVariables <IDictionary>] [-WaitOption <WaitType>] [-SecureParameters] [-PassThru]
 [<CommonParameters>]
```

### SessionId
```
Start-ADTProcessAsUser -FilePath <String> [-ArgumentList <String[]>] [-WorkingDirectory <String>] [-HideWindow]
 [-ProcessCreationFlags <CREATE_PROCESS>] [-InheritEnvironmentVariables] -SessionId <UInt32>
 [-UseLinkedAdminToken] [-SuccessExitCodes <Int32[]>] [-ConsoleTimeoutInSeconds <UInt32>] [-IsGuiApplication]
 [-NoRedirectOutput] [-MergeStdErrAndStdOut] [-OutputDirectory <String>] [-NoTerminateOnTimeout]
 [-AdditionalEnvironmentVariables <IDictionary>] [-SecureParameters] [-PassThru] [<CommonParameters>]
```

### UsernameWithWait
```
Start-ADTProcessAsUser -FilePath <String> [-ArgumentList <String[]>] [-WorkingDirectory <String>] [-HideWindow]
 [-ProcessCreationFlags <CREATE_PROCESS>] [-InheritEnvironmentVariables] [-Wait] -Username <String>
 [-UseLinkedAdminToken] [-SuccessExitCodes <Int32[]>] [-ConsoleTimeoutInSeconds <UInt32>] [-IsGuiApplication]
 [-NoRedirectOutput] [-MergeStdErrAndStdOut] [-OutputDirectory <String>] [-NoTerminateOnTimeout]
 [-AdditionalEnvironmentVariables <IDictionary>] [-WaitOption <WaitType>] [-SecureParameters] [-PassThru]
 [<CommonParameters>]
```

### Username
```
Start-ADTProcessAsUser -FilePath <String> [-ArgumentList <String[]>] [-WorkingDirectory <String>] [-HideWindow]
 [-ProcessCreationFlags <CREATE_PROCESS>] [-InheritEnvironmentVariables] -Username <String>
 [-UseLinkedAdminToken] [-SuccessExitCodes <Int32[]>] [-ConsoleTimeoutInSeconds <UInt32>] [-IsGuiApplication]
 [-NoRedirectOutput] [-MergeStdErrAndStdOut] [-OutputDirectory <String>] [-NoTerminateOnTimeout]
 [-AdditionalEnvironmentVariables <IDictionary>] [-SecureParameters] [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
Invokes a process from SYSTEM in another user's session.

## EXAMPLES

### EXAMPLE 1
```
Start-ADTProcessAsUser -FilePath "$($adtSession.DirFiles)\setup.exe" -Parameters '/S' -SuccessExitCodes 0, 500
```

## PARAMETERS

### -FilePath
Path to the executable to invoke.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ArgumentList
Arguments for the invoked executable.

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

### -WorkingDirectory
The 'start-in' directory for the invoked executable.

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

### -HideWindow
Specifies whether the window should be hidden or not.

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

### -ProcessCreationFlags
One or more flags to control the process's invocation.

```yaml
Type: CREATE_PROCESS
Parameter Sets: (All)
Aliases:
Accepted values: DEBUG_PROCESS, DEBUG_ONLY_THIS_PROCESS, CREATE_SUSPENDED, DETACHED_PROCESS, CREATE_NEW_CONSOLE, NORMAL_PRIORITY_CLASS, IDLE_PRIORITY_CLASS, HIGH_PRIORITY_CLASS, REALTIME_PRIORITY_CLASS, CREATE_NEW_PROCESS_GROUP, CREATE_UNICODE_ENVIRONMENT, CREATE_SEPARATE_WOW_VDM, CREATE_SHARED_WOW_VDM, BELOW_NORMAL_PRIORITY_CLASS, ABOVE_NORMAL_PRIORITY_CLASS, INHERIT_PARENT_AFFINITY, CREATE_PROTECTED_PROCESS, EXTENDED_STARTUPINFO_PRESENT, PROCESS_MODE_BACKGROUND_BEGIN, PROCESS_MODE_BACKGROUND_END, CREATE_SECURE_PROCESS, CREATE_BREAKAWAY_FROM_JOB, CREATE_PRESERVE_CODE_AUTHZ_LEVEL, CREATE_DEFAULT_ERROR_MODE, CREATE_NO_WINDOW

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -InheritEnvironmentVariables
Specifies whether the process should inherit the user's environment state.

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

### -Wait
Specifies whether to wait for the invoked excecutable to finish.

```yaml
Type: SwitchParameter
Parameter Sets: PrimaryActiveUserSessionWithWait, AllActiveUserSessionsWithWait, SessionIdWithWait, UsernameWithWait
Aliases:

Required: True
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -Username
The username of the user's session to invoke the executable in.

```yaml
Type: String
Parameter Sets: UsernameWithWait, Username
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -SessionId
The session ID of the user to invoke the executable in.

```yaml
Type: UInt32
Parameter Sets: SessionIdWithWait, SessionId
Aliases:

Required: True
Position: Named
Default value: 0
Accept pipeline input: False
Accept wildcard characters: False
```

### -AllActiveUserSessions
Specifies that the executable should be invoked in all active sessions.

```yaml
Type: SwitchParameter
Parameter Sets: AllActiveUserSessionsWithWait, AllActiveUserSessions
Aliases:

Required: True
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -PrimaryActiveUserSession
Specifies that the executable should be invoked in the primary (active) user session.

```yaml
Type: SwitchParameter
Parameter Sets: PrimaryActiveUserSessionWithWait, PrimaryActiveUserSession
Aliases:

Required: True
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -UseLinkedAdminToken
Specifies that an admin token (if available) should be used for the invocation.

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

### -SuccessExitCodes
Specifies one or more exit codes that the function uses to consider the invocation successful.

```yaml
Type: Int32[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ConsoleTimeoutInSeconds
Specifies the timeout in seconds to wait for a console application to finish its task.

```yaml
Type: UInt32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: 0
Accept pipeline input: False
Accept wildcard characters: False
```

### -IsGuiApplication
Indicates that the executed application is a GUI-based app, not a console-based app.

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

### -NoRedirectOutput
Specifies that stdout/stderr output should not be redirected to file.

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

### -MergeStdErrAndStdOut
Specifies that the stdout/stderr streams should be merged into a single output.

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

### -OutputDirectory
Specifies the output directory for the redirected stdout/stderr streams.

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

### -NoTerminateOnTimeout
Specifies that the process shouldn't terminate on timeout.

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

### -AdditionalEnvironmentVariables
Specifies additional environment variables to inject into the user's session.

```yaml
Type: IDictionary
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WaitOption
Specifies the wait type to use when waiting for an invoked executable to finish.

```yaml
Type: WaitType
Parameter Sets: PrimaryActiveUserSessionWithWait, AllActiveUserSessionsWithWait, SessionIdWithWait, UsernameWithWait
Aliases:
Accepted values: WaitForAny, WaitForAll

Required: False
Position: Named
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

### -PassThru
If NoWait is not specified, returns an object with ExitCode, STDOut and STDErr output from the process.
If NoWait is specified, returns an object with Id, Handle and ProcessName.

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

### System.Threading.Tasks.Task[System.Int32]
### Returns a task object indicating the process's result.
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
