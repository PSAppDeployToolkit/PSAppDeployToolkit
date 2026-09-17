#-----------------------------------------------------------------------------
#
# MARK: Publish-ADTDocusaurusExport
#
#-----------------------------------------------------------------------------

function Publish-ADTDocusaurusExport
{
    # Initialise the module build function. The restore table is set up ahead of the try so
    # that the finally clause has something to read no matter how early the failure came.
    Initialize-ADTModuleBuildFunction
    $gitConfigRestore = [ordered]@{}
    try
    {
        # Clone the destination repo. The authorization header reaches git through the environment rather than
        # -c or git config, which would put it in the runner's process list for anything able to read one.
        # The finally clause puts each variable back as it was found, which for the token means removing it.
        Write-ADTBuildLogEntry -Message "Cloning destination repository, this may take a while."
        $destBranch = 'main'; $destRepo = "https://github.com/$env:GITHUB_REPOSITORY_OWNER/website.git"
        $gitAuthToken = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes("x-access-token:$env:API_TOKEN_GITHUB"))
        $destBase = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), [System.IO.Path]::GetRandomFileName())
        $destPath = "$destBase\docs\reference\functions"
        $gitConfigValues = [ordered]@{
            GIT_CONFIG_COUNT = '1'
            GIT_CONFIG_KEY_0 = 'http.https://github.com/.extraheader'
            GIT_CONFIG_VALUE_0 = "AUTHORIZATION: basic $gitAuthToken"
        }
        foreach ($gitConfigValue in $gitConfigValues.GetEnumerator())
        {
            # Read through the environment rather than the provider so that a variable that was
            # never set is a null to put back, and not an error for having nothing to read.
            $gitConfigRestore.Add($gitConfigValue.Key, [System.Environment]::GetEnvironmentVariable($gitConfigValue.Key))
            Set-Item -LiteralPath "Env:$($gitConfigValue.Key)" -Value $gitConfigValue.Value
        }
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
        # A null is a variable that wasn't set before, and it has to be removed rather than assigned back.
        # Assigning one leaves the variable behind holding nothing instead of taking it away, and git refuses
        # to run at all on an empty GIT_CONFIG_COUNT.
        foreach ($gitConfigValue in $gitConfigRestore.GetEnumerator())
        {
            if ($null -eq $gitConfigValue.Value)
            {
                Remove-Item -LiteralPath "Env:$($gitConfigValue.Key)" -ErrorAction Ignore
            }
            else
            {
                Set-Item -LiteralPath "Env:$($gitConfigValue.Key)" -Value $gitConfigValue.Value
            }
        }
    }
}
