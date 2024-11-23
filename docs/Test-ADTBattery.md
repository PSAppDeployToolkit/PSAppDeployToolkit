---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Test-ADTBattery

## SYNOPSIS
Tests whether the local machine is running on AC power or not.

## SYNTAX

```
Test-ADTBattery [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
Tests whether the local machine is running on AC power and returns true/false.
For detailed information, use the -PassThru option to get a hashtable containing various battery and power status properties.

## EXAMPLES

### EXAMPLE 1
```
Test-ADTBattery
```

Checks if the local machine is running on AC power and returns true or false.

### EXAMPLE 2
```
(Test-ADTBattery -PassThru).IsLaptop
```

Returns true if the current system is a laptop, otherwise false.

## PARAMETERS

### -PassThru
Outputs a hashtable containing the following properties:
- IsLaptop
- IsUsingACPower
- ACPowerLineStatus
- BatteryChargeStatus
- BatteryLifePercent
- BatteryLifeRemaining
- BatteryFullLifetime

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

### None
### You cannot pipe objects to this function.
## OUTPUTS

### PSADT.Types.BatteryInfo
### Returns a hashtable containing the following properties:
### - IsLaptop
### - IsUsingACPower
### - ACPowerLineStatus
### - BatteryChargeStatus
### - BatteryLifePercent
### - BatteryLifeRemaining
### - BatteryFullLifetime
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
