---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Get-ADTEnvironment

## SYNOPSIS
Retrieves the environment data for the ADT module.

## SYNTAX

```
Get-ADTEnvironment [<CommonParameters>]
```

## DESCRIPTION
The Get-ADTEnvironment function retrieves the environment data for the ADT module.
This function ensures that the ADT module has been initialized before attempting to retrieve the environment data.
If the module is not initialized, it throws an error.

## EXAMPLES

### EXAMPLE 1
```
$environment = Get-ADTEnvironment
```

This example retrieves the environment data for the ADT module and stores it in the $environment variable.

## PARAMETERS

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable.
For more information, see about_CommonParameters (http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
### You cannot pipe objects to this function.
## OUTPUTS

### System.Collections.Hashtable
### Returns the environment data as a hashtable.
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
