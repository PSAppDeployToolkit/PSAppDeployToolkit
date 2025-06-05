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
        [ValidateScript({
                if ([System.String]::IsNullOrWhiteSpace($_))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName BaseDirectory -ProvidedValue $_ -ExceptionMessage 'The specified input is null or empty.'))
                }
                if (![System.IO.Directory]::Exists($_))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName BaseDirectory -ProvidedValue $_ -ExceptionMessage 'The specified directory does not exist.'))
                }
                return $_
            })]
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

            # Skip if the path is fully qualified.
            if ([System.IO.Path]::IsPathRooted($asset.Value))
            {
                continue
            }

            # Get the asset's full path based on the supplied BaseDirectory.
            # Fall back to the module's path if the asset is unable to be found.
            $assetPath = foreach ($directory in $($BaseDirectory[($BaseDirectory.Count - 1)..(0)]; $Script:ADT.Directories.Defaults.Config))
            {
                if (($assetPath = Get-Item -LiteralPath "$directory\$($_.($asset.Key))" -ErrorAction Ignore))
                {
                    $assetPath.FullName
                    break
                }
            }

            # Throw if we found no asset.
            if (!$assetPath)
            {
                $naerParams = @{
                    Exception = [System.IO.FileNotFoundException]::new("Failed to resolve the asset [$($asset.Key)] to a valid file path.", $_.($asset.Key))
                    Category = [System.Management.Automation.ErrorCategory]::ObjectNotFound
                    ErrorId = 'DialogAssetNotFound'
                    TargetObject = $_.($asset.Key)
                    RecommendedAction = "Ensure the file exists and try again."
                }
                $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
            }
            $_.($asset.Key) = $assetPath
        }
    }

    # Internal filter to expand variables.
    filter Expand-ADTVariablesInConfig
    {
        # Go recursive if we've received a hashtable, otherwise just update the values.
        foreach ($section in $($_.GetEnumerator()))
        {
            if ($section.Value -is [System.String])
            {
                $_.($section.Key) = $ExecutionContext.InvokeCommand.ExpandString($section.Value)
            }
            elseif ($section.Value -is [System.Collections.Hashtable])
            {
                $section.Value | & $MyInvocation.MyCommand
            }
        }
    }

    # Import the config from disk.
    $config = Import-ADTModuleDataFile @PSBoundParameters -FileName config.psd1

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
        $null = [PSADT.UserInterface.Dialogs.DialogStyle]$config.UI.DialogStyle
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }

    # Expand out environment variables and asset file paths.
    ($adtEnv = Get-ADTEnvironmentTable).GetEnumerator() | & { process { New-Variable -Name $_.Key -Value $_.Value -Option Constant } end { $config | Expand-ADTVariablesInConfig } }
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
    $config.Toolkit.TempPath = [System.IO.Path]::Combine($config.Toolkit.TempPath, $adtEnv.appDeployToolkitName)

    # Finally, return the config for usage within module.
    return $config
}
