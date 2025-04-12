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

    .PARAMETER BlockExecution
        Option to prevent the user from launching processes/applications, specified in -CloseProcesses, during the deployment.

    .PARAMETER PromptToSave
        Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button. Option does not work in SYSTEM context unless toolkit launched with "psexec.exe -s -i" to run it as an interactive process under the SYSTEM account.

    .PARAMETER PersistPrompt
        Specify whether to make the Show-ADTInstallationWelcome prompt persist in the center of the screen every couple of seconds, specified in the config.psd1. The user will have no option but to respond to the prompt. This only takes effect if deferral is not allowed or has expired.

    .PARAMETER MinimizeWindows
        Specifies whether to minimize other windows when displaying prompt.

    .PARAMETER NoMinimizeWindows
        This parameter will be removed in PSAppDeployToolkit 4.2.0.

    .PARAMETER NotTopMost
        Specifies whether the windows is the topmost window.

    .PARAMETER CustomText
        Specify whether to display a custom message specified in the `strings.psd1` file. Custom message must be populated for each language section in the `strings.psd1` file.

    .PARAMETER CustomMessageText
    Specify the custom message text to display in the dialog (Fluent UI only).

    .PARAMETER CheckDiskSpace
        Specify whether to check if there is enough disk space for the deployment to proceed.

        If this parameter is specified without the RequiredDiskSpace parameter, the required disk space is calculated automatically based on the size of the script source and associated files.

    .PARAMETER RequiredDiskSpace
        Specify required disk space in MB, used in combination with CheckDiskSpace.

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
        Show-ADTInstallationWelcome -AllowDefer -DeferDeadline '25/08/2013'

        Allow the user to defer the deployment until the deadline is reached.

    .EXAMPLE
        Show-ADTInstallationWelcome -CloseProcesses @{ Name = 'winword' }, @{ Name = 'excel' } -BlockExecution -AllowDefer -DeferTimes 10 -DeferDeadline '25/08/2013' -CloseProcessesCountdown 600

        Close Word and Excel and prevent the user from launching the applications while the deployment is in progress.

        Allow the user to defer the deployment a maximum of 10 times or until the deadline is reached, whichever happens first.

        When deferral expires, prompt the user to close the applications and automatically close them after 10 minutes.

    .NOTES
        An active ADT session is NOT required to use this function.

        The process descriptions are retrieved via Get-Process, with a fall back on the process name if no description is available. Alternatively, you can specify the description yourself with a '=' symbol - see examples.

        The dialog box will timeout after the timeout specified in the config.psd1 file (default 55 minutes) to prevent Intune/SCCM deployments from timing out and returning a failure code. When the dialog times out, the script will exit and return a 1618 code (SCCM fast retry code).

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Show-ADTInstallationWelcome
    #>

    [CmdletBinding(DefaultParameterSetName = 'Interactive')]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcesses', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesCheckDiskSpace', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesCloseProcessesCountdown', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesCloseProcessesCountdownCheckDiskSpace', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesForceCloseProcessesCountdown', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDefer', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCheckDiskSpace', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdown', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdownCheckDiskSpace', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdown', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdownCheckDiskSpace', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdown', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcesses', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCheckDiskSpace', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdown', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdownCheckDiskSpace', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdown', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdownCheckDiskSpace', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdown', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'SilentCloseProcesses', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [Parameter(Mandatory = $true, ParameterSetName = 'SilentCloseProcessesCheckDiskSpace', HelpMessage = "Specify process names and an optional process description, e.g. @{ Name = 'winword'; Description = 'Microsoft Word' }")]
        [ValidateNotNullOrEmpty()]
        [PSADT.Types.ProcessObject[]]$CloseProcesses,

        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveAllowDefer', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveAllowDeferCheckDiskSpace', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveAllowDeferForceCountdown', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveAllowDeferForceCountdownCheckDiskSpace', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDefer', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCheckDiskSpace', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdown', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdownCheckDiskSpace', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdown', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdown', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box.')]
        [System.Management.Automation.SwitchParameter]$AllowDefer,

        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcesses', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box only if an app needs to be closed.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCheckDiskSpace', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box only if an app needs to be closed.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdown', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box only if an app needs to be closed.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdownCheckDiskSpace', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box only if an app needs to be closed.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdown', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box only if an app needs to be closed.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box only if an app needs to be closed.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdown', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box only if an app needs to be closed.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to enable the optional defer button on the dialog box only if an app needs to be closed.')]
        [System.Management.Automation.SwitchParameter]$AllowDeferCloseProcesses,

        [Parameter(Mandatory = $true, ParameterSetName = 'Silent', HelpMessage = 'Specify whether to prompt user or force close the applications.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'SilentCheckDiskSpace', HelpMessage = 'Specify whether to prompt user or force close the applications.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'SilentCloseProcesses', HelpMessage = 'Specify whether to prompt user or force close the applications.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'SilentCloseProcessesCheckDiskSpace', HelpMessage = 'Specify whether to prompt user or force close the applications.')]
        [System.Management.Automation.SwitchParameter]$Silent,

        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesCloseProcessesCountdown', HelpMessage = 'Specify a countdown to display before automatically closing applications where deferral is not allowed or has expired.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify a countdown to display before automatically closing applications where deferral is not allowed or has expired.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdown', HelpMessage = 'Specify a countdown to display before automatically closing applications where deferral is not allowed or has expired.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify a countdown to display before automatically closing applications where deferral is not allowed or has expired.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdown', HelpMessage = 'Specify a countdown to display before automatically closing applications where deferral is not allowed or has expired.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify a countdown to display before automatically closing applications where deferral is not allowed or has expired.')]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$CloseProcessesCountdown,

        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesForceCloseProcessesCountdown', HelpMessage = 'Specify a countdown to display before automatically closing applications whether or not deferral is allowed.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify a countdown to display before automatically closing applications whether or not deferral is allowed.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdown', HelpMessage = 'Specify a countdown to display before automatically closing applications whether or not deferral is allowed.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify a countdown to display before automatically closing applications whether or not deferral is allowed.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdown', HelpMessage = 'Specify a countdown to display before automatically closing applications whether or not deferral is allowed.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify a countdown to display before automatically closing applications whether or not deferral is allowed.')]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$ForceCloseProcessesCountdown,

        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveAllowDeferForceCountdown', HelpMessage = 'Specify a countdown to display before automatically proceeding with the deployment when a deferral is enabled.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveAllowDeferForceCountdownCheckDiskSpace', HelpMessage = 'Specify a countdown to display before automatically proceeding with the deployment when a deferral is enabled.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdown', HelpMessage = 'Specify a countdown to display before automatically proceeding with the deployment when a deferral is enabled.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdownCheckDiskSpace', HelpMessage = 'Specify a countdown to display before automatically proceeding with the deployment when a deferral is enabled.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdown', HelpMessage = 'Specify a countdown to display before automatically proceeding with the deployment when a deferral is enabled.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdownCheckDiskSpace', HelpMessage = 'Specify a countdown to display before automatically proceeding with the deployment when a deferral is enabled.')]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$ForceCountdown,

        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDefer', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDeferCheckDiskSpace', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDefer', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCheckDiskSpace', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdown', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdownCheckDiskSpace', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdown', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdown', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcesses', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCheckDiskSpace', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdown', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdownCheckDiskSpace', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdown', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdown', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$DeferTimes,

        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDefer', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDeferCheckDiskSpace', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDefer', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCheckDiskSpace', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdown', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdownCheckDiskSpace', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdown', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdown', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcesses', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCheckDiskSpace', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdown', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdownCheckDiskSpace', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdown', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdown', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$DeferDays,

        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDefer', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDeferCheckDiskSpace', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDefer', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCheckDiskSpace', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdown', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdownCheckDiskSpace', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdown', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdownCheckDiskSpace', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdown', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcesses', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCheckDiskSpace', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdown', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdownCheckDiskSpace', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdown', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdownCheckDiskSpace', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdown', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = "Specify the deadline (in either your local UI culture's date format, or ISO8601 format) for which deferral will expire as an option.")]
        [ValidateNotNullOrEmpty()]
        [System.String]$DeferDeadline,

        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDefer', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDeferCheckDiskSpace', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDefer', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCheckDiskSpace', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdown', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdownCheckDiskSpace', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdown', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdown', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcesses', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCheckDiskSpace', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdown', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdownCheckDiskSpace', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdown', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdown', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specifies the time span that must elapse before prompting the user again if a process listed in [-CloseProcesses] is still running after a deferral.')]
        [ValidateNotNullOrEmpty()]
        [System.TimeSpan]$DeferRunInterval,

        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcesses', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesCheckDiskSpace', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesCloseProcessesCountdown', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesForceCloseProcessesCountdown', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDefer', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCheckDiskSpace', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdown', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdownCheckDiskSpace', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdown', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdown', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcesses', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCheckDiskSpace', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdown', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdownCheckDiskSpace', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdown', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdown', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SilentCloseProcesses', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SilentCloseProcessesCheckDiskSpace', HelpMessage = 'Specify whether to block execution of the processes during deployment.')]
        [System.Management.Automation.SwitchParameter]$BlockExecution,

        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcesses', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesCheckDiskSpace', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesCloseProcessesCountdown', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesForceCloseProcessesCountdown', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDefer', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCheckDiskSpace', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdown', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdownCheckDiskSpace', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdown', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdown', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcesses', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCheckDiskSpace', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdown', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdownCheckDiskSpace', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdown', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdown', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [System.Management.Automation.SwitchParameter]$PromptToSave,

        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCheckDiskSpace', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcesses', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesCheckDiskSpace', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDefer', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDeferCheckDiskSpace', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDeferForceCountdown', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDeferForceCountdownCheckDiskSpace', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesCloseProcessesCountdown', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesForceCloseProcessesCountdown', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDefer', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCheckDiskSpace', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdown', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdownCheckDiskSpace', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdown', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdown', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcesses', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCheckDiskSpace', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdown', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdownCheckDiskSpace', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdown', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdown', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1.')]
        [System.Management.Automation.SwitchParameter]$PersistPrompt,

        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCheckDiskSpace', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcesses', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesCheckDiskSpace', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDefer', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDeferCheckDiskSpace', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDeferForceCountdown', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDeferForceCountdownCheckDiskSpace', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesCloseProcessesCountdown', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesForceCloseProcessesCountdown', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDefer', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCheckDiskSpace', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdown', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdownCheckDiskSpace', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdown', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdown', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcesses', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCheckDiskSpace', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdown', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdownCheckDiskSpace', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdown', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdown', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [System.Management.Automation.SwitchParameter]$MinimizeWindows,

        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive', HelpMessage = 'This parameter is obsolete and will be removed in PSAppDeployToolkit 4.2.0.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCheckDiskSpace', HelpMessage = 'This parameter is obsolete and will be removed in PSAppDeployToolkit 4.2.0.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcesses', HelpMessage = 'This parameter is obsolete and will be removed in PSAppDeployToolkit 4.2.0.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesCheckDiskSpace', HelpMessage = 'This parameter is obsolete and will be removed in PSAppDeployToolkit 4.2.0.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDefer', HelpMessage = 'This parameter is obsolete and will be removed in PSAppDeployToolkit 4.2.0.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDeferCheckDiskSpace', HelpMessage = 'This parameter is obsolete and will be removed in PSAppDeployToolkit 4.2.0.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDeferForceCountdown', HelpMessage = 'This parameter is obsolete and will be removed in PSAppDeployToolkit 4.2.0.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDeferForceCountdownCheckDiskSpace', HelpMessage = 'This parameter is obsolete and will be removed in PSAppDeployToolkit 4.2.0.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesCloseProcessesCountdown', HelpMessage = 'This parameter is obsolete and will be removed in PSAppDeployToolkit 4.2.0.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'This parameter is obsolete and will be removed in PSAppDeployToolkit 4.2.0.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesForceCloseProcessesCountdown', HelpMessage = 'This parameter is obsolete and will be removed in PSAppDeployToolkit 4.2.0.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'This parameter is obsolete and will be removed in PSAppDeployToolkit 4.2.0.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDefer', HelpMessage = 'This parameter is obsolete and will be removed in PSAppDeployToolkit 4.2.0.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCheckDiskSpace', HelpMessage = 'This parameter is obsolete and will be removed in PSAppDeployToolkit 4.2.0.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdown', HelpMessage = 'This parameter is obsolete and will be removed in PSAppDeployToolkit 4.2.0.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdownCheckDiskSpace', HelpMessage = 'This parameter is obsolete and will be removed in PSAppDeployToolkit 4.2.0.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdown', HelpMessage = 'This parameter is obsolete and will be removed in PSAppDeployToolkit 4.2.0.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'This parameter is obsolete and will be removed in PSAppDeployToolkit 4.2.0.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdown', HelpMessage = 'This parameter is obsolete and will be removed in PSAppDeployToolkit 4.2.0.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'This parameter is obsolete and will be removed in PSAppDeployToolkit 4.2.0.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcesses', HelpMessage = 'This parameter is obsolete and will be removed in PSAppDeployToolkit 4.2.0.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCheckDiskSpace', HelpMessage = 'This parameter is obsolete and will be removed in PSAppDeployToolkit 4.2.0.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdown', HelpMessage = 'This parameter is obsolete and will be removed in PSAppDeployToolkit 4.2.0.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdownCheckDiskSpace', HelpMessage = 'This parameter is obsolete and will be removed in PSAppDeployToolkit 4.2.0.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdown', HelpMessage = 'This parameter is obsolete and will be removed in PSAppDeployToolkit 4.2.0.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'This parameter is obsolete and will be removed in PSAppDeployToolkit 4.2.0.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdown', HelpMessage = 'This parameter is obsolete and will be removed in PSAppDeployToolkit 4.2.0.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'This parameter is obsolete and will be removed in PSAppDeployToolkit 4.2.0.')]
        [System.Obsolete("This parameter will be removed in PSAppDeployToolkit 4.2.0.")]
        [System.Management.Automation.SwitchParameter]$NoMinimizeWindows,

        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCheckDiskSpace', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcesses', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesCheckDiskSpace', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDefer', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDeferCheckDiskSpace', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDeferForceCountdown', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDeferForceCountdownCheckDiskSpace', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesCloseProcessesCountdown', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesCloseProcessesCountdownCheckDiskSpace', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesForceCloseProcessesCountdown', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDefer', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCheckDiskSpace', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdown', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdownCheckDiskSpace', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdown', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdownCheckDiskSpace', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdown', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcesses', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCheckDiskSpace', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdown', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdownCheckDiskSpace', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdown', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdownCheckDiskSpace', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdown', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = "Specifies whether the window shouldn't be on top of other windows.")]
        [System.Management.Automation.SwitchParameter]$NotTopMost,

        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCheckDiskSpace', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcesses', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesCheckDiskSpace', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDefer', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDeferCheckDiskSpace', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDeferForceCountdown', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDeferForceCountdownCheckDiskSpace', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesCloseProcessesCountdown', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesForceCloseProcessesCountdown', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDefer', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCheckDiskSpace', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdown', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdownCheckDiskSpace', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdown', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdown', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcesses', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCheckDiskSpace', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdown', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdownCheckDiskSpace', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdown', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdown', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [System.String]$CustomMessageText,

        [Parameter(Mandatory = $false, ParameterSetName = 'Interactive', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCheckDiskSpace', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcesses', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesCheckDiskSpace', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDefer', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDeferCheckDiskSpace', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDeferForceCountdown', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDeferForceCountdownCheckDiskSpace', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesCloseProcessesCountdown', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesForceCloseProcessesCountdown', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDefer', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCheckDiskSpace', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdown', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdownCheckDiskSpace', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdown', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdown', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcesses', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCheckDiskSpace', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdown', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdownCheckDiskSpace', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdown', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdown', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to display a custom message specified in the [strings.psd1] file. Custom message must be populated for each language section in the [strings.psd1] file.')]
        [System.Management.Automation.SwitchParameter]$CustomText,

        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCheckDiskSpace', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesCheckDiskSpace', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveAllowDeferCheckDiskSpace', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveAllowDeferForceCountdownCheckDiskSpace', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCheckDiskSpace', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdownCheckDiskSpace', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCheckDiskSpace', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdownCheckDiskSpace', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'SilentCheckDiskSpace', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [Parameter(Mandatory = $true, ParameterSetName = 'SilentCloseProcessesCheckDiskSpace', HelpMessage = 'Specify whether to check if there is enough disk space for the deployment to proceed. If this parameter is specified without the [-RequiredDiskSpace] parameter, the required disk space is calculated automatically based on the size of the script source and associated files.')]
        [System.Management.Automation.SwitchParameter]$CheckDiskSpace,

        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCheckDiskSpace', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesCheckDiskSpace', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDeferCheckDiskSpace', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveAllowDeferForceCountdownCheckDiskSpace', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCheckDiskSpace', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCountdownCheckDiskSpace', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCheckDiskSpace', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCountdownCheckDiskSpace', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [Parameter(Mandatory = $false, ParameterSetName = 'InteractiveCloseProcessesAllowDeferCloseProcessesForceCloseProcessesCountdownCheckDiskSpace', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SilentCheckDiskSpace', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SilentCloseProcessesCheckDiskSpace', HelpMessage = 'Specify required disk space in MB, used in combination with [-CheckDiskSpace].')]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$RequiredDiskSpace
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
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = !$adtSession; HelpMessage = "Title of the prompt." }
                    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
                    ($defaultValue = [System.Management.Automation.PSDefaultValueAttribute]::new())
                    $defaultValue.Help = "(Get-ADTSession).InstallTitle"
                )
            ))
        $paramDictionary.Add('Subtitle', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'Subtitle', [System.String], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = !$adtSession -and ($adtConfig.UI.DialogStyle -eq 'Fluent'); HelpMessage = "Subtitle of the prompt." }
                    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
                    ($defaultValue = [System.Management.Automation.PSDefaultValueAttribute]::new())
                    $defaultValue.Help = "(Get-ADTStringTable).Prompt.Subtitle.((Get-ADTSession).DeploymentType.ToString())"
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
        $adtEnv = Get-ADTEnvironmentTable

        # Log the deprecation of -NoMinimizeWindows to the log.
        if ($PSBoundParameters.ContainsKey('NoMinimizeWindows'))
        {
            Write-ADTLogEntry -Message "The parameter [-NoMinimizeWindows] is obsolete and will be removed in PSAppDeployToolkit 4.2.0." -Severity 2
        }

        # Set up DeploymentType if not specified.
        $DeploymentType = if ($adtSession)
        {
            $adtSession.DeploymentType.ToString()
        }
        else
        {
            'Install'
        }

        # Set up remainder if not specified.
        if (!$PSBoundParameters.ContainsKey('Title'))
        {
            $PSBoundParameters.Add('Title', $adtSession.InstallTitle)
        }
        if (!$PSBoundParameters.ContainsKey('Subtitle'))
        {
            $PSBoundParameters.Add('Subtitle', $adtStrings.WelcomePrompt.Fluent.Subtitle.$DeploymentType)
        }
        if (!$PSBoundParameters.ContainsKey('CustomMessageText'))
        {
            $PSBoundParameters.Add('CustomMessageText', $CustomMessageText)
        }

        # Instantiate new object to hold all data needed within this call.
        $welcomeState = [PSADT.Types.WelcomeState]::new()
        $deferDeadlineUniversal = $null
        $promptResult = $null
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
                    $Silent = $true
                }

                # If using Zero-Config MSI Deployment, append any executables found in the MSI to the CloseProcesses list
                if ($adtSession -and ($msiExecutables = $adtSession.GetDefaultMsiExecutablesList()))
                {
                    $CloseProcesses = $(if ($CloseProcesses) { $CloseProcesses }; $msiExecutables)
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
                            Show-ADTInstallationPrompt -Message ([System.String]::Format($adtStrings.DiskSpace.Message.$DeploymentType, $PSBoundParameters.Title, $RequiredDiskSpace, $freeDiskSpace)) -ButtonRightText OK -Icon Error
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

                        # Reset switches.
                        $checkDeferDays = $DeferDays -ne 0
                        $checkDeferDeadline = !!$DeferDeadline

                        # Process deferrals.
                        if ($DeferTimes -ne 0)
                        {
                            [System.Int32]$DeferTimes = if ($deferHistoryTimes -ge 0)
                            {
                                Write-ADTLogEntry -Message "Defer history shows [$($deferHistory.DeferTimesRemaining)] deferrals remaining."
                                $deferHistory.DeferTimesRemaining - 1
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
                        if ($checkDeferDays -and $AllowDefer)
                        {
                            $deferDeadlineUniversal = if ($deferHistoryDeadline)
                            {
                                Write-ADTLogEntry -Message "Defer history shows a deadline date of [$deferHistoryDeadline]."
                                Get-ADTUniversalDate -DateTime $deferHistoryDeadline
                            }
                            else
                            {
                                Get-ADTUniversalDate -DateTime ([System.DateTime]::Now.AddDays($DeferDays).ToString([System.Globalization.DateTimeFormatInfo]::CurrentInfo.UniversalSortableDateTimePattern))
                            }
                            Write-ADTLogEntry -Message "The user has until [$deferDeadlineUniversal] before deferral expires."

                            if ((Get-ADTUniversalDate) -gt $deferDeadlineUniversal)
                            {
                                Write-ADTLogEntry -Message 'Deferral has expired.'
                                $AllowDefer = $false
                            }
                        }

                        # Check deferral deadlines.
                        if ($checkDeferDeadline -and $AllowDefer)
                        {
                            # Validate date.
                            try
                            {
                                $deferDeadlineUniversal = Get-ADTUniversalDate -DateTime $DeferDeadline
                                Write-ADTLogEntry -Message "The user has until [$deferDeadlineUniversal] remaining."

                                if ((Get-ADTUniversalDate) -gt $deferDeadlineUniversal)
                                {
                                    Write-ADTLogEntry -Message 'Deferral has expired.'
                                    $AllowDefer = $false
                                }
                            }
                            catch
                            {
                                Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Date is not in the correct format for the current culture. Type the date in the current locale format, such as 20/08/2014 (Europe) or 08/20/2014 (United States). If the script is intended for multiple cultures, specify the date in the universal sortable date/time format, e.g. '2013-08-22 11:51:52Z'."
                            }
                        }
                    }

                    # Check if all deferrals have expired.
                    if (($DeferTimes -lt 0) -and !$deferDeadlineUniversal)
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

                    while (($welcomeState.RunningApps = Get-ADTRunningApplications -ProcessObjects $CloseProcesses) -or (($promptResult -ne 'Defer') -and ($promptResult -ne 'Close')))
                    {
                        # Get all unique running process descriptions.
                        $welcomeState.RunningAppDescriptions = $welcomeState.RunningApps | Select-Object -ExpandProperty Description | Sort-Object -Unique

                        # Define parameters for welcome prompt.
                        $promptParams = @{
                            Title = $PSBoundParameters.Title
                            Subtitle = $PSBoundParameters.Subtitle
                            DeploymentType = $DeploymentType
                            CloseProcessesCountdown = [System.TimeSpan]::FromSeconds($CloseProcessesCountdown)
                            ForceCloseProcessesCountdown = !!$ForceCloseProcessesCountdown
                            ForceCountdown = !!$ForceCountdown
                            PersistPrompt = $PersistPrompt
                            MinimizeWindows = $MinimizeWindows
                            CustomText = $CustomText
                            CustomMessageText = $CustomMessageText
                            NotTopMost = $NotTopMost
                        }
                        if ($CloseProcesses) { $promptParams.Add('ProcessObjects', $CloseProcesses) }

                        # Check if we need to prompt the user to defer, to defer and close apps, or not to prompt them at all
                        if ($AllowDefer)
                        {
                            # If there is deferral and closing apps is allowed but there are no apps to be closed, break the while loop.
                            if ($AllowDeferCloseProcesses -and !$welcomeState.RunningAppDescriptions)
                            {
                                break
                            }
                            elseif (($promptResult -ne 'Close') -or ($welcomeState.RunningAppDescriptions -and ($promptResult -ne 'Continue')))
                            {
                                # Otherwise, as long as the user has not selected to close the apps or the processes are still running and the user has not selected to continue, prompt user to close running processes with deferral.
                                $deferParams = @{ AllowDefer = $true; DeferTimes = $DeferTimes }; if ($deferDeadlineUniversal) { $deferParams.Add('DeferDeadline', $deferDeadlineUniversal) }

                                # Exit gracefully if DeferRunInterval is set, a last deferral time exists, and the interval has not yet elapsed.
                                if ($DeferRunInterval)
                                {
                                    Write-ADTLogEntry -Message "DeferRunInterval of [$DeferRunInterval] is specified. Checking DeferRunIntervalLastTime."
                                    if (-not ([String]::IsNullOrEmpty($deferHistoryRunIntervalLastTime)))
                                    {
                                        $deferRunIntervalLastTime = Get-ADTUniversalDate -DateTime $deferHistoryRunIntervalLastTime
                                        $deferRunIntervalNextTime = Get-ADTUniversalDate -DateTime ([System.DateTime]::Parse($deferRunIntervalLastTime).ToUniversalTime().Add($DeferRunInterval).ToString([System.Globalization.DateTimeFormatInfo]::CurrentInfo.UniversalSortableDateTimePattern))
                                        if ([System.DateTime]::Parse($deferRunIntervalNextTime) -gt [System.DateTime]::Parse((Get-ADTUniversalDate)))
                                        {
                                            Write-ADTLogEntry -Message "DeferRunInterval has not elapsed. Exiting gracefully."
                                            Close-ADTSession -ExitCode $adtConfig.UI.DefaultExitCode
                                        }
                                    }
                                }
                                $promptResult = & $Script:CommandTable."Show-ADTWelcomePrompt$($adtConfig.UI.DialogStyle)" @promptParams @deferParams
                            }
                        }
                        elseif ($welcomeState.RunningAppDescriptions -or !!$forceCountdown)
                        {
                            # If there is no deferral and processes are running, prompt the user to close running processes with no deferral option.
                            $promptResult = & $Script:CommandTable."Show-ADTWelcomePrompt$($adtConfig.UI.DialogStyle)" @promptParams
                        }
                        else
                        {
                            # If there is no deferral and no processes running, break the while loop.
                            break
                        }

                        # Process the form results.
                        if ($promptResult -eq 'Continue')
                        {
                            # If the user has clicked OK, wait a few seconds for the process to terminate before evaluating the running processes again.
                            Write-ADTLogEntry -Message 'The user selected to continue...'
                            if (!$welcomeState.RunningApps)
                            {
                                # Break the while loop if there are no processes to close and the user has clicked OK to continue.
                                break
                            }
                            [System.Threading.Thread]::Sleep(2000)
                        }
                        elseif ($promptResult -eq 'Close')
                        {
                            # Force the applications to close.
                            Write-ADTLogEntry -Message 'The user selected to force the application(s) to close...'
                            if ($PromptToSave -and $adtEnv.SessionZero -and !$adtEnv.IsProcessUserInteractive)
                            {
                                Write-ADTLogEntry -Message 'Specified [-PromptToSave] option will not be available, because current process is running in session zero and is not interactive.' -Severity 2
                            }

                            # Update the process list right before closing, in case it changed.
                            $PromptToSaveTimeout = [System.TimeSpan]::FromSeconds($adtConfig.UI.PromptToSaveTimeout)
                            foreach ($runningApp in ($welcomeState.RunningApps = Get-ADTRunningApplications -ProcessObject $CloseProcesses -InformationAction SilentlyContinue))
                            {
                                # If the PromptToSave parameter was specified and the process has a window open, then prompt the user to save work if there is work to be saved when closing window.
                                if ($PromptToSave -and !($adtEnv.SessionZero -and !$adtEnv.IsProcessUserInteractive) -and ($AllOpenWindowsForRunningProcess = Get-ADTWindowTitle -ParentProcess $runningApp.Process.ProcessName -InformationAction SilentlyContinue | Select-Object -First 1) -and ($runningApp.Process.MainWindowHandle -ne [IntPtr]::Zero))
                                {
                                    foreach ($OpenWindow in $AllOpenWindowsForRunningProcess)
                                    {
                                        try
                                        {
                                            # Try to bring the window to the front before closing. This doesn't always work.
                                            Write-ADTLogEntry -Message "Stopping process [$($runningApp.Process.ProcessName)] with window title [$($OpenWindow.WindowTitle)] and prompt to save if there is work to be saved (timeout in [$($adtConfig.UI.PromptToSaveTimeout)] seconds)..."
                                            $null = try
                                            {
                                                [PSADT.Utilities.WindowUtilities]::BringWindowToFront($OpenWindow.WindowHandle)
                                            }
                                            catch
                                            {
                                                $null
                                            }

                                            # Close out the main window and spin until completion.
                                            if ($runningApp.Process.CloseMainWindow())
                                            {
                                                $promptToSaveStart = [System.Diagnostics.Stopwatch]::StartNew()
                                                do
                                                {
                                                    if (!($IsWindowOpen = Get-ADTWindowTitle -WindowHandle $OpenWindow.WindowHandle -InformationAction SilentlyContinue | Select-Object -First 1))
                                                    {
                                                        break
                                                    }
                                                    [System.Threading.Thread]::Sleep(3000)
                                                }
                                                while ($IsWindowOpen -and ($promptToSaveStart.Elapsed -lt $PromptToSaveTimeout))

                                                if ($IsWindowOpen)
                                                {
                                                    Write-ADTLogEntry -Message "Exceeded the [$($adtConfig.UI.PromptToSaveTimeout)] seconds timeout value for the user to save work associated with process [$($runningApp.Process.ProcessName)] with window title [$($OpenWindow.WindowTitle)]." -Severity 2
                                                }
                                                else
                                                {
                                                    Write-ADTLogEntry -Message "Window [$($OpenWindow.WindowTitle)] for process [$($runningApp.Process.ProcessName)] was successfully closed."
                                                }
                                            }
                                            else
                                            {
                                                Write-ADTLogEntry -Message "Failed to call the CloseMainWindow() method on process [$($runningApp.Process.ProcessName)] with window title [$($OpenWindow.WindowTitle)] because the main window may be disabled due to a modal dialog being shown." -Severity 3
                                            }
                                        }
                                        catch
                                        {
                                            Write-ADTLogEntry -Message "Failed to close window [$($OpenWindow.WindowTitle)] for process [$($runningApp.Process.ProcessName)].`n$(Resolve-ADTErrorRecord -ErrorRecord $_)" -Severity 3
                                        }
                                        finally
                                        {
                                            $runningApp.Process.Refresh()
                                        }
                                    }
                                }
                                else
                                {
                                    Write-ADTLogEntry -Message "Stopping process $($runningApp.Process.ProcessName)..."
                                    Stop-Process -Name $runningApp.Process.ProcessName -Force -ErrorAction Ignore
                                }
                            }

                            if ($welcomeState.RunningApps = Get-ADTRunningApplications -ProcessObjects $CloseProcesses -InformationAction SilentlyContinue)
                            {
                                # Apps are still running, give them 2s to close. If they are still running, the Welcome Window will be displayed again.
                                Write-ADTLogEntry -Message 'Sleeping for 2 seconds because the processes are still not closed...'
                                [System.Threading.Thread]::Sleep(2000)
                            }
                        }
                        elseif ($promptResult -eq 'Timeout')
                        {
                            # Stop the script (if not actioned before the timeout value).
                            Write-ADTLogEntry -Message 'Deployment not actioned before the timeout value.'
                            $BlockExecution = $false
                            if ($adtSession -and (($DeferTimes -ge 0) -or $deferDeadlineUniversal))
                            {
                                Set-ADTDeferHistory -DeferTimesRemaining $DeferTimes -DeferDeadline $deferDeadlineUniversal
                            }

                            # Dispose the welcome prompt timer here because if we dispose it within the Show-ADTWelcomePrompt function we risk resetting the timer and missing the specified timeout period.
                            if ($welcomeState.WelcomeTimer)
                            {
                                $welcomeState.WelcomeTimer.Dispose()
                                $welcomeState.WelcomeTimer = $null
                            }

                            # Restore minimized windows.
                            if ($MinimizeWindows)
                            {
                                $null = $adtEnv.ShellApp.UndoMinimizeAll()
                            }
                            if ($adtSession)
                            {
                                Close-ADTSession -ExitCode $adtConfig.UI.DefaultExitCode
                            }
                        }
                        elseif ($promptResult -eq 'Defer')
                        {
                            #  Stop the script (user chose to defer)
                            Write-ADTLogEntry -Message 'Deployment deferred by the user.'
                            $BlockExecution = $false

                            # Update defer history, including DeferRunInterval if specified.
                            if ($DeferRunInterval)
                            {
                                Set-ADTDeferHistory -DeferTimesRemaining $DeferTimes -DeferDeadline $deferDeadlineUniversal -DeferRunInterval $DeferRunInterval
                            }
                            else
                            {
                                Set-ADTDeferHistory -DeferTimesRemaining $DeferTimes -DeferDeadline $deferDeadlineUniversal
                            }

                            # Restore minimized windows.
                            if ($MinimizeWindows)
                            {
                                $null = $adtEnv.ShellApp.UndoMinimizeAll()
                            }
                            if ($adtSession)
                            {
                                Close-ADTSession -ExitCode $adtConfig.UI.DeferExitCode
                            }
                        }
                    }
                }
                elseif (($runningApps = Get-ADTRunningApplications -ProcessObjects $CloseProcesses -InformationAction SilentlyContinue))
                {
                    # Force the processes to close silently, without prompting the user.
                    Write-ADTLogEntry -Message "Force closing application(s) [$(($runningApps.Description | Sort-Object -Unique) -join ',')] without prompting user."
                    Stop-Process -InputObject $runningApps.Process -Force -ErrorAction Ignore
                    [System.Threading.Thread]::Sleep(2000)
                }

                # If block execution switch is true, call the function to block execution of these processes.
                if ($BlockExecution -and $CloseProcesses)
                {
                    Write-ADTLogEntry -Message '[-BlockExecution] parameter specified.'
                    Block-ADTAppExecution -ProcessName $CloseProcesses.Name
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
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
