# Show-InstallationWelcome

## SYNOPSIS

Show a welcome dialog prompting the user with information about the installation and actions to be performed before the installation can begin.

## SYNTAX

 `Show-InstallationWelcome [-CloseApps <String>] [-Silent] [-CloseAppsCountdown <Int32>] [-ForceCloseAppsCountdown <Int32>] [-PromptToSave] [-PersistPrompt] [-BlockExecution] [-AllowDefer] [-AllowDeferCloseApps] [-DeferTimes <Int32>] [-DeferDays <Int32>] [-DeferDeadline <String>] [-MinimizeWindows <Boolean>] [-TopMost <Boolean>] [-ForceCountdown <Int32>] [-CustomText] [<CommonParameters>]`

`Show-InstallationWelcome [-CloseApps <String>] [-Silent] [-CloseAppsCountdown <Int32>] [-ForceCloseAppsCountdown <Int32>] [-PromptToSave] [-PersistPrompt] [-BlockExecution] [-AllowDefer] [-AllowDeferCloseApps] [-DeferTimes <Int32>] [-DeferDays <Int32>] [-DeferDeadline <String>] -CheckDiskSpace [-RequiredDiskSpace <Int32>] [-MinimizeWindows <Boolean>] [-TopMost <Boolean>] [-ForceCountdown <Int32>] [-CustomText] [<CommonParameters>]`

## DESCRIPTION

The following prompts can be included in the welcome dialog:

a) Close the specified running applications, or optionally close the applications without showing a prompt (using the -Silent switch).

b) Defer the installation a certain number of times, for a certain number of days or until a deadline is reached.

c) Countdown until applications are automatically closed.

d) Prevent users from launching the specified applications while the installation is in progress.

Notes:

The process descriptions are retrieved from WMI, with a fall back on the process name if no description is available. Alternatively, you can specify the description yourself with a '=' symbol - see examples.

The dialog box will timeout after the timeout specified in the XML configuration file (default 1 hour and 55 minutes) to prevent SCCM installations from timing out and returning a failure code to SCCM. When the dialog times out, the script will exit and return a 1618 code (SCCM fast retry code).

## PARAMETERS

`-CloseApps <String>`

Name of the process to stop (do not include the .exe). Specify multiple processes separated by a comma. Specify custom descriptions like this: "winword=Microsoft Office

Word,excel=Microsoft Office Excel"

`-Silent [<SwitchParameter>]`

Stop processes without prompting the user.

`-CloseAppsCountdown <Int32>`

Option to provide a countdown in seconds until the specified applications are automatically closed. This only takes effect if deferral is not allowed or has expired.

`-ForceCloseAppsCountdown <Int32>`

Option to provide a countdown in seconds until the specified applications are automatically closed regardless of whether deferral is allowed.

`-PromptToSave [<SwitchParameter>]`

Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button. Option does not work in SYSTEM context unless

toolkit launched with "psexec.exe -s -i" to run it as an interactive process under the SYSTEM account.

`-PersistPrompt [<SwitchParameter>]`

Specify whether to make the prompt persist in the center of the screen every 10 seconds. The user will have no option but to respond to the prompt. This only takes effect if deferral is

not allowed or has expired.

`-BlockExecution [<SwitchParameter>]`

Option to prevent the user from launching the process/application during the installation.

`-AllowDefer [<SwitchParameter>]`

Enables an optional defer button to allow the user to defer the installation.

`-AllowDeferCloseApps [<SwitchParameter>]`

Enables an optional defer button to allow the user to defer the installation only if there are running applications that need to be closed.

`-DeferTimes <Int32>`

Specify the number of times the installation can be deferred.

`-DeferDays <Int32>`

Specify the number of days since first run that the installation can be deferred. This is converted to a deadline.

`-DeferDeadline <String>`

Specify the deadline date until which the installation can be deferred.

Specify the date in the local culture if the script is intended for that same culture.

If the script is intended to run on EN-US machines, specify the date in the format: "08/25/2013" or "08-25-2013" or "08-25-2013 18:00:00"

If the script is intended for multiple cultures, specify the date in the universal sortable date/time format: "2013-08-22 11:51:52Z"

The deadline date will be displayed to the user in the format of their culture.

`-CheckDiskSpace [<SwitchParameter>]`

Specify whether to check if there is enough disk space for the installation to proceed.

If this parameter is specified without the RequiredDiskSpace parameter, the required disk space is calculated automatically based on the size of the script source and associated files.

`-RequiredDiskSpace <Int32>`

Specify required disk space in MB, used in combination with CheckDiskSpace.

`-MinimizeWindows <Boolean>`

Specifies whether to minimize other windows when displaying prompt. Default: `$true`.

`-TopMost <Boolean>`

Specifies whether the windows is the topmost window. Default: `$true`.

`-ForceCountdown <Int32>`

Specify a countdown to display before automatically proceeding with the installation when a deferral is enabled.

`-CustomText [<SwitchParameter>]`

Specify whether to display a custom message specified in the XML file. Custom message must be populated for each language section in the XML.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Show-InstallationWelcome -CloseApps 'iexplore,winword,excel'`

Prompt the user to close Internet Explorer, Word and Excel.

-------------------------- EXAMPLE 2 --------------------------

`PS C:>Show-InstallationWelcome -CloseApps 'winword,excel' -Silent`

Close Word and Excel without prompting the user.

-------------------------- EXAMPLE 3 --------------------------

`PS C:>Show-InstallationWelcome -CloseApps 'winword,excel' -BlockExecution`

Close Word and Excel and prevent the user from launching the applications while the installation is in progress.

-------------------------- EXAMPLE 4 --------------------------

`PS C:>Show-InstallationWelcome -CloseApps 'winword=Microsoft Office Word,excel=Microsoft Office Excel' -CloseAppsCountdown 600`

Prompt the user to close Word and Excel, with customized descriptions for the applications and automatically close the applications after 10 minutes.

-------------------------- EXAMPLE 5 --------------------------

`PS C:>Show-InstallationWelcome -CloseApps 'winword,msaccess,excel' -PersistPrompt`

Prompt the user to close Word, MSAccess and Excel.

By using the PersistPrompt switch, the dialog will return to the center of the screen every 10 seconds so the user cannot ignore it by dragging it aside.

-------------------------- EXAMPLE 6 --------------------------

`PS C:>Show-InstallationWelcome -AllowDefer -DeferDeadline '25/08/2013'`

Allow the user to defer the installation until the deadline is reached.

-------------------------- EXAMPLE 7 --------------------------

`PS C:>Show-InstallationWelcome -CloseApps 'winword,excel' -BlockExecution -AllowDefer -DeferTimes 10 -DeferDeadline '25/08/2013' -CloseAppsCountdown 600`

Close Word and Excel and prevent the user from launching the applications while the installation is in progress.

Allow the user to defer the installation a maximum of 10 times or until the deadline is reached, whichever happens first.

When deferral expires, prompt the user to close the applications and automatically close them after 10 minutes.

## REMARKS

To see the examples, type: `Get-Help Show-InstallationWelcome -Examples`

For more information, type: `Get-Help Show-InstallationWelcome -Detailed`

For technical information, type: `Get-Help Show-InstallationWelcome -Full`

For online help, type: `Get-Help Show-InstallationWelcome -Online`
