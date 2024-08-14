filter Write-ADTLogEntry
{
    <#

    .SYNOPSIS
    Write messages to a log file in CMTrace.exe compatible format or Legacy text file format.

    .DESCRIPTION
    Write messages to a log file in CMTrace.exe compatible format or Legacy text file format and optionally display in the console.

    .PARAMETER Message
    The message to write to the log file or output to the console.

    .PARAMETER Severity
    Defines message type. When writing to console or CMTrace.exe log format, it allows highlighting of message type.
    Options: 0 = Success (highlighted in green), 1 = Information (default), 2 = Warning (highlighted in yellow), 3 = Error (highlighted in red)

    .PARAMETER Source
    The source of the message being logged.

    .PARAMETER ScriptSection
    The heading for the portion of the script that is being executed. Default is: $installPhase.

    .PARAMETER PassThru
    Return the message that was passed to the function.

    .PARAMETER DebugMessage
    Specifies that the message is a debug message. Debug messages only get logged if -LogDebugMessage is set to $true.

    .INPUTS
    System.String. The message to write to the log file or output to the console.

    .OUTPUTS
    System.String[]. This function returns the provided output if -PassThru is specified.

    .EXAMPLE
    Write-ADTLogEntry -Message "Installing patch MS15-031" -Source 'Add-Patch'

    .EXAMPLE
    Write-ADTLogEntry -Message "Script is running on Windows 8" -Source 'Test-ValidOS'

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [AllowEmptyCollection()]
        [System.String[]]$Message,

        [Parameter(Mandatory = $false)]
        [ValidateRange(0, 3)]
        [System.Nullable[System.UInt32]]$Severity,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Source,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ScriptSection,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$DebugMessage
    )

    # The internals of this are within the session's class object.
    (Get-ADTSession).WriteLogEntry($Message, $Severity, $Source, $ScriptSection, $DebugMessage)
    if ($PassThru) {return $Message}
}
