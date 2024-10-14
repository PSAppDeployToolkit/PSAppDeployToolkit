---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Show-ADTInstallationProgress

## SYNOPSIS
Displays a progress dialog in a separate thread with an updateable custom message.

## SYNTAX

```
Show-ADTInstallationProgress [[-WindowLocation] <String>] [[-MessageAlignment] <TextAlignment>] [-NotTopMost]
 [-NoRelocation] -WindowTitle <String> [-WindowSubtitle <String>] -StatusMessage <String>
 -StatusMessageDetail <String> [<CommonParameters>]
```

## DESCRIPTION
Creates a WPF window in a separate thread to display a marquee style progress ellipse with a custom message that can be updated.
The status message supports line breaks.

The first time this function is called in a script, it will display a balloon tip notification to indicate that the installation has started (provided balloon tips are enabled in the configuration).

## EXAMPLES

### EXAMPLE 1
```
Show-ADTInstallationProgress
```

Uses the default status message from the XML configuration file.

### EXAMPLE 2
```
Show-ADTInstallationProgress -StatusMessage 'Installation in Progress...'
```

Displays a progress dialog with the status message 'Installation in Progress...'.

### EXAMPLE 3
```
Show-ADTInstallationProgress -StatusMessage "Installation in Progress...`nThe installation may take 20 minutes to complete."
```

Displays a progress dialog with a multiline status message.

### EXAMPLE 4
```
Show-ADTInstallationProgress -StatusMessage 'Installation in Progress...' -WindowLocation 'BottomRight' -NotTopMost
```

Displays a progress dialog with the status message 'Installation in Progress...', positioned at the bottom right of the screen, and not set as topmost.

## PARAMETERS

### -WindowLocation
The location of the progress window.
Default: center of the screen.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: Default
Accept pipeline input: False
Accept wildcard characters: False
```

### -MessageAlignment
The text alignment to use for the status message.
Default: center.

```yaml
Type: TextAlignment
Parameter Sets: (All)
Aliases:
Accepted values: Left, Right, Center, Justify

Required: False
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NotTopMost
Specifies whether the progress window shouldn't be topmost.
Default: $false.

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

### -NoRelocation
Specifies whether to not reposition the window upon updating the message.
Default: $false.

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

### -StatusMessage
The status message to be displayed.
The default status message is taken from the configuration file.

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

### -StatusMessageDetail
The status message detail to be displayed with a fluent progress window.
The default status message is taken from the configuration file.

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

### -WindowSubtitle
The subtitle of the window to be displayed with a fluent progress window.
The default is null.

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

### -WindowTitle
The title of the window to be displayed.
The default is the derived value from $InstallTitle.

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
### This function does not generate any output.
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)

