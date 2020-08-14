# Execute-MSP

## SYNOPSIS

Reads SummaryInfo targeted product codes in MSP file and determines if the MSP file applies to any installed products

If a valid installed product is found, triggers the Execute-MSI function to patch the installation.

## SYNTAX

 `Execute-MSP [-Path] <String> [<CommonParameters>]`

## DESCRIPTION

## PARAMETERS

`-Path <String>`

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Execute-MSP -Path 'Adobe_Reader_11.0.3_EN.msp'`

## REMARKS

To see the examples, type: `Get-Help Execute-MSP -Examples`

For more information, type: `Get-Help Execute-MSP -Detailed`

For technical information, type: `Get-Help Execute-MSP -Full`

For online help, type: `Get-Help Execute-MSP -Online`
