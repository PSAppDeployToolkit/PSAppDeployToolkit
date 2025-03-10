<#

.SYNOPSIS
PSAppDeployToolkit - Provides the ability to extend and customize the toolkit by adding your own functions that can be re-used.

.DESCRIPTION
This script is a template that allows you to extend the toolkit with your own custom functions.

This script is dot-sourced by the AppDeployToolkitMain.ps1 script which contains the logic and functions required to install or uninstall an application.

PSAppDeployToolkit is licensed under the GNU LGPLv3 License - © 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).

This program is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the
Free Software Foundation, either version 3 of the License, or any later version. This program is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License
for more details. You should have received a copy of the GNU Lesser General Public License along with this program. If not, see <http://www.gnu.org/licenses/>.

.INPUTS
None. You cannot pipe objects to this script.

.OUTPUTS
None. This script does not generate any output.

.LINK
https://psappdeploytoolkit.com

#>

##*===============================================
##* MARK: VARIABLE DECLARATION
##*===============================================


##*===============================================
##* MARK: FUNCTION LISTINGS
##*===============================================


##*===============================================
##* MARK: SCRIPT BODY
##*===============================================

if ((Test-Path -LiteralPath Microsoft.PowerShell.Core\Variable::scriptParentPath) -and $scriptParentPath)
{
    Write-ADTLogEntry -Message "Script [$($MyInvocation.MyCommand.Definition)] dot-source invoked by [$(((Get-Variable -Name MyInvocation).Value).ScriptName)]" -ScriptSection Initialization
}
else
{
    Write-ADTLogEntry -Message "Script [$($MyInvocation.MyCommand.Definition)] invoked directly" -ScriptSection Initialization
}
