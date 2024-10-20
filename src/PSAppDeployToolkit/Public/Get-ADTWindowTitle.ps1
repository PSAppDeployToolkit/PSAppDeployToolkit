#-----------------------------------------------------------------------------
#
# MARK: Get-ADTWindowTitle
#
#-----------------------------------------------------------------------------

function Get-ADTWindowTitle
{
    <#
    .SYNOPSIS
        Search for an open window title and return details about the window.

    .DESCRIPTION
        Search for a window title. If window title searched for returns more than one result, then details for each window will be displayed.

        Returns the following properties for each window:
        - WindowTitle
        - WindowHandle
        - ParentProcess
        - ParentProcessMainWindowHandle
        - ParentProcessId

        Function does not work in SYSTEM context unless launched with "psexec.exe -s -i" to run it as an interactive process under the SYSTEM account.

    .PARAMETER WindowTitle
        The title of the application window to search for using regex matching.

    .PARAMETER GetAllWindowTitles
        Get titles for all open windows on the system.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        PSADT.Types.WindowInfo

        Returns a PSADT.Types.WindowInfo object with the following properties:
        - WindowTitle
        - WindowHandle
        - ParentProcess
        - ParentProcessMainWindowHandle
        - ParentProcessId

    .EXAMPLE
        Get-ADTWindowTitle -WindowTitle 'Microsoft Word'

        Gets details for each window that has the words "Microsoft Word" in the title.

    .EXAMPLE
        Get-ADTWindowTitle -GetAllWindowTitles

        Gets details for all windows with a title.

    .EXAMPLE
        Get-ADTWindowTitle -GetAllWindowTitles | Where-Object { $_.ParentProcess -eq 'WINWORD' }

        Get details for all windows belonging to Microsoft Word process with name "WINWORD".

    .NOTES
        An active ADT session is NOT required to use this function.

        Function does not work in SYSTEM context unless launched with "psexec.exe -s -i" to run it as an interactive process under the SYSTEM account.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding()]
    [OutputType([PSADT.Types.WindowInfo])]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'SearchWinTitle')]
        [AllowEmptyString()]
        [System.String]$WindowTitle,

        [Parameter(Mandatory = $true, ParameterSetName = 'GetAllWinTitles')]
        [System.Management.Automation.SwitchParameter]$GetAllWindowTitles
    )

    begin
    {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue
    }

    process
    {
        if ($GetAllWindowTitles)
        {
            Write-ADTLogEntry -Message 'Finding all open window title(s).'
        }
        else
        {
            Write-ADTLogEntry -Message "Finding open window title(s) [$WindowTitle] using regex matching."
        }

        try
        {
            try
            {
                # Cache all running processes.
                $processes = [System.Diagnostics.Process]::GetProcesses()

                # Get all window handles for visible windows and loop through the visible ones.
                [PSADT.GUI.UiAutomation]::EnumWindows() | & {
                    process
                    {
                        # Return early if we're null.
                        if ($null -eq $_)
                        {
                            return
                        }

                        # Return early if window isn't visible.
                        if (![PSADT.PInvoke.NativeMethods]::IsWindowVisible($_))
                        {
                            return
                        }

                        # Return early if the window doesn't have any text.
                        if (!($VisibleWindowTitle = [PSADT.GUI.UiAutomation]::GetWindowText($_)))
                        {
                            return
                        }

                        # Return early if the window title doesn't match the search criteria.
                        if (!$GetAllWindowTitles -or ($VisibleWindowTitle -notmatch $WindowTitle))
                        {
                            return
                        }

                        # Return early if the window doesn't have an associated process.
                        if (!($process = $processes | Where-Object -Property Id -EQ -Value ([PSADT.PInvoke.NativeMethods]::GetWindowThreadProcessId($_)) | Select-Object -First 1))
                        {
                            return
                        }

                        # Build custom object with details about the window and the process.
                        return [PSADT.Types.WindowInfo]::new(
                            $VisibleWindowTitle,
                            $_,
                            $Process.ProcessName,
                            $Process.MainWindowHandle,
                            $Process.Id
                        )
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
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to get requested window title(s)."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
