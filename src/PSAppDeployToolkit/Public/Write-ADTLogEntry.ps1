#-----------------------------------------------------------------------------
#
# MARK: Write-ADTLogEntry
#
#-----------------------------------------------------------------------------

function Write-ADTLogEntry
{
    <#
    .SYNOPSIS
        Write messages to a log file in CMTrace.exe compatible format or Legacy text file format.

    .DESCRIPTION
        Write messages to a log file in CMTrace.exe compatible format or Legacy text file format and optionally display in the console. This function supports different severity levels and can be used to log debug messages if required.

    .PARAMETER Message
        The message to write to the log file or output to the console.

    .PARAMETER Severity
        Defines message type. When writing to console or CMTrace.exe log format, it allows highlighting of message type.

    .PARAMETER Source
        The source of the message being logged.

    .PARAMETER ScriptSection
        The heading for the portion of the script that is being executed.

    .PARAMETER LogType
        Choose whether to write a CMTrace.exe compatible log file or a Legacy text log file.

    .PARAMETER LogFileDirectory
        Set the directory where the log file will be saved.

    .PARAMETER LogFileName
        Set the name of the log file.

    .PARAMETER HostLogStream
        Controls how the log entry is written to the console window.

    .PARAMETER PassThru
        Return the message that was passed to the function.

    .PARAMETER DebugMessage
        Specifies that the message is a debug message. Debug messages only get logged if -LogDebugMessage is set to $true.

    .INPUTS
        System.String

        The message to write to the log file or output to the console.

    .OUTPUTS
        PSADT.Module.LogEntry[]

        This function returns the provided output if -PassThru is specified.

    .EXAMPLE
        Write-ADTLogEntry -Message "Installing patch MS15-031" -Source 'Add-Patch'

        Writes a log entry indicating that patch MS15-031 is being installed.

    .EXAMPLE
        Write-ADTLogEntry -Message "Script is running on Windows 11" -Source 'Test-ValidOS'

        Writes a log entry indicating that the script is running on Windows 11.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Write-ADTLogEntry
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Message,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSADT.Module.LogSeverity]$Severity,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Source = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ScriptSection = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSADT.Module.LogStyle]$LogType,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$LogFileDirectory = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$LogFileName = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSADT.Module.HostLogStream]$HostLogStream,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$DebugMessage
    )

    begin
    {
        # Get the caller's preference values and set them within this scope.
        Set-ADTPreferenceVariables -SessionState $ExecutionContext.SessionState

        # Set up collector for piped in messages.
        $messages = [System.Collections.Generic.List[System.String]]::new()

        # Force the HostLogStream to none if InformationPreference or WarningPreference is silent.
        $bypassSession = if ((($Severity -le 1) -and ($InformationPreference -match '^(SilentlyContinue|Ignore)$')) -or (($Severity -eq 2) -and ($WarningPreference -match '^(SilentlyContinue|Ignore)$')))
        {
            !($PSBoundParameters.HostLogStream = $HostLogStream = [PSADT.Module.HostLogStream]::None)
        }
    }

    process
    {
        # Add all non-null messages to the collector.
        $Message | & {
            process
            {
                if (![System.String]::IsNullOrWhiteSpace($_))
                {
                    $messages.Add($_)
                }
            }
        }
    }

    end
    {
        # Return early if we have no messages to write out.
        if (!$messages.Count)
        {
            return
        }

        # If we don't have an active session, write the message to the verbose stream (4).
        $logEntries = if (!$bypassSession -and (Test-ADTSessionActive))
        {
            (Get-ADTSession).WriteLogEntry(
                $messages,
                $DebugMessage,
                $(if ($PSBoundParameters.ContainsKey('Severity')) { $Severity }),
                $(if ($PSBoundParameters.ContainsKey('Source')) { $Source }),
                $(if ($PSBoundParameters.ContainsKey('ScriptSection')) { $ScriptSection }),
                $(if ($PSBoundParameters.ContainsKey('LogFileDirectory')) { $LogFileDirectory }),
                $(if ($PSBoundParameters.ContainsKey('LogFileName')) { $LogFileName }),
                $(if ($PSBoundParameters.ContainsKey('LogType')) { $LogType }),
                $HostLogStream
            )
        }
        elseif (!$DebugMessage)
        {
            if ($PSBoundParameters.ContainsKey('LogFileDirectory') -and $PSBoundParameters.ContainsKey('LogFileName') -and !$PSBoundParameters.ContainsKey('LogType') -and !(Test-ADTModuleInitialized))
            {
                Initialize-ADTModule
            }
            [PSADT.Module.LogUtilities]::WriteLogEntry(
                $messages,
                $(if ($PSBoundParameters.ContainsKey('HostLogStream')) { $HostLogStream } else { ([PSADT.Module.HostLogStream]::None, [PSADT.Module.HostLogStream]::Verbose)[$VerbosePreference.Equals([System.Management.Automation.ActionPreference]::Continue)] }),
                $false,
                $(if ($PSBoundParameters.ContainsKey('Severity')) { $Severity }),
                $(if ($PSBoundParameters.ContainsKey('Source')) { $Source }),
                $(if ($PSBoundParameters.ContainsKey('ScriptSection')) { $ScriptSection }),
                $(if ($PSBoundParameters.ContainsKey('LogFileDirectory')) { $LogFileDirectory }),
                $(if ($PSBoundParameters.ContainsKey('LogFileName')) { $LogFileName }),
                $(if ($PSBoundParameters.ContainsKey('LogType')) { $LogType })
            )
        }
        if ($PassThru -and $logEntries)
        {
            return $logEntries
        }
    }
}
