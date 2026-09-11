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
        $destBranch = 'main'; $dstRepo = "https://github.com/$env:GITHUB_REPOSITORY_OWNER/website.git"
        $gitAuthToken = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes("x-access-token:$env:API_TOKEN_GITHUB"))
        $gitAuthHeader = "AUTHORIZATION: basic $gitAuthToken"
        $destBase = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), [System.IO.Path]::GetRandomFileName())
        $destPath = "$destBase\docs\reference\functions"
        $null = git -c "http.https://github.com/.extraheader=$gitAuthHeader" clone -q -b $destBranch $dstRepo $destBase
        if ($Global:LASTEXITCODE)
        {
            throw "The cloning of the destination repository failed."
        }

        # Update the docs from the source repo to the destination.
        Write-ADTBuildLogEntry -Message "Updating the markdown files in destination repository."
        Remove-Item -Path "$destPath\*" -Force -Confirm:$false
        Get-ChildItem -Path "$($Script:ModuleConstants.Paths.DocusaurusOutput)\commands\*" -File | Copy-Item -Destination $destPath

        # Change into the repository's directory.
        Push-Location -LiteralPath $destBase
        try
        {
            # Add any changes that may exist.
            $null = git -c core.safecrlf=false add --all

            # Commit any changes if found.
            if (git diff --cached)
            {
                # Do the commit.
                $commitMsg = "Commit of document changes from https://github.com/$env:GITHUB_REPOSITORY/commit/$env:GITHUB_SHA"
                Write-ADTBuildLogEntry -Message "Documents changed, committing as `"$commitMsg`"."
                $null = git config user.email "$env:USERNAME@psappdeploytoolkit.com"
                $null = git config user.name "PSAppDeployToolkit Action Workflow"
                $null = git commit -q -a -m $commitMsg
                if ($Global:LASTEXITCODE)
                {
                    throw "The committing of destination repo changes failed."
                }

                # Push it to the website.
                Write-ADTBuildLogEntry -Message "Pushing committed changes to origin."
                $null = git -c "http.https://github.com/.extraheader=$gitAuthHeader" push origin -q
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
