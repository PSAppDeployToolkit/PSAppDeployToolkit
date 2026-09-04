<#

.SYNOPSIS
PSAppDeployToolkit - This module script contains the PSADT core runtime and functions using by a Invoke-AppDeployToolkit.ps1 script.

.DESCRIPTION
This module can be directly imported from the command line via Import-Module, but it is usually imported by the Invoke-AppDeployToolkit.ps1 script.

This module can usually be updated to the latest version without impacting your per-application Invoke-AppDeployToolkit.ps1 scripts. Please check release notes before upgrading.

PSAppDeployToolkit is licensed under the GNU LGPLv3 License - © 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).

This program is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the
Free Software Foundation, either version 3 of the License, or any later version. This program is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License
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
if (!([System.Environment]::StackTrace.Split(0x0A) -like '*Microsoft.PowerShell.Commands.ModuleCmdletBase.LoadModuleManifest(*'))
{
    throw [System.Management.Automation.ErrorRecord]::new(
        [System.InvalidOperationException]::new("This module must be imported via its .psd1 file, which is recommended for any module that provides such a file."),
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
    # Build out lookup table for all cmdlets used within module.
    $CommandTable = [System.Collections.Generic.Dictionary[System.String, System.Management.Automation.CommandInfo]]::new()
    $ExecutionContext.SessionState.InvokeCommand.GetCmdlets() | & { process { if ($_.PSSnapIn -and $_.PSSnapIn.Name.Equals('Microsoft.PowerShell.Core') -and $_.PSSnapIn.IsDefault) { $CommandTable.Add($_.Name, $_) } } }
    [System.Collections.ObjectModel.ReadOnlyCollection[System.Management.Automation.PSModuleInfo]]$ImportedModules = Import-Module -Global -Force -PassThru -ErrorAction Stop -FullyQualifiedName $(
        @{ ModuleName = 'Microsoft.PowerShell.Archive'; Guid = 'eb74e8da-9ae2-482a-a648-e96550fb8733'; ModuleVersion = '1.0' }
        @{ ModuleName = 'Microsoft.PowerShell.Management'; Guid = 'eefcb906-b326-4e99-9f54-8b4bb6ef3c6d'; ModuleVersion = '1.0' }
        @{ ModuleName = 'Microsoft.PowerShell.Security'; Guid = 'a94c8c7e-9810-47c0-b8af-65089c13a35a'; ModuleVersion = '1.0' }
        @{ ModuleName = 'Microsoft.PowerShell.Utility'; Guid = '1da87e53-152b-403e-98dc-74d7b4d63d59'; ModuleVersion = '1.0' }
        @{ ModuleName = 'CimCmdlets'; Guid = 'fb6cc51d-c096-4b38-b78d-0fed6277096a'; ModuleVersion = '1.0' }
        @{ ModuleName = 'Dism'; Guid = '389c464d-8b8d-48e9-aafe-6d8a590d6798'; ModuleVersion = '1.0' }
        @{ ModuleName = 'International'; Guid = '561544e6-3a83-4d24-b140-78ad771eaf10'; ModuleVersion = '1.0' }
        @{ ModuleName = 'NetAdapter'; Guid = '1042b422-63a8-4016-a6d6-293e19e8f8a6'; ModuleVersion = '1.0' }
        @{ ModuleName = 'ScheduledTasks'; Guid = '5378ee8e-e349-49bb-83b9-f3d9c396c0a6'; ModuleVersion = '1.0' }
    )
    $ImportedModules.ExportedCommands.Values | & {
        process
        {
            if (!$_.CommandType.Equals([System.Management.Automation.CommandTypes]::Alias))
            {
                $CommandTable.Add($_.Name, $_)
            }
        }
    }

    # Set required variables to ensure module functionality.
    New-Variable -Name ErrorActionPreference -Value ([System.Management.Automation.ActionPreference]::Stop) -Option Constant -Force
    New-Variable -Name InformationPreference -Value ([System.Management.Automation.ActionPreference]::Continue) -Option Constant -Force
    New-Variable -Name ProgressPreference -Value ([System.Management.Automation.ActionPreference]::SilentlyContinue) -Option Constant -Force
    New-Variable -Name ImportedModules -Value $ImportedModules -Option Constant -Force

    # Ensure module operates under the strictest of conditions.
    Set-StrictMode -Version 3

    # Store the module info in a variable for further usage.
    if (!(Get-Variable -Name ModuleInfo -ErrorAction Ignore))
    {
        New-Variable -Name ModuleInfo -Option Constant -Value $MyInvocation.MyCommand.ScriptBlock.Module -Force
    }

    # Store build information pertaining to this module's state.
    New-Variable -Name Module -Option Constant -Force -Value ([ordered]@{
            Manifest = Import-LocalizedData -BaseDirectory ([System.Management.Automation.WildcardPattern]::Escape($PSScriptRoot)) -FileName PSAppDeployToolkit.psd1
            Assemblies = [System.Collections.ObjectModel.ReadOnlyCollection[System.String]]$(if (!$PSVersionTable.PSEdition.Equals('Desktop'))
                {
                    "$PSScriptRoot\lib\net8.0\PSAppDeployToolkit.dll", "$PSScriptRoot\lib\net8.0\PSADT.Interop.dll", "$PSScriptRoot\lib\net8.0\PSADT.dll", "$PSScriptRoot\lib\net8.0\PSADT.UserInterface.dll", "$PSScriptRoot\lib\net8.0\PSADT.ClientServer.Server.dll", "$PSScriptRoot\lib\net8.0\Microsoft.Windows.SDK.NET.dll", "$PSScriptRoot\lib\net8.0\PSADT.WindowsRuntime.dll"
                }
                else
                {
                    "$PSScriptRoot\lib\net472\PSAppDeployToolkit.dll", "$PSScriptRoot\lib\net472\PSADT.Interop.dll", "$PSScriptRoot\lib\net472\PSADT.dll", "$PSScriptRoot\lib\net472\PSADT.UserInterface.dll", "$PSScriptRoot\lib\net472\PSADT.ClientServer.Server.dll", "$PSScriptRoot\lib\net472\PSADT.WindowsRuntime.dll"
                })
            Compiled = $MyInvocation.MyCommand.Name.Equals('PSAppDeployToolkit.psm1')
            Signed = (Get-AuthenticodeSignature -LiteralPath $MyInvocation.MyCommand.Path).Status.Equals([System.Management.Automation.SignatureStatus]::Valid)
        }).AsReadOnly()

    # Import our assemblies, factoring in whether they're on a network share or not.
    $(if ($PSVersionTable.PSEdition.Equals('Desktop')) { "$PSScriptRoot\lib\net472\System.Collections.Immutable.dll" } $Module.Assemblies) | & {
        begin
        {
            # Cache loaded assemblies to test whether they're already loaded.
            $domainAssemblies = [System.AppDomain]::CurrentDomain.GetAssemblies()

            # Determine whether we're on a network location.
            $isNetworkLocation = [System.Uri]::new($PSScriptRoot).IsUnc -or (($PSScriptRoot -match '^[A-Za-z]:\\') -and [System.IO.DriveInfo]::new($Matches.0).DriveType.Equals([System.IO.DriveType]::Network))

            # Add in system assemblies.
            Add-Type -AssemblyName @(
                'System.ServiceProcess'
            )
        }

        process
        {
            # Test whether the assembly is already loaded.
            if (!$_.EndsWith('\System.Collections.Immutable.dll') -and ($existingAssembly = $domainAssemblies | & { process { if (!$_.IsDynamic -and [System.IO.Path]::GetFileName($_.Location).Equals([System.IO.Path]::GetFileName($args[0]))) { return $_ } } } $_ | Select-Object -First 1))
            {
                # Test the loaded assembly for SHA256 hash equality, returning early if the assembly is OK.
                if (!(Get-FileHash -LiteralPath $existingAssembly.Location).Hash.Equals((Get-FileHash -LiteralPath $_).Hash))
                {
                    throw [System.Management.Automation.ErrorRecord]::new(
                        [System.InvalidProgramException]::new("A PSAppDeployToolkit assembly of a different file hash is already loaded. Please restart PowerShell and try again."),
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
                    [System.Security.Cryptography.CryptographicException]::new("The assembly [$_] has an invalid digital signature and cannot be loaded."),
                    'ADTAssemblyFileSignatureError',
                    [System.Management.Automation.ErrorCategory]::SecurityError,
                    $badFile
                )
            }

            # If loading from an SMB path, load unsafely. This is OK because in signed (release) modules, we're validating the signature above.
            if ($isNetworkLocation)
            {
                $null = [System.Reflection.Assembly]::UnsafeLoadFrom($_)
            }
            else
            {
                Add-Type -LiteralPath $_
            }
        }

        end
        {
            # Prime the pump for WinRT on Windows PowerShell 5.1.
            if ($PSVersionTable.PSEdition.Equals('Desktop'))
            {
                $null = [Windows.UI.Notifications.ToastNotification, Windows.UI.Notifications, ContentType = WindowsRuntime]
            }
        }
    }

    # Remove any previous functions that may have been defined.
    if ($Module.Compiled)
    {
        $FunctionPaths = [System.Collections.Generic.List[System.String]]::new()
        $PrivateFuncs = [System.Collections.Generic.HashSet[System.String]]::new()
        $null = $MyInvocation.MyCommand.ScriptBlock.Ast.EndBlock.Statements | & {
            process
            {
                if ($_ -is [System.Management.Automation.Language.FunctionDefinitionAst])
                {
                    $i = $_.Name.Split(':')[-1]
                    if ($_.Name.Contains(':'))
                    {
                        $PrivateFuncs.Add($i)
                    }
                    $FunctionPaths.Add("Microsoft.PowerShell.Core\Function::$i")
                }
            }
        }
        New-Variable -Name FunctionPaths -Option Constant -Value $FunctionPaths.AsReadOnly() -Force
        New-Variable -Name PrivateFuncs -Option Constant -Value ([System.Collections.Frozen.FrozenSet]::ToFrozenSet($PrivateFuncs, $null)) -Force
        Remove-Item -LiteralPath $FunctionPaths -Force -ErrorAction Ignore
    }
}
catch
{
    throw
}
