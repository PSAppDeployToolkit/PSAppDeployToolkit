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
        - Archive

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

# Function to get GitHub release URLs.
function Get-GitHubReleaseAssetUri
{
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'FilePattern', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [CmdletBinding()]
    [OutputType([System.Uri])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Account,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Repository,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$FilePattern,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Regex
    )

    # Get the list of URLs from GitHub's API.
    $links = (Invoke-RestMethod -UseBasicParsing -Uri "https://api.github.com/repos/$Account/$Repository/releases/latest").assets.browser_download_url
    $match = if ($Regex) { { $_ -match $FilePattern } } else { { $_ -like $FilePattern } }

    # Find the one that matches the pattern and confirm we have a singular result.
    if (!(($link = $links | Where-Object { $_.Split('/').Where($match) }) | Measure-Object).Count.Equals(1))
    {
        $PSCmdlet.ThrowTerminatingError([System.Management.Automation.ErrorRecord]::new(
                [System.InvalidOperationException]::new("The match against the provided file pattern returned an invalid result."),
                'UriPatternMatchInvalidResult',
                [System.Management.Automation.ErrorCategory]::InvalidResult,
                $link
            ))
    }
    return [System.Uri]$link
}

# Default variables.
$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
$ProgressPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue
Set-StrictMode -Version 3
$ModuleName = [System.Text.RegularExpressions.Regex]::Match((Get-Item $BuildFile).Name, '^(.*)\.build\.ps1$').Groups[1].Value
$BuildScriptPath = $MyInvocation.MyCommand.Path
[System.Version]$requiredPSVersion = '5.1.0'

# Define our C# solutions to compile.
$buildItems = @(
    @{
        SourcePath = 'Sources\PSADT'
        SolutionPath = 'Sources\PSADT\PSADT.sln'
        OutputPath = 'src\PSAppDeployToolkit\lib'
        OutputFile = 'src\PSAppDeployToolkit\lib\net462\PSADT.dll'
    },
    @{
        SourcePath = 'Sources\PSADT.Invoke'
        SolutionPath = 'Sources\PSADT.Invoke\PSADT.Invoke.sln'
        OutputPath = 'src\PSAppDeployToolkit\Frontend\v4', 'src\PSAppDeployToolkit\Frontend\v3'
        OutputFile = 'src\PSAppDeployToolkit\Frontend\v4\Invoke-AppDeployToolkit.exe', 'src\PSAppDeployToolkit\Frontend\v3\Deploy-Application.exe'
    },
    @{
        SourcePath = 'Sources\PSADT.UserInterface'
        SolutionPath = 'Sources\PSADT.UserInterface\PSADT.UserInterface.sln'
        OutputPath = 'src\PSAppDeployToolkit\lib'
        OutputFile = 'src\PSAppDeployToolkit\lib\net462\PSADT.UserInterface.dll'
    }
)

# Default build.
$str = @()
$str = 'Clean', 'ValidateRequirements', 'ImportModuleManifest'
$str += 'FormattingCheck'
$str += 'Analyze', 'Test'
$str += 'CreateHelpStart'
$str2 = $str
$str2 += 'Build', 'Archive'
$str += 'Build', 'IntegrationTest', 'Archive'
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
    $Script:ArchivePath = Join-Path -Path $BuildRoot -ChildPath 'Archive'
    $Script:BuildModuleRoot = Join-Path -Path $Script:ArtifactsPath -ChildPath "Module\$Script:ModuleName"
    $Script:BuildModuleRootFile = Join-Path -Path $Script:BuildModuleRoot -ChildPath "$($Script:ModuleName).psm1"

    # Import this module's manifest via the language parser. This allows us to test with potential extra variables that are permitted in manifests.
    # https://github.com/PowerShell/PowerShell/blob/7ca7aae1d13d19e38c7c26260758f474cb9bef7f/src/System.Management.Automation/engine/Modules/ModuleCmdletBase.cs#L509-L512
    $manifestInfo = [System.Management.Automation.Language.Parser]::ParseFile($Script:ModuleManifestFile, [ref]$null, [ref]$null).GetScriptBlock()
    $manifestInfo.CheckRestrictedLanguage([System.String[]]$null, [System.String[]]('PSEdition'), $true); $manifestInfo = $manifestInfo.InvokeReturnAsIs()
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
Add-BuildTask DotNetBuild -Before TestModuleManifest {
    # Find Visual Studio on the current device.
    Write-Build White '      Compiling C# projects...'
    Write-Build Gray '        Downloading vswhere.exe to find msbuild.exe...'
    $vswhereUri = Get-GitHubReleaseAssetUri -Account Microsoft -Repository vswhere -FilePattern ($vswhereExe = 'vswhere.exe')
    Invoke-WebRequest -UseBasicParsing -Uri $vswhereUri -OutFile ($vswhereExe = "$([System.IO.Path]::GetTempPath())$vswhereExe")
    if (!($msbuildPath = & $vswhereExe -requires Microsoft.Component.MSBuild -find MSBuild\Current\Bin\MSBuild.exe))
    {
        throw 'msbuild.exe command not found. Ensure Visual Studio is installed on this system.'
    }

    # Process each build item.
    Write-Build Gray '        Determining C# solutions requiring compilation...'
    foreach ($buildItem in $Script:buildItems)
    {
        if ($env:GITHUB_ACTIONS -ne 'true')
        {
            if (!((git status --porcelain) -match "^.{3}$([regex]::Escape($buildItem.SourcePath.Replace('\','/')))/"))
            {
                # Get the last commit date of the output file, which is similar to ISO 8601 format but with spaces and no T between date and time
                $buildItem.OutputFile | ForEach-Object {
                    # Get commit date via git, parse the result, then add one second and convert to proper ISO 8601 format..
                    $lastCommitDate = git log -1 --format="%ci" -- [System.IO.Path]::Combine($Script:RepoRootPath, $_)
                    $lastCommitDate = [DateTime]::ParseExact($lastCommitDate, "yyyy-MM-dd HH:mm:ss K", [System.Globalization.CultureInfo]::InvariantCulture)
                    $sinceDateString = $lastCommitDate.AddSeconds(1).ToString('yyyy-MM-ddTHH:mm:ssK')

                    # Get the list of source files modified since the last commit date of the file we're comparing against
                    if (!(git log --name-only --since=$sinceDateString --diff-filter=ACDMTUXB --pretty=format: -- [System.IO.Path]::Combine($Script:RepoRootPath, $buildItem.SourcePath) | Where-Object { ![string]::IsNullOrWhiteSpace($buildItem) } | Sort-Object -Unique))
                    {
                        Write-Build Gray "          No files have been modified in $($buildItem.SourcePath), nothing to build."
                        continue
                    }
                    Write-Build Blue "          Files have been modified in $($buildItem.SourcePath) since the last commit date of $_ ($lastCommitDate), build required."
                }
            }
            else
            {
                Write-Build Blue "          Uncommitted file changes found under $($buildItem.SourcePath), build required."
            }
        }

        # Build a debug and release config of each project.
        Write-Build Gray "            Building $(($solutionPath = [System.IO.Path]::Combine($Script:RepoRootPath, $buildItem.SolutionPath)))..."
        & $msbuildPath $solutionPath -target:Rebuild -restore -p:configuration=Release -p:platform="Any CPU" -nodeReuse:false -m
        if ($LASTEXITCODE) { throw "Failed to build solution `"$($buildItem.SolutionPath -replace '^.+\\')`". Exit code: $LASTEXITCODE" }
        & $msbuildPath $solutionPath -target:Rebuild -restore -p:configuration=Debug -p:platform="Any CPU" -nodeReuse:false -m
        if ($LASTEXITCODE) { throw "Failed to build solution `"$($buildItem.SolutionPath -replace '^.+\\')`". Exit code: $LASTEXITCODE" }

        # Copy the debug configuration into the module's folder within the repo. The release copy will come later on directly into the artifact.
        $sourcePath = [System.IO.Path]::Combine($Script:RepoRootPath, $buildItem.SolutionPath.Replace('.sln', ''), 'bin\Debug\*')
        $buildItem.OutputPath | ForEach-Object {
            Write-Build Gray "          Copying from  $sourcePath to $(($destPath = [System.IO.Path]::Combine($Script:RepoRootPath, $_)))..."
            Copy-Item -Path $sourcePath -Destination $destPath -Recurse -Force
        }
    }

    # For Invoke-AppDeployToolkit.exe, we need an additional check to make sure the assembly is renamed.
    if ([System.IO.File]::Exists("$($Script:RepoRootPath)\src\PSAppDeployToolkit\Frontend\v3\Invoke-AppDeployToolkit.exe"))
    {
        Remove-Item -LiteralPath "$($Script:RepoRootPath)\src\PSAppDeployToolkit\Frontend\v3\Deploy-Application.exe" -Force -Confirm:$false
        Rename-Item -LiteralPath "$($Script:RepoRootPath)\src\PSAppDeployToolkit\Frontend\v3\Invoke-AppDeployToolkit.exe" -NewName Deploy-Application.exe -Force -Confirm:$false
    }
    Write-Build Green '      ...C# Compilation Complete!'
}

# Synopsis: Import the current module manifest file for processing.
Add-BuildTask TestModuleManifest -Before ImportModuleManifest {
    Write-Build White '      Running module manifest tests...'
    Assert-Build (Test-Path $Script:ModuleManifestFile) 'Unable to locate the module manifest file.'
    Assert-Build (Get-ChildItem $Script:ModuleManifestFile | Test-ModuleManifest -ErrorAction Ignore) 'Module Manifest test did not pass verification.'
    Write-Build Green '      ...Module Manifest Verification Complete!'
}

# Synopsis: Load the module project.
Add-BuildTask ImportModuleManifest {
    Write-Build White '      Attempting to load the project module.'
    $Script:moduleCommandTable = & (Import-Module $Script:ModuleManifestFile -Force -PassThru) { $CommandTable }
    Write-Build Green "      ...$Script:ModuleName imported successfully"
}

# Synopsis: Clean and reset Artifacts and Archive directories.
Add-BuildTask Clean {
    Write-Build White '      Clean up our Artifacts/Archive directory...'
    $null = Remove-Item $Script:ArtifactsPath -Force -Recurse -ErrorAction Ignore
    $null = New-Item $Script:ArtifactsPath -ItemType Directory
    $null = Remove-Item $Script:ArchivePath -Force -Recurse -ErrorAction Ignore
    $null = New-Item $Script:ArchivePath -ItemType Directory
    Write-Build Green '      ...Clean Complete!'
}

# Synopsis: Analyze scripts to verify if they adhere to desired coding format (Stroustrup / OTBS / Allman).
Add-BuildTask FormattingCheck {
    Write-Build White '      Performing script formatting checks...'
    if (($scriptAnalyzerResults = $Script:BuildScriptPath, $Script:ModuleSourcePath | Invoke-ScriptAnalyzer -Setting CodeFormattingAllman -ExcludeRule PSAlignAssignmentStatement -Recurse -Fix:($env:GITHUB_ACTIONS -ne 'true') -Verbose:$false))
    {
        $scriptAnalyzerResults | Format-Table
        throw '      PSScriptAnalyzer code formatting check did not adhere to {0} standards' -f $scriptAnalyzerParams.Setting
    }
    Write-Build Green '      ...Formatting Analyze Complete!'
}

# Synopsis: Invokes PSScriptAnalyzer against the Module source path.
Add-BuildTask Analyze {
    Write-Build White '      Performing Module ScriptAnalyzer checks...'
    if (($scriptAnalyzerResults = $Script:BuildScriptPath, $Script:ModuleSourcePath | Invoke-ScriptAnalyzer -Setting PSScriptAnalyzerSettings.psd1 -Recurse -Verbose:$false))
    {
        $scriptAnalyzerResults | Format-Table
        throw '      One or more PSScriptAnalyzer errors/warnings where found.'
    }
    Write-Build Green '      ...Module Analyze Complete!'
}

# Synopsis: Invokes Script Analyzer against the Tests path if it exists.
Add-BuildTask AnalyzeTests -After Analyze {
    if (Test-Path -Path $Script:TestsPath)
    {
        Write-Build White '      Performing Test ScriptAnalyzer checks...'
        if (($scriptAnalyzerResults = Invoke-ScriptAnalyzer -Path $Script:TestsPath -Setting PSScriptAnalyzerSettings.psd1 -ExcludeRule PSUseDeclaredVarsMoreThanAssignments -Recurse -Verbose:$false))
        {
            $scriptAnalyzerResults | Format-Table
            throw '      One or more PSScriptAnalyzer errors/warnings where found.'
        }
        Write-Build Green '      ...Test Analyze Complete!'
    }
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
        $pesterConfiguration.CodeCoverage.Path = "..\..\..\src\$ModuleName\*\*.ps1"
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
    $pesterConfiguration.CodeCoverage.Path = "$PSScriptRoot\$ModuleName\*\*.ps1"
    $pesterConfiguration.CodeCoverage.CoveragePercentTarget = $Script:coverageThreshold
    $pesterConfiguration.CodeCoverage.OutputPath = '..\..\..\cov.xml'
    $pesterConfiguration.CodeCoverage.OutputFormat = 'CoverageGutters'
    Invoke-Pester -Configuration $pesterConfiguration
    Write-Build Green '      ...Code Coverage report generated!'
}

# Synopsis: Build help for module.
Add-BuildTask CreateHelpStart {
    Write-Build White '      Performing all help related actions.'
    Write-Build Gray '           Importing platyPS v0.12.0 ...'
    Import-Module platyPS -RequiredVersion 0.12.0
    Write-Build Gray '           ...platyPS imported successfully.'
}

# Synopsis: Build markdown help files for module and fail if help information is missing.
Add-BuildTask CreateMarkdownHelp -After CreateHelpStart {
    # Generate markdown files.
    Write-Build Gray '           Generating markdown files...'
    $null = New-MarkdownHelp -Module $ModuleName -OutputFolder "$Script:ArtifactsPath\docs\" -Locale en-US -FwLink NA -HelpVersion $Script:ModuleVersion -WithModulePage -Force
    Write-Build Gray '           ...Markdown generation completed.'

    # Replace multi-line EXAMPLES.
    Write-Build Gray '           Replacing markdown elements...'
    ($OutputDir = "$Script:ArtifactsPath\docs\") | Get-ChildItem -File | ForEach-Object {
        $content = [System.IO.File]::ReadAllText($_.FullName)
        $newContent = $content.Trim() -replace '(## EXAMPLE [^`]+?```\r\n[^`\r\n]+?\r\n)(```\r\n\r\n)([^#]+?\r\n)(\r\n)([^#]+)(#)', '$1$3$2$4$5$6'
        if ($newContent -ne $content)
        {
            [System.IO.File]::WriteAllLines($_.FullName, $newContent.Split("`n").TrimEnd())
        }
    }

    # Replace each missing element we need for a proper generic module page .md file
    $ModulePageFileContent = Get-Content -Raw ($ModulePage = "$Script:ArtifactsPath\docs\$($ModuleName).md")
    $ModulePageFileContent = $ModulePageFileContent -replace '{{Manually Enter Description Here}}', $Script:ModuleDescription
    $Script:FunctionsToExport | ForEach-Object {
        Write-Build DarkGray "             Updating definition for the following function: $($_)"
        $TextToReplace = "{{Manually Enter $($_) Description Here}}"
        $ReplacementText = (Get-Help -Detailed $_).Synopsis
        $ModulePageFileContent = $ModulePageFileContent -replace $TextToReplace, $ReplacementText
    }
    $ModulePageFileContent | Out-File $ModulePage -Force -Encoding:utf8
    Write-Build Gray '           ...Markdown replacements complete.'

    # Validate Guid of export is correct.
    Write-Build Gray '           Verifying GUID...'
    if (Select-String -Path "$Script:ArtifactsPath\docs\*.md" -Pattern "(00000000-0000-0000-0000-000000000000)")
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
        . $BuildRoot\MarkdownRepair.ps1
        $OutputDir | Get-ChildItem -File | ForEach-Object {
            Repair-PlatyPSMarkdown -Path $_.FullName
        }
    }

    # Validate nothing is missing.
    Write-Build Gray '           Checking for missing documentation in md files...'
    if ((($MissingDocumentation = Select-String -Path "$Script:ArtifactsPath\docs\*.md" -Pattern "({{.*}})") | Measure-Object).Count -gt 0)
    {
        Write-Build Yellow '             The documentation that got generated resulted in missing sections which should be filled out.'
        Write-Build Yellow '             Please review the following sections in your comment based help, fill out missing information and rerun this build:'
        Write-Build Yellow '             (Note: This can happen if the .EXTERNALHELP CBH is defined for a function before running this build.)'
        Write-Build Yellow "             Path of files with issues: $Script:ArtifactsPath\docs\"
        $MissingDocumentation | Select-Object FileName, LineNumber, Line | Format-Table -AutoSize
        throw 'Missing documentation. Please review and rebuild.'
    }

    # Validate all exports have a synopsis.
    Write-Build Gray '           Checking for missing SYNOPSIS in md files...'
    $fSynopsisOutput = Select-String -Path "$Script:ArtifactsPath\docs\*.md" -Pattern "^## SYNOPSIS$" -Context 0, 1 | ForEach-Object {
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
    $null = New-ExternalHelp "$Script:ArtifactsPath\docs" -OutputPath "$Script:ArtifactsPath\en-US\" -Force
    Write-Build Gray '           ...External xml help file created!'
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
    .EXTERNALHELP $($ModuleName)-help.xml
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
        $sourcePath = [System.IO.Path]::Combine($Script:RepoRootPath, $buildItem.SolutionPath.Replace('.sln', ''), 'bin\Release\*')
        $buildItem.OutputPath.Replace("src\PSAppDeployToolkit\", $null) | ForEach-Object {
            $destPath = [System.IO.Path]::Combine($Script:BuildModuleRoot, $_)
            Write-Build Gray "        Copying from  $sourcePath to $destPath..."
            Copy-Item -Path $sourcePath -Destination $destPath -Recurse -Force
        }
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

        # If our file isn't internal, redefine its command calls to be via the module's CommandTable.
        if (!$file.BaseName.EndsWith('Internal') -and !$file.BaseName.StartsWith('Imports'))
        {
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
            if (!$scrAst.EndBlock.Statements.Count.Equals(1))
            {
                throw "More than one statement is defined in [$($file.Name)]."
            }

            # Recursively get all CommandAst objects that have an unknown InvocationOperator (bare word within a script).
            $commandAsts = $scrAst.FindAll({ ($args[0] -is [System.Management.Automation.Language.CommandAst]) -and $args[0].InvocationOperator.Equals([System.Management.Automation.Language.TokenKind]::Unknown) }, $true)

            # Throw if there's a found CommandAst object where the first command element isn't a bare word (something unknowh has happened here).
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
                    if (!$Script:moduleCommandTable.Contains($_.Text))
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

    # Update the parent level docs.
    if (Test-Path "$Script:ArtifactsPath\docs")
    {
        Write-Build Gray '        Overwriting docs output...'
        if (-not (Test-Path '..\docs\'))
        {
            New-Item -Path '..\docs\' -ItemType Directory -Force | Out-Null
        }
        Move-Item "$Script:ArtifactsPath\docs\*.md" -Destination '..\docs\' -Force
        Remove-Item "$Script:ArtifactsPath\docs" -Recurse -Force
        Write-Build Gray '        ...Docs output completed.'
    }

    # Sign our files if we're running on main.
    if ($env:GITHUB_ACTIONS -eq 'true' -and $env:GITHUB_REF -eq 'refs/heads/main')
    {
        if (!(Get-Command -Name 'azuresigntool' -ErrorAction Ignore))
        {
            throw 'AzureSignTool not found.'
        }
        Write-Build Gray '        Signing module...'
        Get-ChildItem -Path $Script:BuildModuleRoot -Include '*.psm1', 'PSAppDeployToolkit.psd1', 'AppDeployToolkitMain.ps1', 'PSADT*.dll', 'Deploy-Application.exe', 'Invoke-AppDeployToolkit.exe' -Exclude 'PSAppDeployToolkit.Extensions.psm1' -Recurse | ForEach-Object {
            & azuresigntool sign -s -kvu https://psadt-kv-prod-codesign.vault.azure.net -kvc PSADT -kvm -tr http://timestamp.digicert.com -td sha256 "$_"
            if ($LASTEXITCODE -ne 0) { throw "Failed to sign file `"$_`". Exit code: $LASTEXITCODE" }
        }
    }
    else
    {
        Write-Build Yellow '        Not running main branch in GitHub Actions, skipping code signing...'
    }

    # Create our templates.
    Write-Build Gray '        Creating templates...'
    New-ADTTemplate -Destination $Script:ArtifactsPath -Name 'Template_v3' -Version 3 -ModulePath $Script:BuildModuleRoot
    New-ADTTemplate -Destination $Script:ArtifactsPath -Name 'Template_v4' -Version 4 -ModulePath $Script:BuildModuleRoot
    New-ADTTemplate -Destination $Script:ArtifactsPath -Name 'Template_v4_PSCore' -Version 4 -PSCore -ModulePath $Script:BuildModuleRoot
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

# Synopsis: Creates an archive of the built module.
Add-BuildTask Archive {
    # Set up required paths.
    Write-Build White '        Performing Archive...'
    if (Test-Path -Path ($archivePath = Join-Path -Path $BuildRoot -ChildPath Archive))
    {
        $null = Remove-Item -Path $archivePath -Recurse -Force
    }
    $null = New-Item -Path $archivePath -ItemType Directory -Force

    # Add in required assemblies for Windows PowerShell.
    if ($PSEdition -eq 'Desktop')
    {
        Add-Type -AssemblyName System.IO.Compression.FileSystem
    }

    # Perform archive process.
    Get-ChildItem -Path $Script:ArtifactsPath -Directory -Exclude ccReport, testOutput | ForEach-Object {
        $zipFileName = '{0}_{1}_{2}.zip' -f $Script:ModuleName, $Script:ModuleVersion, $_.Name
        $zipFilePath = Join-Path -Path $archivePath -ChildPath $zipFileName
        [System.IO.Compression.ZipFile]::CreateFromDirectory($_.FullName, $zipFilePath)
    }
    Write-Build Green '        ...Archive Complete!'
}
