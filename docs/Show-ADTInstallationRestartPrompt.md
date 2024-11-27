---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Show-ADTInstallationRestartPrompt

## SYNOPSIS
Displays a restart prompt with a countdown to a forced restart.

## SYNTAX

```
Show-ADTInstallationRestartPrompt [[-CountdownSeconds] <UInt32>] [[-CountdownNoHideSeconds] <UInt32>]
 [[-SilentCountdownSeconds] <UInt32>] [-SilentRestart] [-NoCountdown] [-NotTopMost] -Title <String>
 -Subtitle <String> [<CommonParameters>]
```

## DESCRIPTION
Displays a restart prompt with a countdown to a forced restart.
The prompt can be customized with a title, countdown duration, and whether it should be topmost.
It also supports silent mode where the restart can be triggered without user interaction.

## EXAMPLES

### EXAMPLE 1
```
Show-ADTInstallationRestartPrompt -NoCountdown
```

Displays a restart prompt without a countdown.

### EXAMPLE 2
```
Show-ADTInstallationRestartPrompt -Countdownseconds 300
```

Displays a restart prompt with a 300-second countdown.

### EXAMPLE 3
```
Show-ADTInstallationRestartPrompt -CountdownSeconds 600 -CountdownNoHideSeconds 60
```

Displays a restart prompt with a 600-second countdown and triggers a silent restart with a 60-second countdown in silent mode.

## PARAMETERS

### -CountdownSeconds
Specifies the number of seconds to display the restart prompt.
Default: 60

```yaml
Type: UInt32
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: 60
Accept pipeline input: False
Accept wildcard characters: False
```

### -CountdownNoHideSeconds
Specifies the number of seconds to display the restart prompt without allowing the window to be hidden.
Default: 30

```yaml
Type: UInt32
Parameter Sets: (All)
Aliases:

Required: False
Position: 2
Default value: 30
Accept pipeline input: False
Accept wildcard characters: False
```

### -SilentCountdownSeconds
Specifies number of seconds to countdown for the restart when the toolkit is running in silent mode and NoSilentRestart is $false.
Default: 5

```yaml
Type: UInt32
Parameter Sets: (All)
Aliases:

Required: False
Position: 3
Default value: 5
Accept pipeline input: False
Accept wildcard characters: False
```

### -SilentRestart
Specifies whether the restart should be triggered when Deploy mode is silent or very silent.

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

### -NoCountdown
Specifies whether the user should receive a prompt to immediately restart their workstation.

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

### -NotTopMost
Specifies whether the prompt shouldn't be topmost, above all other windows.

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

### -Subtitle
Subtitle of the prompt.
Default: the application deployment type.

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

### -Title
Title of the prompt.
Default: the application installation name.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
### You cannot pipe objects to this function.
## OUTPUTS

### None
### This function does not generate any output.
## NOTES
Be mindful of the countdown you specify for the reboot as code directly after this function might NOT be able to execute - that includes logging.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
