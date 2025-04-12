<#

.SYNOPSIS
PSAppDeployToolkit - Provides the ability to extend and customize the toolkit by adding your own functions that can be re-used.

.DESCRIPTION
This script is a template that allows you to extend the toolkit with your own custom functions.

This script is dot-sourced by the AppDeployToolkitMain.ps1 script which contains the logic and functions required to install or uninstall an application.

.INPUTS
None. You cannot pipe objects to this script.

.OUTPUTS
None. This script does not generate any output.

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
