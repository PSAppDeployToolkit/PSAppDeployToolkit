# Test-NetworkConnection

## SYNOPSIS

Tests for an active local network connection, excluding wireless and virtual network adapters.

## SYNTAX

 `Test-NetworkConnection [<CommonParameters>]`

## DESCRIPTION

Tests for an active local network connection, excluding wireless and virtual network adapters, by querying the Win32_NetworkAdapter WMI class.

## PARAMETERS

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Test-NetworkConnection`

## REMARKS

To see the examples, type: `Get-Help Test-NetworkConnection -Examples`

For more information, type: `Get-Help Test-NetworkConnection -Detailed`

For technical information, type: `Get-Help Test-NetworkConnection -Full`

For online help, type: `Get-Help Test-NetworkConnection -Online`
