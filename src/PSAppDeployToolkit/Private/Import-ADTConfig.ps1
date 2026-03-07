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
            if ($asset.Value -is [System.Collections.Hashtable])
            {
                $asset.Value | & $MyInvocation.MyCommand; continue
            }

            # Skip if the value is null (some are optional).
            if (($asset.Key.Equals('LogoDark') -or $asset.Key.Equals('TaskbarIcon')) -and [System.String]::IsNullOrWhiteSpace($asset.Value))
            {
                continue
            }

            # Skip if the path is a Base64 string.
            if ($null -ne [PSADT.Utilities.MiscUtilities]::GetBase64StringBytes($asset.Value))
            {
                continue
            }

            # Skip if the path is fully qualified.
            if ([System.IO.Path]::IsPathRooted($asset.Value))
            {
                continue
            }

            # Get the asset's full path based on the supplied BaseDirectory.
            if ($BaseDirectory)
            {
                foreach ($directory in $BaseDirectory[($BaseDirectory.Length - 1)..(0)])
                {
                    if (($assetPath = Get-Item -LiteralPath "$directory\$($_.($asset.Key))" -ErrorAction Ignore))
                    {
                        $_.($asset.Key) = $assetPath.FullName
                        break
                    }
                }
            }
        }
    }

    # Internal filter to verify signedness of integer values.
    filter Get-ADTConfigIntegerKeyNames
    {
        # Go recursive if we've received a hashtable, otherwise just get the values.
        foreach ($section in $($_.GetEnumerator()))
        {
            # Re-process if this is a hashtable.
            if ($section.Value -is [System.Collections.Hashtable])
            {
                $section.Value | & $MyInvocation.MyCommand; continue
            }

            # Output the key to the caller.
            if (($section.Value -is [System.Int32]) -and !('DefaultExitCode', 'DeferExitCode', 'FluentAccentColor').Contains($section.Key))
            {
                $section.Key
            }
        }
    }
    filter Confirm-ADTConfigIntegersGreaterThanZero
    {
        # Go recursive if we've received a hashtable, otherwise just test the values.
        foreach ($section in $($_.GetEnumerator()))
        {
            # Re-process if this is a hashtable.
            if ($section.Value -is [System.Collections.Hashtable])
            {
                $section.Value | & $MyInvocation.MyCommand; continue
            }

            # Confirm the value signedness.
            if (($section.Value -is [System.Int32]) -and $integerKeys.Contains($section.Key) -and ($section.Value -le 0))
            {
                $naerParams = @{
                    Exception = [System.ArgumentOutOfRangeException]::new("The value for [$($section.Key)] must be greater than zero.", $null)
                    Category = [System.Management.Automation.ErrorCategory]::InvalidData
                    ErrorId = 'ConfigIntLessThanOrEqualToZero'
                    TargetObject = $_.($section.Key)
                    RecommendedAction = "Review your configuration and try again."
                }
                $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
            }
        }
    }

    # Import the config from disk and verify all integers are valid.
    $integerKeys = $Script:ADT.ModuleDefaults.Config.([System.String]::Empty).Ast.EndBlock.Statements.PipelineElements.Expression.SafeGetValue() | Get-ADTConfigIntegerKeyNames
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
    ($adtEnv = Get-ADTEnvironmentTable).PSObject.Properties | & { process { New-Variable -Name $_.Name -Value $_.Value -Option Constant } end { Expand-ADTVariablesInHashtable -Hashtable $config -SessionState $ExecutionContext.SessionState } }
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
