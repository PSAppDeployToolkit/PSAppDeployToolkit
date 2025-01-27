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
        Options: 0 = Success (highlighted in green), 1 = Information (default), 2 = Warning (highlighted in yellow), 3 = Error (highlighted in red)

    .PARAMETER Source
        The source of the message being logged.

    .PARAMETER ScriptSection
        The heading for the portion of the script that is being executed. Default is: "$($adtSession.InstallPhase)".

    .PARAMETER LogType
        Choose whether to write a CMTrace.exe compatible log file or a Legacy text log file.

    .PARAMETER LogFileDirectory
        Set the directory where the log file will be saved.

    .PARAMETER LogFileName
        Set the name of the log file.

    .PARAMETER PassThru
        Return the message that was passed to the function.

    .PARAMETER DebugMessage
        Specifies that the message is a debug message. Debug messages only get logged if -LogDebugMessage is set to $true.

    .INPUTS
        System.String

        The message to write to the log file or output to the console.

    .OUTPUTS
        System.String[]

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
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding()]
    [OutputType([System.Collections.Specialized.StringCollection])]
    param
    (
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
        [ValidateSet('CMTrace', 'Legacy')]
        [System.String]$LogType,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$LogFileDirectory,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$LogFileName,

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
        $messages = [System.Collections.Specialized.StringCollection]::new()
    }

    process
    {
        # Return early if the InformationPreference is silent.
        if (($Severity -le 1) -and ($InformationPreference -match '^(SilentlyContinue|Ignore)$'))
        {
            return
        }

        # Add all non-null messages to the collector.
        $null = $Message | & {
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
        if (Test-ADTSessionActive)
        {
            (Get-ADTSession).WriteLogEntry(
                $messages,
                $DebugMessage,
                $Severity,
                $(if ($PSBoundParameters.ContainsKey('Source')) { $Source }),
                $(if ($PSBoundParameters.ContainsKey('ScriptSection')) { $ScriptSection }),
                $(if ($PSBoundParameters.ContainsKey('LogFileDirectory')) { $LogFileDirectory }),
                $(if ($PSBoundParameters.ContainsKey('LogFileName')) { $LogFileName }),
                $(if ($PSBoundParameters.ContainsKey('LogType')) { $LogType }),
                $null
            )
        }
        elseif (!$DebugMessage)
        {
            if ($PSBoundParameters.ContainsKey('LogFileDirectory') -and $PSBoundParameters.ContainsKey('LogFileName') -and !$PSBoundParameters.ContainsKey('LogType') -and !(Test-ADTModuleInitialized))
            {
                Initialize-ADTModule
            }
            [PSADT.Module.LoggingUtilities]::WriteLogEntry(
                $messages,
                ([PSADT.Module.HostLogStream]::None, [PSADT.Module.HostLogStream]::Verbose)[$VerbosePreference.Equals([System.Management.Automation.ActionPreference]::Continue)],
                $false,
                $Severity,
                $(if ($PSBoundParameters.ContainsKey('Source')) { $Source }),
                $(if ($PSBoundParameters.ContainsKey('ScriptSection')) { $ScriptSection }),
                $(if ($PSBoundParameters.ContainsKey('LogFileDirectory')) { $LogFileDirectory }),
                $(if ($PSBoundParameters.ContainsKey('LogFileName')) { $LogFileName }),
                $(if ($PSBoundParameters.ContainsKey('LogType')) { $LogType })
            )
        }

        # Return the provided message if PassThru is true.
        if ($PassThru)
        {
            return $messages
        }
    }
}
