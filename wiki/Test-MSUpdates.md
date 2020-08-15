# Test-MSUpdates

## SYNOPSIS

Test whether a Microsoft Windows update is installed.

## SYNTAX

 `Test-MSUpdates [-KBNumber] <String> [[-ContinueOnError] <Boolean>] [<CommonParameters>]`

## DESCRIPTION

Test whether a Microsoft Windows update is installed.

## PARAMETERS

`-KBNumber <String>`

KBNumber of the update.

`-ContinueOnError <Boolean>`

Suppress writing log message to console on failure to write message to log file. Default is: `$true`.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Test-MSUpdates -KBNumber 'KB2549864'`

## REMARKS

To see the examples, type: `Get-Help Test-MSUpdates -Examples`

For more information, type: `Get-Help Test-MSUpdates -Detailed`

For technical information, type: `Get-Help Test-MSUpdates -Full`

For online help, type: `Get-Help Test-MSUpdates -Online`
