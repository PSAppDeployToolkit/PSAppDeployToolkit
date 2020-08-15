# Set-ServiceStartMode

## SYNOPSIS

Set the service startup mode.

## SYNTAX

 `Set-ServiceStartMode [-Name] <String> [[-ComputerName] <String>] [-StartMode] <String> [[-ContinueOnError] <Boolean>] [<CommonParameters>]`

## DESCRIPTION

Set the service startup mode.

## PARAMETERS

`-Name <String>`

Specify the name of the service.

`-ComputerName <String>`

Specify the name of the computer. Default is: the local computer.

`-StartMode <String>`

Specify startup mode for the service. Options: Automatic, Automatic (Delayed Start), Manual, Disabled, Boot, System.

`-ContinueOnError <Boolean>`

Continue if an error is encountered. Default is: `$true`.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Set-ServiceStartMode -Name 'wuauserv' -StartMode 'Automatic (Delayed Start)'`

## REMARKS

To see the examples, type: `Get-Help Set-ServiceStartMode -Examples`

For more information, type: `Get-Help Set-ServiceStartMode -Detailed`

For technical information, type: `Get-Help Set-ServiceStartMode -Full`

For online help, type: `Get-Help Set-ServiceStartMode -Online`
