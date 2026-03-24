#-----------------------------------------------------------------------------
#
# MARK: Get-ADTSessionCacheScriptDirectory
#
#-----------------------------------------------------------------------------

function Private:Get-ADTSessionCacheScriptDirectory
{
    # Determine whether we've got a valid script directory for caching purposes and throw if we don't.
    $scriptDir = if ($adtSession = Get-ADTSession)
    {
        if ($adtSession.ScriptDirectory.Count)
        {
            $adtSession.ScriptDirectory | & { process { if (Test-Path -LiteralPath (Join-Path -Path $_ -ChildPath Files) -PathType Container) { return $_ } } } | Select-Object -Last 1
        }
        elseif ($adtSession.DirFiles -and (Test-Path -LiteralPath $adtSession.DirFiles -PathType Container))
        {
            [System.IO.DirectoryInfo]::new($adtSession.DirFiles).Parent.FullName
        }
    }
    if (!$scriptDir)
    {
        $naerParams = @{
            Exception = [System.IO.DirectoryNotFoundException]::new("None of the current session's ScriptDirectory paths contain any Files/SupportFiles directories.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidResult
            ErrorId = 'ScriptDirectoryInvalid'
            TargetObject = $adtSession.ScriptDirectory
            RecommendedAction = "Please review the session's ScriptDirectory listing, then try again."
        }
        throw (New-ADTErrorRecord @naerParams)
    }
    return $scriptDir
}
