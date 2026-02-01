#-----------------------------------------------------------------------------
#
# MARK: Invoke-ADTModuleCompilation
#
#-----------------------------------------------------------------------------

function Invoke-ADTModuleCompilation
{
    # Initialise the module build function.
    Initialize-ADTModuleBuildFunction
    try
    {
        # Compile the project into a singular psm1 file.
        Write-ADTBuildLogEntry -Message "Generating compiled .psm1 file from module sources, please wait..."
        $sourceOnlyData = [ordered]@{
            'ImportsFirst.ps1' = "$($Script:ModuleConstants.Paths.ModuleSource)\ImportsFirst.ps1"
            'Private' = "$($Script:ModuleConstants.Paths.ModuleSource)\Private\*.ps1"
            'Public' = "$($Script:ModuleConstants.Paths.ModuleSource)\Public\*.ps1"
            'ImportsLast.ps1' = "$($Script:ModuleConstants.Paths.ModuleSource)\ImportsLast.ps1"
        }
        $scriptContent = foreach ($file in (Get-ChildItem -Path ([System.String[]]$sourceOnlyData.Values) -Recurse))
        {
            # Import the script file as a string for substring replacement.
            Write-ADTBuildLogEntry -Message "Reading file [$($file.FullName)] for merging."
            $text = [System.IO.File]::ReadAllText($file.FullName).Trim()

            # Parse the ps1 file and store its AST.
            $scrAst = [System.Management.Automation.Language.Parser]::ParseInput($text, [ref]$null, [ref]($errors = $null))

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
            if ($scrAst.FindAll({ $args[0].ToString() -match '^(\$?env:|(Microsoft.PowerShell.Core\\)?Environment::)' }, $true).Count -and !$file.Name.Equals('Update-ADTEnvironmentPsProvider.ps1'))
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
                        if (!$Script:ModuleBuildState.CommandTable.ContainsKey($_.Text))
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

        # Start generating the compiled output on disk.
        Write-ADTBuildLogEntry -Message "Building release module at [$($Script:ModuleConstants.Paths.ModuleSource)]."
        $null = [System.IO.Directory]::CreateDirectory($Script:ModuleConstants.Paths.ModuleSource)
        Copy-Item -LiteralPath $Script:ModuleConstants.Paths.ModuleSource -Destination $Script:ModuleConstants.Paths.ModuleOutput -Recurse -Force
        Get-ChildItem -LiteralPath $Script:ModuleConstants.Paths.ModuleOutput | & { process { if (([System.String[]]$sourceOnlyData.Keys).Contains($_.Name)) { return $_ } } } | Remove-Item -Recurse -Force
        [System.IO.File]::WriteAllLines([System.IO.Path]::Combine($Script:ModuleConstants.Paths.ModuleOutput, "$($Script:ModuleConstants.ModuleName).psm1"), $scriptContent, [System.Text.UTF8Encoding]::new($true, $true))

        # Replace debug DLLs with release copies.
        if ($Script:ModuleBuildState.HaveDotNetSdk)
        {
            # Copy the files as required, replacing everything in the destination.
            Write-ADTBuildLogEntry "Replacing debug DLLs with release DLL files."
            foreach ($buildItem in $Script:ModuleConstants.DotNetBuildItems)
            {
                $sourcePath = [System.IO.Path]::Combine($buildItem.BinaryPath.Replace('Debug', 'Release'), '*')
                foreach ($destPath in $buildItem.OutputPath.Replace($Script:ModuleConstants.Paths.ModuleSource, $Script:ModuleConstants.Paths.ModuleOutput))
                {
                    Copy-Item -Path $sourcePath -Destination $destPath -Recurse -Force
                }
            }

            # For Invoke-AppDeployToolkit.exe, we need an additional check to make sure the assembly is renamed.
            Write-ADTBuildLogEntry -Message "Renaming [Invoke-AppDeployToolkit.exe] to [Deploy-Application.exe] for v3 compatibility template."
            if ([System.IO.File]::Exists("$($Script:ModuleConstants.Paths.ModuleOutput)\Frontend\v3\Invoke-AppDeployToolkit.exe"))
            {
                Remove-Item -LiteralPath "$($Script:ModuleConstants.Paths.ModuleOutput)\Frontend\v3\Deploy-Application.exe" -Force -Confirm:$false
                Rename-Item -LiteralPath "$($Script:ModuleConstants.Paths.ModuleOutput)\Frontend\v3\Invoke-AppDeployToolkit.exe" -NewName Deploy-Application.exe -Force -Confirm:$false
            }

            # Strip PDB files from all non-lib locations (we want them in lib for stack traces, etc).
            Write-ADTBuildLogEntry -Message "Removing PDB files from all non-lib locations."
            Get-ChildItem -LiteralPath $Script:ModuleConstants.Paths.ModuleOutput -Directory | Where-Object -Property Name -NE -Value lib | Get-ChildItem -Filter *.pdb -Recurse | Remove-Item -Force
        }
        else
        {
            Write-ADTBuildLogEntry "Leaving debug DLL files in place since we did not re-compile C# sources."
        }
        Complete-ADTModuleBuildFunction
    }
    catch
    {
        Complete-ADTModuleBuildFunction -ErrorRecord $_
        throw
    }
}
