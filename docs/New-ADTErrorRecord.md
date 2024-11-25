---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# New-ADTErrorRecord

## SYNOPSIS
Creates a new ErrorRecord object.

## SYNTAX

```
New-ADTErrorRecord [-Exception] <Exception> [-Category] <ErrorCategory> [[-ErrorId] <String>]
 [[-TargetObject] <Object>] [[-TargetName] <String>] [[-TargetType] <String>] [[-Activity] <String>]
 [[-Reason] <String>] [[-RecommendedAction] <String>] [<CommonParameters>]
```

## DESCRIPTION
This function creates a new ErrorRecord object with the specified exception, error category, and optional parameters.
It allows for detailed error information to be captured and returned to the caller, who can then throw the error.

## EXAMPLES

### EXAMPLE 1
```
$exception = [System.Exception]::new("An error occurred.")
$category = \[System.Management.Automation.ErrorCategory\]::NotSpecified
New-ADTErrorRecord -Exception $exception -Category $category -ErrorId "CustomErrorId" -TargetObject $null -TargetName "TargetName" -TargetType "TargetType" -Activity "Activity" -Reason "Reason" -RecommendedAction "RecommendedAction"
```


Creates a new ErrorRecord object with the specified parameters.

## PARAMETERS

### -Exception
The exception object that caused the error.

```yaml
Type: Exception
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Category
The category of the error.

```yaml
Type: ErrorCategory
Parameter Sets: (All)
Aliases:
Accepted values: NotSpecified, OpenError, CloseError, DeviceError, DeadlockDetected, InvalidArgument, InvalidData, InvalidOperation, InvalidResult, InvalidType, MetadataError, NotImplemented, NotInstalled, ObjectNotFound, OperationStopped, OperationTimeout, SyntaxError, ParserError, PermissionDenied, ResourceBusy, ResourceExists, ResourceUnavailable, ReadError, WriteError, FromStdErr, SecurityError, ProtocolError, ConnectionError, AuthenticationError, LimitsExceeded, QuotaExceeded, NotEnabled

Required: True
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ErrorId
The identifier for the error.
Default is 'NotSpecified'.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 3
Default value: NotSpecified
Accept pipeline input: False
Accept wildcard characters: False
```

### -TargetObject
The target object that the error is related to.

```yaml
Type: Object
Parameter Sets: (All)
Aliases:

Required: False
Position: 4
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TargetName
The name of the target that the error is related to.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 5
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TargetType
The type of the target that the error is related to.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 6
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Activity
The activity that was being performed when the error occurred.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 7
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Reason
The reason for the error.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 8
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -RecommendedAction
The recommended action to resolve the error.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 9
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
