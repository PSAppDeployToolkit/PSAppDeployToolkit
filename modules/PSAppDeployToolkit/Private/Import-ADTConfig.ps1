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
        [AllowNull()][PSAppDeployToolkit.Attributes.AllowNullButNotEmptyOrWhiteSpace()]
        [PSAppDeployToolkit.Attributes.ValidateUnique()]
        [System.String[]]$BaseDirectory,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [PSAppDeployToolkit.Foundation.EnvironmentTable]$Environment
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
            if ([PSADT.FileSystem.FileSystemUtilities]::IsPathFullyQualified($asset.Value))
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
                if ($_.($asset.Key) -match '^\\?\.\.\\')
                {
                    $_.($asset.Key) = [System.IO.Path]::Combine($BaseDirectory[-1], $_.($asset.Key) -replace '^\\?\.\.\\')
                }
            }
            elseif ($_.($asset.Key) -match '^\\?\.\.')
            {
                $naerParams = @{
                    Exception = [System.InvalidOperationException]::new("The config value [$($_.($asset.Key))] is invalid without a ScriptDirectory specified.")
                    Category = [System.Management.Automation.ErrorCategory]::InvalidData
                    ErrorId = 'RelativePathWithoutScriptDirectory'
                    TargetObject = $_.($asset.Key)
                    RecommendedAction = "Review your configuration and try again."
                }
                $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
            }
        }
    }

    # Internal filter to verify signedness of integer values.
    function Get-ADTConfigIntegerKeyNames
    {
        begin
        {
            # Set up excluded values.
            [System.String[]]$excludedValues = 'DefaultExitCode', 'DeferExitCode', 'FluentAccentColor', 'FluentAccentColorDark'
        }

        process
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
                if (($section.Value -is [System.Int32]) -and !$excludedValues.Contains($section.Key))
                {
                    $section.Key
                }
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
    $integerKeys = (Get-ADTModuleDefaults).Config.([System.String]::Empty).Ast.EndBlock.Statements.PipelineElements.Expression.SafeGetValue() | Get-ADTConfigIntegerKeyNames
    $null = $PSBoundParameters.Remove('Environment'); $config = Import-ADTModuleDataFile @PSBoundParameters -FileName config.psd1
    $config | Confirm-ADTConfigIntegersGreaterThanZero
    Update-ADTConfigTempVariables -Config $config

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
    $Environment.PSObject.Properties | & { process { New-Variable -Name $_.Name -Value $_.Value -Option Constant } end { Expand-ADTVariablesInHashtable -Hashtable $config -SessionState $ExecutionContext.SessionState } }
    $config.Assets | Update-ADTAssetFilePath

    # Change paths to user accessible ones if the caller doesn't own the configured ones.
    Update-ADTConfigAccessiblePaths -Config $config

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
