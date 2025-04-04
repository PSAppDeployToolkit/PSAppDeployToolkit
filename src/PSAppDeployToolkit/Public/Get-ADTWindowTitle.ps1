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
        One or more titles of the application window to search for using regex matching.

    .PARAMETER WindowHandle
        One or more window handles of the application window to search for.

    .PARAMETER ParentProcess
        One or more process names of the application window to search for.

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

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTWindowTitle
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'WindowTitle', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'WindowHandle', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'ParentProcess', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'GetAllWindowTitles', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [CmdletBinding()]
    [OutputType([PSADT.Types.WindowInfo])]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'SearchWinTitle')]
        [AllowEmptyString()]
        [System.String[]]$WindowTitle,

        [Parameter(Mandatory = $true, ParameterSetName = 'SearchWinHandle')]
        [AllowEmptyString()]
        [System.IntPtr[]]$WindowHandle,

        [Parameter(Mandatory = $true, ParameterSetName = 'SearchParentProcess')]
        [AllowEmptyString()]
        [System.String[]]$ParentProcess,

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
        # Announce commencement.
        switch ($PSCmdlet.ParameterSetName)
        {
            GetAllWinTitles
            {
                Write-ADTLogEntry -Message 'Finding all open window title(s).'
                break
            }
            SearchWinTitle
            {
                Write-ADTLogEntry -Message "Finding open windows matching the specified title(s)."
                break
            }
            SearchWinHandle
            {
                Write-ADTLogEntry -Message "Finding open windows matching the specified handle(s)."
                break
            }
            SearchWinHandle
            {
                Write-ADTLogEntry -Message "Finding open windows matching the specified parent process(es)."
                break
            }
        }

        try
        {
            try
            {
                # Cache all running processes.
                $processes = [System.Diagnostics.Process]::GetProcesses() | & {
                    process
                    {
                        if ($WindowHandle -and ($_.MainWindowHandle -notin $WindowHandle))
                        {
                            return
                        }
                        if ($ParentProcess -and ($_.ProcessName -notin $ParentProcess))
                        {
                            return
                        }
                        return $_
                    }
                }

                # Get all window handles for visible windows and loop through the visible ones.
                [PSADT.Utilities.WindowUtilities]::EnumWindows() | & {
                    process
                    {
                        # Return early if we're null.
                        if ($null -eq $_)
                        {
                            return
                        }

                        # Return early if window isn't visible.
                        if (![PSADT.LibraryInterfaces.User32]::IsWindowVisible($_))
                        {
                            return
                        }

                        # Return early if the window doesn't have any text.
                        if (!($VisibleWindowTitle = [PSADT.Utilities.WindowUtilities]::GetWindowText($_)))
                        {
                            return
                        }

                        # Return early if the visible window title doesn't match our filter.
                        if ($WindowTitle -and ($VisibleWindowTitle -notmatch "($([System.String]::Join('|', $WindowTitle)))"))
                        {
                            return
                        }

                        # Return early if the window doesn't have an associated process.
                        if (!($process = $processes | Where-Object -Property Id -EQ -Value ([PSADT.Utilities.WindowUtilities]::GetWindowThreadProcessId($_)) | Select-Object -First 1))
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
