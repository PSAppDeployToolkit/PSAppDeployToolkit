#-----------------------------------------------------------------------------
#
# MARK: Get-ADTSessionCacheScriptDirectory
#
#-----------------------------------------------------------------------------

function Private:Get-ADTSessionCacheScriptDirectory
{
    # Determine whether we've got a valid script directory for caching purposes and throw if we don't.
    $scriptDir = if (($adtSession = Get-ADTSession).ScriptDirectory -and $adtSession.ScriptDirectory.Count)
    {
        if ($adtSession.ScriptDirectory.Count -gt 1)
        {
            $adtSession.ScriptDirectory | & { process { if ([System.IO.Directory]::Exists([System.IO.Path]::Combine($_, 'Files'))) { return $_ } } } | Select-Object -First 1
        }
        elseif ([System.IO.Directory]::Exists([System.IO.Path]::Combine($($adtSession.ScriptDirectory), 'Files')))
        {
            $($adtSession.ScriptDirectory)
        }
        elseif ($adtSession.DirFiles -and [System.IO.Directory]::Exists($adtSession.DirFiles))
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
