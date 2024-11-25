---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Invoke-ADTObjectMethod

## SYNOPSIS
Invoke method on any object.

## SYNTAX

### Positional (Default)
```
Invoke-ADTObjectMethod [-InputObject] <Object> [-MethodName] <String> [[-ArgumentList] <Object[]>]
 [<CommonParameters>]
```

### Named
```
Invoke-ADTObjectMethod [-InputObject] <Object> [-MethodName] <String> [-Parameter] <Hashtable>
 [<CommonParameters>]
```

## DESCRIPTION
Invoke method on any object with or without using named parameters.

## EXAMPLES

### EXAMPLE 1
```
$ShellApp = New-Object -ComObject 'Shell.Application'
$null = Invoke-ADTObjectMethod -InputObject $ShellApp -MethodName 'MinimizeAll'
```


Minimizes all windows.

### EXAMPLE 2
```
$ShellApp = New-Object -ComObject 'Shell.Application'
$null = Invoke-ADTObjectMethod -InputObject $ShellApp -MethodName 'Explore' -Parameter @{'vDir'='C:\Windows'}
```


Opens the C:\Windows folder in a Windows Explorer window.

## PARAMETERS

### -InputObject
Specifies an object which has methods that can be invoked.

```yaml
Type: Object
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MethodName
Specifies the name of a method to invoke.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ArgumentList
Argument to pass to the method being executed.
Allows execution of method without specifying named parameters.

```yaml
Type: Object[]
Parameter Sets: Positional
Aliases:

Required: False
Position: 3
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Parameter
Argument to pass to the method being executed.
Allows execution of method by using named parameters.

```yaml
Type: Hashtable
Parameter Sets: Named
Aliases:

Required: True
Position: 3
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

### System.Object
### The object returned by the method being invoked.
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
