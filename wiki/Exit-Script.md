# Exit-Script

## SYNOPSIS

Exit the script, perform cleanup actions, and pass an exit code to the parent process.

## SYNTAX

 `Exit-Script [[-ExitCode] <Int32>] [<CommonParameters>]`

## DESCRIPTION

Always use when exiting the script to ensure cleanup actions are performed.

## PARAMETERS

`-ExitCode <Int32>`

The exit code to be passed from the script to the parent process, e.g. SCCM

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Exit-Script -ExitCode 0`

-------------------------- EXAMPLE 2 --------------------------

`PS C:>Exit-Script -ExitCode 1618`

## REMARKS

To see the examples, type: `Get-Help Exit-Script -Examples`

For more information, type: `Get-Help Exit-Script -Detailed`

For technical information, type: `Get-Help Exit-Script -Full`

For online help, type: `Get-Help Exit-Script -Online`
