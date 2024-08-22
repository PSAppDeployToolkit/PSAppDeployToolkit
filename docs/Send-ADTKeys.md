---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: http://msdn.microsoft.com/en-us/library/System.Windows.Forms.SendKeys(v=vs.100).aspx
schema: 2.0.0
---

# Send-ADTKeys

## SYNOPSIS
Send a sequence of keys to one or more application windows.

## SYNTAX

### WindowTitle
```
Send-ADTKeys [-WindowTitle] <String> [-Keys] <String> [[-WaitSeconds] <Int32>] [<CommonParameters>]
```

### AllWindowTitles
```
Send-ADTKeys [-GetAllWindowTitles] [-Keys] <String> [[-WaitSeconds] <Int32>] [<CommonParameters>]
```

### WindowHandle
```
Send-ADTKeys [-WindowHandle] <IntPtr> [-Keys] <String> [[-WaitSeconds] <Int32>] [<CommonParameters>]
```

## DESCRIPTION
Send a sequence of keys to one or more application windows.
If the window title searched for returns more than one window, then all of them will receive the sent keys.

Function does not work in SYSTEM context unless launched with "psexec.exe -s -i" to run it as an interactive process under the SYSTEM account.

## EXAMPLES

### EXAMPLE 1
```
Send-ADTKeys -WindowTitle 'foobar - Notepad' -Keys 'Hello world'
```

Send the sequence of keys "Hello world" to the application titled "foobar - Notepad".

### EXAMPLE 2
```
Send-ADTKeys -WindowTitle 'foobar - Notepad' -Keys 'Hello world' -WaitSeconds 5
```

Send the sequence of keys "Hello world" to the application titled "foobar - Notepad" and wait 5 seconds.

### EXAMPLE 3
```
Send-ADTKeys -WindowHandle ([IntPtr]17368294) -Keys 'Hello World'
```

Send the sequence of keys "Hello World" to the application with a Window Handle of '17368294'.

## PARAMETERS

### -WindowTitle
The title of the application window to search for using regex matching.

```yaml
Type: String
Parameter Sets: WindowTitle
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -GetAllWindowTitles
Get titles for all open windows on the system.

```yaml
Type: SwitchParameter
Parameter Sets: AllWindowTitles
Aliases:

Required: True
Position: 2
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -WindowHandle
Send keys to a specific window where the Window Handle is already known.

```yaml
Type: IntPtr
Parameter Sets: WindowHandle
Aliases:

Required: True
Position: 3
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Keys
The sequence of keys to send.
Info on Key input at: http://msdn.microsoft.com/en-us/library/System.Windows.Forms.SendKeys(v=vs.100).aspx

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 4
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WaitSeconds
An optional number of seconds to wait after the sending of the keys.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: 5
Default value: 0
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
### This function does not return any objects.
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[http://msdn.microsoft.com/en-us/library/System.Windows.Forms.SendKeys(v=vs.100).aspx](http://msdn.microsoft.com/en-us/library/System.Windows.Forms.SendKeys(v=vs.100).aspx)

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
