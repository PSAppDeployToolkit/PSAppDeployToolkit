# Test-ServiceExists

## SYNOPSIS

Check to see if a service exists.

## SYNTAX

 `Test-ServiceExists [-Name] <String> [[-ComputerName] <String>] [-PassThru] [[-ContinueOnError] <Boolean>] [<CommonParameters>]`

## DESCRIPTION

Check to see if a service exists (using WMI method because Get-Service will generate ErrorRecord if service doesn't exist).

## PARAMETERS

`-Name <String>`

Specify the name of the service.

Note: Service name can be found by executing "Get-Service | Format-Table -AutoSize -Wrap" or by using the properties screen of a service in services.msc.

`-ComputerName <String>`

Specify the name of the computer. Default is: the local computer.

`-PassThru[<SwitchParameter>]`

Return the WMI service object.

`-ContinueOnError <Boolean>`

Continue if an error is encountered. Default is: `$true`.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Test-ServiceExists -Name 'wuauserv'`

-------------------------- EXAMPLE 2 --------------------------

`PS C:>Test-ServiceExists -Name 'testservice' -PassThru | Where-Object { $_ } | ForEach-Object { $_.Delete() }`

Check if a service exists and then delete it by using the -PassThru parameter.

## REMARKS

To see the examples, type: `Get-Help Test-ServiceExists -Examples`

For more information, type: `Get-Help Test-ServiceExists -Detailed`

For technical information, type: `Get-Help Test-ServiceExists -Full`

For online help, type: `Get-Help Test-ServiceExists -Online`
