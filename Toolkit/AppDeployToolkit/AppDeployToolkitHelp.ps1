<#
.SYNOPSIS
	Displays a graphical console to browse the help for the App Deployment Toolkit functions
.DESCRIPTION
	Displays a graphical console to browse the help for the App Deployment Toolkit functions
.EXAMPLE
	AppDeployToolkitHelp.ps1
.NOTES
.LINK
	Http://psappdeploytoolkit.codeplex.com
"#>

#*===============================================
#* VARIABLE DECLARATION
#*===============================================

# Variables: Script
$appDeployHelpScriptFriendlyName = "App Deploy Toolkit Help"
$appDeployHelpScriptVersion = "2.0.0"
$appDeployHelpScriptDate = "08/07/2013"

# Variables: Environment
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Definition
# Dot source the App Deploy Toolkit Functions
."$scriptDirectory\AppDeployToolkitMain.ps1" -DisableLogging

#*=============================================
#* END VARIABLE DECLARATION
#*=============================================

#*=============================================
#* FUNCTION LISTINGS
#*=============================================

Function Show-HelpConsole {

#region Import the Assemblies
[reflection.assembly]::loadwithpartialname("System.Windows.Forms") | Out-Null
[reflection.assembly]::loadwithpartialname("System.Drawing") | Out-Null
#endregion

#region Generated Form Objects
$HelpForm = New-Object System.Windows.Forms.Form
$HelpListBox = New-Object System.Windows.Forms.ListBox
$HelpTextBox = New-Object System.Windows.Forms.RichTextBox
$InitialFormWindowState = New-Object System.Windows.Forms.FormWindowState
#endregion Generated Form Objects

#region Generated Form Code
$System_Drawing_Size = New-Object System.Drawing.Size
$System_Drawing_Size.Height = 665
$System_Drawing_Size.Width = 957
$HelpForm.ClientSize = $System_Drawing_Size
$HelpForm.DataBindings.DefaultDataSourceUpdateMode = 0
$HelpForm.Name = "HelpForm"
$HelpForm.Text = "PowerShell App Deployment Toolkit Help Console"
$HelpForm.WindowState = "Normal"
$HelpForm.ShowInTaskbar = $true
$HelpForm.FormBorderStyle = 'Fixed3D'
$HelpForm.MaximizeBox = $false
$HelpForm.Icon = New-Object System.Drawing.Icon ($AppDeployLogoIcon)

$HelpListBox.Anchor = 7
$HelpListBox.BorderStyle = 1
$HelpListBox.DataBindings.DefaultDataSourceUpdateMode = 0
$HelpListBox.Font = New-Object System.Drawing.Font("Microsoft Sans Serif",9.75,1,3,1)
$HelpListBox.FormattingEnabled = $True
$HelpListBox.ItemHeight = 16
$System_Drawing_Point = New-Object System.Drawing.Point
$System_Drawing_Point.X = 0
$System_Drawing_Point.Y = 0
$HelpListBox.Location = $System_Drawing_Point
$HelpListBox.Name = "HelpListBox"
$System_Drawing_Size = New-Object System.Drawing.Size
$System_Drawing_Size.Height = 658
$System_Drawing_Size.Width = 271
$HelpListBox.Size = $System_Drawing_Size
$HelpListBox.Sorted = $True
$HelpListBox.TabIndex = 2

$HelpListBox.add_SelectedIndexChanged(
	{
	$HelpTextBox.Text = $(Get-Help -Name $($HelpListBox.SelectedItems) -Detailed | Out-String)
	}
)

$helpFunctions = Get-Command -CommandType Function | Where {$_.HelpUri -match "psappdeploytoolkit" -and $_.Definition -notmatch "internal script function"} | Select Name -ExpandProperty Name
Foreach ($helpFunction in $helpFunctions) {
	$HelpListBox.Items.Add($helpFunction) | Out-Null 
}

$HelpForm.Controls.Add($HelpListBox)

$HelpTextBox.Anchor = 11
$HelpTextBox.BorderStyle = 1
$HelpTextBox.DataBindings.DefaultDataSourceUpdateMode = 0
$HelpTextBox.Font = New-Object System.Drawing.Font("Microsoft Sans Serif",8.5,0,3,1)
$HelpTextBox.ForeColor = [System.Drawing.Color]::FromArgb(255,0,0,0)
$System_Drawing_Point = New-Object System.Drawing.Point
$System_Drawing_Point.X = 277
$System_Drawing_Point.Y = 0
$HelpTextBox.Location = $System_Drawing_Point
$HelpTextBox.Name = "HelpTextBox"
$HelpTextBox.ReadOnly = $True
$System_Drawing_Size = New-Object System.Drawing.Size
$System_Drawing_Size.Height = 658
$System_Drawing_Size.Width = 680
$HelpTextBox.Size = $System_Drawing_Size
$HelpTextBox.TabIndex = 1
$HelpTextBox.Text = ""

$HelpForm.Controls.Add($HelpTextBox)

#endregion Generated Form Code

#Save the initial state of the form
$InitialFormWindowState = $HelpForm.WindowState
#Init the OnLoad event to correct the initial state of the form
$HelpForm.add_Load($OnLoadForm_StateCorrection)
#Show the Form
$HelpForm.ShowDialog()| Out-Null

} #End Function



#*=============================================
#* END FUNCTION LISTINGS
#*=============================================

#*=============================================
#* SCRIPT BODY
#*=============================================

Write-Log "Loading $appDeployHelpScriptFriendlyName console..."

# Show the help console
Show-HelpConsole

Write-Log "$appDeployHelpScriptFriendlyName console closed."