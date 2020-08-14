# Get-FreeDiskSpace

## SYNOPSIS

Retrieves the free disk space in MB on a particular drive (defaults to system drive)

## SYNTAX

 `Get-FreeDiskSpace [[-Drive] <String>] [[-ContinueOnError] <Boolean>] [<CommonParameters>]`

## DESCRIPTION

Retrieves the free disk space in MB on a particular drive (defaults to system drive)

## PARAMETERS

`-Drive <String>`

Drive to check free disk space on

`-ContinueOnError <Boolean>`

Continue if an error is encountered. Default is: `$true`.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Get-FreeDiskSpace -Drive 'C:'`

## REMARKS

To see the examples, type: `Get-Help Get-FreeDiskSpace -Examples`

For more information, type: `Get-Help Get-FreeDiskSpace -Detailed`

For technical information, type: `Get-Help Get-FreeDiskSpace -Full`

For online help, type: `Get-Help Get-FreeDiskSpace -Online`
