# Test-Battery

## SYNOPSIS

Tests whether the local machine is running on AC power or not.

## SYNTAX

 `Test-Battery [-PassThru] [<CommonParameters>]`

## DESCRIPTION

Tests whether the local machine is running on AC power and returns true/false. For detailed information, use -PassThru option.

## PARAMETERS

`-PassThru[<SwitchParameter>]`

Outputs a hashtable containing the following properties:

IsLaptop, IsUsingACPower, ACPowerLineStatus, BatteryChargeStatus, BatteryLifePercent, BatteryLifeRemaining, BatteryFullLifetime

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Test-Battery`

-------------------------- EXAMPLE 2 --------------------------

`PS C:>(Test-Battery -PassThru).IsLaptop`

Determines if the current system is a laptop or not.

## REMARKS

To see the examples, type: `Get-Help Test-Battery -Examples`

For more information, type: `Get-Help Test-Battery -Detailed`

For technical information, type: `Get-Help Test-Battery -Full`

For online help, type: `Get-Help Test-Battery -Online`
