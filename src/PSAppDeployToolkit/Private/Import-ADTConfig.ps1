#-----------------------------------------------------------------------------
#
# MARK: Import-ADTConfig
#
#-----------------------------------------------------------------------------

function Private:Import-ADTConfig
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [AllowNull()][PSAppDeployToolkit.Foundation.AllowNullButNotEmptyOrWhiteSpace()]
        [System.String[]]$BaseDirectory
    )

    # Internal filter to process asset file paths.
    filter Update-ADTAssetFilePath
    {
        # Go recursive if we've received a hashtable, otherwise just update the values.
        foreach ($asset in $($_.GetEnumerator()))
        {
            # Re-process if this is a hashtable.
            if ($asset.get_Value() -is [System.Collections.Hashtable])
            {
                $asset.get_Value() | & $MyInvocation.get_MyCommand(); continue
            }

            # Skip if the value is null (some are optional).
            if ($asset.get_Key() -eq 'TaskbarIcon' -and [System.String]::IsNullOrWhiteSpace($asset.get_Value()))
            {
                continue
            }

            # Skip if the path is a Base64 string.
            if ($null -ne [PSADT.Utilities.MiscUtilities]::GetBase64StringBytes($asset.get_Value()))
            {
                continue
            }

            # Skip if the path is fully qualified.
            if ([System.IO.Path]::IsPathRooted($asset.get_Value()))
            {
                continue
            }

            # Get the asset's full path based on the supplied BaseDirectory.
            # Fall back to the module's path if the asset is unable to be found.
            $assetPath = foreach ($directory in $(if ($BaseDirectory) { $BaseDirectory[($BaseDirectory.get_Length() - 1)..(0)] }; "$Script:PSScriptRoot\Config"))
            {
                if (($assetPath = Get-Item -LiteralPath "$directory\$($_.($asset.get_Key()))" -ErrorAction Ignore))
                {
                    $assetPath.get_FullName()
                    break
                }
            }

            # Throw if we found no asset.
            if (!$assetPath)
            {
                $naerParams = @{
                    Exception = [System.IO.FileNotFoundException]::new("Failed to resolve the asset [$($asset.get_Key())] to a valid file path.", $_.($asset.get_Key()))
                    Category = [System.Management.Automation.ErrorCategory]::ObjectNotFound
                    ErrorId = 'DialogAssetNotFound'
                    TargetObject = $_.($asset.get_Key())
                    RecommendedAction = "Ensure the file exists and try again."
                }
                $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
            }
            $_.($asset.get_Key()) = $assetPath
        }
    }

    # Internal filter to verify signedness of integer values.
    filter Confirm-ADTConfigIntegersGreaterThanZero
    {
        # Go recursive if we've received a hashtable, otherwise just update the values.
        foreach ($section in $($_.GetEnumerator()))
        {
            # Re-process if this is a hashtable.
            if ($section.get_Value() -is [System.Collections.Hashtable])
            {
                $section.get_Value() | & $MyInvocation.get_MyCommand(); continue
            }

            # Confirm the value signedness.
            if (($section.get_Value() -is [System.Int32]) -and !('DefaultExitCode', 'DeferExitCode', 'FluentAccentColor').Contains($section.get_Key()) -and ($section.get_Value() -le 0))
            {
                $naerParams = @{
                    Exception = [System.ArgumentOutOfRangeException]::new("The value for [$($section.get_Key())] must be greater than zero.", $null)
                    Category = [System.Management.Automation.ErrorCategory]::InvalidData
                    ErrorId = 'ConfigIntLessThanOrEqualToZero'
                    TargetObject = $_.($section.get_Key())
                    RecommendedAction = "Review your configuration and try again."
                }
                $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
            }
        }
    }

    # Import the config from disk and verify all integers are valid.
    $config = Import-ADTModuleDataFile @PSBoundParameters -FileName config.psd1
    $config | Confirm-ADTConfigIntegersGreaterThanZero

    # Confirm the specified dialog type is valid.
    if (($config.UI.DialogStyle -ne 'Classic') -and (Test-ADTNonNativeCaller))
    {
        $config.UI.DialogStyle = if ($config.UI.ContainsKey('DialogStyleCompatMode'))
        {
            $config.UI.DialogStyleCompatMode
        }
        else
        {
            'Classic'
        }
    }
    try
    {
        $null = [PSADT.UserInterface.DialogStyle]$config.UI.DialogStyle
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }

    # Expand out environment variables and asset file paths.
    ($adtEnv = Get-ADTEnvironmentTable).GetEnumerator() | & { process { New-Variable -Name $_.get_Key() -Value $_.get_Value() -Option Constant } end { Expand-ADTVariablesInHashtable -Hashtable $config -SessionState $ExecutionContext.get_SessionState() } }
    $config.Assets | Update-ADTAssetFilePath

    # Change paths to user accessible ones if user isn't an admin.
    if (!$adtEnv.IsAdmin)
    {
        if (![System.String]::IsNullOrWhiteSpace($config.Toolkit.TempPathNoAdminRights))
        {
            $config.Toolkit.TempPath = $config.Toolkit.TempPathNoAdminRights
        }
        if (![System.String]::IsNullOrWhiteSpace($config.Toolkit.RegPathNoAdminRights))
        {
            $config.Toolkit.RegPath = $config.Toolkit.RegPathNoAdminRights
        }
        if (![System.String]::IsNullOrWhiteSpace($config.Toolkit.LogPathNoAdminRights))
        {
            $config.Toolkit.LogPath = $config.Toolkit.LogPathNoAdminRights
        }
        if (![System.String]::IsNullOrWhiteSpace($config.MSI.LogPathNoAdminRights))
        {
            $config.MSI.LogPath = $config.MSI.LogPathNoAdminRights
        }
    }

    # Append the toolkit's name onto the temporary path.
    $config.Toolkit.TempPath = Join-Path -Path $config.Toolkit.TempPath -ChildPath $adtEnv.appDeployToolkitName

    # Finally, handle some correctly renamed language identifiers for 4.1.1.
    if (![System.String]::IsNullOrWhiteSpace($config.UI.LanguageOverride))
    {
        $translator = @{
            'CZ' = 'cs'
            'ZH-Hans' = 'zh-CN'
            'ZH-Hant' = 'zh-HK'
        }
        if ($translator.ContainsKey($config.UI.LanguageOverride))
        {
            $config.UI.LanguageOverride = $translator.($config.UI.LanguageOverride)
        }
    }

    # Finally, return the config for usage within module.
    return $config
}
