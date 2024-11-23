---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Get-ADTUniversalDate

## SYNOPSIS
Returns the date/time for the local culture in a universal sortable date time pattern.

## SYNTAX

```
Get-ADTUniversalDate [[-DateTime] <String>] [<CommonParameters>]
```

## DESCRIPTION
Converts the current datetime or a datetime string for the current culture into a universal sortable date time pattern, e.g.
2013-08-22 11:51:52Z.

## EXAMPLES

### EXAMPLE 1
```
Get-ADTUniversalDate
```

Returns the current date in a universal sortable date time pattern.

### EXAMPLE 2
```
Get-ADTUniversalDate -DateTime '25/08/2013'
```

Returns the date for the current culture in a universal sortable date time pattern.

## PARAMETERS

### -DateTime
Specify the DateTime in the current culture.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: [System.DateTime]::Now.ToString([System.Globalization.DateTimeFormatInfo]::CurrentInfo.UniversalSortableDateTimePattern)
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
### You cannot pipe objects to this function.
## OUTPUTS

### System.String
### Returns the date/time for the local culture in a universal sortable date time pattern.
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
