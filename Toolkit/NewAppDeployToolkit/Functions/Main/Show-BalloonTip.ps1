#region Function Show-BalloonTip
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
.EXAMPLE
	Show-BalloonTip -BalloonTipText 'Installation Started' -BalloonTipTitle 'Application Name'
.EXAMPLE
	Show-BalloonTip -BalloonTipIcon 'Info' -BalloonTipText 'Installation Started' -BalloonTipTitle 'Application Name' -BalloonTipTime 1000
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true,Position=0)]
		[ValidateNotNullOrEmpty()]
		[string]$BalloonTipText,
		[Parameter(Mandatory=$false,Position=1)]
		[ValidateNotNullorEmpty()]
		[string]$BalloonTipTitle = $installTitle,
		[Parameter(Mandatory=$false,Position=2)]
		[ValidateSet('Error','Info','None','Warning')]
		[Windows.Forms.ToolTipIcon]$BalloonTipIcon = 'Info',
		[Parameter(Mandatory=$false,Position=3)]
		[ValidateNotNullorEmpty()]
		[int32]$BalloonTipTime = 10000,
		[Parameter(Mandatory=$false,Position=4)]
		[switch]$NoWait = $false
	)

	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionInfo -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		## Skip balloon if in silent mode, disabled in the config or presentation is detected
		$presentationDetected = Test-PowerPoint
		If (($deployModeSilent) -or (-not $configShowBalloonNotifications) -or $presentationDetected) { 
			Write-Log -Message "Bypassing Show-BalloonTip [Mode:$deployMode, Config Show Balloon Notifications:$configShowBalloonNotifications, Presentation Detected:$presentationDetected]. BalloonTipText:$BalloonTipText" -Source ${CmdletName}
			Return 
		}
		## Dispose of previous balloon
		If ($script:notifyIcon) { Try { $script:notifyIcon.Dispose() } Catch {} }
		## NoWait - Create the balloontip icon asynchronously
		If ($NoWait) {
			Write-Log -Message "Displaying balloon tip notification asynchronously with message [$BalloonTipText]." -Source ${CmdletName}
			## Create a script block to display the balloon notification in a new PowerShell process so that we can wait to cleanly dispose of the balloon tip without having to make the deployment script wait
			## Scriptblock text has to be as short as possible because it is passed as a parameter to powershell
			## Don't strongly type parameter BalloonTipIcon as System.Drawing assembly not loaded yet in asynchronous scriptblock so will throw error
			[scriptblock]$notifyIconScriptBlock = {
Param(
[Parameter(Mandatory=$true,Position=0)]
[ValidateNotNullOrEmpty()]
[string]$BalloonTipText,
[Parameter(Mandatory=$false,Position=1)]
[ValidateNotNullorEmpty()]
[string]$BalloonTipTitle,
[Parameter(Mandatory=$false,Position=2)]
[ValidateSet('Error','Info','None','Warning')]
$BalloonTipIcon, 
[Parameter(Mandatory=$false,Position=3)]
[ValidateNotNullorEmpty()]
[int32]$BalloonTipTime,
[Parameter(Mandatory=$false,Position=4)]
[ValidateNotNullorEmpty()]
[string]$AppDeployLogoIcon
)	
Add-Type -AssemblyName 'System.Windows.Forms','System.Drawing' -ErrorAction 'Stop'
$BalloonTipIconText = [String]::Concat($BalloonTipTitle,' - ',$BalloonTipText)
if ($BalloonTipIconText.Length -gt 63) { $BalloonTipIconText = [String]::Concat($BalloonTipIconText.Substring(0,60),'...') }
[Windows.Forms.ToolTipIcon]$BalloonTipIcon = $BalloonTipIcon
$script:notifyIcon = New-Object -TypeName 'System.Windows.Forms.NotifyIcon' -Property @{
BalloonTipIcon = $BalloonTipIcon
BalloonTipText = $BalloonTipText
BalloonTipTitle = $BalloonTipTitle
Icon = New-Object -TypeName 'System.Drawing.Icon' -ArgumentList $AppDeployLogoIcon
Text = $BalloonTipIconText
Visible = $true
}
$script:notifyIcon.ShowBalloonTip($BalloonTipTime)
Start-Sleep -Milliseconds ($BalloonTipTime)
$script:notifyIcon.Dispose() }
			## Invoke a separate PowerShell process passing the script block as a command and associated parameters to display the balloon tip notification asynchronously
			Try {
				Execute-Process -Path "$PSHOME\powershell.exe" -Parameters "-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -Command &{$notifyIconScriptBlock} `'$BalloonTipText`' `'$BalloonTipTitle`' `'$BalloonTipIcon`' `'$BalloonTipTime`' `'$AppDeployLogoIcon`'" -NoWait -WindowStyle 'Hidden' -CreateNoWindow
			}
			Catch { }
		}
		## Otherwise create the balloontip icon synchronously
		Else {
			Write-Log -Message "Displaying balloon tip notification with message [$BalloonTipText]." -Source ${CmdletName}
			## Prepare Text - Cut it if longer than 63 chars
			$BalloonTipIconText = [String]::Concat($BalloonTipTitle,' - ',$BalloonTipText)
			if ($BalloonTipIconText.Length -gt 63) { $BalloonTipIconText = [String]::Concat($BalloonTipIconText.Substring(0,60),'...') }
			## Create the BalloonTip
			[Windows.Forms.ToolTipIcon]$BalloonTipIcon = $BalloonTipIcon
			$script:notifyIcon = New-Object -TypeName 'System.Windows.Forms.NotifyIcon' -Property @{
				BalloonTipIcon = $BalloonTipIcon
				BalloonTipText = $BalloonTipText
				BalloonTipTitle = $BalloonTipTitle
				Icon = New-Object -TypeName 'System.Drawing.Icon' -ArgumentList $AppDeployLogoIcon
				Text = $BalloonTipIconText
				Visible = $true
			}
			## Display the balloon tip notification
			$script:notifyIcon.ShowBalloonTip($BalloonTipTime)
		}
	}
	End {
		Write-FunctionInfo -CmdletName ${CmdletName} -Footer
	}
}
#endregion
