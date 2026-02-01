#-----------------------------------------------------------------------------
#
# MARK: Invoke-ADTDocusaurusExport
#
#-----------------------------------------------------------------------------

function Invoke-ADTDocusaurusExport
{
    # Initialise the module build function.
    Initialize-ADTModuleBuildFunction
    try
    {
        # Generate docusaurus files.
        Write-ADTBuildLogEntry -Message "Generating Docusaurus exports from platyPS markdown, please wait..."
        $null = New-DocusaurusHelp -PlatyPSMarkdownPath $Script:ModuleConstants.Paths.MarkdownOutput -DocsFolder $Script:ModuleConstants.Paths.DocusaurusOutput -NoPlaceHolderExamples

        # Post-process the exported markdown files.
        Write-ADTBuildLogEntry -Message "Post-processing Docusaurus exports."
        foreach ($file in (Get-ChildItem -Path "$($Script:ModuleConstants.Paths.DocusaurusOutput)\Commands\*.mdx"))
        {
            # Trim the file, fix hard-coded line breaks, and fix manually defined code fences.
            if (($content = [System.IO.File]::ReadAllText($file.FullName)) -ne ($newContent = $content.Trim().Replace('&lt;br /&gt;', '<br />').Replace('\{', '{').Replace('\}', '}') -replace '```\s+```powershell\s+```powershell\s+', "``````powershell`n"))
            {
                [System.IO.File]::WriteAllLines($file.FullName, $newContent.Split("`n").TrimEnd())
            }
        }
        Complete-ADTModuleBuildFunction
    }
    catch
    {
        Complete-ADTModuleBuildFunction -ErrorRecord $_
        throw
    }
}
