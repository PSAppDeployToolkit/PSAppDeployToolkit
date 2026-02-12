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
if (!([System.Environment]::StackTrace.Split("`n") -like '*Microsoft.PowerShell.Commands.ModuleCmdletBase.LoadModuleManifest(*'))
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
                        SourcePath = [System.IO.Path]::Combine([System.IO.Directory]::GetParent($PSScriptRoot).FullName, 'PSADT')
                        SolutionPath = [System.IO.Path]::Combine([System.IO.Directory]::GetParent($PSScriptRoot).Parent.FullName, 'PSADT.slnx')
                        BinaryPath = [System.IO.Path]::Combine([System.IO.Directory]::GetParent($PSScriptRoot).FullName, 'PSADT\PSADT.ClientServer.Client.Launcher\bin\Debug\net472')
                        OutputPath = ([System.Collections.ObjectModel.ReadOnlyCollection[System.String]][System.String[]]$(
                                [System.IO.Path]::Combine([System.IO.Directory]::GetParent($PSScriptRoot).FullName, 'PSAppDeployToolkit\lib')
                            ))
                        OutputFile = ([System.Collections.ObjectModel.ReadOnlyCollection[System.String]][System.String[]]$(
                                [System.IO.Path]::Combine([System.IO.Directory]::GetParent($PSScriptRoot).FullName, 'PSAppDeployToolkit\lib\PSADT.ClientServer.Client.Launcher.exe'),
                                [System.IO.Path]::Combine([System.IO.Directory]::GetParent($PSScriptRoot).FullName, 'PSAppDeployToolkit\lib\PSADT.ClientServer.Client.exe'),
                                [System.IO.Path]::Combine([System.IO.Directory]::GetParent($PSScriptRoot).FullName, 'PSAppDeployToolkit\lib\PSADT.ClientServer.Server.dll'),
                                [System.IO.Path]::Combine([System.IO.Directory]::GetParent($PSScriptRoot).FullName, 'PSAppDeployToolkit\lib\PSADT.dll'),
                                [System.IO.Path]::Combine([System.IO.Directory]::GetParent($PSScriptRoot).FullName, 'PSAppDeployToolkit\lib\PSADT.UserInterface.dll'),
                                [System.IO.Path]::Combine([System.IO.Directory]::GetParent($PSScriptRoot).FullName, 'PSAppDeployToolkit\lib\PSAppDeployToolkit.dll')
                            ))
                    }).AsReadOnly(),
                    ([ordered]@{
                        SourcePath = [System.IO.Path]::Combine([System.IO.Directory]::GetParent($PSScriptRoot).FullName, 'PSADT.Invoke')
                        SolutionPath = [System.IO.Path]::Combine([System.IO.Directory]::GetParent($PSScriptRoot).Parent.FullName, 'PSADT.Invoke.slnx')
                        BinaryPath = [System.IO.Path]::Combine([System.IO.Directory]::GetParent($PSScriptRoot).FullName, 'PSADT.Invoke\PSADT.Invoke\bin\Release\net472')
                        OutputPath = ([System.Collections.ObjectModel.ReadOnlyCollection[System.String]][System.String[]]$(
                                [System.IO.Path]::Combine([System.IO.Directory]::GetParent($PSScriptRoot).FullName, 'PSAppDeployToolkit\Frontend\v4')
                                [System.IO.Path]::Combine([System.IO.Directory]::GetParent($PSScriptRoot).FullName, 'PSAppDeployToolkit\Frontend\v3')
                            ))
                        OutputFile = ([System.Collections.ObjectModel.ReadOnlyCollection[System.String]][System.String[]]$(
                                [System.IO.Path]::Combine([System.IO.Directory]::GetParent($PSScriptRoot).FullName, 'PSAppDeployToolkit\Frontend\v4\Invoke-AppDeployToolkit.exe'),
                                [System.IO.Path]::Combine([System.IO.Directory]::GetParent($PSScriptRoot).FullName, 'PSAppDeployToolkit\Frontend\v3\Deploy-Application.exe')
                            ))
                    }).AsReadOnly()
                ))
            Paths = ([ordered]@{
                    Repository = [System.IO.Directory]::GetParent($PSScriptRoot).Parent.FullName
                    SourceRoot = [System.IO.Directory]::GetParent($PSScriptRoot).FullName
                    ModuleSource = $PSScriptRoot -replace '\.Build$'
                    AdmxTemplate = [System.IO.Path]::Combine($PSScriptRoot -replace '\.Build$', 'ADMX', 'PSAppDeployToolkit.admx')
                    ModuleConfig = [System.IO.Path]::Combine($PSScriptRoot -replace '\.Build$', 'Config')
                    ModuleStrings = [System.IO.Path]::Combine($PSScriptRoot -replace '\.Build$', 'Strings')
                    UnitTests = [System.IO.Path]::Combine([System.IO.Directory]::GetParent($PSScriptRoot).FullName, 'Tests', 'Unit')
                    IntegrationTests = [System.IO.Path]::Combine([System.IO.Directory]::GetParent($PSScriptRoot).FullName, 'Tests', 'Integration')
                    BuildOutput = [System.IO.Path]::Combine([System.IO.Directory]::GetParent($PSScriptRoot).FullName, 'Artifacts')
                    ModuleOutput = [System.IO.Path]::Combine([System.IO.Directory]::GetParent($PSScriptRoot).FullName, 'Artifacts', 'Module', 'PSAppDeployToolkit')
                    TestOutput = [System.IO.Path]::Combine([System.IO.Directory]::GetParent($PSScriptRoot).FullName, 'Artifacts', 'TestOutput')
                    MarkdownOutput = [System.IO.Path]::Combine([System.IO.Directory]::GetParent($PSScriptRoot).FullName, 'Artifacts', 'platyPS')
                    DocusaurusOutput = [System.IO.Path]::Combine([System.IO.Directory]::GetParent($PSScriptRoot).FullName, 'Artifacts', 'Docusaurus')
                    CodeCoverageOutput = [System.IO.Path]::Combine([System.IO.Directory]::GetParent($PSScriptRoot).FullName, 'Artifacts', 'CodeCoverage')
                }).AsReadOnly()
            InitializationArtwork = ([ordered]@{
                    Banner = [System.Text.Encoding]::GetEncoding(437).GetString([System.Convert]::FromBase64String('DQogICAgICAgICAgINvb29vb27sg29vb29vb27sg29vb29u7INvb29vb27sg29vb29vb29u7DQogICAgICAgICAgINvbyc3N29u729vJzc3Nzbzb28nNzdvbu9vbyc3N29u7yM3N29vJzc28DQogICAgICAgICAgINvb29vb28m829vb29vb27vb29vb29vbutvbuiAg29u6ICAg29u6ICAgDQogICAgICAgICAgINvbyc3NzbwgyM3Nzc3b27rb28nNzdvbutvbuiAg29u6ICAg29u6ICAgDQogICAgICAgICAgINvbuiAgICAg29vb29vb27rb27ogINvbutvb29vb28m8ICAg29u6ICAgDQogICAgICAgICAgIMjNvCAgICAgyM3Nzc3NzbzIzbwgIMjNvMjNzc3NzbwgICAgyM28ICAgDQo='))
                    Subtitle = "   PSAppDeployToolkit: Enterprise App Deployment, Simplified.`n Copyright (C) 2026 PSAppDeployToolkit Team. All rights reserved.`n --------------------------------------------------------------`n"
                    Style = 'Raster'
                }).AsReadOnly()
            RequiredModules = ([System.Collections.ObjectModel.ReadOnlyCollection[Microsoft.PowerShell.Commands.ModuleSpecification]][Microsoft.PowerShell.Commands.ModuleSpecification[]]$(
                    @{ ModuleName = 'PSScriptAnalyzer'; Guid = 'd6245802-193d-4068-a631-8863a4342a18'; ModuleVersion = '1.24.0' }
                    @{ ModuleName = 'Pester'; Guid = 'a699dea5-2c73-4616-a270-1f7abb777e71'; ModuleVersion = '5.7.1' }
                ))
            ModuleName = 'PSAppDeployToolkit'
            ModuleSpecification = [Microsoft.PowerShell.Commands.ModuleSpecification]@{ ModuleName = [System.Management.Automation.WildcardPattern]::Escape([System.IO.Path]::Combine($PSScriptRoot -replace '\.Build$', 'PSAppDeployToolkit.psd1')); Guid = '8c3c366b-8606-4576-9f2d-4051144f7ca2'; ModuleVersion = '4.2.0' }
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
