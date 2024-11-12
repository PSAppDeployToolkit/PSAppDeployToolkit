---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# New-ADTValidateScriptErrorRecord

## SYNOPSIS
Creates a new ErrorRecord for script validation errors.

## SYNTAX

```
New-ADTValidateScriptErrorRecord [-ParameterName] <String> [-ProvidedValue] <Object>
 [-ExceptionMessage] <String> [[-InnerException] <Exception>] [<CommonParameters>]
```

## DESCRIPTION
This function creates a new ErrorRecord object for script validation errors.
It takes the parameter name, provided value, exception message, and an optional inner exception to build a detailed error record.
This helps in identifying and handling invalid parameter values in scripts.

## EXAMPLES

### EXAMPLE 1
```
$paramName = "FilePath"
PS C:\\\>$providedValue = "C:\InvalidPath"
PS C:\\\>$exceptionMessage = "The specified path does not exist."
PS C:\\\>New-ADTValidateScriptErrorRecord -ParameterName $paramName -ProvidedValue $providedValue -ExceptionMessage $exceptionMessage
```


Creates a new ErrorRecord for a validation error with the specified parameters.

## PARAMETERS

### -ParameterName
The name of the parameter that caused the validation error.

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

### -ProvidedValue
The value provided for the parameter that caused the validation error.

```yaml
Type: Object
Parameter Sets: (All)
Aliases:

Required: True
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExceptionMessage
The message describing the validation error.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 3
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -InnerException
An optional inner exception that provides more details about the validation error.

```yaml
Type: Exception
Parameter Sets: (All)
Aliases:

Required: False
Position: 4
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

### System.Management.Automation.ErrorRecord
### This function returns an ErrorRecord object.
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
