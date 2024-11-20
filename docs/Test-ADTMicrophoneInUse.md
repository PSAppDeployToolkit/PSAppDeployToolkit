---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Test-ADTMicrophoneInUse

## SYNOPSIS
Tests whether the device's microphone is in use.

## SYNTAX

```
Test-ADTMicrophoneInUse [<CommonParameters>]
```

## DESCRIPTION
Tests whether someone is using the microphone on their device.
This could be within Teams, Zoom, a game, or any other app that uses a microphone.

## EXAMPLES

### EXAMPLE 1
```
Test-ADTMicrophoneInUse
```

Checks if the microphone is in use and returns true or false.

## PARAMETERS

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable.
For more information, see about_CommonParameters (http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
### You cannot pipe objects to this function.
## OUTPUTS

### System.Boolean
### Returns $true if the microphone is in use, otherwise returns $false.
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
