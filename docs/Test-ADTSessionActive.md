---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Test-ADTSessionActive

## SYNOPSIS
Checks if there is an active ADT session.

## SYNTAX

```
Test-ADTSessionActive
```

## DESCRIPTION
This function checks if there is an active ADT (App Deploy Toolkit) session by retrieving the module data and returning the count of active sessions.

## EXAMPLES

### EXAMPLE 1
```
Test-ADTSessionActive
```

Checks if there is an active ADT session and returns true or false.

## PARAMETERS

## INPUTS

### None
### You cannot pipe objects to this function.
## OUTPUTS

### System.Boolean
### Returns $true if there is at least one active session, otherwise $false.
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
