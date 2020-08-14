# Get-ScheduledTask

## SYNOPSIS

Retrieve all details for scheduled tasks on the local computer.

## SYNTAX

 `Get-ScheduledTask [[-TaskName] <String>] [[-ContinueOnError] <Boolean>] [<CommonParameters>]`

## DESCRIPTION

Retrieve all details for scheduled tasks on the local computer using schtasks.exe. All property names have spaces and colons removed.

## PARAMETERS

`-TaskName <String>`

Specify the name of the scheduled task to retrieve details for. Uses regex match to find scheduled task.

`-ContinueOnError <Boolean>`

Continue if an error is encountered. Default: `$true`.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Get-ScheduledTask`

To display a list of all scheduled task properties.

-------------------------- EXAMPLE 2 --------------------------

`PS C:>Get-ScheduledTask | Out-GridView`

To display a grid view of all scheduled task properties.

-------------------------- EXAMPLE 3 --------------------------

`PS C:>Get-ScheduledTask | Select-Object -Property TaskName`

To display a list of all scheduled task names.

## REMARKS

To see the examples, type: `Get-Help Get-ScheduledTask -Examples`

For more information, type: `Get-Help Get-ScheduledTask -Detailed`

For technical information, type: `Get-Help Get-ScheduledTask -Full`

For online help, type: `Get-Help Get-ScheduledTask -Online`
