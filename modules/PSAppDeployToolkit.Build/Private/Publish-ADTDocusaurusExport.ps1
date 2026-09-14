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
        # Clone the destination repo. The authorization header reaches git through the environment rather than
        # through -c or git config, both of which pass it as an argument and so put it in the runner's process list
        # for anything able to read one. Every git call below inherits it, and the finally clause takes it away again.
        Write-ADTBuildLogEntry -Message "Cloning destination repository, this may take a while."
        $destBranch = 'main'; $destRepo = "https://github.com/$env:GITHUB_REPOSITORY_OWNER/website.git"
        $gitAuthToken = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes("x-access-token:$env:API_TOKEN_GITHUB"))
        $destBase = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), [System.IO.Path]::GetRandomFileName())
        $destPath = "$destBase\docs\reference\functions"
        $env:GIT_CONFIG_COUNT = '1'
        $env:GIT_CONFIG_KEY_0 = 'http.https://github.com/.extraheader'
        $env:GIT_CONFIG_VALUE_0 = "AUTHORIZATION: basic $gitAuthToken"
        $null = git clone -q -b $destBranch $destRepo $destBase
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
                $null = git push origin -q
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
    finally
    {
        Remove-Item -LiteralPath Env:GIT_CONFIG_COUNT, Env:GIT_CONFIG_KEY_0, Env:GIT_CONFIG_VALUE_0 -ErrorAction Ignore
    }
}
