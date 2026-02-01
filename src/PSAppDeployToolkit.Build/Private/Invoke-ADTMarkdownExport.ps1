#-----------------------------------------------------------------------------
#
# MARK: Invoke-ADTMarkdownExport
#
#-----------------------------------------------------------------------------

function Invoke-ADTMarkdownExport
{
    # Internal worker function for PowerShell 7.4.0 and greater.
    function Repair-ADTMarkdownExport
    {
        <#

        .SYNOPSIS
            Repair PlatyPS generated markdown files.

        .NOTES
            This file is temporarily required to handle platyPS help generation (https://github.com/PowerShell/platyPS/issues/595).
            This is a result of a breaking change introduced in PowerShell 7.4.0: https://learn.microsoft.com/en-us/powershell/scripting/whats-new/what-s-new-in-powershell-74?view=powershell-7.4 (Breaking Changes: Added the ProgressAction parameter to the Common Parameters).
            This code was modified from source: https://github.com/PowerShell/platyPS/issues/595#issuecomment-1820971702
        #>

        [CmdletBinding()]
        param
        (
            [Parameter(Mandatory = $true)]
            [ValidateNotNullOrEmpty()]
            [System.String[]]$Path,

            [Parameter(Mandatory = $false)]
            [ValidateNotNullOrEmpty()]
            [System.String[]]$ParameterName = 'ProgressAction'
        )

        # Internal worker functions.
        function Remove-CommonParameterFromMarkdown
        {
            [CmdletBinding()]
            param
            (
                [Parameter(Mandatory = $true)]
                [ValidateNotNullOrEmpty()]
                [System.String[]]$Path,

                [Parameter(Mandatory = $false)]
                [ValidateNotNullOrEmpty()]
                [System.String[]]$ParameterName = 'ProgressAction'
            )

            # Process each file.
            foreach ($p in $Path)
            {
                # Grab file contents and parse for each parameter.
                $content = (Get-Content -Path $p -Raw).TrimEnd()
                $updateFile = $false
                foreach ($param in $ParameterName)
                {
                    # Remove the parameter block
                    if (!$Param.StartsWith('-'))
                    {
                        $param = "-$($param)"
                    }
                    $newContent = $content -replace "(?m)^### $param\r?\n[\S\s]*?(?=#{2,3}?)", ''

                    # Remove the parameter from the syntax block
                    $newContent = $newContent -replace " \[$param\s?.*?]", ''
                    if ($null -ne (Compare-Object -ReferenceObject $content -DifferenceObject $newContent))
                    {
                        # Update file content
                        $content = $newContent
                        $updateFile = $true
                    }
                }

                # Save file if content has changed
                if ($updateFile)
                {
                    $newContent | Out-File -Encoding utf8 -FilePath $p
                }
            }
        }
        function Add-MissingCommonParameterToMarkdown
        {
            [CmdletBinding()]
            param
            (
                [Parameter(Mandatory = $true)]
                [ValidateNotNullOrEmpty()]
                [System.String[]]$Path,

                [Parameter(Mandatory = $false)]
                [ValidateNotNullOrEmpty()]
                [System.String[]]$ParameterName = 'ProgressAction'
            )

            # Process each file.
            foreach ($p in $Path)
            {
                # Grab file contents and parse for each parameter.
                $content = (Get-Content -Path $p -Raw).TrimEnd()
                $updateFile = $false
                foreach ($NewParameter in $ParameterName)
                {
                    if (!$NewParameter.StartsWith('-'))
                    {
                        $NewParameter = "-$($NewParameter)"
                    }
                    $newContent = $content -replace '(?m)^This cmdlet supports the common parameters:(.+?)\.', {
                        $Params = $_.Groups[1].Captures[0].ToString() -split ' '
                        $CommonParameters = @()
                        foreach ($CommonParameter in $Params)
                        {
                            if ($CommonParameter.StartsWith('-'))
                            {
                                $CommonParameters += if ($CommonParameter.EndsWith(','))
                                {
                                    $CommonParameter.Substring(0, $CommonParameter.Length - 1)
                                }
                                elseif ($p.EndsWith('.'))
                                {
                                    $CommonParameter.Substring(0, $CommonParameter.Length - 1)
                                }
                                else
                                {
                                    $CommonParameter
                                }
                            }
                        }
                        if ($NewParameter -notin $CommonParameters)
                        {
                            $CommonParameters += $NewParameter
                        }
                        $CommonParameters[-1] = "and $($CommonParameters[-1]). "
                        return "This cmdlet supports the common parameters: " + (($CommonParameters | Sort-Object) -join ', ')
                    }
                    if ($null -ne (Compare-Object -ReferenceObject $content -DifferenceObject $newContent))
                    {
                        $updateFile = $true
                        $content = $newContent
                    }
                }

                # Save file if content has changed.
                if ($updateFile)
                {
                    $newContent | Out-File -Encoding utf8 -FilePath $p
                }
            }
        }

        # Process requested markdown files.
        Remove-CommonParameterFromMarkdown @PSBoundParameters
        Add-MissingCommonParameterToMarkdown @PSBoundParameters
    }

    # Initialise the module build function.
    Initialize-ADTModuleBuildFunction
    try
    {
        # Generate markdown files.
        Write-ADTBuildLogEntry -Message "Generating markdown exports with platyPS, please wait..."
        $null = New-MarkdownHelp -Module $Script:ModuleConstants.ModuleName -OutputFolder $Script:ModuleConstants.Paths.MarkdownOutput -Locale en-US -HelpVersion $MyInvocation.MyCommand.Module.Version -Force

        # Post-process the exported markdown files.
        Write-ADTBuildLogEntry -Message "Post-processing platyPS markdown exports."
        foreach ($file in (Get-ChildItem -LiteralPath $Script:ModuleConstants.Paths.MarkdownOutput -File))
        {
            # Read the file as a string, not an array.
            $content = [System.IO.File]::ReadAllText($file.FullName)

            # Trim the file, fix multi-line EXAMPLES, and unescape tilde characters.
            $newContent = ($content.Trim() -replace '(## EXAMPLE [^`]+?```\r\n[^`\r\n]+?\r\n)(```\r\n\r\n)([^#]+?\r\n)(\r\n)([^#]+)(#)', '$1$3$2$4$5$6').Replace('PS C:\\\>', $null).Replace('\`', '`')

            # Escape and slashes within a parameter's `Default value` yaml property.
            $newContent = [System.Text.RegularExpressions.Regex]::Replace($newContent, '(?<=^Default value: .*?)(\\)', '\\', [System.Text.RegularExpressions.RegexOptions]::Multiline)

            # Write the content back to disk if there's changes.
            if ($newContent -ne $content)
            {
                [System.IO.File]::WriteAllLines($file.FullName, $newContent.Split("`n").TrimEnd())
            }
        }

        # Validate Guid of export is correct.
        Write-ADTBuildLogEntry -Message "Confirming markdown export GUID validity."
        if (Select-String -Path "$($Script:ModuleConstants.Paths.MarkdownOutput)\*.md" -Pattern "(00000000-0000-0000-0000-000000000000)")
        {
            throw 'The documentation that got generated resulted in a generic GUID. Check the GUID entry of your module manifest and try again.'
        }

        # Perform amendments for PowerShell 7.4.x or higher targets. https://github.com/PowerShell/platyPS/issues/595.
        if ($PSVersionTable.PSVersion -ge [version]'7.4.0')
        {
            Write-ADTBuildLogEntry -Message "Performing repairs for PowerShell 7.4.0 and later."
            Repair-PlatyPSMarkdown -Path (Get-ChildItem -LiteralPath $Script:ModuleConstants.Paths.MarkdownOutput -File).FullName
        }

        # Validate nothing is missing.
        Write-ADTBuildLogEntry -Message "Testing for any missing sections in exported markdown files."
        if ([Microsoft.PowerShell.Commands.MatchInfo[]]$MissingDocumentation = Select-String -Path "$($Script:ModuleConstants.Paths.MarkdownOutput)\*.md" -Pattern "({{.*}})")
        {
            for ($i = 0; $i -lt $MissingDocumentation.Count; $i++)
            {
                $output = ($MissingDocumentation[$i] | Select-Object -Property FileName, LineNumber, Line | Format-List -Property * | Out-String -Width ([System.Int32]::MaxValue)).Trim().Split("`n").Trim() -replace '^', "$([System.Char]0x2022) "
                Write-ADTBuildLogEntry -Message "Output for missing documentation MatchInfo [$($i+1)/$($MissingDocumentation.Count)]" -ForegroundColor DarkRed
                Write-ADTBuildLogEntry -Message $output -ForegroundColor DarkRed
            }
            throw 'The documentation that got generated resulted in missing sections which should be filled out.'
        }

        # Validate all exports have a synopsis.
        Write-ADTBuildLogEntry -Message "Testing for any missing synopses in exported markdown files."
        $fSynopsisOutput = Select-String -Path "$($Script:ModuleConstants.Paths.MarkdownOutput)\*.md" -Pattern "^## SYNOPSIS$" -Context 0, 1 | & {
            process
            {
                if ($null -eq $_.Context.DisplayPostContext.ToCharArray())
                {
                    $_.FileName
                }
            }
        }
        if ($fSynopsisOutput)
        {
            Write-ADTBuildLogEntry -Message ("The following files are missing SYNOPSIS:", $fSynopsisOutput -replace '^', "$([System.Char]0x2022) ")
            throw 'One or more markdown files are missing a SYNOPSIS section.'
        }
        Complete-ADTModuleBuildFunction
    }
    catch
    {
        Complete-ADTModuleBuildFunction -ErrorRecord $_
        throw
    }
}
