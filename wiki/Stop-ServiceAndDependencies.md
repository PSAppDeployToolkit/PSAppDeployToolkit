# Stop-ServiceAndDependencies

## SYNOPSIS

Stop Windows service and its dependencies.

## SYNTAX

 `Stop-ServiceAndDependencies [-Name] <String> [[-ComputerName] <String>] [-SkipServiceExistsTest] [-SkipDependentServices] [[-PendingStatusWait] <TimeSpan>] [-PassThru] [[-ContinueOnError]`

<Boolean>] [<CommonParameters>]

## DESCRIPTION

Stop Windows service and its dependencies.

## PARAMETERS

`-Name <String>`

Specify the name of the service.

`-ComputerName <String>`

Specify the name of the computer. Default is: the local computer.

`-SkipServiceExistsTest [<SwitchParameter>]`

Choose to skip the test to check whether or not the service exists if it was already done outside of this function.

`-SkipDependentServices [<SwitchParameter>]`

Choose to skip checking for and stopping dependent services. Default is: `$false`.

`-PendingStatusWait <TimeSpan>`

The amount of time to wait for a service to get out of a pending state before continuing. Default is 60 seconds.

`-PassThru[<SwitchParameter>]`

Return the System.ServiceProcess.ServiceController service object.

`-ContinueOnError <Boolean>`

Continue if an error is encountered. Default is: `$true`.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Stop-ServiceAndDependencies -Name 'wuauserv'`

## REMARKS

To see the examples, type: `Get-Help Stop-ServiceAndDependencies -Examples`

For more information, type: `Get-Help Stop-ServiceAndDependencies -Detailed`

For technical information, type: `Get-Help Stop-ServiceAndDependencies -Full`

For online help, type: `Get-Help Stop-ServiceAndDependencies -Online`
