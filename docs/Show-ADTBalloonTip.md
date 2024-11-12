---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Show-ADTBalloonTip

## SYNOPSIS
Displays a balloon tip notification in the system tray.

## SYNTAX

```
Show-ADTBalloonTip [-BalloonTipText] <String> [-BalloonTipIcon <ToolTipIcon>] [-BalloonTipTime <UInt32>]
 -BalloonTipTitle <String> [<CommonParameters>]
```

## DESCRIPTION
Displays a balloon tip notification in the system tray.
This function can be used to show notifications to the user with customizable text, title, icon, and display duration.
For Windows 10 OS and above, a Toast notification is displayed in place of a balloon tip if toast notifications are enabled in the XML config file.

## EXAMPLES

### EXAMPLE 1
```
Show-ADTBalloonTip -BalloonTipText 'Installation Started' -BalloonTipTitle 'Application Name'
```

Displays a balloon tip with the text 'Installation Started' and the title 'Application Name'.

### EXAMPLE 2
```
Show-ADTBalloonTip -BalloonTipIcon 'Info' -BalloonTipText 'Installation Started' -BalloonTipTitle 'Application Name' -BalloonTipTime 1000
```

Displays a balloon tip with the info icon, the text 'Installation Started', the title 'Application Name', and a display duration of 1000 milliseconds.

## PARAMETERS

### -BalloonTipText
Text of the balloon tip.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -BalloonTipIcon
Icon to be used.
Options: 'Error', 'Info', 'None', 'Warning'.
Default is: Info.

```yaml
Type: ToolTipIcon
Parameter Sets: (All)
Aliases:
Accepted values: None, Info, Warning, Error

Required: False
Position: Named
Default value: Info
Accept pipeline input: False
Accept wildcard characters: False
```

### -BalloonTipTime
Time in milliseconds to display the balloon tip.
Default: 10000.

```yaml
Type: UInt32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: 10000
Accept pipeline input: False
Accept wildcard characters: False
```

### -BalloonTipTitle
Title of the balloon tip.

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
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable.
For more information, see about_CommonParameters (http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
### You cannot pipe objects to this function.
## OUTPUTS

### None
### This function does not return any output.
## NOTES
An active ADT session is NOT required to use this function.

For Windows 10 OS and above, a Toast notification is displayed in place of a balloon tip if toast notifications are enabled in the XML config file.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
