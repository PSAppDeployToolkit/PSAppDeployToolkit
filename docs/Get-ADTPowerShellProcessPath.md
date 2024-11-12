---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Get-ADTPowerShellProcessPath

## SYNOPSIS
Retrieves the path to the PowerShell executable.

## SYNTAX

```
Get-ADTPowerShellProcessPath
```

## DESCRIPTION
The Get-ADTPowerShellProcessPath function returns the path to the PowerShell executable.
It determines whether the current PowerShell session is running in Windows PowerShell or PowerShell Core and returns the appropriate executable path.

## EXAMPLES

### EXAMPLE 1
```
Get-ADTPowerShellProcessPath
```

This example retrieves the path to the PowerShell executable for the current session.

## PARAMETERS

## INPUTS

### None
### You cannot pipe objects to this function.
## OUTPUTS

### System.String
### Returns the path to the PowerShell executable as a string.
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
