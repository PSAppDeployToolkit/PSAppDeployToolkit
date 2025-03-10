<#

.SYNOPSIS
PSAppDeployToolkit - This module script contains the PSADT core runtime and functions using by a Invoke-AppDeployToolkit.ps1 script.

.DESCRIPTION
This module can be directly imported from the command line via Import-Module, but it is usually imported by the Invoke-AppDeployToolkit.ps1 script.

This module can usually be updated to the latest version without impacting your per-application Invoke-AppDeployToolkit.ps1 scripts. Please check release notes before upgrading.

PSAppDeployToolkit is licensed under the GNU LGPLv3 License - © 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).

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

# Throw if this psm1 file isn't being imported via our manifest.
if (!([System.Environment]::StackTrace.Split("`n") -like '*Microsoft.PowerShell.Commands.ModuleCmdletBase.LoadModuleManifest(*'))
{
    throw [System.Management.Automation.ErrorRecord]::new(
        [System.InvalidOperationException]::new("This module must be imported via its .psd1 file, which is recommended for all modules that supply a .psd1 file."),
        'ModuleImportError',
        [System.Management.Automation.ErrorCategory]::InvalidOperation,
        $MyInvocation.MyCommand.ScriptBlock.Module
    )
}

# Clock when the module import starts so we can track it.
[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'ModuleImportStart', Justification = "This variable is used within ImportsLast.ps1 and therefore cannot be seen here.")]
$ModuleImportStart = [System.DateTime]::Now

# Rethrowing caught exceptions makes the error output from Import-Module look better.
try
{
    # Determine if we're doing a minimal startup (i.e. on Linux but want to allow New-ADTTemplate to funcion).
    $MinimumStartup = $PSEdition.Equals('Core') -and !$IsWindows

    # Build out lookup table for all cmdlets used within module, starting with the core cmdlets.
    $CommandTable = [System.Collections.Generic.Dictionary[System.String, System.Management.Automation.CommandInfo]]::new()
    $ExecutionContext.SessionState.InvokeCommand.GetCmdlets() | & { process { if ($_.PSSnapIn -and $_.PSSnapIn.Name.Equals('Microsoft.PowerShell.Core') -and $_.PSSnapIn.IsDefault) { $CommandTable.Add($_.Name, $_) } } }

    # Expand command lookup table with cmdlets used through this module.
    & {
        $RequiredModules = $(
            if (!$MinimumStartup)
            {
                @{ ModuleName = 'CimCmdlets'; Guid = 'fb6cc51d-c096-4b38-b78d-0fed6277096a'; ModuleVersion = '1.0' }
                @{ ModuleName = 'Dism'; Guid = '389c464d-8b8d-48e9-aafe-6d8a590d6798'; ModuleVersion = '1.0' }
                @{ ModuleName = 'International'; Guid = '561544e6-3a83-4d24-b140-78ad771eaf10'; ModuleVersion = '1.0' }
                @{ ModuleName = 'NetAdapter'; Guid = '1042b422-63a8-4016-a6d6-293e19e8f8a6'; ModuleVersion = '1.0' }
                @{ ModuleName = 'ScheduledTasks'; Guid = '5378ee8e-e349-49bb-83b9-f3d9c396c0a6'; ModuleVersion = '1.0' }
            }
            @{ ModuleName = 'Microsoft.PowerShell.Archive'; Guid = 'eb74e8da-9ae2-482a-a648-e96550fb8733'; ModuleVersion = '1.0' }
            @{ ModuleName = 'Microsoft.PowerShell.Management'; Guid = 'eefcb906-b326-4e99-9f54-8b4bb6ef3c6d'; ModuleVersion = '1.0' }
            @{ ModuleName = 'Microsoft.PowerShell.Security'; Guid = 'a94c8c7e-9810-47c0-b8af-65089c13a35a'; ModuleVersion = '1.0' }
            @{ ModuleName = 'Microsoft.PowerShell.Utility'; Guid = '1da87e53-152b-403e-98dc-74d7b4d63d59'; ModuleVersion = '1.0' }
        )
        (Import-Module -FullyQualifiedName $RequiredModules -Global -Force -PassThru -ErrorAction Stop).ExportedCommands.Values | & { process { $CommandTable.Add($_.Name, $_) } }
    }

    # Set required variables to ensure module functionality.
    New-Variable -Name ErrorActionPreference -Value ([System.Management.Automation.ActionPreference]::Stop) -Option Constant -Force
    New-Variable -Name InformationPreference -Value ([System.Management.Automation.ActionPreference]::Continue) -Option Constant -Force
    New-Variable -Name ProgressPreference -Value ([System.Management.Automation.ActionPreference]::SilentlyContinue) -Option Constant -Force

    # Ensure module operates under the strictest of conditions.
    Set-StrictMode -Version 3

    # Throw if any previous version of the unofficial PSADT module is found on the system.
    if (Get-Module -FullyQualifiedName @{ ModuleName = 'PSADT'; Guid = '41b2dd67-8447-4c66-b08a-f0bd0d5458b9'; ModuleVersion = '1.0' } -ListAvailable -Refresh)
    {
        Write-Warning -Message "This module should not be used while the unofficial v3 PSADT module is installed."
    }

    # Store build information pertaining to this module's state.
    New-Variable -Name Module -Option Constant -Force -Value ([ordered]@{
            Manifest = Import-LocalizedData -BaseDirectory $PSScriptRoot -FileName 'PSAppDeployToolkit.psd1'
            Assemblies = (Get-ChildItem -LiteralPath $PSScriptRoot\lib -File -Filter PSADT*.dll).FullName
            Compiled = $MyInvocation.MyCommand.Name.Equals('PSAppDeployToolkit.psm1')
            Signed = $(if (!$MinimumStartup) { (Get-AuthenticodeSignature -LiteralPath $MyInvocation.MyCommand.Path).Status.Equals([System.Management.Automation.SignatureStatus]::Valid) })
        }).AsReadOnly()

    # Perform remaining Windows setup.
    if (!$MinimumStartup)
    {
        # Import our assemblies, factoring in whether they're on a network share or not.
        $Module.Assemblies | & {
            begin
            {
                # Cache loaded assemblies to test whether they're already loaded.
                $domainAssemblies = [System.AppDomain]::CurrentDomain.GetAssemblies()

                # Determine whether we're on a network location.
                $isNetworkLocation = [System.Uri]::new($PSScriptRoot).IsUnc -or (($PSScriptRoot -match '^[A-Za-z]:\\') -and [System.IO.DriveInfo]::new($Matches.0).DriveType.Equals([System.IO.DriveType]::Network))

                # Add in system assemblies.
                Add-Type -AssemblyName @(
                    'System.ServiceProcess'
                    'System.Drawing'
                    'System.Windows.Forms'
                    'PresentationCore'
                    'PresentationFramework'
                    'WindowsBase'
                )
            }

            process
            {
                # Test whether the assembly is already loaded.
                if (($existingAssembly = $domainAssemblies | & { process { if ([System.IO.Path]::GetFileName($_.Location).Equals([System.IO.Path]::GetFileName($args[0]))) { return $_ } } } $_ | Select-Object -First 1))
                {
                    # Test the loaded assembly for SHA256 hash equality, returning early if the assembly is OK.
                    if (!(Get-FileHash -LiteralPath $existingAssembly.Location).Hash.Equals((Get-FileHash -LiteralPath $_).Hash))
                    {
                        throw [System.Management.Automation.ErrorRecord]::new(
                            [System.InvalidOperationException]::new("A PSAppDeployToolkit assembly of a different file hash is already loaded. Please restart PowerShell and try again."),
                            'ConflictingModuleLoaded',
                            [System.Management.Automation.ErrorCategory]::InvalidOperation,
                            $existingAssembly
                        )
                    }
                    return
                }

                # If we're on a compiled build, confirm the DLLs are signed before proceeding.
                if ($Module.Signed -and !($badFile = Get-AuthenticodeSignature -LiteralPath $_).Status.Equals([System.Management.Automation.SignatureStatus]::Valid))
                {
                    throw [System.Management.Automation.ErrorRecord]::new(
                        [System.InvalidOperationException]::new("The assembly [$_] has an invalid digital signature and cannot be loaded."),
                        'ADTAssemblyFileSignatureError',
                        [System.Management.Automation.ErrorCategory]::SecurityError,
                        $badFile
                    )
                }

                # If loading from an SMB path, load unsafely. This is OK because in signed (release) modules, we're validating the signature above.
                if ($isNetworkLocation)
                {
                    [System.Reflection.Assembly]::UnsafeLoadFrom($_)
                }
                else
                {
                    Add-Type -LiteralPath $_
                }
            }
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
    }

    # Remove any previous functions that may have been defined.
    if ($Module.Compiled)
    {
        New-Variable -Name FunctionPaths -Option Constant -Value ($MyInvocation.MyCommand.ScriptBlock.Ast.EndBlock.Statements | & { process { if ($_ -is [System.Management.Automation.Language.FunctionDefinitionAst]) { return "Microsoft.PowerShell.Core\Function::$($_.Name)" } } })
        Remove-Item -LiteralPath $FunctionPaths -Force -ErrorAction Ignore
    }
}
catch
{
    throw
}
