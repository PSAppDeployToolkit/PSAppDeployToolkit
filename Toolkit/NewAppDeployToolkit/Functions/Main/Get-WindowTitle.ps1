#region Function Get-WindowTitle
Function Get-WindowTitle {
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
.EXAMPLE
	Get-WindowTitle -WindowTitle 'Microsoft Word'
	Gets details for each window that has the words "Microsoft Word" in the title.
.EXAMPLE
	Get-WindowTitle -GetAllWindowTitles
	Gets details for all windows with a title.
.EXAMPLE
	Get-WindowTitle -GetAllWindowTitles | Where-Object { $_.ParentProcess -eq 'WINWORD' }
	Get details for all windows belonging to Microsoft Word process with name "WINWORD".
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true,ParameterSetName='SearchWinTitle')]
		[AllowEmptyString()]
		[string]$WindowTitle,
		[Parameter(Mandatory=$true,ParameterSetName='GetAllWinTitles')]
		[ValidateNotNullorEmpty()]
		[switch]$GetAllWindowTitles = $false,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[switch]$DisableFunctionLogging = $false
	)

	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionInfo -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			If ($PSCmdlet.ParameterSetName -eq 'SearchWinTitle') {
				If (-not $DisableFunctionLogging) { Write-Log -Message "Finding open window title(s) [$WindowTitle] using regex matching." -Source ${CmdletName} }
			}
			ElseIf ($PSCmdlet.ParameterSetName -eq 'GetAllWinTitles') {
				If (-not $DisableFunctionLogging) { Write-Log -Message 'Finding all open window title(s).' -Source ${CmdletName} }
			}

			## Get all window handles for visible windows
			[IntPtr[]]$VisibleWindowHandles = [PSADT.UiAutomation]::EnumWindows() | Where-Object { [PSADT.UiAutomation]::IsWindowVisible($_) }

			## Discover details about each visible window that was discovered
			ForEach ($VisibleWindowHandle in $VisibleWindowHandles) {
				If (-not $VisibleWindowHandle) { Continue }
				## Get the window title
				[string]$VisibleWindowTitle = [PSADT.UiAutomation]::GetWindowText($VisibleWindowHandle)
				If ($VisibleWindowTitle) {
					## Get the process that spawned the window
					[Diagnostics.Process]$Process = Get-Process -ErrorAction 'Stop' | Where-Object { $_.Id -eq [PSADT.UiAutomation]::GetWindowThreadProcessId($VisibleWindowHandle) }
					If ($Process) {
						## Build custom object with details about the window and the process
						[psobject]$VisibleWindow = New-Object -TypeName 'PSObject' -Property @{
							WindowTitle = $VisibleWindowTitle
							WindowHandle = $VisibleWindowHandle
							ParentProcess= $Process.ProcessName
							ParentProcessMainWindowHandle = $Process.MainWindowHandle
							ParentProcessId = $Process.Id
						}

						## Only save/return the window and process details which match the search criteria
						If ($PSCmdlet.ParameterSetName -eq 'SearchWinTitle') {
							$MatchResult = $VisibleWindow.WindowTitle -match $WindowTitle
							If ($MatchResult) {
								[psobject[]]$VisibleWindows += $VisibleWindow
							}
						}
						ElseIf ($PSCmdlet.ParameterSetName -eq 'GetAllWinTitles') {
							[psobject[]]$VisibleWindows += $VisibleWindow
						}
					}
				}
			}
		}
		Catch {
			If (-not $DisableFunctionLogging) { Write-Log -Message "Failed to get requested window title(s). `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName} }
		}
	}
	End {
		Write-Output -InputObject $VisibleWindows

		If ($DisableFunctionLogging) { . $RevertScriptLogging }
		Write-FunctionInfo -CmdletName ${CmdletName} -Footer
	}
}
#endregion
