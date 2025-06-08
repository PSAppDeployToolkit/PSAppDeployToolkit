<#
.SYNOPSIS
    An Invoke-Build Build file.

.DESCRIPTION
    Build steps can include:
        - ValidateRequirements
        - ImportModuleManifest
        - Clean
        - Analyze
        - FormattingCheck
        - Test
        - DevCC
        - CreateHelpStart
        - Build
        - IntegrationTest

.EXAMPLE
    Invoke-Build

    This will perform the default build Add-BuildTasks: see below for the default Add-BuildTask execution.

.EXAMPLE
    Invoke-Build -Add-BuildTask Analyze,Test

    This will perform only the Analyze and Test Add-BuildTasks.

.NOTES
    https://github.com/nightroman/Invoke-Build
    https://github.com/nightroman/Invoke-Build/wiki/Build-Scripts-Guidelines
    If using VSCode you can use the generated tasks.json to execute the various tasks in this build file.
        Ctrl + P | then type task (add space) - you will then be presented with a list of available tasks to run
    The 'InstallDependencies' Add-BuildTask isn't present here.
        Module dependencies are installed at a previous step in the pipeline.
        If your manifest has module dependencies include all required modules in your CI/CD bootstrap file:
            AWS - install_modules.ps1
            Azure - actions_bootstrap.ps1
            GitHub Actions - actions_bootstrap.ps1
            AppVeyor  - actions_bootstrap.ps1
#>

# Default variables.
$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
$ProgressPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue
Set-StrictMode -Version 3
$ModuleName = [System.Text.RegularExpressions.Regex]::Match((Get-Item $BuildFile).Name, '^(.*)\.build\.ps1$').Groups[1].Value
[System.Version]$requiredPSVersion = '5.1.0'

# Helper function for comparing hashtable key structures.
function Confirm-HashtableStructuresAreEqual
{
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Hashtable]$Reference,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Hashtable]$Comparison
    )

    # Throw if there's any missing/extra keys.
    $refKeys = $Reference.Keys; $cmpKeys = $Comparison.Keys
    if ($missing = $refKeys | & { process { if (!$cmpKeys.Contains($_)) { return $_ } } })
    {
        throw "The following hashtable keys are missing: ['$([System.String]::Join("', '", $missing))']."
    }
    if ($extras = $cmpKeys | & { process { if (!$refKeys.Contains($_)) { return $_ } } })
    {
        throw "The following hashtable keys are extras: ['$([System.String]::Join("', '", $extras))']."
    }

    # Test each key's value, recursively processing child hashtables.
    foreach ($key in $refKeys)
    {
        # Cache each hashtable's key value and whether it's a hashtable.
        $vRef = $Reference[$key]; $vCmp = $Comparison[$key]
        $vRefIsHash = $vRef -is [System.Collections.Hashtable]
        $vCmpIsHash = $vCmp -is [System.Collections.Hashtable]

        # If one is hashtable and the other isn’t, that’s a mismatch.
        if ($vRefIsHash -xor $vCmpIsHash)
        {
            throw "The key value [$key] is a hashtable on one side and not the other."
        }
        elseif ($vRefIsHash -and $vCmpIsHash)
        {
            Confirm-HashtableStructuresAreEqual -Reference $vRef -Comparison $vCmp
        }
    }
    return $true
}
function Confirm-ADTAdmxTemplateMatchesConfig
{
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ConfigPath,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$AdmxPath
    )

    # Internal worker function for processing each hashtable.
    function Confirm-ADTAdmxCategoryMatchesConfigSection
    {
        [CmdletBinding()]
        param
        (
            [Parameter(Mandatory = $true)]
            [ValidateNotNullOrEmpty()]
            [System.String]$Category,

            [Parameter(Mandatory = $true)]
            [ValidateNotNullOrEmpty()]
            [System.Collections.Hashtable]$Section
        )

        # Recursively process subsections that are hashtables.
        $sectionProps = foreach ($kvp in $Section.GetEnumerator())
        {
            if ($kvp.Value -is [System.Collections.Hashtable])
            {
                Confirm-ADTAdmxCategoryMatchesConfigSection -Category $kvp.Key -Section $kvp.Value
            }
            else
            {
                $kvp.Key
            }
        }

        # Test our collected session properties.
        $admxProps = $admxData.policyDefinitions.policies.policy | & { process { if ($_.parentCategory.ref.Equals($Category)) { return $_.Name.Split('_')[0] } } }
        if ($missing = $sectionProps | & { process { if (($admxProps -notcontains $_) -and (!$categoryExclusions.ContainsKey($Category) -or ($categoryExclusions.$Category -notcontains $_))) { return $_ } } })
        {
            throw "The ADMX category [$Category] is missing the following config options: ['$([System.String]::Join("', '", $missing))']."
        }
        if ($extras = $admxProps | & { process { if ($sectionProps -notcontains $_) { return $_ } } })
        {
            throw "The ADMX category [$Category] has the following extra config options: ['$([System.String]::Join("', '", $extras))']."
        }
    }

    # Define list of category exclusions.
    $categoryExclusions = @{
        Toolkit = @('RequireAdmin')
    }

    # Import config and XML as required.
    $adtConfig = Import-PowerShellDataFile -LiteralPath $ConfigPath
    $admxData = [System.Xml.XmlDocument]::new()
    $admxData.Load($AdmxPath)

    # Process the hashtable. We assume that each initial section is a hashtable.
    foreach ($kvp in $adtConfig.GetEnumerator())
    {
        Confirm-ADTAdmxCategoryMatchesConfigSection -Category $kvp.Key -Section $kvp.Value
    }
    return $true
}

# Define our C# solutions to compile.
$buildItems = @(
    @{
        SourcePath = 'src\PSADT'
        SolutionPath = 'src\PSADT\PSADT.slnx'
        BinaryPath = 'src\PSADT\PSADT.ClientServer.Client\bin\Debug\net462'
        OutputPath = 'src\PSAppDeployToolkit\lib'
        OutputFile = 'src\PSAppDeployToolkit\lib\PSADT.ClientServer.Client.exe', 'src\PSAppDeployToolkit\lib\PSADT.ClientServer.Server.dll', 'src\PSAppDeployToolkit\lib\PSADT.dll', 'src\PSAppDeployToolkit\lib\PSADT.UserInterface.dll'
    },
    @{
        SourcePath = 'src\PSADT.Invoke'
        SolutionPath = 'src\PSADT.Invoke\PSADT.Invoke.slnx'
        BinaryPath = 'src\PSADT.Invoke\PSADT.Invoke\bin\Release\net462'
        OutputPath = 'src\PSAppDeployToolkit\Frontend\v4', 'src\PSAppDeployToolkit\Frontend\v3'
        OutputFile = 'src\PSAppDeployToolkit\Frontend\v4\Invoke-AppDeployToolkit.exe', 'src\PSAppDeployToolkit\Frontend\v3\Deploy-Application.exe'
    }
)

# Default build.
$str = @()
$str = 'Clean', 'ValidateRequirements'
$str += 'TestModuleManifest'
$str += 'DotNetBuild'
$str += 'ImportModuleManifest'
$str += 'EncodingCheck'
$str += 'FormattingCheck'
$str += 'ConfigCheck'
$str += 'StringTableCheck'
$str += 'Analyze'
$str += 'Test'
$str += 'CreateHelpStart'
$str2 = $str
$str2 += 'Build'
$str += 'Build', 'IntegrationTest'
Add-BuildTask -Name . -Jobs $str

# Local testing build process.
Add-BuildTask TestLocal Clean, ImportModuleManifest, Analyze, Test

# Local help file creation process.
Add-BuildTask HelpLocal Clean, ImportModuleManifest, CreateHelpStart

# Full build sans integration tests.
Add-BuildTask BuildNoIntegration -Jobs $str2

# Pre-build variables to be used by other portions of the script.
Enter-Build {
    # Set up required paths.
    $Script:RepoRootPath = Split-Path -Path $BuildRoot -Parent
    $Script:ModuleSourcePath = Join-Path -Path $BuildRoot -ChildPath $Script:ModuleName
    $Script:ModuleFiles = Join-Path -Path $Script:ModuleSourcePath -ChildPath '*'
    $Script:ModuleManifestFile = Join-Path -Path $Script:ModuleSourcePath -ChildPath "$($Script:ModuleName).psd1"
    $Script:TestsPath = Join-Path -Path $BuildRoot -ChildPath 'Tests'
    $Script:UnitTestsPath = Join-Path -Path $Script:TestsPath -ChildPath 'Unit'
    $Script:IntegrationTestsPath = Join-Path -Path $Script:TestsPath -ChildPath 'Integration'
    $Script:ArtifactsPath = Join-Path -Path $BuildRoot -ChildPath 'Artifacts'
    $Script:MarkdownExportPath = "$Script:ArtifactsPath\platyPS\"
    $Script:DocusaurusExportPath = "$Script:ArtifactsPath\Docusaurus\"
    $Script:BuildModuleRoot = Join-Path -Path $Script:ArtifactsPath -ChildPath "Module\$Script:ModuleName"
    $Script:BuildModuleRootFile = Join-Path -Path $Script:BuildModuleRoot -ChildPath "$($Script:ModuleName).psm1"

    $manifestInfo = Import-LocalizedData -BaseDirectory $Script:ModuleSourcePath -FileName "$($Script:ModuleName).psd1"
    $Script:ModuleVersion = $manifestInfo.ModuleVersion
    $Script:ModuleDescription = $manifestInfo.Description
    $Script:FunctionsToExport = $manifestInfo.FunctionsToExport

    # Ensure our builds fail until if below a minimum defined code test coverage threshold.
    $Script:coverageThreshold = 0
    [System.Version]$Script:MinPesterVersion = '5.2.2'
    [System.Version]$Script:MaxPesterVersion = '5.99.99'
    $Script:testOutputFormat = 'NUnitXML'
}

# Define headers as separator, task path, synopsis, and location, e.g. for Ctrl+Click in VSCode.
# Also change the default color to Green. If you need task start times, use `$Task.Started`.
Set-BuildHeader {
    param
    (
        $Path
    )

    Write-Build DarkMagenta ('=' * 79)
    Write-Build DarkGray "Task $Path : $(Get-BuildSynopsis $Task)"
    Write-Build DarkGray "At $($Task.InvocationInfo.ScriptName):$($Task.InvocationInfo.ScriptLineNumber)"
    Write-Build Yellow "Manifest File: $Script:ModuleManifestFile"
    Write-Build Yellow "Manifest Version: $($manifestInfo.ModuleVersion)"
}

# Define footers similar to default but change the color to DarkGray.
Set-BuildFooter {
    param
    (
        $Path
    )

    Write-Build DarkGray "Done $Path, $($Task.Elapsed)"
}

# Synopsis: Validate system requirements are met.
Add-BuildTask ValidateRequirements {
    Write-Build White "      Verifying at least PowerShell $Script:requiredPSVersion..."
    Assert-Build ($PSVersionTable.PSVersion -ge $Script:requiredPSVersion) "At least Powershell $Script:requiredPSVersion is required for this build to function properly"
    Write-Build Green '      ...Verification Complete!'
}

# Synopsis: Compile our defined C# solutions.
Add-BuildTask DotNetBuild {
    # Find Visual Studio on the current device.
    Write-Build White '      Compiling C# projects...'
    if (!($msbuildPath = & "$([System.Environment]::GetFolderPath('ProgramFilesX86'))\Microsoft Visual Studio\Installer\vswhere.exe" -requires Microsoft.Component.MSBuild -find MSBuild\Current\Bin\MSBuild.exe))
    {
        throw 'msbuild.exe command not found. Ensure Visual Studio is installed on this system.'
    }

    # Process each build item.
    Write-Build Gray '        Determining C# solutions requiring compilation...'
    foreach ($buildItem in $Script:buildItems)
    {
        # Define default build actions. We always need to do a release build.
        $buildConfigs = [System.Collections.Generic.List[System.String]]'Release'

        # Only build a debug version if files have been modified.
        if ($env:GITHUB_ACTIONS -ne 'true')
        {
            foreach ($sourcePath in $buildItem.SourcePath)
            {
                if (!((git status --porcelain) -match "^.{3}$([regex]::Escape($sourcePath.Replace('\','/')))/"))
                {
                    # Get the last commit date of the output file, which is similar to ISO 8601 format but with spaces and no T between date and time
                    $buildItem.OutputFile | ForEach-Object {
                        # Get commit date via git, parse the result, then add one second and convert to proper ISO 8601 format..
                        $lastCommitDate = git log -1 --format="%ci" -- ([System.IO.Path]::Combine($Script:RepoRootPath, $_))
                        $lastCommitDate = [DateTime]::ParseExact($lastCommitDate, "yyyy-MM-dd HH:mm:ss K", [System.Globalization.CultureInfo]::InvariantCulture)
                        $sinceDateString = $lastCommitDate.AddSeconds(1).ToString('yyyy-MM-ddTHH:mm:ssK')

                        # Get the list of source files modified since the last commit date of the file we're comparing against
                        if (git log --name-only --since=$sinceDateString --diff-filter=ACDMTUXB --pretty=format: -- ([System.IO.Path]::Combine($Script:RepoRootPath, $sourcePath)) | Where-Object { ![string]::IsNullOrWhiteSpace($buildItem) } | Sort-Object -Unique)
                        {
                            Write-Build Blue "          Files have been modified in $($sourcePath) since the last commit date of $_ ($($lastCommitDate.ToString('yyyy-MM-ddTHH:mm:ssK'))), debug build required."
                            $buildConfigs.Add('Debug')
                        }
                        else
                        {
                            Write-Build Gray "          No files have been modified in $($sourcePath) since the last commit date of $_ ($($lastCommitDate.ToString('yyyy-MM-ddTHH:mm:ssK'))), debug build not required."
                        }
                    }
                }
                else
                {
                    Write-Build Blue "          Uncommitted file changes found under $($sourcePath), debug build required."
                    $buildConfigs.Add('Debug')
                }
            }
        }

        # Manually clean out old build and obj folders for good measure.
        foreach ($sourcePath in $buildItem.SourcePath)
        {
            Write-Build Gray "            Removing previous bin and obj folders from $($sourcePath)..."
            $sourcePath = [System.IO.Path]::Combine($Script:RepoRootPath, $sourcePath)
            'bin', 'obj' | ForEach-Object { Get-ChildItem -LiteralPath $sourcePath -Directory -Filter $_ -Recurse } | Remove-Item -Recurse -Force
        }

        # Build a debug and release config of each project.
        Write-Build Gray "            Building $(($solutionPath = [System.IO.Path]::Combine($Script:RepoRootPath, $buildItem.SolutionPath)))..."
        foreach ($buildType in ($buildConfigs | Select-Object -Unique))
        {
            & $msbuildPath $solutionPath -target:Rebuild -restore -p:configuration=$buildType -p:platform="Any CPU" -nodeReuse:false -m -verbosity:minimal
            if ($LASTEXITCODE) { throw "Failed to build solution `"$($buildItem.SolutionPath -replace '^.+\\')`". Exit code: $LASTEXITCODE" }

            # Copy the debug configuration into the module's folder within the repo. The release copy will come later on directly into the artifact.
            if ($buildType.Equals('Debug'))
            {
                $sourcePath = [System.IO.Path]::Combine($Script:RepoRootPath, $buildItem.BinaryPath, '*')
                $buildItem.OutputPath | ForEach-Object {
                    $destPath = [System.IO.Path]::Combine($Script:RepoRootPath, $_)
                    Write-Build Gray "          Copying from $sourcePath to $destPath..."
                    Copy-Item -Path $sourcePath -Destination $destPath -Recurse -Force
                }
            }
        }
    }

    # For Invoke-AppDeployToolkit.exe, we need an additional check to make sure the assembly is renamed.
    if ([System.IO.File]::Exists("$($Script:RepoRootPath)\src\PSAppDeployToolkit\Frontend\v3\Invoke-AppDeployToolkit.exe"))
    {
        Remove-Item -LiteralPath "$($Script:RepoRootPath)\src\PSAppDeployToolkit\Frontend\v3\Deploy-Application.exe" -Force -Confirm:$false
        Remove-Item -LiteralPath "$($Script:RepoRootPath)\src\PSAppDeployToolkit\Frontend\v3\Invoke-AppDeployToolkit.pdb" -Force -Confirm:$false -ErrorAction Ignore
        Rename-Item -LiteralPath "$($Script:RepoRootPath)\src\PSAppDeployToolkit\Frontend\v3\Invoke-AppDeployToolkit.exe" -NewName Deploy-Application.exe -Force -Confirm:$false
    }
    Write-Build Green '      ...C# Compilation Complete!'
}

# Synopsis: Import the current module manifest file for processing.
Add-BuildTask TestModuleManifest {
    Write-Build White '      Running module manifest tests...'
    Assert-Build (Test-Path $Script:ModuleManifestFile) 'Unable to locate the module manifest file.'
    Assert-Build (Get-ChildItem $Script:ModuleManifestFile | Test-ModuleManifest -ErrorAction Ignore) 'Module Manifest test did not pass verification.'
    Assert-Build (!(Get-Module -Name $Script:ModuleName)) 'Conflicting module already imported.'
    Assert-Build (!(Get-ChildItem -LiteralPath $env:PSModulePath.Split(';') -Filter $Script:ModuleName -ErrorAction Ignore)) 'Conflicting module within a PSModulePath directory.'
    Write-Build Green '      ...Module Manifest Verification Complete!'
}

# Synopsis: Load the module project.
Add-BuildTask ImportModuleManifest {
    Write-Build White '      Attempting to load the project module.'
    $Script:moduleCommandTable = & (Import-Module $Script:ModuleManifestFile -Force -PassThru) { $CommandTable }
    Write-Build Green "      ...$Script:ModuleName imported successfully"
}

# Synopsis: Clean and reset Artifacts directory.
Add-BuildTask Clean {
    Write-Build White '      Clean up our Artifacts directory...'
    $null = Remove-Item $Script:ArtifactsPath -Force -Recurse -ErrorAction Ignore
    $null = New-Item $Script:ArtifactsPath -ItemType Directory
    Write-Build Green '      ...Clean Complete!'
}

# Synopsis: Analyze scripts to verify that the file encoding is UTF-8 with a BOM.
Add-BuildTask EncodingCheck {
    Write-Build White '      Performing script encoding checks...'
    Get-ChildItem -Path "$BuildRoot\*.ps*1" -Recurse -File | & {
        begin
        {
            # Create byte array to read into file.
            $bom = [System.Byte[]]::new(4)
        }

        process
        {
            # Open the file, read out the first 4 bytes, then close it out.
            $stream = [System.IO.FileStream]::new($_.FullName, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read)
            $null = $stream.Read($bom, 0, $bom.Count)
            $stream.Flush(); $stream.Close()

            # Throw if the byte order mark doesn't match utf8-bom.
            if (!(($bom[0] -eq 0xEF) -and ($bom[1] -eq 0xBB) -and ($bom[2] -eq 0xBF)))
            {
                throw "The file encoding for [$($_.FullName)] is not UTF-8 with BOM."
            }
        }
    }
    Write-Build Green '      ...Encoding Analyze Complete!'
}

# Synopsis: Analyze scripts to verify if they adhere to desired coding format (Stroustrup / OTBS / Allman).
Add-BuildTask FormattingCheck {
    Write-Build White '      Performing script formatting checks...'
    if (($scriptAnalyzerResults = Invoke-ScriptAnalyzer -Path $Script:BuildRoot -Setting CodeFormattingAllman -ExcludeRule PSAlignAssignmentStatement -Recurse -Fix:($env:GITHUB_ACTIONS -ne 'true') -Verbose:$false | Where-Object { !$_.RuleName.Equals('PSUseToExportFieldsInManifest') -or !$_.ScriptName.Equals('PSAppDeployToolkit.Extensions.psd1') }))
    {
        $scriptAnalyzerResults | Format-Table
        throw '      PSScriptAnalyzer code formatting check did not adhere to defined standards'
    }
    Write-Build Green '      ...Formatting Analyze Complete!'
}

# Synopsis: Invokes PSScriptAnalyzer against the Module source path.
Add-BuildTask Analyze {
    Write-Build White '      Performing Module ScriptAnalyzer checks...'
    if (($scriptAnalyzerResults = Invoke-ScriptAnalyzer -Path $Script:BuildRoot -ExcludeRule PSUseShouldProcessForStateChangingFunctions, PSUseSingularNouns -Recurse -Verbose:$false | Where-Object { !$_.RuleName.Equals('PSUseToExportFieldsInManifest') -or !$_.ScriptName.Equals('PSAppDeployToolkit.Extensions.psd1') }))
    {
        $scriptAnalyzerResults | Format-Table
        throw '      One or more PSScriptAnalyzer errors/warnings where found.'
    }
    Write-Build Green '      ...Module Analyze Complete!'
}

# Synopsis: Analyze config and sure all options are properly represented within the ADMX template.
Add-BuildTask ConfigCheck {
    Write-Build White '      Performing config ADMX template check...'
    try
    {
        $null = Confirm-ADTAdmxTemplateMatchesConfig -ConfigPath $ModuleSourcePath\Config\config.psd1 -AdmxPath $ModuleSourcePath\ADMX\$ModuleName.admx
    }
    catch
    {
        throw "      $($_.Exception.Message)"
    }
    Write-Build Green '      ...ADMX Template Checks Complete!'
}

# Synopsis: Analyze all translation files to ensure they match the structure of the English file.
Add-BuildTask StringTableCheck {
    Write-Build White '      Performing language translation file checks...'
    Get-ChildItem -LiteralPath $ModuleSourcePath\Strings -Directory | Get-ChildItem -File | & {
        begin
        {
            # Load and cache the English strings.psd1 file.
            $Reference = Import-PowerShellDataFile -LiteralPath $ModuleSourcePath\Strings\strings.psd1
        }

        process
        {
            # Throw if there's any missing/extra keys.
            Write-Build Gray "      Testing [$($_.FullName)]..."
            try
            {
                $null = Confirm-HashtableStructuresAreEqual -Reference $Reference -Comparison (Import-PowerShellDataFile -LiteralPath $_.FullName)
            }
            catch
            {
                throw "      $($_.Exception.Message)"
            }
        }
    }
    Write-Build Green '      ...Language Translation File Tests Complete!'
}

# Synopsis: Invokes all Pester Unit Tests in the Tests\Unit folder (if it exists).
Add-BuildTask Test {
    # (Re-)Import Pester module.
    Write-Build White "      Importing desired Pester version. Min: $Script:MinPesterVersion Max: $Script:MaxPesterVersion"
    Remove-Module -Name Pester -Force -ErrorAction Ignore # there are instances where some containers have Pester already in the session
    Import-Module -Name Pester -MinimumVersion $Script:MinPesterVersion -MaximumVersion $Script:MaxPesterVersion

    # Set up required paths.
    $codeCovPath = "$Script:ArtifactsPath\ccReport\"
    if (!(Test-Path $codeCovPath))
    {
        New-Item -Path $codeCovPath -ItemType Directory | Out-Null
    }
    $testOutPutPath = "$Script:ArtifactsPath\testOutput\"
    if (!(Test-Path $testOutPutPath))
    {
        New-Item -Path $testOutPutPath -ItemType Directory | Out-Null
    }

    # Perform unit testing.
    if (Test-Path -Path $Script:UnitTestsPath)
    {
        # Perform tests.
        Write-Build White '      Performing Pester Unit Tests...'
        $pesterConfiguration = New-PesterConfiguration
        $pesterConfiguration.run.Path = $Script:UnitTestsPath
        $pesterConfiguration.Run.PassThru = $true
        $pesterConfiguration.Run.Exit = $false
        $pesterConfiguration.CodeCoverage.Enabled = $true
        $pesterConfiguration.CodeCoverage.Path = "..\..\..\src\$Script:ModuleName\*\*.ps1"
        $pesterConfiguration.CodeCoverage.CoveragePercentTarget = $Script:coverageThreshold
        $pesterConfiguration.CodeCoverage.OutputPath = "$codeCovPath\CodeCoverage.xml"
        $pesterConfiguration.CodeCoverage.OutputFormat = 'JaCoCo'
        $pesterConfiguration.TestResult.Enabled = $true
        $pesterConfiguration.TestResult.OutputPath = "$testOutPutPath\PesterTests.xml"
        $pesterConfiguration.TestResult.OutputFormat = $Script:testOutputFormat
        $pesterConfiguration.Output.Verbosity = 'Detailed'
        $testResults = Invoke-Pester -Configuration $pesterConfiguration

        # This will output a nice json for each failed test (if running in CodeBuild)
        if ($env:CODEBUILD_BUILD_ARN)
        {
            $testResults.TestResult | ForEach-Object
            {
                if ($_.Result -ne 'Passed')
                {
                    ConvertTo-Json -InputObject $_ -Compress
                }
            }
        }

        # Publish results.
        Assert-Build (($numberFails = $testResults.FailedCount) -eq 0) ('Failed "{0}" unit tests.' -f $numberFails)
        Write-Build Gray ('      ...CODE COVERAGE - CommandsExecutedCount: {0}' -f $testResults.CodeCoverage.CommandsExecutedCount)
        Write-Build Gray ('      ...CODE COVERAGE - CommandsAnalyzedCount: {0}' -f $testResults.CodeCoverage.CommandsAnalyzedCount)
        if ($testResults.CodeCoverage.CommandsExecutedCount -ne 0)
        {
            # Report on coverage percentage.
            [System.UInt32]$coveragePercent = '{0:N2}' -f ($testResults.CodeCoverage.CommandsExecutedCount / $testResults.CodeCoverage.CommandsAnalyzedCount * 100)
            if ($coveragePercent -lt $coverageThreshold)
            {
                throw ('Failed to meet code coverage threshold of {0}% with only {1}% coverage' -f $coverageThreshold, $coveragePercent)
            }
            Write-Build Cyan "      $('Covered {0}% of {1} analyzed commands in {2} files.' -f $coveragePercent,$testResults.CodeCoverage.CommandsAnalyzedCount,$testResults.CodeCoverage.FilesAnalyzedCount)"
            Write-Build Green '      ...Pester Unit Tests Complete!'
        }
    }
}

# Synopsis: Used primarily during active development to generate xml file to graphically display code coverage in VSCode using Coverage Gutters.
Add-BuildTask DevCC {
    Write-Build White '      Generating code coverage report at root...'
    Write-Build White "      Importing desired Pester version. Min: $Script:MinPesterVersion Max: $Script:MaxPesterVersion"
    Remove-Module -Name Pester -Force -ErrorAction Ignore  # there are instances where some containers have Pester already in the session
    Import-Module -Name Pester -MinimumVersion $Script:MinPesterVersion -MaximumVersion $Script:MaxPesterVersion -ErrorAction 'Stop'
    $pesterConfiguration = New-PesterConfiguration
    $pesterConfiguration.run.Path = $Script:UnitTestsPath
    $pesterConfiguration.CodeCoverage.Enabled = $true
    $pesterConfiguration.CodeCoverage.Path = "$PSScriptRoot\$Script:ModuleName\*\*.ps1"
    $pesterConfiguration.CodeCoverage.CoveragePercentTarget = $Script:coverageThreshold
    $pesterConfiguration.CodeCoverage.OutputPath = '..\..\..\cov.xml'
    $pesterConfiguration.CodeCoverage.OutputFormat = 'CoverageGutters'
    Invoke-Pester -Configuration $pesterConfiguration
    Write-Build Green '      ...Code Coverage report generated!'
}

# Synopsis: Build help for module.
Add-BuildTask CreateHelpStart {
    Write-Build White '      Performing all help related actions.'
    Write-Build Gray '           Importing platyPS...'
    Import-Module platyPS -RequiredVersion 0.14.2
    Write-Build Gray '           ...platyPS imported successfully.'
}

# Synopsis: Build markdown help files for module and fail if help information is missing.
Add-BuildTask CreateMarkdownHelp -After CreateHelpStart {
    # Generate markdown files.
    Write-Build Gray '           Generating markdown files...'
    $null = New-MarkdownHelp -Module $Script:ModuleName -OutputFolder $Script:MarkdownExportPath -Locale en-US -FwLink NA -HelpVersion $Script:ModuleVersion -Force
    Write-Build Gray '           ...Markdown generation completed.'

    # Post-process the exported markdown files.
    Write-Build Gray '           Replacing markdown elements...'
    $Script:MarkdownExportPath | Get-ChildItem -File | ForEach-Object {
        # Read the file as a string, not an array.
        $content = [System.IO.File]::ReadAllText($_.FullName)

        # Trim the file, fix multi-line EXAMPLES, and unescape tilde characters.
        $newContent = ($content.Trim() -replace '(## EXAMPLE [^`]+?```\r\n[^`\r\n]+?\r\n)(```\r\n\r\n)([^#]+?\r\n)(\r\n)([^#]+)(#)', '$1$3$2$4$5$6').Replace('PS C:\\\>', $null).Replace('\`', '`')

        # Escape and slashes within a parameter's `Default value` yaml property.
        $newContent = [System.Text.RegularExpressions.Regex]::Replace($newContent, '(?<=^Default value: .*?)(\\)', '\\', [System.Text.RegularExpressions.RegexOptions]::Multiline)

        # Write the content back to disk if there's changes.
        if ($newContent -ne $content)
        {
            [System.IO.File]::WriteAllLines($_.FullName, $newContent.Split("`n").TrimEnd())
        }
    }
    Write-Build Gray '           ...Markdown replacements complete.'

    # Validate Guid of export is correct.
    Write-Build Gray '           Verifying GUID...'
    if (Select-String -Path "$Script:MarkdownExportPath*.md" -Pattern "(00000000-0000-0000-0000-000000000000)")
    {
        Write-Build Yellow '             The documentation that got generated resulted in a generic GUID. Check the GUID entry of your module manifest.'
        throw 'Missing GUID. Please review and rebuild.'
    }

    # Perform amendments for PowerShell 7.4.x or higher targets.
    # https://github.com/PowerShell/platyPS/issues/595
    Write-Build Gray '           Evaluating if running 7.4.0 or higher...'
    if ($PSVersionTable.PSVersion -ge [version]'7.4.0')
    {
        Write-Build Gray '               Performing Markdown repair'
        . $BuildRoot\Tools\MarkdownRepair.ps1
        $Script:MarkdownExportPath | Get-ChildItem -File | ForEach-Object {
            Repair-PlatyPSMarkdown -Path $_.FullName
        }
    }

    # Validate nothing is missing.
    Write-Build Gray '           Checking for missing documentation in md files...'
    if ((($MissingDocumentation = Select-String -Path "$Script:MarkdownExportPath*.md" -Pattern "({{.*}})") | Measure-Object).Count -gt 0)
    {
        Write-Build Yellow '             The documentation that got generated resulted in missing sections which should be filled out.'
        Write-Build Yellow '             Please review the following sections in your comment based help, fill out missing information and rerun this build:'
        Write-Build Yellow '             (Note: This can happen if the .EXTERNALHELP CBH is defined for a function before running this build.)'
        Write-Build Yellow "             Path of files with issues: $Script:MarkdownExportPath"
        $MissingDocumentation | Select-Object FileName, LineNumber, Line | Format-Table -AutoSize
        throw 'Missing documentation. Please review and rebuild.'
    }

    # Validate all exports have a synopsis.
    Write-Build Gray '           Checking for missing SYNOPSIS in md files...'
    $fSynopsisOutput = Select-String -Path "$Script:MarkdownExportPath*.md" -Pattern "^## SYNOPSIS$" -Context 0, 1 | ForEach-Object {
        if ($null -eq $_.Context.DisplayPostContext.ToCharArray())
        {
            $_.FileName
        }
    }
    if ($fSynopsisOutput)
    {
        Write-Build Yellow "             The following files are missing SYNOPSIS:"
        $fSynopsisOutput
        throw 'SYNOPSIS information missing. Please review.'
    }
    Write-Build Gray '           ...Markdown generation complete.'
}

# Synopsis: Build the external xml help file from markdown help files with PlatyPS.
Add-BuildTask CreateExternalHelp -After CreateMarkdownHelp $null; $null = {
    Write-Build Gray '           Creating external xml help file...'
    $null = New-ExternalHelp $Script:MarkdownExportPath -OutputPath "$Script:ArtifactsPath\en-US\" -Force
    Write-Build Gray '           ...External xml help file created!'
}

# Synopsis: Build docusaurus help files from our markdown exports.
Add-BuildTask CreateDocusaurusHelp -After CreateMarkdownHelp {
    Write-Build Gray '           Generating docusaurus files...'
    New-DocusaurusHelp -PlatyPSMarkdownPath $Script:MarkdownExportPath -DocsFolder $Script:DocusaurusExportPath -NoPlaceHolderExamples | Where-Object { $_ -isnot [System.IO.DirectoryInfo] }
    Write-Build Gray '           ...Docusaurus generation complete.'
    Write-Build Gray '           Unescape forced line break elements...'
    Get-ChildItem -Path "$($Script:DocusaurusExportPath)Commands\*.mdx" | ForEach-Object {
        # Trim the file, fix hard-coded line breaks, and fix manually defined code fences.
        if (($content = [System.IO.File]::ReadAllText($_.FullName)) -ne ($newContent = $content.Trim().Replace('&lt;br /&gt;', '<br />') -replace '```\s+```powershell\s+```powershell\s+', "``````powershell`n"))
        {
            [System.IO.File]::WriteAllLines($_.FullName, $newContent.Split("`n").TrimEnd())
        }
    }
    Write-Build Gray '           ...Forced line break unescaping complete.'
}

Add-BuildTask CreateHelpComplete -After CreateExternalHelp {
    Write-Build Green '      ...CreateHelp Complete!'
}

# Synopsis: Replace comment based help (CBH) with external help in all public functions for this project.
Add-BuildTask UpdateCBH -After AssetCopy $null; $null = {
    # Define replacements.
    $CBHPattern = "(?ms)(\<#.*\.SYNOPSIS.*?#>)"
    $ExternalHelp = @"
<#
    .EXTERNALHELP $($Script:ModuleName)-help.xml
    #>
"@

    # Perform replacements as required.
    Get-ChildItem -Path "$Script:ArtifactsPath\Public\*.ps1" -File | ForEach-Object {
        $FormattedOutFile = $_.FullName
        Write-Output "      Replacing CBH in file: $($FormattedOutFile)"
        $UpdatedFile = (Get-Content  $FormattedOutFile -Raw) -replace $CBHPattern, $ExternalHelp
        $UpdatedFile | Out-File -FilePath $FormattedOutFile -Force -Encoding:utf8
    }
}

# Synopsis: Copies module assets to Artifacts folder.
Add-BuildTask AssetCopy -Before Build {
    Write-Build White '      Copying assets to Artifacts...'
    New-Item -Path $Script:BuildModuleRoot -ItemType Directory -Force | Out-Null
    Copy-Item -Path "$Script:ModuleSourcePath\*" -Destination $Script:BuildModuleRoot -Exclude "$($Script:ModuleName).ps*1" -Recurse
    foreach ($buildItem in $Script:buildItems)
    {
        $sourcePath = [System.IO.Path]::Combine($Script:RepoRootPath, $buildItem.SolutionPath.Replace('.slnx', ''), 'bin\Release\net462\*')
        $buildItem.OutputPath.Replace("src\PSAppDeployToolkit\", $null) | ForEach-Object {
            $destPath = [System.IO.Path]::Combine($Script:BuildModuleRoot, $_)
            Write-Build Gray "        Copying from $sourcePath to $destPath..."
            Copy-Item -Path $sourcePath -Destination $destPath -Recurse -Force
        }
    }

    # For Invoke-AppDeployToolkit.exe, we need an additional check to make sure the assembly is renamed.
    if ([System.IO.File]::Exists("$($Script:BuildModuleRoot)\Frontend\v3\Invoke-AppDeployToolkit.exe"))
    {
        Remove-Item -LiteralPath "$($Script:BuildModuleRoot)\Frontend\v3\Deploy-Application.exe" -Force -Confirm:$false
        Rename-Item -LiteralPath "$($Script:BuildModuleRoot)\Frontend\v3\Invoke-AppDeployToolkit.exe" -NewName Deploy-Application.exe -Force -Confirm:$false
    }
    Write-Build Green '      ...Assets Copy Complete!'
}

# Synopsis: Builds the Module to the Artifacts folder.
Add-BuildTask Build {
    # Perform initial module manifest copy.
    Write-Build White '      Performing Module Build'
    Write-Build Gray '        Copying manifest file to Artifacts...'
    Copy-Item -Path $Script:ModuleManifestFile -Destination $Script:BuildModuleRoot -Recurse
    Write-Build Gray '        ...manifest copy complete.'

    # Compile the project into a singular psm1 file.
    Write-Build Gray '        Merging Public and Private functions to one module file...'
    $scriptContent = foreach ($file in (Get-ChildItem -Path $Script:BuildModuleRoot\ImportsFirst.ps1, $Script:BuildModuleRoot\Private\*.ps1, $Script:BuildModuleRoot\Public\*.ps1, $Script:BuildModuleRoot\ImportsLast.ps1 -Recurse))
    {
        # Import the script file as a string for substring replacement.
        $text = [System.IO.File]::ReadAllText($file.FullName).Trim()

        # Parse the ps1 file and store its AST.
        $tokens = $null
        $errors = $null
        $scrAst = [System.Management.Automation.Language.Parser]::ParseInput($text, [ref]$tokens, [ref]$errors)

        # Throw if we had any parsing errors.
        if ($errors)
        {
            throw "Received $(($errCount = ($errors | Measure-Object).Count)) error$(if (!$errCount.Equals(1)) {'s'}) while parsing [$($file.Name)]."
        }

        # Throw if we don't have exactly one statement.
        if (!$scrAst.EndBlock.Statements.Count.Equals(1) -and ($file.Name -notmatch '^Imports(First|Last)\.ps1$'))
        {
            throw "More than one statement is defined in [$($file.Name)]."
        }

        # Throw if there's any AST values matching PowerShell's environment provider.
        if ($scrAst.FindAll({ $args[0].ToString() -match '^(\$?env:|(Microsoft.PowerShell.Core\\)?Environment::)' }, $true).Count)
        {
            throw "The usage of PowerShell environment provider or drive within [$($file.Name)] is forbidden."
        }

        # If our file isn't internal, redefine its command calls to be via the module's CommandTable.
        if (!$file.BaseName.EndsWith('Internal'))
        {
            # Recursively get all CommandAst objects that have an unknown InvocationOperator (bare word within a script).
            $commandAsts = $scrAst.FindAll({ ($args[0] -is [System.Management.Automation.Language.CommandAst]) -and $args[0].InvocationOperator.Equals([System.Management.Automation.Language.TokenKind]::Unknown) }, $true)

            # Throw if there's a found CommandAst object where the first command element isn't a bare word (something unknown has happened here).
            if ($commandAsts.GetEnumerator().ForEach({ if (($_.CommandElements[0] -isnot [System.Management.Automation.Language.StringConstantExpressionAst]) -or !$_.CommandElements[0].StringConstantType.Equals([System.Management.Automation.Language.StringConstantType]::BareWord)) { return $_ } }).Count)
            {
                throw "One or more found CommandAst objects within [$($file.Name)] were invalid."
            }

            # Get all bare-word constants and process in reverse. We reverse the list so that we
            # do the last found items first so the substring values in the AST are always correct.
            $commandAsts | & { process { $_.CommandElements[0].Extent } } | Sort-Object -Property EndOffset -Descending | . {
                process
                {
                    # Don't replace the calls to any internally defined functions.
                    if (!$_.Text.Equals($file.BaseName) -and $scrAst.FindAll({ ($args[0] -is [System.Management.Automation.Language.FunctionDefinitionAst]) -and $args[0].Name.Equals($_.Text) }, $true).Count)
                    {
                        return
                    }

                    # Throw if the CommandTable doesn't contain the command.
                    if (!$Script:moduleCommandTable.ContainsKey($_.Text))
                    {
                        throw "Unable to find the command [$($_.Text)] from [$($file.Name)] within the module's CommandTable."
                    }

                    # Remove the offending text and replace with a CommandTable access.
                    $text = $text.Remove($_.StartOffset, $_.EndOffset - $_.StartOffset)
                    $text = $text.Insert($_.StartOffset, "& `$Script:CommandTable.'$($_.Text)'")
                }
            }
        }

        # Write out the processed file back to disk.
        $text; [System.String]::Empty; [System.String]::Empty
    }
    [System.IO.File]::WriteAllLines($Script:BuildModuleRootFile, $scriptContent)
    Write-Build Gray '        ...Module creation complete.'

    # Clean up artifacts that are no longer required.
    Write-Build Gray '        Cleaning up leftover artifacts...'
    if (Test-Path "$Script:BuildModuleRoot\Public")
    {
        Remove-Item "$Script:BuildModuleRoot\Public" -Recurse -Force
    }
    if (Test-Path "$Script:BuildModuleRoot\Private")
    {
        Remove-Item "$Script:BuildModuleRoot\Private" -Recurse -Force
    }
    if (Test-Path "$Script:BuildModuleRoot\ImportsFirst.ps1")
    {
        Remove-Item "$Script:BuildModuleRoot\ImportsFirst.ps1" -Force -ErrorAction Ignore
    }
    if (Test-Path "$Script:BuildModuleRoot\ImportsLast.ps1")
    {
        Remove-Item "$Script:BuildModuleRoot\ImportsLast.ps1" -Force -ErrorAction Ignore
    }

    # Remove any PDB files that might have snuck in.
    Write-Build Gray '        Removing PDB files from output...'
    Get-ChildItem -LiteralPath $Script:BuildModuleRoot -Filter *.pdb -Recurse | Remove-Item -Force
    Write-Build Gray '        ...PDB removal completed.'

    # Update the parent level docs.
    if (Test-Path $Script:MarkdownExportPath)
    {
        Write-Build Gray '        Overwriting docs output...'
        if (!(Test-Path '..\docs\'))
        {
            New-Item -Path '..\docs\' -ItemType Directory -Force | Out-Null
        }
        Get-ChildItem -LiteralPath '..\docs\' -File | Remove-Item -Force -Confirm:$false
        Move-Item "$($Script:DocusaurusExportPath)Commands\*" -Destination '..\docs\' -Force
        Remove-Item $Script:DocusaurusExportPath -Recurse -Force
        Remove-Item $Script:MarkdownExportPath -Recurse -Force
        Write-Build Gray '        ...Docs output completed.'
    }

    # Sign our files if we're running on a branch enabled for code-signing.
    if (($canSign = ($env:GITHUB_ACTIONS -eq 'true') -and ($env:GITHUB_REF_NAME -match '^(main|develop|4.0.x)$')))
    {
        if (!(Get-Command -Name 'azuresigntool' -ErrorAction Ignore))
        {
            throw 'AzureSignTool not found.'
        }
        Write-Build Gray '        Signing module...'
        Get-ChildItem -Path $Script:BuildModuleRoot -Include '*.ps*1', 'PSADT*.dll', 'PSADT.ClientServer.Client.exe', 'Wpf.Ui*.dll', 'Deploy-Application.exe', 'Invoke-AppDeployToolkit.exe' -Recurse | ForEach-Object {
            & azuresigntool sign -s -kvu https://psadt-kv-prod-codesign.vault.azure.net -kvc PSADT -kvm -tr http://timestamp.digicert.com -td sha256 "$_"
            if ($LASTEXITCODE -ne 0) { throw "Failed to sign file `"$_`". Exit code: $LASTEXITCODE" }
        }
    }
    else
    {
        Write-Build Yellow '        Not running main or develop branch in GitHub Actions, skipping code signing...'
    }

    # Create our templates.
    Write-Build Gray '        Creating templates...'
    $spParams = @{
        FilePath = [System.Diagnostics.Process]::GetCurrentProcess().Path
        ArgumentList = "$(if (!$canSign) {"-ExecutionPolicy Bypass "})-NonInteractive -NoProfile -NoLogo -Command `$ErrorActionPreference = 'Stop'; Import-Module -Name '$Script:BuildModuleRoot'; $([System.String]::Join('; ', (3, 4).ForEach({"New-ADTTemplate -Destination '$Script:ArtifactsPath' -Name 'Template_v$_' -Version $_"})))"
        NoNewWindow = $true
        Wait = $true
    }
    if ((Start-Process @spParams -PassThru).ExitCode -ne 0)
    {
        throw "Failed to generate frontend templates."
    }
    Write-Build Green '      ...Build Complete!'
}

# Synopsis: Invokes all Pester Integration Tests in the Tests\Integration folder (if it exists).
Add-BuildTask IntegrationTest {
    if (Test-Path -Path $Script:IntegrationTestsPath)
    {
        # (Re-)Import Pester module.
        Write-Build White "      Importing desired Pester version. Min: $Script:MinPesterVersion Max: $Script:MaxPesterVersion"
        Remove-Module -Name Pester -Force -ErrorAction Ignore  # there are instances where some containers have Pester already in the session
        Import-Module -Name Pester -MinimumVersion $Script:MinPesterVersion -MaximumVersion $Script:MaxPesterVersion -ErrorAction 'Stop'

        # Perform integration testing.
        Write-Build White "      Performing Pester Integration Tests..."
        $pesterConfiguration = New-PesterConfiguration
        $pesterConfiguration.run.Path = $Script:IntegrationTestsPath
        $pesterConfiguration.Run.PassThru = $true
        $pesterConfiguration.Run.Exit = $false
        $pesterConfiguration.CodeCoverage.Enabled = $false
        $pesterConfiguration.TestResult.Enabled = $false
        $pesterConfiguration.Output.Verbosity = 'Detailed'
        $testResults = Invoke-Pester -Configuration $pesterConfiguration

        # This will output a nice json for each failed test (if running in CodeBuild).
        if ($env:CODEBUILD_BUILD_ARN)
        {
            $testResults.TestResult | ForEach-Object {
                if ($_.Result -ne 'Passed')
                {
                    ConvertTo-Json -InputObject $_ -Compress
                }
            }
        }

        # Report on failures.
        $numberFails = $testResults.FailedCount
        Assert-Build($numberFails -eq 0) ('Failed "{0}" unit tests.' -f $numberFails)
        Write-Build Green '      ...Pester Integration Tests Complete!'
    }
}
