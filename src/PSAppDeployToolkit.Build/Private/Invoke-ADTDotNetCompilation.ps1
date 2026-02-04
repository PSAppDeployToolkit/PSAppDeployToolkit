#-----------------------------------------------------------------------------
#
# MARK: Invoke-ADTDotNetCompilation
#
#-----------------------------------------------------------------------------

function Invoke-ADTDotNetCompilation
{
    # Initialise the module build function.
    Initialize-ADTModuleBuildFunction
    $testFileChanges = $true
    try
    {
        # Confirm whether we've got dotnet available and it's of a compatible version.
        Write-ADTBuildLogEntry -Message "Locating .NET SDK on this system as required for C# compilation."
        if (($null -eq ($dotnet = Get-Command -Name dotnet -ErrorAction Ignore)) -or ($dotnet.Source -notmatch '\\dotnet\.exe$'))
        {
            throw "Failed to locate the .NET SDK on this system. Please install and try again."
        }
        if ($dotnet.Version -lt $Script:ModuleConstants.MinimumDotNetSdkVersion)
        {
            throw "The currently installed .NET SDK version of $($dotnet.Version) is not supported. Please install .NET SDK $($Script:ModuleConstants.MinimumDotNetSdkVersion) or higher and try again."
        }
        Write-ADTBuildLogEntry -Message "Located .NET SDK [$($dotnet.Version)] at [$($dotnet.Source)]."
        $Script:ModuleBuildState.HaveDotNetSdk = $true

        # Confirm whether we've got a Git client present and whether we're in a repository or not.
        Write-ADTBuildLogEntry -Message "Locating Git on this system to determine whether debug DLLs require compilation."
        if ($testFileChanges -and ($null -eq ($git = Get-Command -Name git -ErrorAction Ignore)) -or ($git.Source -notmatch '\\git\.exe$'))
        {
            Write-ADTBuildLogEntry -Message "Unable to locate git.exe on this system, compiling C# project sources unconditionally." -ForegroundColor Yellow
            $testFileChanges = $false
        }
        if ($testFileChanges -and !$(try { & $git -C $Script:PSScriptRoot rev-parse --is-inside-work-tree 2>&1 } catch { 'false' }).Equals('true'))
        {
            Write-ADTBuildLogEntry -Message "Not currently building from a git repository, compiling C# project sources unconditionally." -ForegroundColor Yellow
            $testFileChanges = $false
        }
        if ($testFileChanges)
        {
            Write-ADTBuildLogEntry -Message "Located Git [$($git.Version)] at [$($git.Source)]."
        }
        else
        {
            Write-ADTBuildLogEntry -Message "Compiling C# project sources, this may take a while."
        }

        # Process each build item.
        foreach ($buildItem in $Script:ModuleConstants.DotNetBuildItems)
        {
            # Only build a debug version if we're outside of a GitHub pipeline.
            $buildConfigs = [System.Collections.Generic.List[System.String]]'Release'
            if ($env:GITHUB_ACTIONS -ne 'true')
            {
                # Build unconditionally if we can't test files.
                if ($testFileChanges)
                {
                    # Test each source path for changed/uncommitted files.
                    Write-ADTBuildLogEntry -Message "Determining C# solutions requiring compilation for [$([System.IO.Path]::GetFileName($buildItem.SolutionPath))], please wait..."
                    foreach ($sourcePath in $buildItem.SourcePath)
                    {
                        # Translate the Win32 slash into a POSIX slash so git can work on it.
                        $gitPath = $sourcePath.Replace("$($Script:ModuleConstants.Paths.Repository)\", [System.Management.Automation.Language.NullString]::Value)
                        if (!((& $git -C $Script:ModuleConstants.Paths.Repository status --porcelain) -match "^.{3}$([System.Text.RegularExpressions.Regex]::Escape($gitPath.Replace('\','/')))/"))
                        {
                            # Get the last commit date of the output file, which is similar to ISO 8601 format but with spaces and no T between date and time
                            foreach ($outputFile in $buildItem.OutputFile)
                            {
                                # Get commit date via git, parse the result, then add one second and convert to proper ISO 8601 format..
                                $lastCommitDate = & $git -C $Script:ModuleConstants.Paths.Repository log -1 --format="%ci" -- $outputFile
                                $lastCommitDate = [DateTime]::ParseExact($lastCommitDate, "yyyy-MM-dd HH:mm:ss K", [System.Globalization.CultureInfo]::InvariantCulture)
                                $sinceDateString = $lastCommitDate.AddSeconds(1).ToString('yyyy-MM-ddTHH:mm:ssK')

                                # Get the list of source files modified since the last commit date of the file we're comparing against
                                if (& $git -C $Script:ModuleConstants.Paths.Repository log --name-only --since=$sinceDateString --diff-filter=ACDMTUXB --pretty=format: -- $gitPath | & { process { if (![System.String]::IsNullOrWhiteSpace($_)) { return $_ } } } | Sort-Object -Unique)
                                {
                                    Write-ADTBuildLogEntry -Message "Files have been modified in [$sourcePath] since the last commit date of [$([System.IO.Path]::GetFileName($outputFile))] ($($lastCommitDate.ToString('yyyy-MM-ddTHH:mm:ssK'))), debug build required."
                                    $buildConfigs.Insert(0, 'Debug')
                                    break
                                }
                                else
                                {
                                    Write-ADTBuildLogEntry -Message "No files have been modified in [$sourcePath] since the last commit date of [$([System.IO.Path]::GetFileName($outputFile))] ($($lastCommitDate.ToString('yyyy-MM-ddTHH:mm:ssK'))), debug build not required."
                                }
                            }
                        }
                        else
                        {
                            Write-ADTBuildLogEntry -Message "Uncommitted file changes found under [$sourcePath], debug build required."
                            $buildConfigs.Insert(0, 'Debug')
                        }
                    }
                }
                else
                {
                    $buildConfigs.Insert(0, 'Debug')
                }
            }

            # Manually clean out old build and obj folders for good measure.
            foreach ($sourcePath in $buildItem.SourcePath)
            {
                Write-ADTBuildLogEntry -Message "Removing previous bin and obj folders from [$sourcePath], please wait..."
                'bin', 'obj' | & { process { Get-ChildItem -LiteralPath $sourcePath -Directory -Filter $_ -Recurse } } | Remove-Item -Recurse -Force
            }

            # Build the module configs we've determined we require.
            foreach ($buildType in ($buildConfigs | Select-Object -Unique))
            {
                # We use dotnet msbuild here as it has better reproducibility and allows us to do compilation and unit tests all in one operation.
                Write-ADTBuildLogEntry -Message "Building [$($buildItem.SolutionPath)] solution in [$buildType] mode, please wait..."
                & $dotnet msbuild $buildItem.SolutionPath -target:"Rebuild,VSTest" -restore -p:configuration=$buildType -p:platform="Any CPU" -nodeReuse:false -m | & {
                    process
                    {
                        if ([System.String]::IsNullOrWhiteSpace(($message = ($_ -replace '^\s+', "$([System.Char]0x2022) ").Trim())))
                        {
                            return
                        }
                        if ($_ -match ': error ')
                        {
                            Write-ADTBuildLogEntry -Message $message -ForegroundColor DarkRed
                        }
                        else
                        {
                            Write-ADTBuildLogEntry -Message $message
                        }
                    }
                }
                if ($Global:LASTEXITCODE)
                {
                    throw "Failed to build solution [$($buildItem.SolutionPath -replace '^.+\\')] with exit code [$Global:LASTEXITCODE]."
                }

                # Copy the debug configuration into the module's folder within the repo. The release copy will come later on directly into the artifact.
                if ($buildType.Equals('Debug'))
                {
                    $sourcePath = [System.IO.Path]::Combine([System.Management.Automation.WildcardPattern]::Escape($buildItem.BinaryPath), '*')
                    foreach ($outputPath in $buildItem.OutputPath)
                    {
                        Write-ADTBuildLogEntry -Message "Copying from [$sourcePath] to [$outputPath], please wait..."
                        if ($outputPath.EndsWith('lib'))
                        {
                            $null = Remove-Item -LiteralPath $outputPath -Force -Recurse -ErrorAction Ignore
                            $null = [System.IO.Directory]::CreateDirectory($outputPath)
                        }
                        Copy-Item -Path $sourcePath -Destination $outputPath -Recurse -Force
                    }
                }
                Write-ADTBuildLogEntry -Message "Built [$($buildItem.SolutionPath)] solution in [$buildType] mode as required." -ForegroundColor DarkGreen
            }
        }

        # For Invoke-AppDeployToolkit.exe, we need an additional check to make sure the assembly is renamed.
        if ([System.IO.File]::Exists("$($Script:ModuleConstants.Paths.ModuleSource)\Frontend\v3\Invoke-AppDeployToolkit.exe"))
        {
            Remove-Item -LiteralPath "$($Script:ModuleConstants.Paths.ModuleSource)\Frontend\v3\Deploy-Application.exe" -Force -Confirm:$false
            Remove-Item -LiteralPath "$($Script:ModuleConstants.Paths.ModuleSource)\Frontend\v3\Invoke-AppDeployToolkit.pdb" -Force -Confirm:$false -ErrorAction Ignore
            Rename-Item -LiteralPath "$($Script:ModuleConstants.Paths.ModuleSource)\Frontend\v3\Invoke-AppDeployToolkit.exe" -NewName Deploy-Application.exe -Force -Confirm:$false
        }
        Write-ADTBuildLogEntry -Message "Confirmed no other C# source operations required."
        Complete-ADTModuleBuildFunction
    }
    catch
    {
        Complete-ADTModuleBuildFunction -ErrorRecord $_
        throw
    }
}
