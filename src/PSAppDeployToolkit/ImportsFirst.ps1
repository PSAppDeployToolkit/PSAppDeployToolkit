<#

.SYNOPSIS
PSAppDeployToolkit - This module script contains the PSADT core runtime and functions using by a Invoke-AppDeployToolkit.ps1 script.

.DESCRIPTION
This module can be directly imported from the command line via Import-Module, but it is usually imported by the Invoke-AppDeployToolkit.ps1 script.

This module can usually be updated to the latest version without impacting your per-application Invoke-AppDeployToolkit.ps1 scripts. Please check release notes before upgrading.

PSAppDeployToolkit is licensed under the GNU LGPLv3 License - (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).

This program is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the
Free Software Foundation, either version 3 of the License, or any later version. This program is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License
for more details. You should have received a copy of the GNU Lesser General Public License along with this program. If not, see <http://www.gnu.org/licenses/>.

.LINK
https://psappdeploytoolkit.com

#>

#-----------------------------------------------------------------------------
#
# MARK: Module Initialization Code
#
#-----------------------------------------------------------------------------

# Clock when the module import starts so we can track it.
[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'ModuleImportStart', Justification = "This variable is used within ImportsLast.ps1 and therefore cannot be seen here.")]
$ModuleImportStart = [System.DateTime]::Now

# Define modules needed to build out CommandTable.
$RequiredModules = [System.Collections.ObjectModel.ReadOnlyCollection[Microsoft.PowerShell.Commands.ModuleSpecification]]$(
    @{ ModuleName = 'CimCmdlets'; Guid = 'fb6cc51d-c096-4b38-b78d-0fed6277096a'; ModuleVersion = '1.0' }
    @{ ModuleName = 'Dism'; Guid = '389c464d-8b8d-48e9-aafe-6d8a590d6798'; ModuleVersion = '1.0' }
    @{ ModuleName = 'International'; Guid = '561544e6-3a83-4d24-b140-78ad771eaf10'; ModuleVersion = '1.0' }
    @{ ModuleName = 'Microsoft.PowerShell.Archive'; Guid = 'eb74e8da-9ae2-482a-a648-e96550fb8733'; ModuleVersion = '1.0' }
    @{ ModuleName = 'Microsoft.PowerShell.Management'; Guid = 'eefcb906-b326-4e99-9f54-8b4bb6ef3c6d'; ModuleVersion = '1.0' }
    @{ ModuleName = 'Microsoft.PowerShell.Security'; Guid = 'a94c8c7e-9810-47c0-b8af-65089c13a35a'; ModuleVersion = '1.0' }
    @{ ModuleName = 'Microsoft.PowerShell.Utility'; Guid = '1da87e53-152b-403e-98dc-74d7b4d63d59'; ModuleVersion = '1.0' }
    @{ ModuleName = 'NetAdapter'; Guid = '1042b422-63a8-4016-a6d6-293e19e8f8a6'; ModuleVersion = '1.0' }
    @{ ModuleName = 'ScheduledTasks'; Guid = '5378ee8e-e349-49bb-83b9-f3d9c396c0a6'; ModuleVersion = '1.0' }
)

# Build out lookup table for all cmdlets used within module, starting with the core cmdlets.
$CommandTable = [ordered]@{}; $ExecutionContext.SessionState.InvokeCommand.GetCmdlets() | & { process { if ($_.PSSnapIn -and $_.PSSnapIn.Name.Equals('Microsoft.PowerShell.Core') -and $_.PSSnapIn.IsDefault) { $CommandTable.Add($_.Name, $_) } } }
(& $CommandTable.'Import-Module' -FullyQualifiedName $RequiredModules -Global -PassThru -ErrorAction Stop).ExportedCommands.Values | & { process { $CommandTable.Add($_.Name, $_) } }

# Set required variables to ensure module functionality.
& $CommandTable.'New-Variable' -Name ErrorActionPreference -Value ([System.Management.Automation.ActionPreference]::Stop) -Option Constant -Force
& $CommandTable.'New-Variable' -Name InformationPreference -Value ([System.Management.Automation.ActionPreference]::Continue) -Option Constant -Force
& $CommandTable.'New-Variable' -Name ProgressPreference -Value ([System.Management.Automation.ActionPreference]::SilentlyContinue) -Option Constant -Force

# Ensure module operates under the strictest of conditions.
& $CommandTable.'Set-StrictMode' -Version 3

# Throw hard if there's already a PSADT assembly loaded from a different location.
if (($assembly = [System.AppDomain]::CurrentDomain.GetAssemblies() | & { process { if ([System.IO.Path]::GetFileName($_.Location).Equals('PSADT.dll')) { return $_ } } } | & $CommandTable.'Select-Object' -First 1) -and !$assembly.Location.StartsWith($Script:PSScriptRoot))
{
    & $CommandTable.'Write-Error' -ErrorRecord ([System.Management.Automation.ErrorRecord]::new(
            [System.InvalidOperationException]::new("A duplicate PSAppDeployToolkit module is already loaded. Please restart PowerShell and try again."),
            'ConflictingModuleLoaded',
            [System.Management.Automation.ErrorCategory]::InvalidOperation,
            $assembly
        ))
}

# Set the process as HiDPI so long as we're in a real console.
if ($Host.Name.Equals('ConsoleHost'))
{
    try
    {
        [PSADT.GUI.UiAutomation]::SetProcessDpiAwarenessForOSVersion()
    }
    catch
    {
        $null = $null
    }
}

# All WinForms-specific initialization code.
try
{
    [System.Windows.Forms.Application]::EnableVisualStyles()
    [System.Windows.Forms.Application]::SetCompatibleTextRenderingDefault($false)
}
catch
{
    $null = $null
}

# Remove any previous functions that may have been defined.
if ($MyInvocation.MyCommand.Name.Equals('PSAppDeployToolkit.psm1'))
{
    & $CommandTable.'New-Variable' -Name FunctionNames -Option Constant -Value ($MyInvocation.MyCommand.ScriptBlock.Ast.EndBlock.Statements | & { process { if ($_ -is [System.Management.Automation.Language.FunctionDefinitionAst]) { return $_.Name } } })
    & $CommandTable.'New-Variable' -Name FunctionPaths -Option Constant -Value ($FunctionNames -replace '^', 'Microsoft.PowerShell.Core\Function::')
    & $CommandTable.'Remove-Item' -LiteralPath $FunctionPaths -Force -ErrorAction Ignore
}
