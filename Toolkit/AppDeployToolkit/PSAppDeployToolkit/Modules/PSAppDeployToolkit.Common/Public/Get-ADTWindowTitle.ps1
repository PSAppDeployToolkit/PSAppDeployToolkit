function Get-ADTWindowTitle
{
    <#

    .SYNOPSIS
    Search for an open window title and return details about the window.

    .DESCRIPTION
    Search for a window title. If window title searched for returns more than one result, then details for each window will be displayed.

    Returns the following properties for each window: WindowTitle, WindowHandle, ParentProcess, ParentProcessMainWindowHandle, ParentProcessId.

    Function does not work in SYSTEM context unless launched with "psexec.exe -s -i" to run it as an interactive process under the SYSTEM account.

    .PARAMETER WindowTitle
    The title of the application window to search for using regex matching.

    .PARAMETER GetAllWindowTitles
    Get titles for all open windows on the system.

    .PARAMETER DisableFunctionLogging
    Disables logging messages to the script log file.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    PSADT.Types.WindowInfo. Returns a PSADT.Types.WindowInfo object with the following properties: WindowTitle, WindowHandle, ParentProcess, ParentProcessMainWindowHandle, ParentProcessId.

    .EXAMPLE
    # Gets details for each window that has the words "Microsoft Word" in the title.
    Get-ADTWindowTitle -WindowTitle 'Microsoft Word'

    .EXAMPLE

    # Gets details for all windows with a title.
    Get-ADTWindowTitle -GetAllWindowTitles

    .EXAMPLE

    # Get details for all windows belonging to Microsoft Word process with name "WINWORD".
    Get-ADTWindowTitle -GetAllWindowTitles | Where-Object { $_.ParentProcess -eq 'WINWORD' }

    .NOTES
    This function can be called without an active ADT session.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'SearchWinTitle')]
        [AllowEmptyString()]
        [System.String]$WindowTitle,

        [Parameter(Mandatory = $true, ParameterSetName = 'GetAllWinTitles')]
        [System.Management.Automation.SwitchParameter]$GetAllWindowTitles,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$DisableFunctionLogging
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        if ($PSCmdlet.ParameterSetName -eq 'SearchWinTitle')
        {
            Write-ADTLogEntry -Message "Finding open window title(s) [$WindowTitle] using regex matching." -DebugMessage:$DisableFunctionLogging
        }
        elseif ($PSCmdlet.ParameterSetName -eq 'GetAllWinTitles')
        {
            Write-ADTLogEntry -Message 'Finding all open window title(s).' -DebugMessage:$DisableFunctionLogging
        }

        try
        {
            try
            {
                # Cache all running processes.
                $processes = [System.Diagnostics.Process]::GetProcesses()

                # Get all window handles for visible windows and loop through the visible ones.
                foreach ($VisibleWindowHandle in [PSADT.UiAutomation]::EnumWindows().Where({$_ -and [PSADT.UiAutomation]::IsWindowVisible($_)}))
                {
                    # Only process handles with window text and an associated running process, and only save/return the window and process details which match the search criteria.
                    if (($VisibleWindowTitle = [PSADT.UiAutomation]::GetWindowText($VisibleWindowHandle)) -and ($process = $processes.Where({$_.Id -eq [PSADT.UiAutomation]::GetWindowThreadProcessId($VisibleWindowHandle)})) -and ($PSCmdlet.ParameterSetName.Equals('SearchWinTitle') -and ($VisibleWindowTitle -notmatch $WindowTitle)))
                    {
                        # Build custom object with details about the window and the process.
                        [PSADT.Types.WindowInfo]@{
                            WindowTitle = $VisibleWindowTitle
                            WindowHandle = $VisibleWindowHandle
                            ParentProcess = $Process.ProcessName
                            ParentProcessMainWindowHandle = $Process.MainWindowHandle
                            ParentProcessId = $Process.Id
                        }
                    }
                }
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Write-ADTLogEntry -Message "Failed to get requested window title(s).`n$(Resolve-ADTErrorRecord -ErrorRecord $_)" -Severity 3 -DebugMessage:$DisableFunctionLogging
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
