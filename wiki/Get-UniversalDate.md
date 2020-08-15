# Get-UniversalDate

## SYNOPSIS

Returns the date/time for the local culture in a universal sortable date time pattern.

## SYNTAX

 `Get-UniversalDate [[-DateTime] <String>] [[-ContinueOnError] <Boolean>] [<CommonParameters>]`

## DESCRIPTION

Converts the current datetime or a datetime string for the current culture into a universal sortable date time pattern, e.g. 2013-08-22 11:51:52Z

## PARAMETERS

`-DateTime <String>`

Specify the DateTime in the current culture.

`-ContinueOnError <Boolean>`

Continue if an error is encountered. Default: `$false`.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Get-UniversalDate`

Returns the current date in a universal sortable date time pattern.

-------------------------- EXAMPLE 2 --------------------------

`PS C:>Get-UniversalDate -DateTime '25/08/2013'`

Returns the date for the current culture in a universal sortable date time pattern.

## REMARKS

To see the examples, type: `Get-Help Get-UniversalDate -Examples`

For more information, type: `Get-Help Get-UniversalDate -Detailed`

For technical information, type: `Get-Help Get-UniversalDate -Full`

For online help, type: `Get-Help Get-UniversalDate -Online`
