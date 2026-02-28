#-----------------------------------------------------------------------------
#
# MARK: Get-ADTSessionCacheScriptDirectory
#
#-----------------------------------------------------------------------------

function Private:Get-ADTSessionCacheScriptDirectory
{
    # Determine whether we've got a valid script directory for caching purposes and throw if we don't.
    $scriptDir = if (($adtSession = Get-ADTSession).get_ScriptDirectory() -and $adtSession.get_ScriptDirectory().get_Count())
    {
        if ($adtSession.get_ScriptDirectory().get_Count() -gt 1)
        {
            $adtSession.get_ScriptDirectory() | & { process { if (Test-Path -LiteralPath (Join-Path -Path $_ -ChildPath Files) -PathType Container) { return $_ } } } | Select-Object -First 1
        }
        elseif (Test-Path -LiteralPath (Join-Path -Path $($adtSession.get_ScriptDirectory()) -ChildPath Files) -PathType Container)
        {
            $($adtSession.get_ScriptDirectory())
        }
        elseif ($adtSession.get_DirFiles() -and (Test-Path -LiteralPath $adtSession.get_DirFiles() -PathType Container))
        {
            [System.IO.DirectoryInfo]::new($adtSession.get_DirFiles()).get_Parent().get_FullName()
        }
    }
    if (!$scriptDir)
    {
        $naerParams = @{
            Exception = [System.IO.DirectoryNotFoundException]::new("None of the current session's ScriptDirectory paths contain any Files/SupportFiles directories.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidResult
            ErrorId = 'ScriptDirectoryInvalid'
            TargetObject = $adtSession.get_ScriptDirectory()
            RecommendedAction = "Please review the session's ScriptDirectory listing, then try again."
        }
        throw (New-ADTErrorRecord @naerParams)
    }
    return $scriptDir
}
