# Get-WindowTitle

## SYNOPSIS

Search for an open window title and return details about the window.

## SYNTAX

 `Get-WindowTitle -WindowTitle <String> [-DisableFunctionLogging] [<CommonParameters>]`

Get-WindowTitle -GetAllWindowTitles [-DisableFunctionLogging] [<CommonParameters>]

## DESCRIPTION

Search for a window title. If window title searched for returns more than one result, then details for each window will be displayed.

Returns the following properties for each window: WindowTitle, WindowHandle, ParentProcess, ParentProcessMainWindowHandle, ParentProcessId.

Function does not work in SYSTEM context unless launched with "psexec.exe -s -i" to run it as an interactive process under the SYSTEM account.

## PARAMETERS

`-WindowTitle <String>`

The title of the application window to search for using regex matching.

`-GetAllWindowTitles [<SwitchParameter>]`

Get titles for all open windows on the system.

`-DisableFunctionLogging [<SwitchParameter>]`

Disables logging messages to the script log file.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Get-WindowTitle -WindowTitle 'Microsoft Word'`

Gets details for each window that has the words "Microsoft Word" in the title.

-------------------------- EXAMPLE 2 --------------------------

`PS C:>Get-WindowTitle -GetAllWindowTitles`

Gets details for all windows with a title.

-------------------------- EXAMPLE 3 --------------------------

`PS C:>Get-WindowTitle -GetAllWindowTitles | Where-Object { $_.ParentProcess -eq 'WINWORD' }`

Get details for all windows belonging to Microsoft Word process with name "WINWORD".

## REMARKS

To see the examples, type: `Get-Help Get-WindowTitle -Examples`

For more information, type: `Get-Help Get-WindowTitle -Detailed`

For technical information, type: `Get-Help Get-WindowTitle -Full`

For online help, type: `Get-Help Get-WindowTitle -Online`
