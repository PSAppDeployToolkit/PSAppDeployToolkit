---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Resolve-ADTErrorRecord

## SYNOPSIS
Enumerates ErrorRecord details.

## SYNTAX

```
Resolve-ADTErrorRecord [-ErrorRecord] <ErrorRecord> [[-Property] <String[]>] [-ExcludeErrorRecord]
 [-ExcludeErrorInvocation] [-ExcludeErrorException] [-ExcludeErrorInnerException] [<CommonParameters>]
```

## DESCRIPTION
Enumerates an ErrorRecord, or a collection of ErrorRecord properties.
This function can filter and display specific properties of the ErrorRecord, and can exclude certain parts of the error details.

## EXAMPLES

### EXAMPLE 1
```
Resolve-ADTErrorRecord
```

Enumerates the details of the last ErrorRecord.

### EXAMPLE 2
```
Resolve-ADTErrorRecord -Property *
```

Enumerates all properties of the last ErrorRecord.

### EXAMPLE 3
```
Resolve-ADTErrorRecord -Property InnerException
```

Enumerates only the InnerException property of the last ErrorRecord.

### EXAMPLE 4
```
Resolve-ADTErrorRecord -ExcludeErrorInvocation
```

Enumerates the details of the last ErrorRecord, excluding the invocation information.

## PARAMETERS

### -ErrorRecord
The ErrorRecord to resolve.
For usage in a catch block, you'd use the automatic variable `$PSItem`.
For usage out of a catch block, you can access the global $Error array's first error (on index 0).

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
The list of properties to display from the ErrorRecord.
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
Exclude ErrorRecord details as represented by $ErrorRecord.

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
Exclude ErrorRecord invocation information as represented by $ErrorRecord.InvocationInfo.

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
Exclude ErrorRecord exception details as represented by $ErrorRecord.Exception.

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
Exclude ErrorRecord inner exception details as represented by $ErrorRecord.Exception.InnerException.
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
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.Management.Automation.ErrorRecord
### Accepts one or more ErrorRecord objects via the pipeline.
## OUTPUTS

### System.String
### Displays the ErrorRecord details.
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
