# Write-Log

## SYNOPSIS

Write messages to a log file in CMTrace.exe compatible format or Legacy text file format.

## SYNTAX

 `Write-Log [-Message] <String> [[-Severity] <Int16>] [[-Source] <String>] [[-ScriptSection] <String>] [[-LogType] <String>] [[-LogFileDirectory] <String>] [[-LogFileName] <String>] [[-MaxLogFileSizeMB] <Decimal>] [[-WriteHost] <Boolean>] [[-ContinueOnError] <Boolean>] [[-PassThru]] [[-DebugMessage]] [[-LogDebugMessage] <Boolean>] [<CommonParameters>]`

## DESCRIPTION

Write messages to a log file in CMTrace.exe compatible format or Legacy text file format and optionally display in the console.

## PARAMETERS

`-Message <String>`

The message to write to the log file or output to the console.

`-Severity <Int16>`

Defines message type. When writing to console or CMTrace.exe log format, it allows highlighting of message type.

Options: 1 = Information (default), 2 = Warning (highlighted in yellow), 3 = Error (highlighted in red)

`-Source <String>`

The source of the message being logged.

`-ScriptSection <String>`

The heading for the portion of the script that is being executed. Default is: $script:installPhase.

`-LogType <String>`

Choose whether to write a CMTrace.exe compatible log file or a Legacy text log file.

`-LogFileDirectory <String>`

Set the directory where the log file will be saved.

`-LogFileName <String>`

Set the name of the log file.

`-MaxLogFileSizeMB <Decimal>`

Maximum file size limit for log file in megabytes (MB). Default is 10 MB.

`-WriteHost <Boolean>`

Write the log message to the console.

`-ContinueOnError <Boolean>`

Suppress writing log message to console on failure to write message to log file. Default is: `$true`.

`-PassThru [<SwitchParameter>]`

Return the message that was passed to the function

`-DebugMessage [<SwitchParameter>]`

Specifies that the message is a debug message. Debug messages only get logged if -LogDebugMessage is set to `$true`.

`-LogDebugMessage <Boolean>`

Debug messages only get logged if this parameter is set to `$true` in the config XML file.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Write-Log -Message "Installing patch MS15-031" -Source 'Add-Patch' -LogType 'CMTrace'`

-------------------------- EXAMPLE 2 --------------------------

`PS C:>Write-Log -Message "Script is running on Windows 8" -Source 'Test-ValidOS' -LogType 'Legacy'`

## REMARKS

To see the examples, type: `Get-Help Write-Log -Examples`

For more information, type: `Get-Help Write-Log -Detailed`

For technical information, type: `Get-Help Write-Log -Full`

For online help, type: `Get-Help Write-Log -Online`
