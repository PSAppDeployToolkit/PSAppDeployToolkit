<#
.SYNOPSIS

PSApppDeployToolkit - Displays a graphical console to browse the help for the App Deployment Toolkit functions.

.DESCRIPTION

Displays a graphical console to browse the help for the App Deployment Toolkit functions

The script dot-sources the AppDeployToolkitMain.ps1 script which contains the logic and functions required to install or uninstall an application.

PSApppDeployToolkit is licensed under the GNU LGPLv3 License - (C) 2023 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham and Muhammad Mashwani).

This program is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the
Free Software Foundation, either version 3 of the License, or any later version. This program is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License
for more details. You should have received a copy of the GNU Lesser General Public License along with this program. If not, see <http://www.gnu.org/licenses/>.

.EXAMPLE

powershell.exe -File .\AppDeployToolkitHelp.ps1

.INPUTS

None

You cannot pipe objects to this script.

.OUTPUTS

None

This script does not generate any output.

.NOTES

.LINK

https://psappdeploytoolkit.com
#>


##*===============================================
##* VARIABLE DECLARATION
##*===============================================

## Variables: Script
[string]$appDeployToolkitHelpName = 'PSAppDeployToolkitHelp'
[string]$appDeployHelpScriptFriendlyName = 'App Deploy Toolkit Help'
[version]$appDeployHelpScriptVersion = [version]'3.9.2'
[string]$appDeployHelpScriptDate = '02/02/2023'

## Variables: Environment
[string]$scriptDirectory = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
#  Dot source the App Deploy Toolkit Functions
. "$scriptDirectory\AppDeployToolkitMain.ps1" -DisableLogging
. "$scriptDirectory\AppDeployToolkitExtensions.ps1"
##*===============================================
##* END VARIABLE DECLARATION
##*===============================================

##*===============================================
##* FUNCTION LISTINGS
##*===============================================

Function Show-HelpConsole {
    ## Import the Assemblies
    Add-Type -AssemblyName 'System.Windows.Forms' -ErrorAction 'Stop'
    Add-Type -AssemblyName System.Drawing -ErrorAction 'Stop'

    ## Get the default font to use in the user interface
    [System.Drawing.Font]$defaultFont = [System.Drawing.SystemFonts]::DefaultFont

    ## Form Objects
    $HelpForm = New-Object -TypeName 'System.Windows.Forms.Form'
    $HelpListBox = New-Object -TypeName 'System.Windows.Forms.ListBox'
    $HelpTextBox = New-Object -TypeName 'System.Windows.Forms.RichTextBox'
    $InitialFormWindowState = New-Object -TypeName 'System.Windows.Forms.FormWindowState'

    ## Form Code
    $System_Drawing_Size = New-Object -TypeName 'System.Drawing.Size'
    $System_Drawing_Size.Height = 665
    $System_Drawing_Size.Width = 957
    $HelpForm.ClientSize = $System_Drawing_Size
    $HelpForm.DataBindings.DefaultDataSourceUpdateMode = 0
    $HelpForm.Name = 'HelpForm'
    $HelpForm.Text = 'PowerShell App Deployment Toolkit Help Console'
    $HelpForm.WindowState = 'Normal'
    $HelpForm.ShowInTaskbar = $true
    $HelpForm.FormBorderStyle = 'Fixed3D'
    $HelpForm.MaximizeBox = $false
    $HelpForm.AutoSize = $true
    $HelpForm.AutoScaleMode = 'Font'
    $HelpForm.AutoScaleDimensions = New-Object System.Drawing.SizeF(6, 13) #Set as if using 96 DPI
    $HelpForm.Icon = New-Object -TypeName 'System.Drawing.Icon' -ArgumentList $AppDeployLogoIcon
    $HelpListBox.Anchor = 7
    $HelpListBox.BorderStyle = 1
    $HelpListBox.DataBindings.DefaultDataSourceUpdateMode = 0
    $HelpListBox.Font = "$($defaultFont.Name), $($defaultFont.Size + 1), style=Regular"
    $HelpListBox.FormattingEnabled = $true
    $HelpListBox.ItemHeight = 16
    $System_Drawing_Point = New-Object -TypeName 'System.Drawing.Point'
    $System_Drawing_Point.X = 0
    $System_Drawing_Point.Y = 0
    $HelpListBox.Location = $System_Drawing_Point
    $HelpListBox.Name = 'HelpListBox'
    $System_Drawing_Size = New-Object -TypeName 'System.Drawing.Size'
    $System_Drawing_Size.Height = 658
    $System_Drawing_Size.Width = 271
    $HelpListBox.Size = $System_Drawing_Size
    $HelpListBox.Sorted = $true
    $HelpListBox.TabIndex = 2
    $HelpListBox.add_SelectedIndexChanged({ $HelpTextBox.Text = Get-Help -Name $HelpListBox.SelectedItem -Full | Out-String })
    $helpFunctions = Get-Command -CommandType 'Function' | Where-Object { ($_.HelpUri -match 'psappdeploytoolkit') -and ($_.Definition -notmatch 'internal script function') } | Select-Object -ExpandProperty Name
    $null = $HelpListBox.Items.AddRange($helpFunctions)
    $HelpForm.Controls.Add($HelpListBox)
    $HelpTextBox.Anchor = 11
    $HelpTextBox.BorderStyle = 1
    $HelpTextBox.DataBindings.DefaultDataSourceUpdateMode = 0
    $HelpTextBox.Font = "$($defaultFont.Name), $($defaultFont.Size), style=Regular"
    $HelpTextBox.ForeColor = [System.Drawing.Color]::FromArgb(255, 0, 0, 0)
    $System_Drawing_Point = New-Object -TypeName System.Drawing.Point
    $System_Drawing_Point.X = 277
    $System_Drawing_Point.Y = 0
    $HelpTextBox.Location = $System_Drawing_Point
    $HelpTextBox.Name = 'HelpTextBox'
    $HelpTextBox.ReadOnly = $True
    $System_Drawing_Size = New-Object -TypeName 'System.Drawing.Size'
    $System_Drawing_Size.Height = 658
    $System_Drawing_Size.Width = 680
    $HelpTextBox.Size = $System_Drawing_Size
    $HelpTextBox.TabIndex = 1
    $HelpTextBox.Text = ''
    $HelpForm.Controls.Add($HelpTextBox)

    ## Save the initial state of the form
    $InitialFormWindowState = $HelpForm.WindowState
    ## Init the OnLoad event to correct the initial state of the form
    $HelpForm.add_Load($OnLoadForm_StateCorrection)
    ## Show the Form
    $null = $HelpForm.ShowDialog()
}

##*===============================================
##* END FUNCTION LISTINGS
##*===============================================

##*===============================================
##* SCRIPT BODY
##*===============================================

Write-Log -Message "Load [$appDeployHelpScriptFriendlyName] console..." -Source $appDeployToolkitHelpName

## Show the help console
Show-HelpConsole

Write-Log -Message "[$appDeployHelpScriptFriendlyName] console closed." -Source $appDeployToolkitHelpName

##*===============================================
##* END SCRIPT BODY
##*===============================================
