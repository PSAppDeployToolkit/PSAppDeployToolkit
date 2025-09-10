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

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        PSADT.WindowManagement.WindowInfo

        Returns a PSADT.WindowManagement.WindowInfo object with the following properties:

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

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$WindowTitle,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.IntPtr[]]$WindowHandle,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$ParentProcess
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        # Bypass if no one's logged onto the device.
        if (!($runAsActiveUser = Get-ADTClientServerUser -AllowSystemFallback))
        {
            Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) as there is no active user logged onto the system."
            return
        }

        # Announce commencement.
        if ($WindowTitle -or $WindowHandle -or $ParentProcess)
        {
            Write-ADTLogEntry -Message "Finding open windows matching the specified criteria."
        }
        else
        {
            Write-ADTLogEntry -Message 'Finding all open window title(s).'
        }

        # Send this to our C# backend to get it done.
        try
        {
            try
            {
                return Invoke-ADTClientServerOperation -GetProcessWindowInfo -User $runAsActiveUser -Options ([PSADT.WindowManagement.WindowInfoOptions]::new($WindowTitle, $WindowHandle, $ParentProcess))
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
