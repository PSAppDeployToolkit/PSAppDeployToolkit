# Get-ServiceStartMode

## SYNOPSIS

Get the service startup mode.

## SYNTAX

 `Get-ServiceStartMode [-Name] <String> [[-ComputerName] <String>] [[-ContinueOnError] <Boolean>] [<CommonParameters>]`

## DESCRIPTION

Get the service startup mode.

## PARAMETERS

`-Name <String>`

Specify the name of the service.

`-ComputerName <String>`

Specify the name of the computer. Default is: the local computer.

`-ContinueOnError <Boolean>`

Continue if an error is encountered. Default is: `$true`.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Get-ServiceStartMode -Name 'wuauserv'`

## REMARKS

To see the examples, type: `Get-Help Get-ServiceStartMode -Examples`

For more information, type: `Get-Help Get-ServiceStartMode -Detailed`

For technical information, type: `Get-Help Get-ServiceStartMode -Full`

For online help, type: `Get-Help Get-ServiceStartMode -Online`
