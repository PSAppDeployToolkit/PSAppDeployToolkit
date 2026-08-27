<#

.SYNOPSIS
PSAppDeployToolkit.Build - This module script contains all the necessary logic to build PSAppDeployToolkit from source.

.DESCRIPTION
This module is designed to facilitate the local building of PSAppDeployToolkit into a release state. It is not designed to be operated outside of this repository.

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
        [System.InvalidOperationException]::new("This module must be imported via its .psd1 file, which is recommended for all modules that supply them."),
        'ModuleImportError',
        [System.Management.Automation.ErrorCategory]::InvalidOperation,
        $MyInvocation.MyCommand.ScriptBlock.Module
    )
}

# Initialise the module as required.
try
{
    # Set required variables to ensure module functionality.
    New-Variable -Name ErrorActionPreference -Value ([System.Management.Automation.ActionPreference]::Stop) -Option Constant -Force
    New-Variable -Name InformationPreference -Value ([System.Management.Automation.ActionPreference]::Continue) -Option Constant -Force
    New-Variable -Name ProgressPreference -Value ([System.Management.Automation.ActionPreference]::SilentlyContinue) -Option Constant -Force
    New-Variable -Name RepositoryRoot -Option Constant -Value ([System.IO.Directory]::GetParent($PSScriptRoot).Parent.FullName) -Force

    # Ensure module operates under the strictest of conditions.
    Set-StrictMode -Version 3

    # Import all necessary functions.
    New-Variable -Name ModuleFiles -Option Constant -Value ([System.Collections.ObjectModel.ReadOnlyCollection[System.IO.FileInfo]]::new([System.IO.FileInfo[]]$([System.IO.Directory]::GetFiles((Join-Path -Path $PSScriptRoot -ChildPath Private)); [System.IO.Directory]::GetFiles((Join-Path -Path $PSScriptRoot -ChildPath Public)))))
    $FunctionPaths = [System.Collections.Generic.List[System.String]]::new()
    $PrivateFuncs = [System.Collections.Generic.List[System.String]]::new()
    $ModuleFiles | & {
        process
        {
            if ([System.IO.Path]::GetDirectoryName($_.FullName).EndsWith('Private'))
            {
                $PrivateFuncs.Add($_.BaseName)
            }
            $FunctionPaths.Add("Microsoft.PowerShell.Core\Function::$($_.BaseName)")
        }
    }
    New-Variable -Name FunctionPaths -Option Constant -Value $FunctionPaths.AsReadOnly() -Force
    New-Variable -Name PrivateFuncs -Option Constant -Value $PrivateFuncs.AsReadOnly() -Force
    Remove-Item -LiteralPath $FunctionPaths -Force -ErrorAction Ignore
    $ModuleFiles.FullName | . { process { . $_ } }
    Set-Item -LiteralPath $FunctionPaths -Options ReadOnly

    # Store constants used throughout the module that are read-only.
    New-Variable -Name ModuleConstants -Option Constant -Force -Value ([ordered]@{
            DotNetBuildItems = ([System.Collections.ObjectModel.ReadOnlyCollection[System.Collections.Specialized.OrderedDictionary]][System.Collections.Specialized.OrderedDictionary[]]$(
                    ([ordered]@{
                        SourcePath = [System.IO.Path]::Combine($RepositoryRoot, 'src')
                        SolutionPath = [System.IO.Path]::Combine($RepositoryRoot, 'PSADT.slnx')
                        BasePath = [System.IO.Path]::Combine($RepositoryRoot, 'modules\PSAppDeployToolkit\lib')
                        PathMap = @{
                            "$([System.Management.Automation.WildcardPattern]::Escape([System.IO.Path]::Combine($RepositoryRoot, 'src\PSADT.ClientServer.Client.Launcher.Compatible\bin\Debug\net472')))\*" = [System.IO.Path]::Combine($RepositoryRoot, 'modules\PSAppDeployToolkit\lib\net472')
                            "$([System.Management.Automation.WildcardPattern]::Escape([System.IO.Path]::Combine($RepositoryRoot, 'src\PSADT.WindowsRuntime\bin\Debug\net8.0-windows10.0.22621.0')))\*" = [System.IO.Path]::Combine($RepositoryRoot, 'modules\PSAppDeployToolkit\lib\net8.0')
                            "$([System.Management.Automation.WildcardPattern]::Escape([System.IO.Path]::Combine($RepositoryRoot, 'src\PSADT.ClientServer.Server\bin\Debug\net8.0')))\*" = [System.IO.Path]::Combine($RepositoryRoot, 'modules\PSAppDeployToolkit\lib\net8.0')
                        }
                        PublishItems = @{
                            ([System.IO.Path]::Combine($RepositoryRoot, 'src\PSADT.WindowsRuntime.TrimHarness\PSADT.WindowsRuntime.TrimHarness.csproj')) = @{
                                ([System.IO.Path]::Combine($RepositoryRoot, 'src\PSADT.WindowsRuntime.TrimHarness\bin\Release\net8.0-windows10.0.22621.0\win-x64\publish\Microsoft.Windows.SDK.NET.dll')) = [System.IO.Path]::Combine($RepositoryRoot, 'modules\PSAppDeployToolkit\lib\net8.0')
                                ([System.IO.Path]::Combine($RepositoryRoot, 'src\PSADT.WindowsRuntime.TrimHarness\bin\Release\net8.0-windows10.0.22621.0\win-x64\publish\WinRT.Runtime.dll')) = [System.IO.Path]::Combine($RepositoryRoot, 'modules\PSAppDeployToolkit\lib\net8.0')
                            }
                        }
                        OutputFile = ([System.Collections.ObjectModel.ReadOnlyCollection[System.String]][System.String[]]$(
                                [System.IO.Path]::Combine($RepositoryRoot, 'modules\PSAppDeployToolkit\lib\net472\PSADT.ClientServer.Client.Compatible.exe')
                                [System.IO.Path]::Combine($RepositoryRoot, 'modules\PSAppDeployToolkit\lib\net472\PSADT.ClientServer.Client.Launcher.Compatible.exe')
                                [System.IO.Path]::Combine($RepositoryRoot, 'modules\PSAppDeployToolkit\lib\net472\PSADT.ClientServer.Client.Launcher.exe')
                                [System.IO.Path]::Combine($RepositoryRoot, 'modules\PSAppDeployToolkit\lib\net472\PSADT.ClientServer.Client.exe')
                                [System.IO.Path]::Combine($RepositoryRoot, 'modules\PSAppDeployToolkit\lib\net472\PSADT.ClientServer.Server.dll')
                                [System.IO.Path]::Combine($RepositoryRoot, 'modules\PSAppDeployToolkit\lib\net472\PSADT.Interop.dll')
                                [System.IO.Path]::Combine($RepositoryRoot, 'modules\PSAppDeployToolkit\lib\net472\PSADT.UserInterface.Interfaces.dll')
                                [System.IO.Path]::Combine($RepositoryRoot, 'modules\PSAppDeployToolkit\lib\net472\PSADT.UserInterface.dll')
                                [System.IO.Path]::Combine($RepositoryRoot, 'modules\PSAppDeployToolkit\lib\net472\PSADT.WindowsRuntime.dll')
                                [System.IO.Path]::Combine($RepositoryRoot, 'modules\PSAppDeployToolkit\lib\net472\PSADT.dll')
                                [System.IO.Path]::Combine($RepositoryRoot, 'modules\PSAppDeployToolkit\lib\net472\PSAppDeployToolkit.dll')
                                [System.IO.Path]::Combine($RepositoryRoot, 'modules\PSAppDeployToolkit\lib\net8.0\PSADT.ClientServer.Server.dll')
                                [System.IO.Path]::Combine($RepositoryRoot, 'modules\PSAppDeployToolkit\lib\net8.0\PSADT.Interop.dll')
                                [System.IO.Path]::Combine($RepositoryRoot, 'modules\PSAppDeployToolkit\lib\net8.0\PSADT.UserInterface.dll')
                                [System.IO.Path]::Combine($RepositoryRoot, 'modules\PSAppDeployToolkit\lib\net8.0\PSADT.WindowsRuntime.dll')
                                [System.IO.Path]::Combine($RepositoryRoot, 'modules\PSAppDeployToolkit\lib\net8.0\PSADT.dll')
                                [System.IO.Path]::Combine($RepositoryRoot, 'modules\PSAppDeployToolkit\lib\net8.0\PSAppDeployToolkit.dll')
                            ))
                    }).AsReadOnly()
                    ([ordered]@{
                        SourcePath = [System.IO.Path]::Combine($RepositoryRoot, 'src')
                        SolutionPath = [System.IO.Path]::Combine($RepositoryRoot, 'PSADT.Invoke.slnx')
                        BasePath = $null
                        PathMap = @{
                            "$([System.Management.Automation.WildcardPattern]::Escape([System.IO.Path]::Combine($RepositoryRoot, 'src\PSADT.Invoke\bin\Release\net472')))\*" = [System.IO.Path]::Combine($RepositoryRoot, 'modules\PSAppDeployToolkit\opt\Frontend\v4')
                        }
                        PublishItems = $null
                        OutputFile = ([System.Collections.ObjectModel.ReadOnlyCollection[System.String]][System.String[]]$(
                                [System.IO.Path]::Combine($RepositoryRoot, 'modules\PSAppDeployToolkit\opt\Frontend\v4\Invoke-AppDeployToolkit.exe')
                            ))
                    }).AsReadOnly()
                ))
            Paths = ([ordered]@{
                    Repository = $RepositoryRoot
                    SourceRoot = [System.IO.Path]::Combine($RepositoryRoot, 'modules')
                    ModuleSource = [System.IO.Path]::Combine($RepositoryRoot, 'modules\PSAppDeployToolkit')
                    AdmxTemplate = [System.IO.Path]::Combine($RepositoryRoot, 'modules\PSAppDeployToolkit\opt\ADMX\PSAppDeployToolkit.admx')
                    UnitTests = [System.IO.Path]::Combine($PSScriptRoot, 'Tests\Unit')
                    IntegrationTests = [System.IO.Path]::Combine($PSScriptRoot, 'Tests\Integration')
                    BuildOutput = [System.IO.Path]::Combine($RepositoryRoot, 'artifacts')
                    ModuleOutput = [System.IO.Path]::Combine($RepositoryRoot, 'artifacts\ModuleOnly\PSAppDeployToolkit')
                    MarkdownOutput = [System.IO.Path]::Combine($RepositoryRoot, 'artifacts\platyPS')
                    DocusaurusOutput = [System.IO.Path]::Combine($RepositoryRoot, 'artifacts\Docusaurus')
                }).AsReadOnly()
            InitializationArtwork = ([ordered]@{
                    Banner = [System.Text.Encoding]::GetEncoding(437).GetString([System.Convert]::FromBase64String('DQogICAgICAgICAgINvb29vb27sg29vb29vb27sg29vb29u7INvb29vb27sg29vb29vb29u7DQogICAgICAgICAgINvbyc3N29u729vJzc3Nzbzb28nNzdvbu9vbyc3N29u7yM3N29vJzc28DQogICAgICAgICAgINvb29vb28m829vb29vb27vb29vb29vbutvbuiAg29u6ICAg29u6ICAgDQogICAgICAgICAgINvbyc3NzbwgyM3Nzc3b27rb28nNzdvbutvbuiAg29u6ICAg29u6ICAgDQogICAgICAgICAgINvbuiAgICAg29vb29vb27rb27ogINvbutvb29vb28m8ICAg29u6ICAgDQogICAgICAgICAgIMjNvCAgICAgyM3Nzc3NzbzIzbwgIMjNvMjNzc3NzbwgICAgyM28ICAgDQo='))
                    Subtitle = "   PSAppDeployToolkit: Enterprise App Deployment, Simplified.`n Copyright (C) 2026 PSAppDeployToolkit Team. All rights reserved.`n --------------------------------------------------------------`n"
                    Style = 'Raster'
                }).AsReadOnly()
            RequiredModules = ([System.Collections.ObjectModel.ReadOnlyCollection[Microsoft.PowerShell.Commands.ModuleSpecification]][Microsoft.PowerShell.Commands.ModuleSpecification[]]$(
                    @{ ModuleName = 'PSScriptAnalyzer'; Guid = 'd6245802-193d-4068-a631-8863a4342a18'; ModuleVersion = '1.25.0' }
                    @{ ModuleName = 'Pester'; Guid = 'a699dea5-2c73-4616-a270-1f7abb777e71'; ModuleVersion = '6.0.1' }
                ))
            ModuleName = 'PSAppDeployToolkit'
            ModuleSpecification = [Microsoft.PowerShell.Commands.ModuleSpecification]@{ ModuleName = [System.Management.Automation.WildcardPattern]::Escape([System.IO.Path]::Combine($RepositoryRoot, 'modules\PSAppDeployToolkit\PSAppDeployToolkit.psd1')); Guid = '8c3c366b-8606-4576-9f2d-4051144f7ca2'; ModuleVersion = '4.2.0' }
            MinimumPowerShellVersion = [System.Version]'5.1'
            MinimumDotNetSdkVersion = [System.Version]'8.0.11'
            UnitTestOutputFormat = 'NUnitXML'
        }).AsReadOnly()

    # Store the module build state globally for sharing between funcs.
    New-Variable -Name ModuleBuildState -Option Constant -Force -Value ([pscustomobject]@{
            StartTime = $null
            CommandTable = $null
            HaveDotNetSdk = $false
        })
}
catch
{
    # Rethrowing caught exceptions makes the error output from Import-Module look better.
    throw
}
