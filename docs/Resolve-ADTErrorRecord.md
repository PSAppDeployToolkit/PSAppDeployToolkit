---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Resolve-ADTErrorRecord

## SYNOPSIS
Enumerates error record details.

## SYNTAX

```
Resolve-ADTErrorRecord [-ErrorRecord] <ErrorRecord> [[-Property] <String[]>] [-ExcludeErrorRecord]
[-ExcludeErrorInvocation] [-ExcludeErrorException] [-ExcludeErrorInnerException] [<CommonParameters>]
```

## DESCRIPTION
Enumerates an error record, or a collection of error record properties.
By default, the details for the last error will be enumerated.
This function can filter and display specific properties of the error record, and can exclude certain parts of the error details.

## EXAMPLES

### EXAMPLE 1
```
Resolve-ADTErrorRecord
```

Enumerates the details of the last error record.

### EXAMPLE 2
```
Resolve-ADTErrorRecord -Property *
```

Enumerates all properties of the last error record.

### EXAMPLE 3
```
Resolve-ADTErrorRecord -Property InnerException
```

Enumerates only the InnerException property of the last error record.

### EXAMPLE 4
```
Resolve-ADTErrorRecord -ExcludeErrorInvocation
```

Enumerates the details of the last error record, excluding the invocation information.

## PARAMETERS

### -ErrorRecord
The error record to resolve.
The default error record is the latest one: $global:Error\[0\].
This parameter will also accept an array of error records.

```yaml
Type: ErrorRecord
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Property
The list of properties to display from the error record.
Use "*" to display all properties.

Default list of error properties is: Message, FullyQualifiedErrorId, ScriptStackTrace, PositionMessage, InnerException

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 2
Default value: ('Message', 'InnerException', 'FullyQualifiedErrorId', 'ScriptStackTrace', 'PositionMessage')
Accept pipeline input: False
Accept wildcard characters: True
```

### -ExcludeErrorRecord
Exclude error record details as represented by $_.

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

### -ExcludeErrorInvocation
Exclude error record invocation information as represented by $_.InvocationInfo.

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

### -ExcludeErrorException
Exclude error record exception details as represented by $_.Exception.

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

### -ExcludeErrorInnerException
Exclude error record inner exception details as represented by $_.Exception.InnerException.
Will retrieve all inner exceptions if there is more than one.

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

### System.Management.Automation.ErrorRecord
### Accepts one or more ErrorRecord objects via the pipeline.
## OUTPUTS

### System.String
### Displays the error record details.
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
