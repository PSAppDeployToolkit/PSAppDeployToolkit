#-----------------------------------------------------------------------------
#
# MARK: Publish-ADTDocusaurusExport
#
#-----------------------------------------------------------------------------

function Publish-ADTDocusaurusExport
{
    # Initialise the module build function.
    Initialize-ADTModuleBuildFunction
    try
    {
        # Clone the destination repo.
        Write-ADTBuildLogEntry -Message "Cloning destination repository, this may take a while."
        $dstBnch = 'main'; $dstRepo = "https://$env:API_TOKEN_GITHUB@github.com/$env:GITHUB_REPOSITORY_OWNER/website.git"
        $dstBase = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), [System.IO.Path]::GetRandomFileName())
        $dstPath = ("$dstBase\docs\reference\functions", "$dstBase\versioned_docs\version-4.0.0\reference\functions")[$env:GITHUB_REF_NAME -match '4\.0\.x']
        $null = git clone -b $dstBnch $dstRepo $dstBase 2>$null
        if ($Global:LASTEXITCODE)
        {
            throw "The cloning of the destination repository failed."
        }

        # Update the docs from the source repo to the destination.
        Write-ADTBuildLogEntry -Message "Updating the markdown files in destination repository."
        Remove-Item -Path "$dstPath\*" -Force -Confirm:$false
        Get-ChildItem -Path "$($Script:ModuleConstants.Paths.DocusaurusOutput)\commands\*" -File | Copy-Item -Destination $dstPath

        # Change into the repository's directory.
        Push-Location -LiteralPath $dstBase
        try
        {
            # Add any changes that may exist.
            $null = git add --all 2>$null

            # Commit any changes if found.
            $res = git diff --cached 2>$null
            if ($res)
            {
                # Set up author details.
                $null = git config user.email "$env:USERNAME@psappdeploytoolkit.com" 2>$null
                $null = git config user.name "PSAppDeployToolkit Action Workflow" 2>$null

                # Do the commit.
                $commitMsg = "Commit of document changes from https://github.com/$env:GITHUB_REPOSITORY/commit/$env:GITHUB_SHA"
                Write-ADTBuildLogEntry -Message "Documents changed, committing as `"$commitMsg`""
                $null = git commit -a -m $commitMsg
                if ($Global:LASTEXITCODE)
                {
                    throw "The committing of destination repo changes failed."
                }

                # Push it to the website.
                Write-ADTBuildLogEntry -Message "Pushing committed changes to origin."
                $null = git push origin 2>$null
                if ($Global:LASTEXITCODE)
                {
                    throw "The pushing of commits from destination repo failed."
                }
            }
            else
            {
                Write-ADTBuildLogEntry -Message "Found no document changes to commit."
            }
        }
        finally
        {
            Pop-Location
        }
        Complete-ADTModuleBuildFunction
    }
    catch
    {
        Complete-ADTModuleBuildFunction -ErrorRecord $_
        throw
    }
}
