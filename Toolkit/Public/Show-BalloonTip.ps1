Function Show-BalloonTip {
	<#
.SYNOPSIS

Displays a balloon tip notification in the system tray.

.DESCRIPTION

Displays a balloon tip notification in the system tray.

.PARAMETER BalloonTipText

Text of the balloon tip.

.PARAMETER BalloonTipTitle

Title of the balloon tip.

.PARAMETER BalloonTipIcon

Icon to be used. Options: 'Error', 'Info', 'None', 'Warning'. Default is: Info.

.PARAMETER BalloonTipTime

Time in milliseconds to display the balloon tip. Default: 10000.

.PARAMETER NoWait

Create the balloontip asynchronously. Default: $false

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.String

Returns the version of the specified file.

.EXAMPLE

Show-BalloonTip -BalloonTipText 'Installation Started' -BalloonTipTitle 'Application Name'

.EXAMPLE

Show-BalloonTip -BalloonTipIcon 'Info' -BalloonTipText 'Installation Started' -BalloonTipTitle 'Application Name' -BalloonTipTime 1000

.NOTES

For Windows 10 OS and above a Toast notification is displayed in place of a balloon tip if toast notifications are enabled in the XML config file.

.LINK

https://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory = $true, Position = 0)]
		[ValidateNotNullOrEmpty()]
		[String]$BalloonTipText,
		[Parameter(Mandatory = $false, Position = 1)]
		[ValidateNotNullorEmpty()]
		[String]$BalloonTipTitle = $installTitle,
		[Parameter(Mandatory = $false, Position = 2)]
		[ValidateSet('Error', 'Info', 'None', 'Warning')]
		[Windows.Forms.ToolTipIcon]$BalloonTipIcon = 'Info',
		[Parameter(Mandatory = $false, Position = 3)]
		[ValidateNotNullorEmpty()]
		[Int32]$BalloonTipTime = 10000,
		[Parameter(Mandatory = $false, Position = 4)]
		[Switch]$NoWait = $false
	)

	Begin {
		## Get the name of this function and write header
		[String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		## Skip balloon if in silent mode, disabled in the config or presentation is detected
		If (($deployModeSilent) -or (-not $configShowBalloonNotifications)) {
			Write-Log -Message "Bypassing Show-BalloonTip [Mode:$deployMode, Config Show Balloon Notifications:$configShowBalloonNotifications]. BalloonTipText:$BalloonTipText" -Source ${CmdletName}
			Return
		}
		If (Test-PowerPoint) {
			Write-Log -Message "Bypassing Show-BalloonTip [Mode:$deployMode, Presentation Detected:$true]. BalloonTipText:$BalloonTipText" -Source ${CmdletName}
			Return
		}
		## Dispose of previous balloon
		If (Test-Path -LiteralPath 'variable:notifyIcon') {
			Try {
				$script:notifyIcon.Dispose()
			} Catch {
			}
		}

		If (($envOSVersionMajor -lt 10) -or ($configToastDisable -eq $true)) {
			## NoWait - Create the balloontip icon asynchronously
			If ($NoWait) {
				Write-Log -Message "Displaying balloon tip notification asynchronously with message [$BalloonTipText]." -Source ${CmdletName}
				## Create a script block to display the balloon notification in a new PowerShell process so that we can wait to cleanly dispose of the balloon tip without having to make the deployment script wait
				## Scriptblock text has to be as short as possible because it is passed as a parameter to powershell
				## Don't strongly type parameter BalloonTipIcon as System.Drawing assembly not loaded yet in asynchronous scriptblock so will throw error
				[ScriptBlock]$notifyIconScriptBlock = {
					Param(
						[Parameter(Mandatory = $true, Position = 0)]
						[ValidateNotNullOrEmpty()]
						[String]$BalloonTipText,
						[Parameter(Mandatory = $false, Position = 1)]
						[ValidateNotNullorEmpty()]
						[String]$BalloonTipTitle,
						[Parameter(Mandatory = $false, Position = 2)]
						[ValidateSet('Error', 'Info', 'None', 'Warning')]
						$BalloonTipIcon = 'Info',
						[Parameter(Mandatory = $false, Position = 3)]
						[ValidateNotNullorEmpty()]
						[Int32]$BalloonTipTime,
						[Parameter(Mandatory = $false, Position = 4)]
						[ValidateNotNullorEmpty()]
						[String]$AppDeployLogoIcon
					)
					Add-Type -AssemblyName 'System.Windows.Forms', 'System.Drawing' -ErrorAction 'Stop'
					$BalloonTipIconText = [String]::Concat($BalloonTipTitle, ' - ', $BalloonTipText)
					If ($BalloonTipIconText.Length -gt 63) {
						$BalloonTipIconText = [String]::Concat($BalloonTipIconText.Substring(0, 60), '...')
					}
					[Windows.Forms.ToolTipIcon]$BalloonTipIcon = $BalloonTipIcon
					$script:notifyIcon = New-Object -TypeName 'System.Windows.Forms.NotifyIcon' -Property @{
						BalloonTipIcon  = $BalloonTipIcon
						BalloonTipText  = $BalloonTipText
						BalloonTipTitle = $BalloonTipTitle
						Icon            = New-Object -TypeName 'System.Drawing.Icon' -ArgumentList ($AppDeployLogoIcon)
						Text            = $BalloonTipIconText
						Visible         = $true
					}

					$script:notifyIcon.ShowBalloonTip($BalloonTipTime)
					Start-Sleep -Milliseconds ($BalloonTipTime)
					$script:notifyIcon.Dispose() }

				## Invoke a separate PowerShell process passing the script block as a command and associated parameters to display the balloon tip notification asynchronously
				Try {
					Execute-Process -Path "$PSHOME\powershell.exe" -Parameters "-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -Command & {$notifyIconScriptBlock} `'$BalloonTipText`' `'$BalloonTipTitle`' `'$BalloonTipIcon`' `'$BalloonTipTime`' `'$AppDeployLogoIcon`'" -NoWait -WindowStyle 'Hidden' -CreateNoWindow
				} Catch {
				}
			}
			## Otherwise create the balloontip icon synchronously
			Else {
				Write-Log -Message "Displaying balloon tip notification with message [$BalloonTipText]." -Source ${CmdletName}
				## Prepare Text - Cut it if longer than 63 chars
				$BalloonTipIconText = [String]::Concat($BalloonTipTitle, ' - ', $BalloonTipText)
				If ($BalloonTipIconText.Length -gt 63) {
					$BalloonTipIconText = [String]::Concat($BalloonTipIconText.Substring(0, 60), '...')
				}
				## Create the BalloonTip
				[Windows.Forms.ToolTipIcon]$BalloonTipIcon = $BalloonTipIcon
				$script:notifyIcon = New-Object -TypeName 'System.Windows.Forms.NotifyIcon' -Property @{
					BalloonTipIcon  = $BalloonTipIcon
					BalloonTipText  = $BalloonTipText
					BalloonTipTitle = $BalloonTipTitle
					Icon            = New-Object -TypeName 'System.Drawing.Icon' -ArgumentList ($AppDeployLogoIcon)
					Text            = $BalloonTipIconText
					Visible         = $true
				}
				## Display the balloon tip notification
				$script:notifyIcon.ShowBalloonTip($BalloonTipTime)
			}
		}
		# Otherwise use toast notification
		Else {
			$toastAppID = $appDeployToolkitName
			$toastAppDisplayName = $configToastAppName

			[scriptblock]$toastScriptBlock = {
				Param(
					[Parameter(Mandatory = $true, Position = 0)]
					[ValidateNotNullOrEmpty()]
					[String]$BalloonTipText,
					[Parameter(Mandatory = $false, Position = 1)]
					[ValidateNotNullorEmpty()]
					[String]$BalloonTipTitle,
					[Parameter(Mandatory = $false, Position = 2)]
					[ValidateNotNullorEmpty()]
					[String]$AppDeployLogoImage,
					[Parameter(Mandatory = $false, Position = 3)]
					[ValidateNotNullorEmpty()]
					[String]$toastAppID,
					[Parameter(Mandatory = $false, Position = 4)]
					[ValidateNotNullorEmpty()]
					[String]$toastAppDisplayName
				)

				# Check for required entries in registry for when using Powershell as application for the toast
				# Register the AppID in the registry for use with the Action Center, if required
				$regPathToastNotificationSettings = 'Registry::HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings'
				$regPathToastApp = 'Registry::HKEY_CURRENT_USER\Software\Classes\AppUserModelId'

				# Create the registry entries
				$null = New-Item -Path "$regPathToastNotificationSettings\$toastAppId" -Force
				# Make sure the app used with the action center is enabled
				$null = New-ItemProperty -Path "$regPathToastNotificationSettings\$toastAppId" -Name 'ShowInActionCenter' -Value 1 -PropertyType 'DWORD' -Force
				$null = New-ItemProperty -Path "$regPathToastNotificationSettings\$toastAppId" -Name 'Enabled' -Value 1 -PropertyType 'DWORD' -Force
				$null = New-ItemProperty -Path "$regPathToastNotificationSettings\$toastAppId" -Name 'SoundFile' -PropertyType 'STRING' -Force

				# Create the registry entries
				$null = New-Item -Path "$regPathToastApp\$toastAppId" -Force
				$null = New-ItemProperty -Path "$regPathToastApp\$toastAppId" -Name 'DisplayName' -Value "$($toastAppDisplayName)" -PropertyType 'STRING' -Force
				$null = New-ItemProperty -Path "$regPathToastApp\$toastAppId" -Name 'ShowInSettings' -Value 0 -PropertyType 'DWORD' -Force
				$null = New-ItemProperty -Path "$regPathToastApp\$toastAppId" -Name 'IconUri' -Value $appDeployLogoImage -PropertyType 'ExpandString' -Force
				$null = New-ItemProperty -Path "$regPathToastApp\$toastAppId" -Name 'IconBackgroundColor' -Value 0 -PropertyType 'ExpandString' -Force

				[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
				[Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null

				## Gets the Template XML so we can manipulate the values
				$Template = [Windows.UI.Notifications.ToastTemplateType]::ToastImageAndText01
				[xml] $ToastTemplate = ([Windows.UI.Notifications.ToastNotificationManager]::GetTemplateContent($Template).GetXml())
				[xml] $ToastTemplate = @"
<toast launch="app-defined-string">
    <visual>
        <binding template="ToastImageAndText02">
            <text id="1">$BalloonTipTitle</text>
            <text id="2">$BalloonTipText</text>
            <image id="1" src="file://$appDeployLogoImage" />
        </binding>
    </visual>
</toast>
"@

				$ToastXml = New-Object -TypeName Windows.Data.Xml.Dom.XmlDocument
				$ToastXml.LoadXml($ToastTemplate.OuterXml)

				$notifier = [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier($toastAppId)
				$notifier.Show($toastXml)

			}

			If ($ProcessNTAccount -eq $runAsActiveUser.NTAccount) {
				Write-Log -Message "Displaying toast notification with message [$BalloonTipText]." -Source ${CmdletName}
				Invoke-Command -ScriptBlock $toastScriptBlock -ArgumentList $BalloonTipText, $BalloonTipTitle, $AppDeployLogoImage, $toastAppID, $toastAppDisplayName
			} Else {
				## Invoke a separate PowerShell process as the current user passing the script block as a command and associated parameters to display the toast notification in the user context
				Try {
					Write-Log -Message "Displaying toast notification with message [$BalloonTipText] using Execute-ProcessAsUser." -Source ${CmdletName}
					$executeToastAsUserScript = "$loggedOnUserTempPath" + "$($appDeployToolkitName)-ToastNotification.ps1"
					Set-Content -Path $executeToastAsUserScript -Value $toastScriptBlock -Force
					Execute-ProcessAsUser -Path "$PSHOME\powershell.exe" -Parameters "-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -Command & { & `"`'$executeToastAsUserScript`' `'$BalloonTipText`' `'$BalloonTipTitle`' `'$AppDeployLogoImage`' `'$toastAppID`' `'$toastAppDisplayName`'`"; Exit `$LastExitCode }" -TempPath $loggedOnUserTempPath -Wait -RunLevel 'LeastPrivilege'
				} Catch {
				}
			}
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}
