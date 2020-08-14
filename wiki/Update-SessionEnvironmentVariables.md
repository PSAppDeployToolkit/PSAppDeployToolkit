# Update-SessionEnvironmentVariables

## SYNOPSIS

Updates the environment variables for the current PowerShell session with any environment variable changes that may have occurred during script execution.

## SYNTAX

 `Update-SessionEnvironmentVariables [-LoadLoggedOnUserEnvironmentVariables] [[-ContinueOnError] <Boolean>] [<CommonParameters>]`

## DESCRIPTION

Environment variable changes that take place during script execution are not visible to the current PowerShell session.

Use this function to refresh the current PowerShell session with all environment variable settings.

## PARAMETERS

`-LoadLoggedOnUserEnvironmentVariables [<SwitchParameter>`

If script is running in SYSTEM context, this option allows loading environment variables from the active console user. If no console user exists but users are logged in, such as on

terminal servers, then the first logged-in non-console user.

`-ContinueOnError <Boolean>`

Continue if an error is encountered. Default is: `$true`.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Update-SessionEnvironmentVariables`

## REMARKS

To see the examples, type: `Get-Help Update-SessionEnvironmentVariables -Examples`

For more information, type: `Get-Help Update-SessionEnvironmentVariables -Detailed`

For technical information, type: `Get-Help Update-SessionEnvironmentVariables -Full`

For online help, type: `Get-Help Update-SessionEnvironmentVariables -Online`
