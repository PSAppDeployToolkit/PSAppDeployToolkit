#-----------------------------------------------------------------------------
#
# MARK: Show-ADTInstallationWelcome
#
#-----------------------------------------------------------------------------

function Show-ADTInstallationWelcome
{
    <#
    .SYNOPSIS
        Show a welcome dialog prompting the user with information about the deployment and actions to be performed before the deployment can begin.

    .DESCRIPTION
        The following prompts can be included in the welcome dialog:

        * Close the specified running applications, or optionally close the applications without showing a prompt (using the `-Silent` switch).
        * Defer the deployment a certain number of times, for a certain number of days or until a deadline is reached.
        * Countdown until applications are automatically closed.
        * Prevent users from launching the specified applications while the deployment is in progress.

    .PARAMETER CloseProcesses
        Name of the process to stop (do not include the .exe). Specify multiple processes separated by a comma. Specify custom descriptions like this: `@{ Name = 'winword'; Description = 'Microsoft Office Word' }, @{ Name = 'excel'; Description = 'Microsoft Office Excel' }`

    .PARAMETER HideCloseButton
        Specifies that the 'Close Processes' button be hidden/disabled to force users to manually close down their running processes.

    .PARAMETER AllowDefer
        Enables an optional defer button to allow the user to defer the deployment.

    .PARAMETER AllowDeferCloseProcesses
        Enables an optional defer button to allow the user to defer the deployment only if there are running applications that need to be closed. This parameter automatically enables `-AllowDefer`.

    .PARAMETER Silent
        Stop processes without prompting the user.

    .PARAMETER CloseProcessesCountdown
        Option to provide a countdown in seconds until the specified applications are automatically closed. This only takes effect if deferral is not allowed or has expired.

    .PARAMETER ForceCloseProcessesCountdown
        Option to provide a countdown in seconds until the specified applications are automatically closed regardless of whether deferral is allowed.

    .PARAMETER ForceCountdown
        Specify a countdown to display before automatically proceeding with the deployment when a deferral is enabled.

    .PARAMETER DeferTimes
        Specify the number of times the deployment can be deferred.

    .PARAMETER DeferDays
        Specify the number of days since first run that the deployment can be deferred. This is converted to a deadline.

    .PARAMETER DeferDeadline
        Specify the deadline date until which the deployment can be deferred.

        Specify the date in the local culture if the script is intended for that same culture.

        If the script is intended to run on en-US machines, specify the date in the format: `08/25/2013`, or `08-25-2013`, or `08-25-2013 18:00:00`.

        If the script is intended for multiple cultures, specify the date in the universal sortable date/time format: `2013-08-22 11:51:52Z`.

        The deadline date will be displayed to the user in the format of their culture.

    .PARAMETER DeferRunInterval
        Specifies the time span that must elapse before prompting the user again if a process listed in 'CloseProcesses' is still running after a deferral.

        This addresses the issue where Intune retries deployments shortly after a user defers, preventing multiple immediate prompts and improving the user experience.

        Example:
        - To specify 30 minutes, use: `([System.TimeSpan]::FromMinutes(30))`.
        - To specify 24 hours, use: `([System.TimeSpan]::FromHours(24))`.

    .PARAMETER WindowLocation
        The location of the dialog on the screen.

    .PARAMETER BlockExecution
        Option to prevent the user from launching processes/applications, specified in -CloseProcesses, during the deployment.

    .PARAMETER PromptToSave
        Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button. Option does not work in SYSTEM context unless toolkit launched with "psexec.exe -s -i" to run it as an interactive process under the SYSTEM account.

    .PARAMETER PersistPrompt
        Specify whether to make the Show-ADTInstallationWelcome prompt persist in the center of the screen every couple of seconds, specified in the config.psd1. The user will have no option but to respond to the prompt. This only takes effect if deferral is not allowed or has expired.

    .PARAMETER MinimizeWindows
        Specifies whether to minimize other windows when displaying prompt.

    .PARAMETER NotTopMost
        Specifies whether the windows is the topmost window.

    .PARAMETER AllowMove
        Specifies that the user can move the dialog on the screen.

    .PARAMETER CustomText
        Specify whether to display a custom message as specified in the `strings.psd1` file below the main preamble. Custom message must be populated for each language section in the `strings.psd1` file.

    .PARAMETER CheckDiskSpace
        Specify whether to check if there is enough disk space for the deployment to proceed.

        If this parameter is specified without the RequiredDiskSpace parameter, the required disk space is calculated automatically based on the size of the script source and associated files.

    .PARAMETER RequiredDiskSpace
        Specify required disk space in MB, used in combination with CheckDiskSpace.

    .PARAMETER PassThru
        Returns the user's prompt choice to the caller for further decision making.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any output.

    .EXAMPLE
        Show-ADTInstallationWelcome -CloseProcesses iexplore, winword, excel

        Prompt the user to close Internet Explorer, Word and Excel.

    .EXAMPLE
        Show-ADTInstallationWelcome -CloseProcesses @{ Name = 'winword' }, @{ Name = 'excel' } -Silent

        Close Word and Excel without prompting the user.

    .EXAMPLE
        Show-ADTInstallationWelcome -CloseProcesses @{ Name = 'winword' }, @{ Name = 'excel' } -BlockExecution

        Close Word and Excel and prevent the user from launching the applications while the deployment is in progress.

    .EXAMPLE
        Show-ADTInstallationWelcome -CloseProcesses @{ Name = 'winword'; Description = 'Microsoft Office Word' }, @{ Name = 'excel'; Description = 'Microsoft Office Excel' } -CloseProcessesCountdown 600

        Prompt the user to close Word and Excel, with customized descriptions for the applications and automatically close the applications after 10 minutes.

    .EXAMPLE
        Show-ADTInstallationWelcome -CloseProcesses @{ Name = 'winword' }, @{ Name = 'msaccess' }, @{ Name = 'excel' } -PersistPrompt

        Prompt the user to close Word, MSAccess and Excel. By using the PersistPrompt switch, the dialog will return to the center of the screen every couple of seconds, specified in the config.psd1, so the user cannot ignore it by dragging it aside.

    .EXAMPLE
        Show-ADTInstallationWelcome -AllowDefer -DeferDeadline '2013-08-25'

        Allow the user to defer the deployment until the deadline is reached.

    .EXAMPLE
        Show-ADTInstallationWelcome -CloseProcesses @{ Name = 'winword' }, @{ Name = 'excel' } -BlockExecution -AllowDefer -DeferTimes 10 -DeferDeadline '2013-08-25' -CloseProcessesCountdown 600

        Close Word and Excel and prevent the user from launching the applications while the deployment is in progress.

        Allow the user to defer the deployment a maximum of 10 times or until the deadline is reached, whichever happens first. When deferral expires, prompt the user to close the applications and automatically close them after 10 minutes.

    .NOTES
        An active ADT session is NOT required to use this function.

        The process descriptions are retrieved via Get-Process, with a fall back on the process name if no description is available. Alternatively, you can specify the description yourself with a '=' symbol - see examples.

        The dialog box will timeout after the timeout specified in the config.psd1 file (default 55 minutes) to prevent Intune/SCCM deployments from timing out and returning a failure code. When the dialog times out, the script will exit and return a 1618 code (SCCM fast retry code).

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Show-ADTInstallationWelcome
    #>

    [CmdletBinding(DefaultParameterSetName = 'Interactive, with no modifying options.')]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, and with processes to close.', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, and a free disk space check.', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, and with a close processes countdown if the user has no available deferrals.', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed irrespective of whether processes to close are open.', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and a free disk space check.', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed only if the processes to close are open.', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and a free disk space check.', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'Silent, and with processes to close.', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'Silent, with processes to close, and a free disk space check.', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [ValidateNotNullOrEmpty()]
        [PSADT.ProcessManagement.ProcessDefinition[]]$CloseProcesses,

        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with processes to close.', HelpMessage = "Specifies that the 'Close Processes' button be hidden/disabled to force users to manually close down their running processes.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and a free disk space check.', HelpMessage = "Specifies that the 'Close Processes' button be hidden/disabled to force users to manually close down their running processes.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with a close processes countdown if the user has no available deferrals.', HelpMessage = "Specifies that the 'Close Processes' button be hidden/disabled to force users to manually close down their running processes.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = "Specifies that the 'Close Processes' button be hidden/disabled to force users to manually close down their running processes.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = "Specifies that the 'Close Processes' button be hidden/disabled to force users to manually close down their running processes.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = "Specifies that the 'Close Processes' button be hidden/disabled to force users to manually close down their running processes.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed irrespective of whether processes to close are open.', HelpMessage = "Specifies that the 'Close Processes' button be hidden/disabled to force users to manually close down their running processes.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and a free disk space check.', HelpMessage = "Specifies that the 'Close Processes' button be hidden/disabled to force users to manually close down their running processes.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = "Specifies that the 'Close Processes' button be hidden/disabled to force users to manually close down their running processes.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = "Specifies that the 'Close Processes' button be hidden/disabled to force users to manually close down their running processes.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = "Specifies that the 'Close Processes' button be hidden/disabled to force users to manually close down their running processes.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = "Specifies that the 'Close Processes' button be hidden/disabled to force users to manually close down their running processes.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = "Specifies that the 'Close Processes' button be hidden/disabled to force users to manually close down their running processes.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = "Specifies that the 'Close Processes' button be hidden/disabled to force users to manually close down their running processes.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed only if the processes to close are open.', HelpMessage = "Specifies that the 'Close Processes' button be hidden/disabled to force users to manually close down their running processes.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and a free disk space check.', HelpMessage = "Specifies that the 'Close Processes' button be hidden/disabled to force users to manually close down their running processes.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = "Specifies that the 'Close Processes' button be hidden/disabled to force users to manually close down their running processes.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = "Specifies that the 'Close Processes' button be hidden/disabled to force users to manually close down their running processes.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = "Specifies that the 'Close Processes' button be hidden/disabled to force users to manually close down their running processes.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = "Specifies that the 'Close Processes' button be hidden/disabled to force users to manually close down their running processes.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = "Specifies that the 'Close Processes' button be hidden/disabled to force users to manually close down their running processes.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = "Specifies that the 'Close Processes' button be hidden/disabled to force users to manually close down their running processes.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Silent, and with processes to close.', HelpMessage = "Specifies that the 'Close Processes' button be hidden/disabled to force users to manually close down their running processes.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Silent, with processes to close, and a free disk space check.', HelpMessage = "Specifies that the 'Close Processes' button be hidden/disabled to force users to manually close down their running processes.")]
        [System.Management.Automation.SwitchParameter]$HideCloseButton,

        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, and with deferral allowed.', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with deferral allowed, and a free disk space check.', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with deferral allowed, and with a continue countdown irrespective of deferrals.', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with deferral allowed, with a continue countdown irrespective of deferrals, and a free disk space check.', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed irrespective of whether processes to close are open.', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and a free disk space check.', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box.')]
        [System.Management.Automation.SwitchParameter]$AllowDefer,

        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed only if the processes to close are open.', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box only if an app needs to be closed.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and a free disk space check.', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box only if an app needs to be closed.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box only if an app needs to be closed.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box only if an app needs to be closed.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box only if an app needs to be closed.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box only if an app needs to be closed.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box only if an app needs to be closed.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box only if an app needs to be closed.')]
        [System.Management.Automation.SwitchParameter]$AllowDeferCloseProcesses,

        [Parameter(Mandatory = $true, ParameterSetName = 'Silent, with no modifying options.', HelpMessage = 'Specify whether to prompt user or force close the applications.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Silent, and with a free disk space check.', HelpMessage = 'Specify whether to prompt user or force close the applications.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Silent, and with processes to close.', HelpMessage = 'Specify whether to prompt user or force close the applications.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Silent, with processes to close, and a free disk space check.', HelpMessage = 'Specify whether to prompt user or force close the applications.')]
        [System.Management.Automation.SwitchParameter]$Silent,

        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, and with a close processes countdown if the user has no available deferrals.', HelpMessage = 'Specify a countdown to display before automatically closing applications where deferral is not allowed or has expired.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specify a countdown to display before automatically closing applications where deferral is not allowed or has expired.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = 'Specify a countdown to display before automatically closing applications where deferral is not allowed or has expired.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specify a countdown to display before automatically closing applications where deferral is not allowed or has expired.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = 'Specify a countdown to display before automatically closing applications where deferral is not allowed or has expired.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specify a countdown to display before automatically closing applications where deferral is not allowed or has expired.')]
        [ValidateNotNullOrEmpty()]
        [System.Nullable[System.UInt32]]$CloseProcessesCountdown,

        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = 'Specify a countdown to display before automatically closing applications whether or not deferral is allowed.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specify a countdown to display before automatically closing applications whether or not deferral is allowed.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = 'Specify a countdown to display before automatically closing applications whether or not deferral is allowed.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specify a countdown to display before automatically closing applications whether or not deferral is allowed.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = 'Specify a countdown to display before automatically closing applications whether or not deferral is allowed.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specify a countdown to display before automatically closing applications whether or not deferral is allowed.')]
        [ValidateNotNullOrEmpty()]
        [System.Nullable[System.UInt32]]$ForceCloseProcessesCountdown,

        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with deferral allowed, and with a continue countdown irrespective of deferrals.', HelpMessage = 'Specify a countdown to display before automatically proceeding with the deployment when a deferral is enabled.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with deferral allowed, with a continue countdown irrespective of deferrals, and a free disk space check.', HelpMessage = 'Specify a countdown to display before automatically proceeding with the deployment when a deferral is enabled.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = 'Specify a countdown to display before automatically proceeding with the deployment when a deferral is enabled.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = 'Specify a countdown to display before automatically proceeding with the deployment when a deferral is enabled.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = 'Specify a countdown to display before automatically proceeding with the deployment when a deferral is enabled.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = 'Specify a countdown to display before automatically proceeding with the deployment when a deferral is enabled.')]
        [ValidateNotNullOrEmpty()]
        [System.Nullable[System.UInt32]]$ForceCountdown,

        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with deferral allowed.', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, and a free disk space check.', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, and with a continue countdown irrespective of deferrals.', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, with a continue countdown irrespective of deferrals, and a free disk space check.', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed irrespective of whether processes to close are open.', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and a free disk space check.', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed only if the processes to close are open.', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and a free disk space check.', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [ValidateNotNullOrEmpty()]
        [System.Nullable[System.UInt32]]$DeferTimes,

        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with deferral allowed.', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, and a free disk space check.', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, and with a continue countdown irrespective of deferrals.', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, with a continue countdown irrespective of deferrals, and a free disk space check.', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed irrespective of whether processes to close are open.', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and a free disk space check.', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed only if the processes to close are open.', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and a free disk space check.', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [ValidateScript({
                if ($null -eq $_)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName DeferDays -ProvidedValue $_ -ExceptionMessage 'The specified DeferDays interval was null.'))
                }
                if ($_ -le 0)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName DeferDays -ProvidedValue $_ -ExceptionMessage 'The specified DeferDays interval must be greater than zero.'))
                }
                return !!$_
            })]
        [System.Nullable[System.Double]]$DeferDays,

        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with deferral allowed.', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, and a free disk space check.', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, and with a continue countdown irrespective of deferrals.', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, with a continue countdown irrespective of deferrals, and a free disk space check.', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed irrespective of whether processes to close are open.', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and a free disk space check.', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed only if the processes to close are open.', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and a free disk space check.', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [ValidateNotNullOrEmpty()]
        [System.DateTime]$DeferDeadline,

        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with deferral allowed.', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, and a free disk space check.', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, and with a continue countdown irrespective of deferrals.', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, with a continue countdown irrespective of deferrals, and a free disk space check.', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed irrespective of whether processes to close are open.', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and a free disk space check.', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed only if the processes to close are open.', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and a free disk space check.', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [ValidateNotNullOrEmpty()]
        [System.TimeSpan]$DeferRunInterval,

        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with no modifying options.', HelpMessage = 'The location of the dialog on the screen.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with a free disk space check.', HelpMessage = 'The location of the dialog on the screen.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with processes to close.', HelpMessage = 'The location of the dialog on the screen.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and a free disk space check.', HelpMessage = 'The location of the dialog on the screen.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with deferral allowed.', HelpMessage = 'The location of the dialog on the screen.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, and a free disk space check.', HelpMessage = 'The location of the dialog on the screen.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, and with a continue countdown irrespective of deferrals.', HelpMessage = 'The location of the dialog on the screen.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, with a continue countdown irrespective of deferrals, and a free disk space check.', HelpMessage = 'The location of the dialog on the screen.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with a close processes countdown if the user has no available deferrals.', HelpMessage = 'The location of the dialog on the screen.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'The location of the dialog on the screen.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = 'The location of the dialog on the screen.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'The location of the dialog on the screen.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed irrespective of whether processes to close are open.', HelpMessage = 'The location of the dialog on the screen.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and a free disk space check.', HelpMessage = 'The location of the dialog on the screen.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = 'The location of the dialog on the screen.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = 'The location of the dialog on the screen.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = 'The location of the dialog on the screen.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'The location of the dialog on the screen.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = 'The location of the dialog on the screen.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'The location of the dialog on the screen.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed only if the processes to close are open.', HelpMessage = 'The location of the dialog on the screen.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and a free disk space check.', HelpMessage = 'The location of the dialog on the screen.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = 'The location of the dialog on the screen.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = 'The location of the dialog on the screen.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = 'The location of the dialog on the screen.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'The location of the dialog on the screen.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = 'The location of the dialog on the screen.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'The location of the dialog on the screen.')]
        [ValidateNotNullOrEmpty()]
        [PSADT.UserInterface.Dialogs.DialogPosition]$WindowLocation,

        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with processes to close.', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and a free disk space check.', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with a close processes countdown if the user has no available deferrals.', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed irrespective of whether processes to close are open.', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and a free disk space check.', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed only if the processes to close are open.', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and a free disk space check.', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Silent, and with processes to close.', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Silent, with processes to close, and a free disk space check.', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [System.Management.Automation.SwitchParameter]$BlockExecution,

        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with processes to close.', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and a free disk space check.', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with a close processes countdown if the user has no available deferrals.', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed irrespective of whether processes to close are open.', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and a free disk space check.', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed only if the processes to close are open.', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and a free disk space check.', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [System.Management.Automation.SwitchParameter]$PromptToSave,

        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with no modifying options.', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with a free disk space check.', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with processes to close.', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and a free disk space check.', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with deferral allowed.', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, and a free disk space check.', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, and with a continue countdown irrespective of deferrals.', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, with a continue countdown irrespective of deferrals, and a free disk space check.', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with a close processes countdown if the user has no available deferrals.', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed irrespective of whether processes to close are open.', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and a free disk space check.', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed only if the processes to close are open.', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and a free disk space check.', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [System.Management.Automation.SwitchParameter]$PersistPrompt,

        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with no modifying options.', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with a free disk space check.', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with processes to close.', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and a free disk space check.', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with deferral allowed.', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, and a free disk space check.', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, and with a continue countdown irrespective of deferrals.', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, with a continue countdown irrespective of deferrals, and a free disk space check.', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with a close processes countdown if the user has no available deferrals.', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed irrespective of whether processes to close are open.', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and a free disk space check.', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed only if the processes to close are open.', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and a free disk space check.', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [System.Management.Automation.SwitchParameter]$MinimizeWindows,

        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with no modifying options.', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with a free disk space check.', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with processes to close.', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and a free disk space check.', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with deferral allowed.', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, and a free disk space check.', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, and with a continue countdown irrespective of deferrals.', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, with a continue countdown irrespective of deferrals, and a free disk space check.', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with a close processes countdown if the user has no available deferrals.', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed irrespective of whether processes to close are open.', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and a free disk space check.', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed only if the processes to close are open.', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and a free disk space check.', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [System.Management.Automation.SwitchParameter]$NotTopMost,

        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with no modifying options.', HelpMessage = "Specifies that the user is allowed to move the dialog on the screen.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with a free disk space check.', HelpMessage = "Specifies that the user is allowed to move the dialog on the screen.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with processes to close.', HelpMessage = "Specifies that the user is allowed to move the dialog on the screen.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and a free disk space check.', HelpMessage = "Specifies that the user is allowed to move the dialog on the screen.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with deferral allowed.', HelpMessage = "Specifies that the user is allowed to move the dialog on the screen.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, and a free disk space check.', HelpMessage = "Specifies that the user is allowed to move the dialog on the screen.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, and with a continue countdown irrespective of deferrals.', HelpMessage = "Specifies that the user is allowed to move the dialog on the screen.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, with a continue countdown irrespective of deferrals, and a free disk space check.', HelpMessage = "Specifies that the user is allowed to move the dialog on the screen.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with a close processes countdown if the user has no available deferrals.', HelpMessage = "Specifies that the user is allowed to move the dialog on the screen.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = "Specifies that the user is allowed to move the dialog on the screen.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = "Specifies that the user is allowed to move the dialog on the screen.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = "Specifies that the user is allowed to move the dialog on the screen.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed irrespective of whether processes to close are open.', HelpMessage = "Specifies that the user is allowed to move the dialog on the screen.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and a free disk space check.', HelpMessage = "Specifies that the user is allowed to move the dialog on the screen.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = "Specifies that the user is allowed to move the dialog on the screen.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = "Specifies that the user is allowed to move the dialog on the screen.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = "Specifies that the user is allowed to move the dialog on the screen.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = "Specifies that the user is allowed to move the dialog on the screen.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = "Specifies that the user is allowed to move the dialog on the screen.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = "Specifies that the user is allowed to move the dialog on the screen.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed only if the processes to close are open.', HelpMessage = "Specifies that the user is allowed to move the dialog on the screen.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and a free disk space check.', HelpMessage = "Specifies that the user is allowed to move the dialog on the screen.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = "Specifies that the user is allowed to move the dialog on the screen.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = "Specifies that the user is allowed to move the dialog on the screen.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = "Specifies that the user is allowed to move the dialog on the screen.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = "Specifies that the user is allowed to move the dialog on the screen.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = "Specifies that the user is allowed to move the dialog on the screen.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = "Specifies that the user is allowed to move the dialog on the screen.")]
        [System.Management.Automation.SwitchParameter]$AllowMove,

        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with no modifying options.', HelpMessage = "Specifies that the user is allowed to minimize the dialog.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with a free disk space check.', HelpMessage = "Specifies that the user is allowed to minimize the dialog.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with processes to close.', HelpMessage = "Specifies that the user is allowed to minimize the dialog.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and a free disk space check.', HelpMessage = "Specifies that the user is allowed to minimize the dialog.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with deferral allowed.', HelpMessage = "Specifies that the user is allowed to minimize the dialog.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, and a free disk space check.', HelpMessage = "Specifies that the user is allowed to minimize the dialog.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, and with a continue countdown irrespective of deferrals.', HelpMessage = "Specifies that the user is allowed to minimize the dialog.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, with a continue countdown irrespective of deferrals, and a free disk space check.', HelpMessage = "Specifies that the user is allowed to minimize the dialog.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with a close processes countdown if the user has no available deferrals.', HelpMessage = "Specifies that the user is allowed to minimize the dialog.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = "Specifies that the user is allowed to minimize the dialog.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = "Specifies that the user is allowed to minimize the dialog.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = "Specifies that the user is allowed to minimize the dialog.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed irrespective of whether processes to close are open.', HelpMessage = "Specifies that the user is allowed to minimize the dialog.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and a free disk space check.', HelpMessage = "Specifies that the user is allowed to minimize the dialog.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = "Specifies that the user is allowed to minimize the dialog.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = "Specifies that the user is allowed to minimize the dialog.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = "Specifies that the user is allowed to minimize the dialog.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = "Specifies that the user is allowed to minimize the dialog.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = "Specifies that the user is allowed to minimize the dialog.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = "Specifies that the user is allowed to minimize the dialog.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed only if the processes to close are open.', HelpMessage = "Specifies that the user is allowed to minimize the dialog.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and a free disk space check.', HelpMessage = "Specifies that the user is allowed to minimize the dialog.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = "Specifies that the user is allowed to minimize the dialog.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = "Specifies that the user is allowed to minimize the dialog.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = "Specifies that the user is allowed to minimize the dialog.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = "Specifies that the user is allowed to minimize the dialog.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = "Specifies that the user is allowed to minimize the dialog.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = "Specifies that the user is allowed to minimize the dialog.")]
        [System.Management.Automation.SwitchParameter]$AllowMinimize,

        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with no modifying options.', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with a free disk space check.', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with processes to close.', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and a free disk space check.', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with deferral allowed.', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, and a free disk space check.', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, and with a continue countdown irrespective of deferrals.', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, with a continue countdown irrespective of deferrals, and a free disk space check.', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with a close processes countdown if the user has no available deferrals.', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed irrespective of whether processes to close are open.', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and a free disk space check.', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed only if the processes to close are open.', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and a free disk space check.', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [System.Management.Automation.SwitchParameter]$CustomText,

        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, and with a free disk space check.', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, and a free disk space check.', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with deferral allowed, and a free disk space check.', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with deferral allowed, with a continue countdown irrespective of deferrals, and a free disk space check.', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and a free disk space check.', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and a free disk space check.', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Silent, and with a free disk space check.', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Silent, with processes to close, and a free disk space check.', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [System.Management.Automation.SwitchParameter]$CheckDiskSpace,

        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with a free disk space check.', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and a free disk space check.', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, and a free disk space check.', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, with a continue countdown irrespective of deferrals, and a free disk space check.', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and a free disk space check.', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and a free disk space check.', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Silent, and with a free disk space check.', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Silent, with processes to close, and a free disk space check.', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [ValidateNotNullOrEmpty()]
        [System.Nullable[System.UInt32]]$RequiredDiskSpace,

        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with no modifying options.', HelpMessage = "Returns the user's prompt choice to the caller for further decision making.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with a free disk space check.', HelpMessage = "Returns the user's prompt choice to the caller for further decision making.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with processes to close.', HelpMessage = "Returns the user's prompt choice to the caller for further decision making.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and a free disk space check.', HelpMessage = "Returns the user's prompt choice to the caller for further decision making.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, and with deferral allowed.', HelpMessage = "Returns the user's prompt choice to the caller for further decision making.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, and a free disk space check.', HelpMessage = "Returns the user's prompt choice to the caller for further decision making.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, and with a continue countdown irrespective of deferrals.', HelpMessage = "Returns the user's prompt choice to the caller for further decision making.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with deferral allowed, with a continue countdown irrespective of deferrals, and a free disk space check.', HelpMessage = "Returns the user's prompt choice to the caller for further decision making.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with a close processes countdown if the user has no available deferrals.', HelpMessage = "Returns the user's prompt choice to the caller for further decision making.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = "Returns the user's prompt choice to the caller for further decision making.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = "Returns the user's prompt choice to the caller for further decision making.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = "Returns the user's prompt choice to the caller for further decision making.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed irrespective of whether processes to close are open.', HelpMessage = "Returns the user's prompt choice to the caller for further decision making.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and a free disk space check.', HelpMessage = "Returns the user's prompt choice to the caller for further decision making.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = "Returns the user's prompt choice to the caller for further decision making.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = "Returns the user's prompt choice to the caller for further decision making.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = "Returns the user's prompt choice to the caller for further decision making.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = "Returns the user's prompt choice to the caller for further decision making.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = "Returns the user's prompt choice to the caller for further decision making.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed irrespective of whether processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = "Returns the user's prompt choice to the caller for further decision making.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, and with deferral allowed only if the processes to close are open.', HelpMessage = "Returns the user's prompt choice to the caller for further decision making.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and a free disk space check.', HelpMessage = "Returns the user's prompt choice to the caller for further decision making.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a continue/defer countdown depending on whether processes to close are open or not.', HelpMessage = "Returns the user's prompt choice to the caller for further decision making.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a continue/defer countdown depending on whether processes to close are open or not, and a free disk space check.', HelpMessage = "Returns the user's prompt choice to the caller for further decision making.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown if the user has no available deferrals.', HelpMessage = "Returns the user's prompt choice to the caller for further decision making.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown if the user has no available deferrals, and a free disk space check.', HelpMessage = "Returns the user's prompt choice to the caller for further decision making.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, and with a close processes countdown irrespective of whether the user can defer or not.', HelpMessage = "Returns the user's prompt choice to the caller for further decision making.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive, with processes to close, with deferral allowed only if the processes to close are open, with a close processes countdown irrespective of whether the user can defer or not, and a free disk space check.', HelpMessage = "Returns the user's prompt choice to the caller for further decision making.")]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    dynamicparam
    {
        # Initialize variables.
        $adtSession = Initialize-ADTModuleIfUnitialized -Cmdlet $PSCmdlet
        $adtStrings = Get-ADTStringTable
        $adtConfig = Get-ADTConfig

        # Define parameter dictionary for returning at the end.
        $paramDictionary = [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()

        # Add in parameters we need as mandatory when there's no active ADTSession.
        $paramDictionary.Add('Title', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'Title', [System.String], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = !$adtSession; HelpMessage = "Title of the prompt. Optionally used to override the active DeploymentSession's `InstallTitle` value." }
                    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
                )
            ))
        $paramDictionary.Add('Subtitle', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'Subtitle', [System.String], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = !$adtSession -and ($adtConfig.UI.DialogStyle -eq 'Fluent'); HelpMessage = "Subtitle of the prompt. Optionally used to override the subtitle defined in the `strings.psd1` file." }
                    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
                )
            ))

        # Return the populated dictionary.
        return $paramDictionary
    }

    begin
    {
        # Throw if we have duplicated process objects.
        if ($CloseProcesses -and !($CloseProcesses.Name | Sort-Object | Get-Unique | Measure-Object).Count.Equals($CloseProcesses.Count))
        {
            $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName CloseProcesses -ProvidedValue $CloseProcesses -ExceptionMessage 'The specified CloseProcesses array contains duplicate processes.'))
        }

        # Initialize function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $welcomeState = @{
            Initialized = $false
            Retries = 0
        }

        # Set up DeploymentType if not specified.
        $DeploymentType = if ($adtSession)
        {
            $adtSession.DeploymentType
        }
        else
        {
            [PSAppDeployToolkit.SessionManagement.DeploymentType]::Install
        }

        # Set up remainder if not specified.
        if (!$PSBoundParameters.ContainsKey('Title'))
        {
            $PSBoundParameters.Add('Title', $adtSession.InstallTitle)
        }
        if (!$PSBoundParameters.ContainsKey('Subtitle'))
        {
            $PSBoundParameters.Add('Subtitle', $adtStrings.CloseAppsPrompt.Fluent.Subtitle.($DeploymentType.ToString()))
        }

        # Instantiate new object to hold all data needed within this call.
        $currentDateTimeLocal = [System.DateTime]::Now
        $deferDeadlineDateTime = $null
        $promptResult = $null

        # Internal worker function to bring up the dialog.
        function Show-ADTWelcomePrompt
        {
            # Initialise the dialog's state if we haven't already done so.
            if (!$welcomeState.Initialized)
            {
                $welcomeState.Initialized = if ($CloseProcesses)
                {
                    Invoke-ADTClientServerOperation -InitCloseAppsDialog -User $runAsActiveUser -CloseProcesses $CloseProcesses
                }
                else
                {
                    Invoke-ADTClientServerOperation -InitCloseAppsDialog -User $runAsActiveUser
                }
            }

            # Minimize all other windows.
            if ($MinimizeWindows)
            {
                Invoke-ADTClientServerOperation -MinimizeAllWindows -User $runAsActiveUser
            }

            # Show the dialog and return the result.
            try
            {
                return Invoke-ADTClientServerOperation -ShowModalDialog -User $runAsActiveUser -DialogType CloseAppsDialog -DialogStyle $adtConfig.UI.DialogStyle -Options $dialogOptions
            }
            catch [System.ApplicationException]
            {
                if ($welcomeState.Retries -ge 3)
                {
                    throw
                }
                Write-ADTLogEntry -Message "The client/server process was terminated unexpectedly.`n$(Resolve-ADTErrorRecord -ErrorRecord $_)" -Severity Error
                Write-ADTLogEntry -Message "Retrying user client/server process again [$((++$welcomeState.Retries))/3] times..."
                $welcomeState.Initialized = $false
                return "TerminatedTryAgain"
            }
        }

        # Internal worker function for updating the deferral history.
        function Update-ADTDeferHistory
        {
            # Open a new hashtable for splatting onto `Set-ADTDeferHistory`.
            $sadhParams = @{}

            # Add all valid parameters.
            if (($DeferTimes -ge 0) -and !$dialogOptions.UnlimitedDeferrals)
            {
                $sadhParams.Add('DeferTimesRemaining', $DeferTimes)
            }
            if ($deferDeadlineDateTime)
            {
                $sadhParams.Add('DeferDeadline', $deferDeadlineDateTime)
            }
            if ($DeferRunInterval)
            {
                $sadhParams.Add('DeferRunInterval', $DeferRunInterval)
                $sadhParams.Add('DeferRunIntervalLastTime', $currentDateTimeLocal)
            }

            # Only call `Set-ADTDeferHistory` if there's values to update.
            if ($sadhParams.Count)
            {
                Set-ADTDeferHistory @sadhParams
            }
        }

        # Internal worker to get running processes, factoring in whether we're running as SYSTEM and the user can see the process or not.
        function Get-ADTRunningProcessesUserCanClose
        {
            param
            (
                [System.Management.Automation.ActionPreference]$InformationAction
            )

            # Return early if there's no processes to close.
            if (!$CloseProcesses -or !($runningApps = Get-ADTRunningProcesses -ProcessObjects $CloseProcesses @PSBoundParameters))
            {
                return
            }

            # If we're not running as SYSTEM, return the process list.
            if (![PSADT.AccountManagement.AccountUtilities]::CallerIsLocalSystem -or $runAsActiveUser.IsLocalAdmin)
            {
                return $runningApps
            }

            # Filter the running apps list based on the process's username.
            return $runningApps | & {
                process
                {
                    if ($_.Username -eq $runAsActiveUser.NTAccount)
                    {
                        return $_
                    }
                }
            }
        }
    }

    process
    {
        try
        {
            try
            {
                # If running in NonInteractive mode, force the processes to close silently.
                if (!$PSBoundParameters.ContainsKey('Silent') -and $adtSession -and ($adtSession.IsNonInteractive() -or $adtSession.IsSilent()))
                {
                    Write-ADTLogEntry -Message "Running $($MyInvocation.MyCommand.Name) silently as the current deployment is NonInteractive or Silent."
                    $Silent = $true
                }

                # Bypass if no one's logged on to answer the dialog.
                if (!$Silent -and !($runAsActiveUser = Get-ADTClientServerUser -AllowSystemFallback))
                {
                    Write-ADTLogEntry -Message "Running $($MyInvocation.MyCommand.Name) silently as there is no active user logged onto the system."
                    $Silent = $true
                }

                # Check disk space requirements if specified
                if ($adtSession -and $CheckDiskSpace -and ($scriptDir = try { Get-ADTSessionCacheScriptDirectory } catch { $null = $null }))
                {
                    Write-ADTLogEntry -Message 'Evaluating disk space requirements.'
                    if (!$PSBoundParameters.ContainsKey('RequiredDiskSpace'))
                    {
                        try
                        {
                            # Determine the size of the Files folder
                            $fso = New-Object -ComObject Scripting.FileSystemObject
                            $RequiredDiskSpace = [System.Math]::Round($fso.GetFolder($scriptDir).Size / 1MB)
                        }
                        catch
                        {
                            Write-ADTLogEntry -Message "Failed to calculate disk space requirement from source files.`n$(Resolve-ADTErrorRecord -ErrorRecord $_)" -Severity 3
                        }
                        finally
                        {
                            $null = try
                            {
                                [System.Runtime.InteropServices.Marshal]::ReleaseComObject($fso)
                            }
                            catch
                            {
                                $null
                            }
                        }
                    }
                    if (($freeDiskSpace = Get-ADTFreeDiskSpace) -lt $RequiredDiskSpace)
                    {
                        Write-ADTLogEntry -Message "Failed to meet minimum disk space requirement. Space Required [$RequiredDiskSpace MB], Space Available [$freeDiskSpace MB]." -Severity 3
                        if (!$Silent)
                        {
                            Show-ADTInstallationPrompt -Message ([System.String]::Format($adtStrings.DiskSpaceText.Message.($DeploymentType.ToString()), $PSBoundParameters.Title, $RequiredDiskSpace, $freeDiskSpace)) -ButtonLeftText OK -Icon Error
                        }
                        Close-ADTSession -ExitCode $adtConfig.UI.DefaultExitCode
                    }
                    Write-ADTLogEntry -Message 'Successfully passed minimum disk space requirement check.'
                }

                # Prompt the user to close running applications and optionally defer if enabled.
                if (!$Silent)
                {
                    # Check Deferral history and calculate remaining deferrals.
                    if ($AllowDefer -or $AllowDeferCloseProcesses)
                    {
                        # Set $AllowDefer to true if $AllowDeferCloseProcesses is true.
                        $AllowDefer = $true

                        # Get the deferral history from the registry.
                        $deferHistory = if ($adtSession) { Get-ADTDeferHistory }
                        $deferHistoryTimes = $deferHistory | Select-Object -ExpandProperty DeferTimesRemaining -ErrorAction Ignore
                        $deferHistoryDeadline = $deferHistory | Select-Object -ExpandProperty DeferDeadline -ErrorAction Ignore
                        $deferHistoryRunIntervalLastTime = $deferHistory | Select-Object -ExpandProperty DeferRunIntervalLastTime -ErrorAction Ignore

                        # Process deferrals.
                        if ($AllowDefer -and $PSBoundParameters.ContainsKey('DeferTimes'))
                        {
                            [System.Int32]$DeferTimes = if ($deferHistoryTimes -ge 0)
                            {
                                Write-ADTLogEntry -Message "Defer history shows [$($deferHistoryTimes)] deferrals remaining."
                                $deferHistoryTimes - 1
                            }
                            else
                            {
                                Write-ADTLogEntry -Message "The user has [$DeferTimes] deferrals remaining."
                                $DeferTimes - 1
                            }

                            if ($DeferTimes -lt 0)
                            {
                                Write-ADTLogEntry -Message 'Deferral has expired.'
                                $AllowDefer = $false
                            }
                        }

                        # Check deferral days before deadline.
                        if ($AllowDefer -and $PSBoundParameters.ContainsKey('DeferDays'))
                        {
                            $deferDeadlineDateTime = if ($deferHistoryDeadline)
                            {
                                Write-ADTLogEntry -Message "Defer history shows a deadline date of [$($deferHistoryDeadline.ToString('O'))]."
                                $deferHistoryDeadline
                            }
                            else
                            {
                                $currentDateTimeLocal.AddDays($DeferDays)
                            }
                            Write-ADTLogEntry -Message "The user has until [$($deferDeadlineDateTime.ToString('O'))] before deferral expires."

                            if ($currentDateTimeLocal -ge $deferDeadlineDateTime)
                            {
                                Write-ADTLogEntry -Message 'Deferral has expired.'
                                $AllowDefer = $false
                            }
                        }

                        # Check deferral deadlines.
                        if ($AllowDefer -and $PSBoundParameters.ContainsKey('DeferDeadline'))
                        {
                            $deferDeadlineDateTime = $DeferDeadline
                            Write-ADTLogEntry -Message "The user has until [$($deferDeadlineDateTime.ToString('O'))] before deferral expires."

                            if ($currentDateTimeLocal -gt $deferDeadlineDateTime)
                            {
                                Write-ADTLogEntry -Message 'Deferral has expired.'
                                $AllowDefer = $false
                            }
                        }
                    }

                    # Check if all deferrals have expired.
                    if (($null -ne $DeferTimes) -and ($DeferTimes -lt 0) -and !$deferDeadlineDateTime)
                    {
                        $AllowDefer = $false
                    }

                    # Keep the same variable for countdown to simplify the code.
                    if ($ForceCloseProcessesCountdown -gt 0)
                    {
                        $CloseProcessesCountdown = $ForceCloseProcessesCountdown
                    }
                    elseif ($ForceCountdown -gt 0)
                    {
                        $CloseProcessesCountdown = $ForceCountdown
                    }

                    # Build out the parameters necessary to show a dialog.
                    $dialogOptions = @{
                        AppTitle = $PSBoundParameters.Title
                        Subtitle = $PSBoundParameters.Subtitle
                        AppIconImage = $adtConfig.Assets.Logo
                        AppIconDarkImage = $adtConfig.Assets.LogoDark
                        AppBannerImage = $adtConfig.Assets.Banner
                        DialogTopMost = !$NotTopMost
                        Language = $Script:ADT.Language
                        MinimizeWindows = !!$MinimizeWindows
                        DialogExpiryDuration = [System.TimeSpan]::FromSeconds($adtConfig.UI.DefaultTimeout)
                        Strings = $adtStrings.CloseAppsPrompt
                    }
                    if ($AllowDefer)
                    {
                        if (($null -eq $DeferTimes) -or ($DeferTimes -ge 0))
                        {
                            $dialogOptions.Add('DeferralsRemaining', [System.UInt32]($DeferTimes + 1))
                        }
                        if ($deferDeadlineDateTime)
                        {
                            $dialogOptions.Add('DeferralDeadline', [System.DateTime]$deferDeadlineDateTime)
                        }
                        if ($dialogOptions.ContainsKey('DeferralsRemaining') -and !$PSBoundParameters.ContainsKey('DeferTimes'))
                        {
                            $dialogOptions.Add('UnlimitedDeferrals', $true)
                        }
                        if ($AllowDeferCloseProcesses)
                        {
                            $dialogOptions.Add('ContinueOnProcessClosure', $true)
                        }
                    }
                    if (!$dialogOptions.ContainsKey('DeferralsRemaining') -and !$dialogOptions.ContainsKey('DeferralDeadline'))
                    {
                        if ($CloseProcessesCountdown -gt 0)
                        {
                            $dialogOptions.Add('CountdownDuration', [System.TimeSpan]::FromSeconds($CloseProcessesCountdown))
                        }
                    }
                    if ($PersistPrompt)
                    {
                        $dialogOptions.Add('DialogPersistInterval', [System.TimeSpan]::FromSeconds($adtConfig.UI.DefaultPromptPersistInterval))
                    }
                    if (($PSBoundParameters.ContainsKey('ForceCloseProcessesCountdown') -or $PSBoundParameters.ContainsKey('ForceCountdown')) -and !$dialogOptions.ContainsKey('CountdownDuration'))
                    {
                        $dialogOptions.Add('CountdownDuration', [System.TimeSpan]::FromSeconds($CloseProcessesCountdown))
                    }
                    if ($HideCloseButton -and ($AllowDefer -or !$dialogOptions.ContainsKey('CountdownDuration')))
                    {
                        $dialogOptions.Add('HideCloseButton', !!$HideCloseButton)
                    }
                    if ($PSBoundParameters.ContainsKey('WindowLocation'))
                    {
                        $dialogOptions.Add('DialogPosition', $WindowLocation)
                    }
                    if ($PSBoundParameters.ContainsKey('AllowMove'))
                    {
                        $dialogOptions.Add('DialogAllowMove', !!$AllowMove)
                    }
                    if ($PSBoundParameters.ContainsKey('AllowMinimize'))
                    {
                        $dialogOptions.Add('DialogAllowMinimize', !!$AllowMinimize)
                    }
                    if ($CustomText)
                    {
                        $dialogOptions.CustomMessageText = $adtStrings.CloseAppsPrompt.CustomMessage
                    }
                    if ($null -ne $CloseProcesses)
                    {
                        $dialogOptions.Add('CloseProcesses', $CloseProcesses)
                    }
                    if ($ForceCountdown -gt 0)
                    {
                        $dialogOptions.Add('ForcedCountdown', !!$ForceCountdown)
                    }
                    if ($null -ne $adtConfig.UI.FluentAccentColor)
                    {
                        $dialogOptions.Add('FluentAccentColor', $adtConfig.UI.FluentAccentColor)
                    }
                    $dialogOptions = [PSADT.UserInterface.DialogOptions.CloseAppsDialogOptions]::new($DeploymentType, $dialogOptions)

                    # Spin until apps are closed, countdown elapses, or deferrals are exhausted.
                    $sessionClosed = $false
                    while (($runningApps = Get-ADTRunningProcessesUserCanClose) -or (($promptResult -ne [PSADT.UserInterface.DialogResults.CloseAppsDialogResult]::Defer) -and ($promptResult -ne [PSADT.UserInterface.DialogResults.CloseAppsDialogResult]::Close)))
                    {
                        # Check if we need to prompt the user to defer, to defer and close apps, or not to prompt them at all
                        if ($AllowDefer)
                        {
                            # If there is deferral and closing apps is allowed but there are no apps to be closed, break the while loop.
                            if ($AllowDeferCloseProcesses -and !$runningApps)
                            {
                                break
                            }
                            elseif (($promptResult -ne 'Close') -or ($runningApps -and ($promptResult -ne 'Continue')))
                            {
                                # Exit gracefully if DeferRunInterval is set, a last deferral time exists, and the interval has not yet elapsed.
                                if ($adtSession -and $DeferRunInterval)
                                {
                                    Write-ADTLogEntry -Message "A DeferRunInterval of [$DeferRunInterval] is specified. Checking DeferRunIntervalLastTime."
                                    if ($deferHistoryRunIntervalLastTime)
                                    {
                                        $deferRunIntervalNextTime = $deferHistoryRunIntervalLastTime.Add($DeferRunInterval) - $currentDateTimeLocal
                                        if ($deferRunIntervalNextTime -gt [System.TimeSpan]::Zero)
                                        {
                                            Write-ADTLogEntry -Message "Next run interval not due until [$(($currentDateTimeLocal + $deferRunIntervalNextTime).ToString('O'))], exiting gracefully."
                                            $sessionClosed = $true; Close-ADTSession -ExitCode $adtConfig.UI.DefaultExitCode
                                        }
                                    }
                                }
                                $promptResult = Show-ADTWelcomePrompt
                            }
                        }
                        elseif ($runningApps -or !!$forceCountdown)
                        {
                            # If there is no deferral and processes are running, prompt the user to close running processes with no deferral option.
                            $promptResult = Show-ADTWelcomePrompt
                        }
                        else
                        {
                            # If there is no deferral and no processes running, break the while loop.
                            break
                        }

                        # Process the form results.
                        if ($promptResult.Equals([PSADT.UserInterface.DialogResults.CloseAppsDialogResult]::Continue))
                        {
                            # If the user has clicked OK, wait a few seconds for the process to terminate before evaluating the running processes again.
                            if (!$AllowDeferCloseProcesses -and !($runningApps = Get-ADTRunningProcessesUserCanClose -InformationAction Ignore))
                            {
                                Write-ADTLogEntry -Message 'The user selected to continue...'
                            }
                            for ($i = 0; $i -lt 5; $i++)
                            {
                                if (($runningApps = Get-ADTRunningProcessesUserCanClose -InformationAction Ignore))
                                {
                                    Write-ADTLogEntry -Message "The application(s) ['$([System.String]::Join("', '", ($runningApps.Description | Sort-Object -Unique)))'] are still running, checking again in 1 second..."
                                    [System.Threading.Thread]::Sleep(1000)
                                    continue
                                }
                                if ($i -ne 0)
                                {
                                    Write-ADTLogEntry -Message "All running application(s) have now closed."
                                }
                                break
                            }
                            if (!$runningApps)
                            {
                                break
                            }
                        }
                        elseif ($promptResult.Equals([PSADT.UserInterface.DialogResults.CloseAppsDialogResult]::Close))
                        {
                            # Force the applications to close. Update the process list right before closing, in case it changed.
                            Write-ADTLogEntry -Message 'The user selected to close the application(s)...'
                            if (($runningApps = Get-ADTRunningProcessesUserCanClose -InformationAction Ignore))
                            {
                                if (!$PromptToSave)
                                {
                                    Write-ADTLogEntry -Message "The parameter [-PromptToSave] not specified, force closing the application(s)."
                                    foreach ($runningApp in $runningApps)
                                    {
                                        Write-ADTLogEntry -Message "Stopping process [$($runningApp.Process.ProcessName)]..."
                                        Stop-Process -Name $runningApp.Process.ProcessName -Force -ErrorAction Ignore
                                    }
                                }
                                else
                                {
                                    Write-ADTLogEntry -Message "The parameter [-PromptToSave] was specified, prompting user to close the application(s) with [$($adtConfig.UI.PromptToSaveTimeout)] second timeout."
                                    Invoke-ADTClientServerOperation -PromptToCloseApps -User $runAsActiveUser -PromptToCloseTimeout ([System.TimeSpan]::FromSeconds($adtConfig.UI.PromptToSaveTimeout))
                                }

                                # Test whether apps are still running. If they are still running, the Welcome Window will be displayed again after 5 seconds.
                                for ($i = 0; $i -lt 5; $i++)
                                {
                                    if (($runningApps = Get-ADTRunningProcessesUserCanClose -InformationAction Ignore))
                                    {
                                        Write-ADTLogEntry -Message "The application(s) ['$([System.String]::Join("', '", ($runningApps.Description | Sort-Object -Unique)))'] are still running, checking again in 1 second..."
                                        [System.Threading.Thread]::Sleep(1000)
                                        continue
                                    }
                                    Write-ADTLogEntry -Message "All running application(s) have now closed."
                                    break
                                }
                                if (!$runningApps)
                                {
                                    break
                                }
                            }
                            else
                            {
                                Write-ADTLogEntry -Message "All running application(s) were already closed."
                                break
                            }
                        }
                        elseif ($promptResult.Equals([PSADT.UserInterface.DialogResults.CloseAppsDialogResult]::Timeout))
                        {
                            # Stop the script (if not actioned before the timeout value).
                            Write-ADTLogEntry -Message 'Deployment not actioned before the timeout value.'
                            $BlockExecution = $false

                            # Restore minimized windows.
                            if ($MinimizeWindows)
                            {
                                Invoke-ADTClientServerOperation -RestoreAllWindows -User $runAsActiveUser
                            }

                            # If there's an active session, update deferral values and close it out.
                            if ($adtSession)
                            {
                                Update-ADTDeferHistory
                                $sessionClosed = $true; Close-ADTSession -ExitCode $adtConfig.UI.DefaultExitCode
                            }
                            return
                        }
                        elseif ($promptResult.Equals([PSADT.UserInterface.DialogResults.CloseAppsDialogResult]::Defer))
                        {
                            #  Stop the script (user chose to defer)
                            Write-ADTLogEntry -Message 'Deployment deferred by the user.'
                            $BlockExecution = $false

                            # Restore minimized windows.
                            if ($MinimizeWindows)
                            {
                                Invoke-ADTClientServerOperation -RestoreAllWindows -User $runAsActiveUser
                            }

                            # If there's an active session, update deferral values and close it out.
                            if ($adtSession)
                            {
                                Update-ADTDeferHistory
                                foreach ($callback in $($Script:ADT.Callbacks.([PSAppDeployToolkit.Foundation.CallbackType]::OnDefer)))
                                {
                                    & $callback
                                }
                                $sessionClosed = $true; Close-ADTSession -ExitCode $adtConfig.UI.DeferExitCode
                            }
                            return
                        }
                        elseif (!$promptResult.Equals('TerminatedTryAgain'))
                        {
                            # We should never get here. It means the dialog result we received was entirely unexpected.
                            $naerParams = @{
                                Exception = [System.InvalidOperationException]::new("An unexpected and invalid result was received by the CloseAppsDialog.")
                                Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                                ErrorId = 'CloseAppsDialogInvalidResult'
                                TargetObject = $promptResult
                                RecommendedAction = "Please report this error to the developers for further review."
                            }
                            throw (New-ADTErrorRecord @naerParams)
                        }
                    }

                    # Break if the session has closed as Close-ADTSession won't be able to break out of the above while loop.
                    if ($sessionClosed)
                    {
                        break
                    }

                    # Close any remaining processes that are open that the user couldn't close.
                    if (($runningApps = if ($CloseProcesses) { Get-ADTRunningProcesses -ProcessObjects $CloseProcesses }))
                    {
                        # Force the processes to close silently, without prompting the user.
                        Write-ADTLogEntry -Message "Force closing application(s) ['$([System.String]::Join("', '", $runningApps.Description))'] that the user had no permissions to close."
                        Stop-Process -InputObject $runningApps.Process -Force -ErrorAction Ignore
                        [System.Threading.Thread]::Sleep(2000)
                    }
                }
                elseif (($runningApps = if ($CloseProcesses) { Get-ADTRunningProcesses -ProcessObjects $CloseProcesses }))
                {
                    # Force the processes to close silently, without prompting the user.
                    Write-ADTLogEntry -Message "Force closing application(s) ['$([System.String]::Join("', '", $runningApps.Description))'] without prompting user."
                    Stop-Process -InputObject $runningApps.Process -Force -ErrorAction Ignore
                    [System.Threading.Thread]::Sleep(2000)
                }

                # If block execution switch is true, call the function to block execution of these processes.
                if ($adtSession -and $BlockExecution -and $CloseProcesses)
                {
                    $baaeParams = @{ Processes = $CloseProcesses }
                    if ($PSBoundParameters.ContainsKey('WindowLocation'))
                    {
                        $baaeParams.Add('WindowLocation', $WindowLocation)
                    }
                    Block-ADTAppExecution @baaeParams
                }

                # Return the dialog result if we have it (non-silent) and the caller has asked for it.
                if ($PassThru -and ($null -ne $promptResult))
                {
                    return $promptResult
                }
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
        finally
        {
            # Close the client/server process if we're running without a session.
            if (!$adtSession -and $Script:ADT.ClientServerProcess)
            {
                Close-ADTClientServerProcess
            }
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
