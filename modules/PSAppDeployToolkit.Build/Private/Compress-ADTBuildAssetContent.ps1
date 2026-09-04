#-----------------------------------------------------------------------------
#
# MARK: Compress-ADTBuildAssetContent
#
#-----------------------------------------------------------------------------

function Compress-ADTBuildAssetContent
{
    # Initialise the module build function.
    Initialize-ADTModuleBuildFunction
    try
    {
        Write-ADTBuildLogEntry -Message "Compressing build system output with 7-Zip, this may take a while."
        if (!($7z = Get-Command -Name 'C:\Program Files (x86)\7-Zip\7z.exe', 'C:\Program Files\7-Zip\7z.exe' -ErrorAction Ignore))
        {
            throw "Failed to locate 7-Zip on this system. Please install and try again."
        }
        $fileSuffix = "$(if ($env:GITHUB_REF_NAME -match 'merge$') { $env:GITHUB_REF_NAME.Split('/')[-2] } else { $env:GITHUB_REF_NAME.Split('/')[-1] })_$([System.DateTime]::Now.ToUniversalTime().ToString('O').Replace(':', [System.Management.Automation.Language.NullString]::Value) -replace '\.\d+')_$($env:GITHUB_SHA.Substring(0, 7))"
        $gitHubAction = !![System.Environment]::GetEnvironmentVariable('GITHUB_OUTPUT')
        foreach ($childItem in ('ModuleOnly', 'Template_v3', 'Template_v4', 'Template_v4_ZeroConfig'))
        {
            if (![System.IO.Directory]::Exists(($source = [System.IO.Path]::Combine($Script:ModuleConstants.Paths.BuildOutput, $childItem))))
            {
                throw "Failed to locate [$source] on disk."
            }
            $archivePath = "$($Script:ModuleConstants.Paths.BuildOutput)\$($Script:ModuleConstants.ModuleName)_$($childItem)__$($fileSuffix).7z"
            $null = & $7z a -t7z -m0=lzma2 -mx=9 -ms=on $archivePath "$source\*"
            if ($gitHubAction)
            {
                "$childItem=$archivePath" >> $env:GITHUB_OUTPUT
            }
            Write-ADTBuildLogEntry -Message "Compressed contents of [$source] to [$archivePath]."
        }
        Complete-ADTModuleBuildFunction
    }
    catch
    {
        Complete-ADTModuleBuildFunction -ErrorRecord $_
        throw
    }
}
